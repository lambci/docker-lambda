FROM lambci/lambda-base

ENV PATH=/var/lang/bin:/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    LANG=en_US.UTF-8 \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs6.10 \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime

RUN rm -rf /var/runtime /var/lang && mkdir /tmp && chown sbx_user1051:495 /tmp && chmod 700 /tmp && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs6.10.tgz | tar -zx -C /

ADD awslambda-mock.js /var/runtime/node_modules/awslambda/build/Release/awslambda.js

WORKDIR /var/task

USER sbx_user1051

ENTRYPOINT ["/var/lang/bin/node", "--max-old-space-size=1229", "--max-semi-space-size=76", "--max-executable-size=153", "--expose-gc", \
  "/var/runtime/node_modules/awslambda/index.js"]

