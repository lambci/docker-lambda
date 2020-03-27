#!/bin/bash

RUNTIMES="provided go1.x nodejs4.3 nodejs6.10 nodejs8.10 nodejs10.x nodejs12.x python2.7 python3.6 python3.7 python3.8 ruby2.5 ruby2.7 java8 java11 dotnetcore2.0 dotnetcore2.1 dotnetcore3.1"

git tag -f latest

for RUNTIME in $RUNTIMES; do
  git tag -f $RUNTIME
done

git tag -f build

for RUNTIME in $RUNTIMES; do
  git tag -f build-${RUNTIME}
done
