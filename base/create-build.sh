#!/bin/sh

docker build --pull --no-cache -t lambci/lambda-base:build -f ./build/Dockerfile .
