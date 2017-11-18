FROM lambci/lambda-base

ENV AWS_EXECUTION_ENV=AWS_Lambda_nodejs \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs.tgz | tar -zx -C /

COPY awslambda-mock.js /var/runtime/node_modules/awslambda/build/Release/awslambda.js

USER sbx_user1051

ENTRYPOINT ["/usr/bin/node", "--max-old-space-size=1229", "--max-new-space-size=153", "--max-executable-size=153", "--expose-gc", \
  "/var/runtime/node_modules/awslambda/bin/awslambda"]

