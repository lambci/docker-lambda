#!/bin/bash

IMAGE_NAME=lambci/lambda-base

curl https://lambci.s3.amazonaws.com/fs/nodejs4.3.tgz | gzip -d | docker import - $IMAGE_NAME
