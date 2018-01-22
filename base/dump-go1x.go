package main

import (
	"context"
	"fmt"
	"github.com/aws/aws-lambda-go/lambda"
	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/aws/external"
	"github.com/aws/aws-sdk-go-v2/service/s3"
	"log"
	"os"
	"os/exec"
)

func HandleRequest(ctx context.Context, event interface{}) (*s3.PutObjectOutput, error) {
	filename := "go1.x.tgz"

	RunShell("tar -cpzf /tmp/" + filename + " --numeric-owner --ignore-failed-read /var/runtime /var/lang")

	fmt.Println("Zipping done! Uploading...")

	cfg, err := external.LoadDefaultAWSConfig()
	if err != nil {
		log.Fatal(err)
	}

	file, err := os.Open("/tmp/" + filename)
	if err != nil {
		log.Fatal(err)
	}

	resp, err := s3.New(cfg).PutObjectRequest(&s3.PutObjectInput{
		ACL:    s3.ObjectCannedACLPublicRead,
		Body:   file,
		Bucket: aws.String("lambci"),
		Key:    aws.String("fs/" + filename),
	}).Send()
	if err != nil {
		log.Fatal(err)
	}

	fmt.Println("Uploading done!")

	RunShell("ps aux")

	RunShell("xargs --null --max-args=1 < /proc/1/environ")

	for _, a := range os.Args {
		fmt.Println(a)
	}
	pwd, _ := os.Getwd()
	fmt.Println(pwd)
	for _, e := range os.Environ() {
		fmt.Println(e)
	}
	fmt.Println(ctx)

	return resp, nil
}

func RunShell(shellCmd string) {
	cmd := exec.Command("sh", "-c", shellCmd)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Run()
}

func main() {
	lambda.Start(HandleRequest)
}

/*
PATH=/usr/local/bin:/usr/bin/:/bin
LANG=en_US.UTF-8
TZ=:UTC
LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib
_LAMBDA_CONTROL_SOCKET=15
_LAMBDA_CONSOLE_SOCKET=17
LAMBDA_TASK_ROOT=/var/task
LAMBDA_RUNTIME_DIR=/var/runtime
_LAMBDA_LOG_FD=24
_LAMBDA_SB_ID=8
_LAMBDA_SHARED_MEM_FD=12
AWS_REGION=us-east-1
AWS_DEFAULT_REGION=us-east-1
AWS_LAMBDA_LOG_GROUP_NAME=/aws/lambda/dump-go1x
AWS_LAMBDA_LOG_STREAM_NAME=2018/01/16/[$LATEST]12d47417179844e3ad55190a93a817d7
AWS_LAMBDA_FUNCTION_NAME=dump-go1x
AWS_LAMBDA_FUNCTION_MEMORY_SIZE=3008
AWS_LAMBDA_FUNCTION_VERSION=$LATEST
_AWS_XRAY_DAEMON_ADDRESS=169.254.79.2
_AWS_XRAY_DAEMON_PORT=2000
AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000
AWS_XRAY_CONTEXT_MISSING=LOG_ERROR
_X_AMZN_TRACE_ID=Parent=41bc1aa71e1174a5
_HANDLER=my_handler
_LAMBDA_RUNTIME_LOAD_TIME=1522376103407

/var/task
/var/task/my_handler

PATH=/usr/local/bin:/usr/bin/:/bin
LANG=en_US.UTF-8
TZ=:UTC
LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib
_LAMBDA_CONTROL_SOCKET=15
_LAMBDA_CONSOLE_SOCKET=17
LAMBDA_TASK_ROOT=/var/task
LAMBDA_RUNTIME_DIR=/var/runtime
_LAMBDA_LOG_FD=24
_LAMBDA_SB_ID=8
_LAMBDA_SHARED_MEM_FD=12
AWS_REGION=us-east-1
AWS_DEFAULT_REGION=us-east-1
AWS_LAMBDA_LOG_GROUP_NAME=/aws/lambda/dump-go1x
AWS_LAMBDA_LOG_STREAM_NAME=2018/01/16/[$LATEST]12d47417179844e3ad55190a93a817d7
AWS_LAMBDA_FUNCTION_NAME=dump-go1x
AWS_LAMBDA_FUNCTION_MEMORY_SIZE=3008
AWS_LAMBDA_FUNCTION_VERSION=$LATEST
_AWS_XRAY_DAEMON_ADDRESS=169.254.79.2
_AWS_XRAY_DAEMON_PORT=2000
AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000
AWS_XRAY_CONTEXT_MISSING=LOG_ERROR
_X_AMZN_TRACE_ID=Parent=41bc1aa71e1174a5
_HANDLER=my_handler
_LAMBDA_RUNTIME_LOAD_TIME=1522376103407

_LAMBDA_SERVER_PORT=60304
AWS_ACCESS_KEY=
AWS_ACCESS_KEY_ID=
AWS_SECRET_KEY=
AWS_SECRET_ACCESS_KEY=
AWS_SESSION_TOKEN=
AWS_SECURITY_TOKEN=

context.Background.WithDeadline(2018-01-12 21:16:44.121702432 +0000 UTC [2.981503691s]).WithValue(
	&lambdacontext.key{},
	&lambdacontext.LambdaContext{
		AwsRequestID:"e1e762a8-f7dd-11e7-8572-1dc9a2c870b7",
		InvokedFunctionArn:"arn:aws:lambda:us-east-1:XXXXXXXXXXXX:function:dump-go1x",
		Identity:lambdacontext.CognitoIdentity{CognitoIdentityID:"", CognitoIdentityPoolID:""},
		ClientContext:lambdacontext.ClientContext{Client:lambdacontext.ClientApplication{InstallationID:"", AppTitle:"", AppVersionCode:"", AppPackageName:""},
		Env:map[string]string(nil),
		Custom:map[string]string(nil)}
	}).WithValue("x-amzn-trace-id", "Root=1-5a5925b8-30ae34971b99966e26b15b1e;Parent=06346dc778d0afed;Sampled=1")
*/
