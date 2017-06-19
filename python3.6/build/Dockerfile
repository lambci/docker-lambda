FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.6 \
    PYTHONPATH=/var/runtime

# For 'clean all' justification, see https://github.com/CentOS/sig-cloud-instance-images/issues/15
# and for 'touch' hack see https://docs.docker.com/engine/userguide/storagedriver/overlayfs-driver/#limitations-on-overlayfs-compatibility
RUN touch /var/lib/rpm/* && \
  yum -y install zlib-devel bzip2 bzip2-devel readline-devel sqlite sqlite-devel openssl openssl-devel && \
  yum clean all

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python3.6.tgz | tar -xz -C / && \
  curl https://www.python.org/ftp/python/3.6.1/Python-3.6.1.tar.xz | tar -xJ && \
  cd Python-3.6.1 && \
  ./configure --prefix=/var/lang --enable-shared && \
  make -j$(getconf _NPROCESSORS_ONLN) install && \
  cd .. && \
  rm -rf Python-3.6.1 && \
  pip3 install awscli
