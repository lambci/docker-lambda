#!/bin/bash

RUNTIMES="nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 java8 go1.x dotnetcore2.0 dotnetcore2.1 provided"

git tag -f latest

for RUNTIME in $RUNTIMES; do
  git tag -f $RUNTIME
done

git tag -f build

for RUNTIME in $RUNTIMES; do
  git tag -f build-${RUNTIME}
done
