#!/bin/bash

source ${PWD}/runtimes.sh

echo -n "Enter repository passphrase: "
read -s DOCKER_CONTENT_TRUST_REPOSITORY_PASSPHRASE
echo

export DOCKER_CONTENT_TRUST=1
export DOCKER_CONTENT_TRUST_REPOSITORY_PASSPHRASE

export PUBLISH_DATE=$(date "+%Y%m%d")

docker push lambci/lambda-base
docker push lambci/lambda-base-2

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  docker tag lambci/lambda:${RUNTIME} lambci/lambda:${PUBLISH_DATE}-${RUNTIME}
  docker push lambci/lambda:${RUNTIME}
  docker push lambci/lambda:${PUBLISH_DATE}-${RUNTIME}
  docker rmi lambci/lambda:${PUBLISH_DATE}-${RUNTIME}
done

docker push lambci/lambda-base:build
docker push lambci/lambda-base-2:build

for RUNTIME in $RUNTIMES; do
  echo build-${RUNTIME}
  docker tag lambci/lambda:build-${RUNTIME} lambci/lambda:${PUBLISH_DATE}-build-${RUNTIME}
  docker push lambci/lambda:build-${RUNTIME}
  docker push lambci/lambda:${PUBLISH_DATE}-build-${RUNTIME}
  docker rmi lambci/lambda:${PUBLISH_DATE}-build-${RUNTIME}
done
