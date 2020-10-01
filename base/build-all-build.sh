#!/bin/bash
set -e

source ${PWD}/runtimes.sh

TOP_DIR="${PWD}/.."

for RUNTIME in $RUNTIMES; do
  echo build-${RUNTIME}

  cd ${TOP_DIR}/${RUNTIME}/build

  docker build -t lambci/lambda:build-${RUNTIME} .
done
