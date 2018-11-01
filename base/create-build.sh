#!/bin/sh

docker build --no-cache -t lambci/lambda-base:build -f ./build/Dockerfile .
