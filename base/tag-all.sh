#!/bin/bash

source ${PWD}/runtimes.sh

git tag -f latest

for RUNTIME in $RUNTIMES; do
  git tag -f $RUNTIME
done

git tag -f build

for RUNTIME in $RUNTIMES; do
  git tag -f build-${RUNTIME}
done
