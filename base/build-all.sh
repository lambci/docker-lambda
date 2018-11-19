#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 java8 go1.x dotnetcore2.0 dotnetcore2.1"

TOP_DIR="${PWD}/.."

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME

  cd ${TOP_DIR}/${RUNTIME}/run

  [ -x ./update_libs.sh ] && ./update_libs.sh

  docker build --no-cache -t lambci/lambda:${RUNTIME} .
done
docker tag lambci/lambda:nodejs4.3 lambci/lambda:latest

for RUNTIME in $RUNTIMES; do
  echo build-${RUNTIME}

  cd ${TOP_DIR}/${RUNTIME}/build

  docker build --no-cache -t lambci/lambda:build-${RUNTIME} .
done
docker tag lambci/lambda:build-nodejs4.3 lambci/lambda:build
