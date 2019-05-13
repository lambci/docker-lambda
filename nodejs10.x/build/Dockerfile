FROM lambci/lambda-base-2:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_nodejs10.x \
    NODE_PATH=/opt/nodejs/node10/node_modules:/opt/nodejs/node_modules:/var/runtime/node_modules

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/nodejs10.x.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN pip3 install -U pip setuptools --no-cache-dir && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir
