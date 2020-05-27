FROM lambci/lambda-base-2:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.8 \
    PKG_CONFIG_PATH=/var/lang/lib/pkgconfig:/usr/lib64/pkgconfig:/usr/share/pkgconfig

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/python3.8.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN pip install -U pip setuptools wheel --no-cache-dir && \
  pip install -U virtualenv pipenv --no-cache-dir && \
  curl -sSL https://raw.githubusercontent.com/python-poetry/poetry/1.0.5/get-poetry.py | POETRY_VERSION=1.0.5 python3 && \
  pip install -U aws-lambda-builders==0.9.0 aws-sam-cli==0.52.0 awscli boto3 --no-cache-dir

ENV PATH=/root/.poetry/bin:$PATH
