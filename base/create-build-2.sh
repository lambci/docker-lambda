#!/bin/sh

docker build --no-cache --squash -t lambci/lambda-base-2:build -f ./build-2/Dockerfile .
