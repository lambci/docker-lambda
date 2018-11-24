#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 java8 go1.x dotnetcore2.0 dotnetcore2.1 provided"

docker push lambci/lambda-base

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  docker push lambci/lambda:${RUNTIME}
done

docker push lambci/lambda-base:build

for RUNTIME in $RUNTIMES; do
  echo build-${RUNTIME}
  docker push lambci/lambda:build-${RUNTIME}
done
