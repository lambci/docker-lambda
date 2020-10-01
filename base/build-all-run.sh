#!/bin/bash
set -e

source ${PWD}/runtimes.sh

TOP_DIR="${PWD}/.."

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME

  cd ${TOP_DIR}/${RUNTIME}/run

  [ -x ./update_libs.sh ] && ./update_libs.sh

  docker build --no-cache -t lambci/lambda:${RUNTIME} .
done
