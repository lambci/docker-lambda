FROM lambci/lambda-base

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_python3.6

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/python3.6.tgz | tar -zx -C /

RUN rm /var/runtime/awslambda/runtime.cpython-36m-x86_64-linux-gnu.so
COPY runtime-mock.py /var/runtime/awslambda/runtime.py

USER sbx_user1051

ENTRYPOINT ["/var/lang/bin/python3.6", "/var/runtime/awslambda/bootstrap.py"]
