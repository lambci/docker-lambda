FROM amazonlinux:2

# Docker doesn't support unpacking from remote URLs with ADD,
# and we don't want to 'docker import' because we can't squash into a small layer
# So this is expected to be downloaded from https://lambci.s3.amazonaws.com/fs/base-2.tgz
ADD ./base-2.tgz /opt/

RUN yum --installroot=/opt reinstall -y filesystem-3.2-25.amzn2.0.4 \
    setup-2.8.71-10.amzn2.0.1 glibc-2.26-39.amzn2 glibc-common-2.26-39.amzn2 && \
  yum --installroot=/opt clean all

FROM scratch

COPY --from=0 /opt /

ENV PATH=/usr/local/bin:/usr/bin/:/bin:/opt/bin \
    LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib \
    LANG=en_US.UTF-8 \
    TZ=:UTC \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime \
    _LAMBDA_CONTROL_SOCKET=14 \
    _LAMBDA_SHARED_MEM_FD=11 \
    _LAMBDA_LOG_FD=9 \
    _LAMBDA_SB_ID=7 \
    _LAMBDA_CONSOLE_SOCKET=16 \
    _LAMBDA_RUNTIME_LOAD_TIME=1530232235231 \
    _AWS_XRAY_DAEMON_ADDRESS=169.254.79.2 \
    _AWS_XRAY_DAEMON_PORT=2000 \
    AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000 \
    AWS_XRAY_CONTEXT_MISSING=LOG_ERROR \
    _X_AMZN_TRACE_ID='Root=1-dc99d00f-c079a84d433534434534ef0d;Parent=91ed514f1e5c03b2;Sampled=1'

RUN chown sbx_user1051:495 /tmp && \
  chmod 700 /tmp

WORKDIR /var/task
