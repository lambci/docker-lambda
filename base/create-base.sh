#!/bin/bash

curl https://lambci.s3.amazonaws.com/fs/nodejs4.3.tgz | gzip -d | docker import - lambci/lambda-base:raw

docker build --squash -t lambci/lambda-base .
