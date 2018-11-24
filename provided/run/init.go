package main

import (
	"bufio"
	"bytes"
	"context"
	"encoding/hex"
	"encoding/json"
	"flag"
	"fmt"
	"github.com/go-chi/chi"
	"github.com/go-chi/render"
	"io/ioutil"
	"math"
	"math/rand"
	"net"
	"net/http"
	"os"
	"os/exec"
	"reflect"
	"regexp"
	"strconv"
	"syscall"
	"time"
)

var okStatusResponse = &StatusResponse{Status: "OK", HTTPStatusCode: 202}

var curRequestID = fakeGuid()
var curState = "STATE_INIT"

var transitions = map[string]map[string]bool{
	"STATE_INIT_ERROR":      map[string]bool{"STATE_INIT": true},
	"STATE_INVOKE_NEXT":     map[string]bool{"STATE_INIT": true, "STATE_INVOKE_NEXT": true, "STATE_INVOKE_RESPONSE": true, "STATE_INVOKE_ERROR": true},
	"STATE_INVOKE_RESPONSE": map[string]bool{"STATE_INVOKE_NEXT": true},
	"STATE_INVOKE_ERROR":    map[string]bool{"STATE_INVOKE_NEXT": true},
}

var mockContext = &MockLambdaContext{}

func main() {
	rand.Seed(time.Now().UTC().UnixNano())

	bootstrapPath := flag.String("bootstrap", "/var/runtime/bootstrap", "path to bootstrap")

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

	mockContext = &MockLambdaContext{
		EventBody:       eventBody,
		FnName:          getEnv("AWS_LAMBDA_FUNCTION_NAME", "test"),
		Version:         getEnv("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST"),
		MemSize:         getEnv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"),
		Timeout:         getEnv("AWS_LAMBDA_FUNCTION_TIMEOUT", "300"),
		Region:          getEnv("AWS_REGION", getEnv("AWS_DEFAULT_REGION", "us-east-1")),
		AccountId:       getEnv("AWS_ACCOUNT_ID", strconv.FormatInt(int64(rand.Int31()), 10)),
		XAmznTraceId:    getEnv("_X_AMZN_TRACE_ID", ""),
		ClientContext:   getEnv("AWS_LAMBDA_CLIENT_CONTEXT", ""),
		CognitoIdentity: getEnv("AWS_LAMBDA_COGNITO_IDENTITY", ""),
		Start:           time.Now(),
		Pid:             1,
		Done:            make(chan bool),
	}
	mockContext.ParseTimeout()
	mockContext.ParseFunctionArn()

	awsAccessKey := getEnv("AWS_ACCESS_KEY", getEnv("AWS_ACCESS_KEY_ID", "SOME_ACCESS_KEY_ID"))
	awsSecretKey := getEnv("AWS_SECRET_KEY", getEnv("AWS_SECRET_ACCESS_KEY", "SOME_SECRET_ACCESS_KEY"))
	awsSessionToken := getEnv("AWS_SESSION_TOKEN", os.Getenv("AWS_SECURITY_TOKEN"))

	os.Setenv("AWS_LAMBDA_FUNCTION_NAME", mockContext.FnName)
	os.Setenv("AWS_LAMBDA_FUNCTION_VERSION", mockContext.Version)
	os.Setenv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", mockContext.MemSize)
	os.Setenv("AWS_LAMBDA_LOG_GROUP_NAME", "/aws/lambda/"+mockContext.FnName)
	os.Setenv("AWS_LAMBDA_LOG_STREAM_NAME", logStreamName(mockContext.Version))
	os.Setenv("AWS_REGION", mockContext.Region)
	os.Setenv("AWS_DEFAULT_REGION", mockContext.Region)
	os.Setenv("_X_AMZN_TRACE_ID", mockContext.XAmznTraceId)
	os.Setenv("_HANDLER", handler)

	cmdPath := *bootstrapPath
	if _, err := os.Stat(cmdPath); os.IsNotExist(err) {
		cmdPath = "/var/task/bootstrap"
		if _, err := os.Stat(cmdPath); os.IsNotExist(err) {
			cmdPath = "/opt/bootstrap"
			if _, err := os.Stat(cmdPath); os.IsNotExist(err) {
				abortRequest(fmt.Errorf("Couldn't find valid bootstrap(s): [/var/task/bootstrap /opt/bootstrap]"))
				return
			}
		}
	}
	cmd := exec.Command(cmdPath)

	cmd.Env = append(os.Environ(),
		"AWS_LAMBDA_RUNTIME_API=127.0.0.1:9001",
		"AWS_ACCESS_KEY_ID="+awsAccessKey,
		"AWS_SECRET_ACCESS_KEY="+awsSecretKey,
	)
	if len(awsSessionToken) > 0 {
		cmd.Env = append(cmd.Env, "AWS_SESSION_TOKEN="+awsSessionToken)
	}
	cmd.Stdout = os.Stderr
	cmd.Stderr = os.Stderr
	cmd.SysProcAttr = &syscall.SysProcAttr{Setpgid: true}

	mockContext.Cmd = cmd

	render.Respond = renderJson

	r := chi.NewRouter()

	r.Route("/2018-06-01", func(r chi.Router) {
		r.Get("/ping", func(w http.ResponseWriter, r *http.Request) {
			w.Write([]byte("pong"))
		})

		r.Route("/runtime", func(r chi.Router) {
			r.
				With(updateState("STATE_INIT_ERROR")).
				Post("/init/error", handleErrorRequest)

			r.
				With(updateState("STATE_INVOKE_NEXT")).
				Get("/invocation/next", func(w http.ResponseWriter, r *http.Request) {
					if mockContext.RequestId == "" {
						mockContext.RequestId = curRequestID
						mockContext.InitEnd = time.Now()
						logStartRequest()
					} else if mockContext.Reply != nil {
						endInvoke(nil)
						return
					}

					w.Header().Set("Content-Type", "application/json")
					w.Header().Set("Lambda-Runtime-Aws-Request-Id", curRequestID)
					w.Header().Set("Lambda-Runtime-Deadline-Ms", strconv.FormatInt(mockContext.Deadline().UnixNano()/1e6, 10))
					w.Header().Set("Lambda-Runtime-Invoked-Function-Arn", mockContext.InvokedFunctionArn)
					w.Header().Set("Lambda-Runtime-Trace-Id", mockContext.XAmznTraceId)

					if mockContext.ClientContext != "" {
						w.Header().Set("Lambda-Runtime-Client-Context", mockContext.ClientContext)
					}
					if mockContext.CognitoIdentity != "" {
						w.Header().Set("Lambda-Runtime-Cognito-Identity", mockContext.CognitoIdentity)
					}

					w.Write([]byte(eventBody))
				})

			r.Route("/invocation/{requestID}", func(r chi.Router) {
				r.Use(awsRequestIDValidator)

				r.
					With(updateState("STATE_INVOKE_RESPONSE")).
					Post("/response", func(w http.ResponseWriter, r *http.Request) {
						body, err := ioutil.ReadAll(r.Body)
						if err != nil {
							render.Render(w, r, &ErrResponse{
								HTTPStatusCode: 500,
								ErrorType:      "BodyReadError", // TODO: not sure what this would be in production?
								ErrorMessage:   err.Error(),
							})
							return
						}

						mockContext.Reply = &InvokeResponse{Payload: body}

						render.Render(w, r, okStatusResponse)
						w.(http.Flusher).Flush()
					})

				r.
					With(updateState("STATE_INVOKE_ERROR")).
					Post("/error", handleErrorRequest)
			})
		})
	})

	listener, err := net.Listen("tcp", ":9001")
	if err != nil {
		abortRequest(err)
		return
	}

	server := &http.Server{Handler: r}

	go server.Serve(listener)

	res, err := http.Get("http://" + listener.Addr().String() + "/2018-06-01/ping")
	if err != nil {
		abortRequest(err)
		return
	}
	body, err := ioutil.ReadAll(res.Body)
	if err != nil || string(body) != "pong" {
		abortRequest(err)
		return
	}

	if err := cmd.Start(); err != nil {
		abortRequest(err)
		return
	}
	go func() {
		cmd.Wait()
		if mockContext.Reply == nil {
			abortRequest(fmt.Errorf("Runtime exited without providing a reason"))
		}
	}()

	<-mockContext.Done
}

func handleErrorRequest(w http.ResponseWriter, r *http.Request) {
	lambdaErr := &LambdaError{}
	statusResponse := okStatusResponse

	body, err := ioutil.ReadAll(r.Body)
	if err != nil || json.Unmarshal(body, lambdaErr) != nil {
		statusResponse = &StatusResponse{Status: "InvalidErrorShape", HTTPStatusCode: 299}
	}

	errorType := r.Header.Get("Lambda-Runtime-Function-Error-Type")
	if errorType != "" {
		lambdaErr.Type = errorType
	}

	mockContext.Reply = &InvokeResponse{Error: lambdaErr}

	render.Render(w, r, statusResponse)
	w.(http.Flusher).Flush()

	endInvoke(nil)
}

func updateState(nextState string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if _, ok := transitions[nextState][curState]; !ok {
				render.Render(w, r, &ErrResponse{
					HTTPStatusCode: 403,
					ErrorType:      "InvalidStateTransition",
					ErrorMessage:   fmt.Sprintf("Transition from %s to %s is not allowed.", curState, nextState),
				})
				return
			}
			curState = nextState
			next.ServeHTTP(w, r)
		})
	}
}

func awsRequestIDValidator(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		requestID := chi.URLParam(r, "requestID")

		if requestID != curRequestID {
			render.Render(w, r, &ErrResponse{
				HTTPStatusCode: 400,
				ErrorType:      "InvalidRequestID",
				ErrorMessage:   "Invalid request ID",
			})
			return
		}

		ctx := context.WithValue(r.Context(), "requestID", requestID)

		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

type StatusResponse struct {
	HTTPStatusCode int    `json:"-"`
	Status         string `json:"status"`
}

func (sr *StatusResponse) Render(w http.ResponseWriter, r *http.Request) error {
	render.Status(r, sr.HTTPStatusCode)
	return nil
}

type ErrResponse struct {
	HTTPStatusCode int    `json:"-"`
	ErrorType      string `json:"errorType,omitempty"`
	ErrorMessage   string `json:"errorMessage"`
}

func (e *ErrResponse) Render(w http.ResponseWriter, r *http.Request) error {
	render.Status(r, e.HTTPStatusCode)
	return nil
}

func renderJson(w http.ResponseWriter, r *http.Request, v interface{}) {
	buf := &bytes.Buffer{}
	enc := json.NewEncoder(buf)
	enc.SetEscapeHTML(true)
	if err := enc.Encode(v); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	if status, ok := r.Context().Value(render.StatusCtxKey).(int); ok {
		w.WriteHeader(status)
	}
	w.Write(buf.Bytes())
}

func abortRequest(err error) {
	endInvoke(&ExitError{err: err})
}

func endInvoke(err error) {
	logStart := false
	if mockContext.RequestId == "" {
		mockContext.RequestId = curRequestID
		logStart = true
	}
	mockContext.MaxMem, _ = allProcsMemoryInMb()
	if mockContext.Cmd != nil && mockContext.Cmd.Process != nil {
		syscall.Kill(-mockContext.Cmd.Process.Pid, syscall.SIGKILL)
	}
	if logStart {
		logStartRequest()
	}
	logEndRequest(err)
	mockContext.Done <- true
}

func logStartRequest() {
	systemLog("START RequestId: " + mockContext.RequestId + " Version: " + mockContext.Version)
}

func logEndRequest(err error) {
	if mockContext.InitEnd.IsZero() {
		mockContext.InitEnd = time.Now()
	}

	initDiffMs := math.Min(float64(mockContext.InitEnd.Sub(mockContext.Start).Nanoseconds()),
		float64(mockContext.TimeoutDuration.Nanoseconds())) / 1e6

	diffMs := math.Min(float64(time.Now().Sub(mockContext.InitEnd).Nanoseconds()),
		float64(mockContext.TimeoutDuration.Nanoseconds())) / 1e6

	initStr := ""
	if mockContext.Cmd != nil && mockContext.Cmd.Path != "/var/runtime/bootstrap" {
		initStr = fmt.Sprintf("Init Duration: %.2f ms\t", initDiffMs)
	}

	systemLog("END RequestId: " + mockContext.RequestId)
	systemLog(fmt.Sprintf(
		"REPORT RequestId: %s\t"+
			initStr+
			"Duration: %.2f ms\t"+
			"Billed Duration: %.f ms\t"+
			"Memory Size: %s MB\t"+
			"Max Memory Used: %d MB\t",
		mockContext.RequestId, diffMs, math.Ceil(diffMs/100)*100, mockContext.MemSize, mockContext.MaxMem))

	if err == nil && mockContext.HasExpired() {
		err = mockContext.TimeoutErr()
	}

	if err != nil {
		responseErr := LambdaError{
			Message: err.Error(),
			Type:    getErrorType(err),
		}
		if responseErr.Type == "errorString" {
			responseErr.Type = ""
			if responseErr.Message == "unexpected EOF" {
				responseErr.Message = "RequestId: " + mockContext.RequestId + " Process exited before completing request"
			}
		} else if responseErr.Type == "ExitError" {
			responseErr.Type = "Runtime.ExitError" // XXX: Hack to add 'Runtime.' to error type
		}
		systemErr(&responseErr)
		os.Exit(1)
	}

	if mockContext.Reply.Error != nil {
		systemErr(mockContext.Reply.Error)
		os.Exit(1)
	}

	// Try to format json as one line – if it's json
	payload := mockContext.Reply.Payload
	payloadObj := &json.RawMessage{}
	if json.Unmarshal(payload, payloadObj) == nil {
		if formattedPayload, err := json.Marshal(payloadObj); err == nil {
			payload = formattedPayload
		}
	}

	fmt.Println(string(payload))
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

func allProcsMemoryInMb() (uint64, error) {
	files, err := ioutil.ReadDir("/proc/")
	if err != nil {
		return 0, err
	}
	totalMem := uint64(0)
	for _, file := range files {
		if pid, err := strconv.Atoi(file.Name()); err == nil {
			pidMem, err := calculateMemoryInKb(pid)
			if err != nil {
				return 0, err
			}
			totalMem += pidMem
		}
	}
	return totalMem / 1024, nil
}

// Thanks to https://stackoverflow.com/a/31881979
func calculateMemoryInKb(pid int) (uint64, error) {
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

	return res, nil
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
func systemErr(err *LambdaError) {
	jsonBytes, _ := json.MarshalIndent(err, "", "  ")
	fmt.Fprintln(os.Stderr, "\033[31m"+string(jsonBytes)+"\033[0m")
}

type ExitError struct {
	err error
}

func (e *ExitError) Error() string {
	return fmt.Sprintf("RequestId: %s Error: %s", curRequestID, e.err.Error())
}

type LambdaError struct {
	Type       string    `json:"errorType,omitempty"`
	Message    string    `json:"errorMessage"`
	StackTrace []*string `json:"stackTrace,omitempty"`
}

type MockLambdaContext struct {
	RequestId          string
	EventBody          string
	FnName             string
	Version            string
	MemSize            string
	Timeout            string
	Region             string
	AccountId          string
	XAmznTraceId       string
	InvokedFunctionArn string
	ClientContext      string
	CognitoIdentity    string
	Start              time.Time
	InitEnd            time.Time
	TimeoutDuration    time.Duration
	Pid                int
	Reply              *InvokeResponse
	Done               chan bool
	Cmd                *exec.Cmd
	MaxMem             uint64
}

func (mc *MockLambdaContext) ParseTimeout() {
	timeoutDuration, err := time.ParseDuration(mc.Timeout + "s")
	if err != nil {
		panic(err)
	}
	mc.TimeoutDuration = timeoutDuration
}

func (mc *MockLambdaContext) ParseFunctionArn() {
	mc.InvokedFunctionArn = getEnv("AWS_LAMBDA_FUNCTION_INVOKED_ARN", arn(mc.Region, mc.AccountId, mc.FnName))
}

func (mc *MockLambdaContext) Deadline() time.Time {
	return mc.Start.Add(mc.TimeoutDuration)
}

func (mc *MockLambdaContext) HasExpired() bool {
	return time.Now().After(mc.Deadline())
}

func (mc *MockLambdaContext) TimeoutErr() error {
	return fmt.Errorf("%s %s Task timed out after %s.00 seconds", time.Now().Format("2006-01-02T15:04:05.999Z"),
		mc.RequestId, mc.Timeout)
}

type InvokeResponse struct {
	Payload []byte
	Error   *LambdaError
}
