#!/bin/bash

EXAMPLES_DIR="$PWD/../examples"

cd ${EXAMPLES_DIR}/nodejs6.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs4.3
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs6.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10

cd ${EXAMPLES_DIR}/nodejs8.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs10.x index.handler
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs12.x index.handler

cd ${EXAMPLES_DIR}/nodejs-native-module
npm install
npm run build
npm test

cd ${EXAMPLES_DIR}/python
docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.7 lambda_function.lambda_handler
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.8 lambda_function.lambda_handler

cd ${EXAMPLES_DIR}/python
docker run --rm -it lambci/lambda:build-python2.7 pip install marisa-trie
docker run --rm -it lambci/lambda:build-python3.6 pip install marisa-trie
docker run --rm -it lambci/lambda:build-python3.7 pip install marisa-trie
docker run --rm -it lambci/lambda:build-python3.8 pip install marisa-trie

cd ${EXAMPLES_DIR}/ruby
docker run --rm -v "$PWD":/var/task lambci/lambda:ruby2.5 lambda_function.lambda_handler
docker run --rm -v "$PWD":/var/task lambci/lambda:ruby2.7 lambda_function.lambda_handler

cd ${EXAMPLES_DIR}/java
docker run --rm -v "$PWD":/app -w /app lambci/lambda:build-java8 gradle build
docker run --rm -v "$PWD/build/docker":/var/task lambci/lambda:java8 org.lambci.lambda.ExampleHandler '{"some": "event"}'
docker run --rm -v "$PWD/build/docker":/var/task lambci/lambda:java11 org.lambci.lambda.ExampleHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/dotnetcore2.0
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.0 dotnet publish -c Release -o pub
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/dotnetcore2.1
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.1 dotnet publish -c Release -o pub
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.1 test::test.Function::FunctionHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/dotnetcore3.1
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore3.1 dotnet publish -c Release -o pub
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore3.1 test::test.Function::FunctionHandler '{"some": "event"}'

cd ${EXAMPLES_DIR}/go1.x
docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x sh -c 'go mod download && go build handler.go'
docker run --rm -v "$PWD":/var/task lambci/lambda:go1.x handler '{"Records": []}'

cd ${EXAMPLES_DIR}/provided
docker run --rm -v "$PWD":/var/task lambci/lambda:provided handler '{"some": "event"}'

# To invoke and keep open:
# docker run --rm -v $PWD:/var/task -e DOCKER_LAMBDA_STAY_OPEN=1 -p 9001:9001 \
#   lambci/lambda:ruby2.5 lambda_function.lambda_handler
