FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_python2.7 \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime /var/lang && \
  curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U pip setuptools --no-cache-dir && \
  pip install -U virtualenv pipenv --no-cache-dir && \
  curl https://lambci.s3.amazonaws.com/fs/python2.7.tgz | tar -zx -C /
