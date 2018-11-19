FROM lambci/lambda-base

ENV PATH=/usr/local/lib64/node-v4.3.x/bin:$PATH \
    LD_LIBRARY_PATH=/usr/local/lib64/node-v4.3.x/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs4.3 \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs4.3.tgz | tar -zx -C /

COPY awslambda-mock.js /var/runtime/node_modules/awslambda/build/Release/awslambda.js

USER sbx_user1051

ENTRYPOINT ["/usr/local/lib64/node-v4.3.x/bin/node", "--expose-gc", "--max-executable-size=160", "--max-semi-space-size=150", "--max-old-space-size=2547", \
  "/var/runtime/node_modules/awslambda/index.js"]

