#!/bin/bash

RUNTIMES="nodejs10.x nodejs12.x python3.8"

rm -rf diff-2
mkdir -p diff-2

for RUNTIME in $RUNTIMES; do
  docker pull lambci/lambda:${RUNTIME}

  mkdir -p ./diff-2/${RUNTIME}/docker/var
  CONTAINER=$(docker create lambci/lambda:${RUNTIME})
  docker cp ${CONTAINER}:/var/runtime ./diff-2/${RUNTIME}/docker/var
  docker cp ${CONTAINER}:/var/lang ./diff-2/${RUNTIME}/docker/var

  curl https://lambci.s3.amazonaws.com/fs/${RUNTIME}.tgz > ./diff-2/${RUNTIME}.tgz

  mkdir -p ./diff-2/${RUNTIME}/lambda
  tar -zxf ./diff-2/${RUNTIME}.tgz -C ./diff-2/${RUNTIME}/lambda -- var/runtime var/lang

  tar -ztf ./diff-2/${RUNTIME}.tgz | sed 's/\/$//' | sort > ./diff-2/${RUNTIME}/fs.lambda.txt
done

curl https://lambci.s3.amazonaws.com/fs/base-2.tgz > ./diff-2/base-2.tgz

{ tar -ztf ./diff-2/nodejs10.x.tgz; tar -ztf ./diff-2/base-2.tgz; } | grep -v '/$' \
  | grep -v '^dev/' | grep -v '^proc/' | grep -v '^sys/' | sort > ./diff-2/nodejs10.x/fs.lambda.txt

docker run --rm --entrypoint bash lambci/lambda:nodejs10.x -c \
  'find() { local d; for d in "$@"; do ls -1A "$d" | while read f; do i="$d/$f"; [ -d "$i" ] && [ ! -L "$i" ] && find "$i" || echo $i; done; done; }; find /' \
  | sed -E 's/^\/+//' | grep -v '^dev/' | grep -v '^proc/' | grep -v '^sys/' | sort > ./diff-2/nodejs10.x/fs.docker.txt

DIFF_DIR="${PWD}/diff-2"

cd ${DIFF_DIR}/nodejs10.x
pwd
diff fs.docker.txt fs.lambda.txt | grep -v 'var/runtime/' | grep -v 'var/lang'
echo
diff docker/var/runtime lambda/var/runtime
echo
diff -qr docker lambda

cd ${DIFF_DIR}/nodejs12.x
pwd
diff docker/var/runtime lambda/var/runtime
diff -qr docker lambda
echo

cd ${DIFF_DIR}/python3.8
pwd
diff docker/var/runtime lambda/var/runtime
diff -qr docker lambda
echo
