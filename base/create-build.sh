#!/bin/sh

docker build -t lambci/lambda-base:build -f ./build/Dockerfile .
