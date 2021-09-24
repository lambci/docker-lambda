FROM lambci/lambda:provided as provided

FROM public.ecr.aws/lambda/nodejs:14 as lambda

ENV PATH=/var/lang/bin:$PATH \
LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
AWS_EXECUTION_ENV=AWS_Lambda_nodejs14.x

COPY --from=provided /var/runtime/init /var/rapid/init

ENTRYPOINT ["/var/rapid/init", "--bootstrap", "/var/runtime/bootstrap", "--enable-msg-logs"]
