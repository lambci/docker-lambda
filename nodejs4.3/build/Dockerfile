FROM lambci/lambda-base:build

ENV PATH=/usr/local/lib64/node-v4.3.x/bin:$PATH \
    LD_LIBRARY_PATH=/usr/local/lib64/node-v4.3.x/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs4.3 \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules \
    npm_config_unsafe-perm=true

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs4.3.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir

CMD ["npm", "rebuild"]
