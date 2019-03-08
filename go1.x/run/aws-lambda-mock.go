package main

import (
	"bufio"
	"bytes"
	"encoding/hex"
	"encoding/json"
	"flag"
	"fmt"
	"github.com/aws/aws-lambda-go/lambda/messages"
	"io/ioutil"
	"math"
	"math/rand"
	"net"
	"net/rpc"
	"os"
	"os/exec"
	"reflect"
	"regexp"
	"strconv"
	"syscall"
	"time"
)

func main() {
	rand.Seed(time.Now().UTC().UnixNano())

	debugMode := flag.Bool("debug", false, "enables delve debugging")
	delvePath := flag.String("delvePath", "/tmp/lambci_debug_files/dlv", "path to delve")
	delvePort := flag.String("delvePort", "5985", "port to start delve server on")
	delveAPI  := flag.String("delveAPI", "1", "delve api version")
	flag.Parse()
	positionalArgs := flag.Args()
	var handler string
	if len(positionalArgs) > 0 {
		handler = positionalArgs[0]
	} else {
		handler = getEnv("AWS_LAMBDA_FUNCTION_HANDLER", getEnv("_HANDLER", "handler"))
	}

	var eventBody string
	if len(positionalArgs) > 1 {
		eventBody = positionalArgs[1]
	} else {
		eventBody = os.Getenv("AWS_LAMBDA_EVENT_BODY")
		if eventBody == "" {
			if os.Getenv("DOCKER_LAMBDA_USE_STDIN") != "" {
				stdin, _ := ioutil.ReadAll(os.Stdin)
				eventBody = string(stdin)
			} else {
				eventBody = "{}"
			}
		}
	}

	mockContext := &MockLambdaContext{
		RequestId: fakeGuid(),
		EventBody: eventBody,
		FnName:    getEnv("AWS_LAMBDA_FUNCTION_NAME", "test"),
		Version:   getEnv("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST"),
		MemSize:   getEnv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"),
		Timeout:   getEnv("AWS_LAMBDA_FUNCTION_TIMEOUT", "300"),
		Region:    getEnv("AWS_REGION", getEnv("AWS_DEFAULT_REGION", "us-east-1")),
		AccountId: getEnv("AWS_ACCOUNT_ID", strconv.FormatInt(int64(rand.Int31()), 10)),
		Start:     time.Now(),
		Pid:       1,
	}
	mockContext.ParseTimeout()

	awsAccessKey := getEnv("AWS_ACCESS_KEY", getEnv("AWS_ACCESS_KEY_ID", "SOME_ACCESS_KEY_ID"))
	awsSecretKey := getEnv("AWS_SECRET_KEY", getEnv("AWS_SECRET_ACCESS_KEY", "SOME_SECRET_ACCESS_KEY"))
	awsSessionToken := getEnv("AWS_SESSION_TOKEN", os.Getenv("AWS_SECURITY_TOKEN"))
	port := getEnv("_LAMBDA_SERVER_PORT", "54321")

	os.Setenv("AWS_LAMBDA_FUNCTION_NAME", mockContext.FnName)
	os.Setenv("AWS_LAMBDA_FUNCTION_VERSION", mockContext.Version)
	os.Setenv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", mockContext.MemSize)
	os.Setenv("AWS_LAMBDA_LOG_GROUP_NAME", "/aws/lambda/"+mockContext.FnName)
	os.Setenv("AWS_LAMBDA_LOG_STREAM_NAME", logStreamName(mockContext.Version))
	os.Setenv("AWS_REGION", mockContext.Region)
	os.Setenv("AWS_DEFAULT_REGION", mockContext.Region)
	os.Setenv("_HANDLER", handler)

	var cmd *exec.Cmd
	if *debugMode == true {
		delveArgs := []string{
			"--listen=:" + *delvePort,
			"--headless=true",
			"--api-version=" + *delveAPI,
			"--log",
			"exec",
			"/var/task/" + handler,
		}
		cmd = exec.Command(*delvePath, delveArgs...)
	} else {
		cmd = exec.Command("/var/task/" + handler)
	}

	cmd.Env = append(os.Environ(),
		"_LAMBDA_SERVER_PORT="+port,
		"AWS_ACCESS_KEY="+awsAccessKey,
		"AWS_ACCESS_KEY_ID="+awsAccessKey,
		"AWS_SECRET_KEY="+awsSecretKey,
		"AWS_SECRET_ACCESS_KEY="+awsSecretKey,
	)
	if len(awsSessionToken) > 0 {
		cmd.Env = append(cmd.Env,
			"AWS_SESSION_TOKEN="+awsSessionToken,
			"AWS_SECURITY_TOKEN="+awsSessionToken,
		)
	}
	cmd.Stdout = os.Stderr
	cmd.Stderr = os.Stderr
	cmd.SysProcAttr = &syscall.SysProcAttr{Setpgid: true}

	var err error

	if err = cmd.Start(); err != nil {
		defer abortRequest(mockContext, err)
		return
	}

	mockContext.Pid = cmd.Process.Pid

	defer syscall.Kill(-mockContext.Pid, syscall.SIGKILL)

	var conn net.Conn
	for {
		conn, err = net.Dial("tcp", ":"+port)
		if mockContext.HasExpired() {
			defer abortRequest(mockContext, mockContext.TimeoutErr())
			return
		}
		if err == nil {
			break
		}
		if oerr, ok := err.(*net.OpError); ok {
			// Connection refused, try again
			if oerr.Op == "dial" && oerr.Net == "tcp" {
				time.Sleep(5 * time.Millisecond)
				continue
			}
		}
		defer abortRequest(mockContext, err)
		return
	}

	client := rpc.NewClient(conn)

	for {
		err = client.Call("Function.Ping", messages.PingRequest{}, &messages.PingResponse{})
		if mockContext.HasExpired() {
			defer abortRequest(mockContext, mockContext.TimeoutErr())
			return
		}
		if err == nil {
			break
		}
		time.Sleep(5 * time.Millisecond)
	}

	// XXX: The Go runtime seems to amortize the startup time, reset it here
	mockContext.Start = time.Now()

	logStartRequest(mockContext)

	err = client.Call("Function.Invoke", mockContext.Request(), &mockContext.Reply)

	// We want the process killed before this, so defer it
	defer logEndRequest(mockContext, err)
}

func abortRequest(mockContext *MockLambdaContext, err error) {
	logStartRequest(mockContext)
	logEndRequest(mockContext, err)
}

func logStartRequest(mockContext *MockLambdaContext) {
	systemLog("START RequestId: " + mockContext.RequestId + " Version: " + mockContext.Version)
}

func logEndRequest(mockContext *MockLambdaContext, err error) {
	curMem, _ := calculateMemoryInMb(mockContext.Pid)
	diffMs := math.Min(float64(time.Now().Sub(mockContext.Start).Nanoseconds()),
		float64(mockContext.TimeoutDuration.Nanoseconds())) / 1e6

	systemLog("END RequestId: " + mockContext.RequestId)
	systemLog(fmt.Sprintf(
		"REPORT RequestId: %s\t"+
			"Duration: %.2f ms\t"+
			"Billed Duration: %.f ms\t"+
			"Memory Size: %s MB\t"+
			"Max Memory Used: %d MB\t",
		mockContext.RequestId, diffMs, math.Ceil(diffMs/100)*100, mockContext.MemSize, curMem))

	if err == nil && mockContext.HasExpired() {
		err = mockContext.TimeoutErr()
	}

	if err != nil {
		responseErr := messages.InvokeResponse_Error{
			Message: err.Error(),
			Type:    getErrorType(err),
		}
		if responseErr.Type == "errorString" {
			responseErr.Type = ""
			if responseErr.Message == "unexpected EOF" {
				responseErr.Message = "RequestId: " + mockContext.RequestId + " Process exited before completing request"
			}
		}
		systemErr(&responseErr)
		os.Exit(1)
	}

	if mockContext.Reply.Error != nil {
		systemErr(mockContext.Reply.Error)
		os.Exit(1)
	}

	fmt.Println(string(mockContext.Reply.Payload))
}

func getEnv(key, fallback string) string {
	value := os.Getenv(key)
	if value != "" {
		return value
	}
	return fallback
}

func fakeGuid() string {
	randBuf := make([]byte, 16)
	rand.Read(randBuf)

	hexBuf := make([]byte, hex.EncodedLen(len(randBuf))+4)

	hex.Encode(hexBuf[0:8], randBuf[0:4])
	hexBuf[8] = '-'
	hex.Encode(hexBuf[9:13], randBuf[4:6])
	hexBuf[13] = '-'
	hex.Encode(hexBuf[14:18], randBuf[6:8])
	hexBuf[18] = '-'
	hex.Encode(hexBuf[19:23], randBuf[8:10])
	hexBuf[23] = '-'
	hex.Encode(hexBuf[24:], randBuf[10:])

	hexBuf[14] = '1' // Make it look like a v1 guid

	return string(hexBuf)
}

func logStreamName(version string) string {
	randBuf := make([]byte, 16)
	rand.Read(randBuf)

	hexBuf := make([]byte, hex.EncodedLen(len(randBuf)))
	hex.Encode(hexBuf, randBuf)

	return time.Now().Format("2006/01/02") + "/[" + version + "]" + string(hexBuf)
}

func arn(region string, accountId string, fnName string) string {
	nonDigit := regexp.MustCompile(`[^\d]`)
	return "arn:aws:lambda:" + region + ":" + nonDigit.ReplaceAllString(accountId, "") + ":function:" + fnName
}

// Thanks to https://stackoverflow.com/a/31881979
func calculateMemoryInMb(pid int) (uint64, error) {
	f, err := os.Open(fmt.Sprintf("/proc/%d/smaps", pid))
	if err != nil {
		return 0, err
	}
	defer f.Close()

	res := uint64(0)
	pfx := []byte("Pss:")
	r := bufio.NewScanner(f)
	for r.Scan() {
		line := r.Bytes()
		if bytes.HasPrefix(line, pfx) {
			var size uint64
			_, err := fmt.Sscanf(string(line[4:]), "%d", &size)
			if err != nil {
				return 0, err
			}
			res += size
		}
	}
	if err := r.Err(); err != nil {
		return 0, err
	}

	return res / 1024, nil
}

func getErrorType(err interface{}) string {
	if errorType := reflect.TypeOf(err); errorType.Kind() == reflect.Ptr {
		return errorType.Elem().Name()
	} else {
		return errorType.Name()
	}
}

func systemLog(msg string) {
	fmt.Fprintln(os.Stderr, "\033[32m"+msg+"\033[0m")
}

// Try to match the output of the Lambda web console
func systemErr(err *messages.InvokeResponse_Error) {
	jsonBytes, _ := json.MarshalIndent(LambdaError{
		Message:    err.Message,
		Type:       err.Type,
		StackTrace: err.StackTrace,
	}, "", "  ")
	fmt.Fprintln(os.Stderr, "\033[31m"+string(jsonBytes)+"\033[0m")
}

type LambdaError struct {
	Message    string                                      `json:"errorMessage"`
	Type       string                                      `json:"errorType,omitempty"`
	StackTrace []*messages.InvokeResponse_Error_StackFrame `json:"stackTrace,omitempty"`
}

type MockLambdaContext struct {
	RequestId       string
	EventBody       string
	FnName          string
	Version         string
	MemSize         string
	Timeout         string
	Region          string
	AccountId       string
	Start           time.Time
	TimeoutDuration time.Duration
	Pid             int
	Reply           *messages.InvokeResponse
}

func (mc *MockLambdaContext) ParseTimeout() {
	timeoutDuration, err := time.ParseDuration(mc.Timeout + "s")
	if err != nil {
		panic(err)
	}
	mc.TimeoutDuration = timeoutDuration
}

func (mc *MockLambdaContext) Deadline() time.Time {
	return mc.Start.Add(mc.TimeoutDuration)
}

func (mc *MockLambdaContext) HasExpired() bool {
	return time.Now().After(mc.Deadline())
}

func (mc *MockLambdaContext) Request() *messages.InvokeRequest {
	return &messages.InvokeRequest{
		Payload:            []byte(mc.EventBody),
		RequestId:          mc.RequestId,
		XAmznTraceId:       getEnv("_X_AMZN_TRACE_ID", ""),
		InvokedFunctionArn: getEnv("AWS_LAMBDA_FUNCTION_INVOKED_ARN", arn(mc.Region, mc.AccountId, mc.FnName)),
		Deadline: messages.InvokeRequest_Timestamp{
			Seconds: mc.Deadline().Unix(),
			Nanos:   int64(mc.Deadline().Nanosecond()),
		},
	}
}

func (mc *MockLambdaContext) TimeoutErr() error {
	return fmt.Errorf("%s %s Task timed out after %s.00 seconds", time.Now().Format("2006-01-02T15:04:05.999Z"),
		mc.RequestId, mc.Timeout)
}
