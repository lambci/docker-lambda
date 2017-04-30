FROM lambci/lambda-base

ENV PATH=/usr/local/lib64/node-v4.3.x/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/usr/local/lib64/node-v4.3.x/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    LANG=en_US.UTF-8 \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime \
    _LAMBDA_CONTROL_SOCKET=14 \
    _LAMBDA_SHARED_MEM_FD=11 \
    _LAMBDA_LOG_FD=9 \
    _LAMBDA_CONSOLE_SOCKET=16 \
    _LAMBDA_RUNTIME_LOAD_TIME=1530232235231 \
    _AWS_XRAY_DAEMON_ADDRESS=169.254.79.2 \
    _AWS_XRAY_DAEMON_PORT=2000 \
    AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000 \
    AWS_XRAY_CONTEXT_MISSING=LOG_ERROR \
    _X_AMZN_TRACE_ID=Root=1-dc99d00f-c079a84d433534434534ef0d;Parent=91ed514f1e5c03b2;Sampled=0

WORKDIR /var/task

ADD yum.conf /etc/yum/yum.conf

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
# Reinstalling filesystem might not succeed fully, but continue anyway
RUN mkdir /tmp && chmod 1777 /tmp && yum reinstall -y filesystem; \
  yum reinstall -y shadow-utils && \
  yum install -y gcc-c++ clang cmake autoconf automake libtool \
    openssl-devel libffi-devel python27-devel xz-libs \
    libmpc-devel mpfr-devel gmp-devel \
    aws-cli zip git vim docker && \
  rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log

