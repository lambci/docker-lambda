#!/bin/bash

EXAMPLES_DIR="$PWD/../examples"

cd ${EXAMPLES_DIR}/nodejs6.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs4.3
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs6.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10

cd ${EXAMPLES_DIR}/nodejs8.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10

cd ${EXAMPLES_DIR}/nodejs-native-module
docker run --rm -v "$PWD":/var/task lambci/lambda:build-nodejs4.3
npm test

cd ${EXAMPLES_DIR}/python
docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.7

cd ${EXAMPLES_DIR}/java
gradle build
docker run --rm -v "$PWD/build/docker":/var/task lambci/lambda:java8 org.lambci.lambda.ExampleHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/dotnetcore2.0
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.0 dotnet publish -c Release -o pub
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/dotnetcore2.1
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.1 dotnet publish -c Release -o pub
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.1 test::test.Function::FunctionHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/go1.x
docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x sh -c 'dep ensure && go build handler.go'
docker run --rm -v "$PWD":/var/task lambci/lambda:go1.x handler '{"Records": []}'
