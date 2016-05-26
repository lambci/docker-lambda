#!/bin/bash

IMAGE_NAME=lambci/lambda-base:build

docker build $BUILD_ARG -t ${IMAGE_NAME} .

