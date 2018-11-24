FROM lambci/lambda:provided


FROM lambci/lambda-base

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_ruby2.5

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/ruby2.5.tgz | tar -zx -C /

COPY --from=0 /var/runtime/init /var/rapid/init

USER sbx_user1051

ENTRYPOINT ["/var/rapid/init", "--bootstrap", "/var/runtime/bootstrap"]
