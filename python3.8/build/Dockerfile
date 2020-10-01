FROM lambci/lambda-base-2:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.8 \
    PKG_CONFIG_PATH=/var/lang/lib/pkgconfig:/usr/lib64/pkgconfig:/usr/share/pkgconfig \
    PIPX_BIN_DIR=/var/lang/bin \
    PIPX_HOME=/var/lang/pipx

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/python3.8.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN pip install -U pip setuptools wheel --no-cache-dir && \
  pip install pipx --no-cache-dir && \
  pipx install virtualenv && \
  pipx install pipenv && \
  pipx install poetry==1.1.0 && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.1.0 && \
  pipx install aws-sam-cli==1.3.2
