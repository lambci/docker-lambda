// Compile with:
// docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x sh -c 'dep ensure && go build handler.go'

// Run with:
// docker run --rm -v "$PWD":/var/task lambci/lambda:go1.x handler '{"Records": []}'

package main

import (
	"context"
	"fmt"
	"github.com/aws/aws-lambda-go/events"
	"github.com/aws/aws-lambda-go/lambda"
)

func HandleRequest(ctx context.Context, event events.S3Event) (string, error) {
	fmt.Println(ctx)

	fmt.Println(event)

	return "Hello World!", nil
}

func main() {
	lambda.Start(HandleRequest)
}
