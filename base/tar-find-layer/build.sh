#!/bin/sh

rm layer.zip

docker run --rm -v "$PWD":/tmp/layer lambci/yumda:2 bash -c "
  yum install -y findutils gzip tar && \
  cd /lambda/opt && \
  zip -yr /tmp/layer/layer.zip .
"
