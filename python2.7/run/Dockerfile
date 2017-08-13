FROM lambci/lambda-base

ENV AWS_EXECUTION_ENV=AWS_Lambda_python2.7 \
    PYTHONPATH=/var/runtime

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python2.7.tgz | tar -zx -C /

RUN rm /var/runtime/awslambda/runtime.so
ADD runtime-mock.py /var/runtime/awslambda/runtime.py

USER sbx_user1051

ENTRYPOINT ["/usr/bin/python2.7", "/var/runtime/awslambda/bootstrap.py"]
