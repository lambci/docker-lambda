#!/bin/bash

curl -O https://lambci.s3.amazonaws.com/fs/nodejs4.3.tgz

docker build --squash -t lambci/lambda-base .

rm ./nodejs4.3.tgz
