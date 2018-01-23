#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 python2.7 python3.6 java8 go1.x"

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

docker run --rm --entrypoint find lambci/lambda:nodejs4.3 / | sed 's/^\///' | sort > ./diff/nodejs4.3/fs.docker.txt

# cd diff/nodejs4.3
# diff docker/var/runtime/node_modules/awslambda/index.js lambda/var/runtime/node_modules/awslambda/index.js
# diff -qr docker lambda | grep -v '/var/runtime/node_modules/aws-sdk'

cd ../python2.7
diff docker/var/runtime/awslambda/bootstrap.py lambda/var/runtime/awslambda/bootstrap.py
diff -qr docker lambda | grep -v '/var/runtime/boto'

# cd ../python3.6
# diff docker/var/runtime/awslambda/bootstrap.py lambda/var/runtime/awslambda/bootstrap.py
# diff -qr docker lambda | grep -v '/var/runtime/boto' | grep -v __pycache__

# cd ../nodejs6.10
# diff docker/var/runtime/node_modules/awslambda/index.js lambda/var/runtime/node_modules/awslambda/index.js
# diff -qr docker lambda | grep -v '/var/runtime/node_modules/aws-sdk'

# cd ../java
# diff -qr docker lambda

# cd ../go1.x
# diff -qr docker lambda
