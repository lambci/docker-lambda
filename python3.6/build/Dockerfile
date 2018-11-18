FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.6 \
    PYTHONPATH=/var/runtime \
    PKG_CONFIG_PATH=/var/lang/lib/pkgconfig:/usr/lib64/pkgconfig:/usr/share/pkgconfig

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python3.6.tgz | tar -xz -C / && \
  yum install -y bzip2-devel && \
  sed -i '/^prefix=/c\prefix=/var/lang' /var/lang/lib/pkgconfig/python-3.6.pc && \
  curl https://www.python.org/ftp/python/3.6.1/Python-3.6.1.tar.xz | tar -xJ && \
  cd Python-3.6.1 && \
  LIBS="$LIBS -lutil -lrt" ./configure --prefix=/var/lang && \
  make -j$(getconf _NPROCESSORS_ONLN) libinstall libainstall inclinstall && \
  cd .. && \
  rm -rf Python-3.6.1

# Add these as a separate layer as they get updated frequently
RUN pip install -U pip setuptools --no-cache-dir && \
  pip install -U virtualenv pipenv awscli boto3 aws-sam-cli aws-lambda-builders==0.0.1-dev --no-cache-dir
