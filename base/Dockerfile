FROM amazonlinux:1

# Docker doesn't support unpacking from remote URLs with ADD,
# and we don't want to 'docker import' because we can't squash into a small layer
# So this is expected to be downloaded from https://lambci.s3.amazonaws.com/fs/base.tgz
ADD ./base.tgz /opt/

RUN yum --installroot=/opt reinstall -y filesystem-2.4.30-3.8.amzn1 && \
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

# pam has problems reinstalling from a non-standard installroot,
# so reinstall everything except filesystem here
RUN yum reinstall -y setup-2.8.14-20.12.amzn1 audit-libs-2.6.5-3.28.amzn1 shadow-utils-4.1.4.2-13.10.amzn1 \
    openssl-1.0.2k-16.152.amzn1 glibc-2.17-292.180.amzn1 glibc-common-2.17-292.180.amzn1 pam-1.1.8-12.33.amzn1 && \
  yum clean all && \
  chown sbx_user1051:495 /tmp && \
  chmod 700 /tmp

WORKDIR /var/task
