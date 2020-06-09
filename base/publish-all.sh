#!/bin/bash

RUNTIMES="provided go1.x nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 python3.7 ruby2.5 java8 dotnetcore2.0 dotnetcore2.1 nodejs10.x nodejs12.x python3.8 ruby2.7 java11 dotnetcore3.1"

echo -n "Enter repository passphrase: "
read -s DOCKER_CONTENT_TRUST_REPOSITORY_PASSPHRASE
echo

export DOCKER_CONTENT_TRUST=1
export DOCKER_CONTENT_TRUST_REPOSITORY_PASSPHRASE

docker push lambci/lambda-base
docker push lambci/lambda-base-2

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  docker push lambci/lambda:${RUNTIME}
done

docker push lambci/lambda-base:build
docker push lambci/lambda-base-2:build

for RUNTIME in $RUNTIMES; do
  echo build-${RUNTIME}
  docker push lambci/lambda:build-${RUNTIME}
done
