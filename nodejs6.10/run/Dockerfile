FROM lambci/lambda-base

ENV PATH=/var/lang/bin:/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    LANG=en_US.UTF-8 \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime \
    _LAMBDA_CONTROL_SOCKET=14 \
    _LAMBDA_SHARED_MEM_FD=11 \
    _LAMBDA_LOG_FD=9 \
    _LAMBDA_CONSOLE_SOCKET=16 \
    _LAMBDA_RUNTIME_LOAD_TIME=1530232235231 \
    _AWS_XRAY_DAEMON_ADDRESS=169.254.79.2 \
    _AWS_XRAY_DAEMON_PORT=2000 \
    AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000 \
    AWS_XRAY_CONTEXT_MISSING=LOG_ERROR \
    _X_AMZN_TRACE_ID=Root=1-dc99d00f-c079a84d433534434534ef0d;Parent=91ed514f1e5c03b2;Sampled=0 \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs6.10 \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules

RUN rm -rf /var/runtime /var/lang && mkdir /tmp && chown sbx_user1051:495 /tmp && chmod 700 /tmp && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs6.10.tgz | tar -zx -C /

ADD awslambda-mock.js /var/runtime/node_modules/awslambda/build/Release/awslambda.js

WORKDIR /var/task

USER sbx_user1051

ENTRYPOINT ["/var/lang/bin/node", "--max-old-space-size=1229", "--max-semi-space-size=76", "--max-executable-size=153", "--expose-gc", \
  "/var/runtime/node_modules/awslambda/index.js"]

