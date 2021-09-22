FROM lambci/lambda-base as base

FROM public.ecr.aws/lambda/nodejs:14 as lambda

FROM lambci/lambda:provided as provided

FROM lambci/lambda-base-2

ENV PATH=/var/lang/bin:$PATH \
LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
AWS_EXECUTION_ENV=AWS_Lambda_nodejs14.x

COPY --from=base /opt/* /var/
COPY --from=lambda /var/lang /var/lang
COPY --from=lambda /var/runtime /var/runtime
COPY --from=provided /var/runtime/init /var/rapid/init

USER sbx_user1051

ENTRYPOINT ["/var/rapid/init", "--bootstrap", "/var/runtime/bootstrap", "--enable-msg-logs"]
