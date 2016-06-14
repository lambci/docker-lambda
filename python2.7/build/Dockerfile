FROM lambci/lambda-base:build

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/task:/var/task/lib \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime

ADD python2.7.tgz /

# Build tools and C bindings for native binaries
RUN     yum -y install libffi-devel xz-libs python27-devel

# Install Pip & Virtualenv
RUN     curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python
RUN     pip install -U pip virtualenv
