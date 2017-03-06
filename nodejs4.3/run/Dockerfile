FROM lambci/lambda-base

ENV PATH=/usr/local/lib64/node-v4.3.x/bin:/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/usr/local/lib64/node-v4.3.x/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    LANG=en_US.UTF-8 \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs4.3 \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime

RUN mkdir /tmp && chown sbx_user1051:495 /tmp && chmod 700 /tmp

ADD awslambda-mock.js /var/runtime/node_modules/awslambda/build/Release/awslambda.js

WORKDIR /var/task

USER sbx_user1051

ENTRYPOINT ["/usr/local/lib64/node-v4.3.x/bin/node", "--max-old-space-size=1229", "--max-semi-space-size=76", "--max-executable-size=153", "--expose-gc", \
  "/var/runtime/node_modules/awslambda/index.js"]

