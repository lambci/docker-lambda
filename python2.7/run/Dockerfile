FROM lambci/lambda-base

ENV PATH=/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/task:/var/task/lib \
    PYTHONPATH=/var/runtime \
    LAMBDA_TASK_ROOT=/var/task \
    LAMBDA_RUNTIME_DIR=/var/runtime \
    LAMBDA_CONSOLE_SOCKET=16 \
    LAMBDA_SHARED_MEM_FD=11 \
    LAMBDA_LOG_FD=9 \
    LAMBDA_CONTROL_SOCKET=14 \
    LAMBDA_RUNTIME_LOAD_TIME=1530232235231

RUN rm -rf /var/runtime

ADD python2.7.tgz /

RUN rm /var/runtime/awslambda/runtime.so
ADD runtime-mock.py /var/runtime/awslambda/runtime.py

# Not sure why permissions don't work just by modifying the owner
RUN rm -rf /tmp && mkdir /tmp && chown -R sbx_user1051:495 /tmp && chmod 700 /tmp

WORKDIR /var/task

USER sbx_user1051

ENTRYPOINT ["/usr/bin/python2.7", "/var/runtime/awslambda/bootstrap.py"]

