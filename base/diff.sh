#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 python2.7 python3.6 java8"

mkdir -p diff

for RUNTIME in $RUNTIMES; do
  docker pull lambci/lambda:${RUNTIME}

  mkdir -p ./diff/${RUNTIME}/docker/var
  CONTAINER=$(docker create lambci/lambda:${RUNTIME})
  docker cp ${CONTAINER}:/var/runtime ./diff/${RUNTIME}/docker/var
  docker cp ${CONTAINER}:/var/lang ./diff/${RUNTIME}/docker/var

  curl https://lambci.s3.amazonaws.com/fs/${RUNTIME}.tgz > ./diff/${RUNTIME}.tgz

  mkdir -p ./diff/${RUNTIME}/lambda
  tar -zxf ./diff/${RUNTIME}.tgz -C ./diff/${RUNTIME}/lambda -- var/runtime var/lang

  tar -ztf ./diff/${RUNTIME}.tgz | sed 's/\/$//' | sort > ./diff/${RUNTIME}/fs.lambda.txt

  curl https://lambci.s3.amazonaws.com/fs/${RUNTIME}.fs.txt > ./diff/${RUNTIME}/fs.full.lambda.txt
done

docker run --entrypoint find lambci/lambda:nodejs4.3 / | sed 's/^\///' | sort > ./diff/nodejs4.3/fs.docker.txt
