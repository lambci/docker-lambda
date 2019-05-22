FROM scratch

# Docker doesn't support unpacking from remote URLs with ADD,
# and we don't want to 'docker import' because we can't squash into a small layer
# So this is expected to be downloaded from https://lambci.s3.amazonaws.com/fs/base-2.tgz
ADD ./base-2.tgz /

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
    _X_AMZN_TRACE_ID='Parent=11560be54abce8ed'

RUN rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log && \
  mkdir -p /root /tmp && \
  chmod 550 /root && \
  chown sbx_user1051:495 /tmp && \
  chmod 700 /tmp

WORKDIR /var/task
