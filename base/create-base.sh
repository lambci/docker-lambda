#!/bin/bash

curl -O https://lambci.s3.amazonaws.com/fs/base.tgz

docker build --squash -t lambci/lambda-base .

rm ./base.tgz
