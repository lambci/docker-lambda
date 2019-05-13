#!/bin/bash

curl -O https://lambci.s3.amazonaws.com/fs/base-2.tgz

docker build --squash -t lambci/lambda-base-2 -f ./base-2/Dockerfile .

rm ./base-2.tgz
