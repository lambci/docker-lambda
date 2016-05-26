#!/bin/bash

CMD="BUILD_ONLY=true npm install --build-from-source \
    bcrypt \
    bignum \
    grpc \
    hiredis \
    ibm_db \
    iconv \
    kerberos \
    leveldown \
    murmurhash-native \
    nodegit \
    node-cmake \
    realm \
    serialport \
    snappy \
    sqlite3 \
    unix-dgram \
    v8-profiler \
    websocket \
    webworker-threads \
    x509 \
    node-sass && \
  cd node_modules/node-sass && npm install && node scripts/build -f
"

docker run -e npm_config_unsafe-perm=true lambci/lambda-base:build sh -c "$CMD" && echo "Success!"
