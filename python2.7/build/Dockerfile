FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_python2.7 \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python2.7.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U virtualenv pipenv --no-cache-dir && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir
