FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_nodejs \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules \
    npm_config_unsafe-perm=true

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs.tgz | tar -zx -C /

CMD ["npm", "rebuild"]
