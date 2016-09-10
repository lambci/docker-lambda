FROM lambci/lambda-base:build

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime && \
  curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U pip virtualenv

ADD python2.7.tgz /

