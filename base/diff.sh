#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 java8 go1.x dotnetcore2.0 dotnetcore2.1"

rm -rf diff
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

docker run --rm --entrypoint find lambci/lambda:python2.7 / | sed 's/^\///' | sort > ./diff/python2.7/fs.docker.txt

DIFF_DIR="${PWD}/diff"

cd ${DIFF_DIR}/python2.7
pwd
diff fs.docker.txt fs.lambda.txt | grep -v '^< dev/' | grep -v '^< proc/' | grep -v '^< sys/' | grep -v 'var/runtime/'
diff docker/var/runtime/awslambda/bootstrap.py lambda/var/runtime/awslambda/bootstrap.py
diff -qr docker lambda

cd ${DIFF_DIR}/nodejs4.3
pwd
diff docker/var/runtime/node_modules/awslambda/index.js lambda/var/runtime/node_modules/awslambda/index.js
diff -qr docker lambda

cd ${DIFF_DIR}/nodejs6.10
pwd
diff docker/var/runtime/node_modules/awslambda/index.js lambda/var/runtime/node_modules/awslambda/index.js
diff -qr docker lambda

cd ${DIFF_DIR}/nodejs8.10
pwd
diff docker/var/runtime/node_modules/awslambda/index.js lambda/var/runtime/node_modules/awslambda/index.js
diff -qr docker lambda

cd ${DIFF_DIR}/python3.6
pwd
diff docker/var/runtime/awslambda/bootstrap.py lambda/var/runtime/awslambda/bootstrap.py
diff -qr docker lambda | grep -v __pycache__

cd ${DIFF_DIR}/java8
pwd
diff -qr docker lambda

cd ${DIFF_DIR}/go1.x
pwd
diff -qr docker lambda

cd ${DIFF_DIR}/dotnetcore2.0
pwd
diff -qr docker lambda

cd ${DIFF_DIR}/dotnetcore2.1
pwd
diff -qr docker lambda
