FROM lambci/lambda-base

ENV PATH=/var/lang/bin:/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.6 \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python3.6.tgz | tar -zx -C /

RUN rm /var/runtime/awslambda/runtime.cpython-36m-x86_64-linux-gnu.so
ADD runtime-mock.py /var/runtime/awslambda/runtime.py

USER sbx_user1051

ENTRYPOINT ["/var/lang/bin/python3.6", "/var/runtime/awslambda/bootstrap.py"]
