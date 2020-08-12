// docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x sh -c \
//   'go mod download && go build -tags lambda.norpc -ldflags="-s -w" bootstrap.go' && \
//   zip bootstrap.zip bootstrap

package main

import (
	"context"
	"fmt"
	"io/ioutil"
	"log"
	"os"
	"os/exec"
	"strings"

	"github.com/aws/aws-lambda-go/lambda"
	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/aws/external"
	"github.com/aws/aws-sdk-go-v2/service/s3"
)

func handleRequest(ctx context.Context, event interface{}) (*s3.PutObjectResponse, error) {
	filename := "provided.al2.tgz"

	runShell("tar -cpzf /tmp/" + filename + " --numeric-owner --ignore-failed-read /var/runtime /var/lang")

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
	}).Send(context.Background())
	if err != nil {
		log.Fatal(err)
	}

	fmt.Println("Uploading done!")

	fmt.Println("Parent env:")
	runShell("xargs --null --max-args=1 < /proc/1/environ")

	fmt.Println("Parent cmdline:")
	content, err := ioutil.ReadFile("/proc/1/cmdline")
	fmt.Println(strings.ReplaceAll(string(content), "\x00", " "))

	fmt.Println("os.Args:")
	for _, a := range os.Args {
		fmt.Println(a)
	}

	fmt.Println("os.Getwd:")
	pwd, _ := os.Getwd()
	fmt.Println(pwd)

	fmt.Println("os.Environ:")
	for _, e := range os.Environ() {
		fmt.Println(e)
	}

	fmt.Println("ctx:")
	fmt.Println(ctx)

	return resp, nil
}

func runShell(shellCmd string) {
	cmd := exec.Command("sh", "-c", shellCmd)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Run()
}

func main() {
	lambda.Start(handleRequest)
}
