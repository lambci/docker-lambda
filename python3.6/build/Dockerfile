FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.6 \
    PYTHONPATH=/var/runtime \
    PKG_CONFIG_PATH=/var/lang/lib/pkgconfig:/usr/lib64/pkgconfig:/usr/share/pkgconfig

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python3.6.tgz | tar -xz -C / && \
  sed -i '/^prefix=/c\prefix=/var/lang' /var/lang/lib/pkgconfig/python-3.6.pc && \
  curl https://www.python.org/ftp/python/3.6.8/Python-3.6.8.tar.xz | tar -xJ && \
  cd Python-3.6.8 && \
  LIBS="$LIBS -lutil -lrt" ./configure --prefix=/var/lang && \
  make -j$(getconf _NPROCESSORS_ONLN) libinstall libainstall inclinstall && \
  cd .. && \
  rm -rf Python-3.6.8

# Add these as a separate layer as they get updated frequently
RUN pip install -U pip setuptools --no-cache-dir && \
  pip install -U virtualenv pipenv --no-cache-dir && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir
