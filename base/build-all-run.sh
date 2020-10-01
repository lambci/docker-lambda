#!/bin/bash
set -e

RUNTIMES="provided go1.x nodejs4.3 nodejs6.10 nodejs8.10 python2.7 python3.6 python3.7 ruby2.5 java8 dotnetcore2.0 dotnetcore2.1 provided.al2 nodejs10.x nodejs12.x python3.8 ruby2.7 java8.al2 java11 dotnetcore3.1"

TOP_DIR="${PWD}/.."

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME

  cd ${TOP_DIR}/${RUNTIME}/run

  [ -x ./update_libs.sh ] && ./update_libs.sh

  docker build --no-cache -t lambci/lambda:${RUNTIME} .
done
