FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.7 \
    PYTHONPATH=/var/runtime \
    PKG_CONFIG_PATH=/var/lang/lib/pkgconfig:/usr/lib64/pkgconfig:/usr/share/pkgconfig

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/python3.7.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN pip install -U pip setuptools --no-cache-dir && \
  pip install -U virtualenv pipenv --no-cache-dir && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir
