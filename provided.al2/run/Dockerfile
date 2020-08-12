FROM lambci/lambda:provided


FROM lambci/lambda-base-2

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH

COPY --from=0 /var/runtime/init /var/runtime/init

USER sbx_user1051

ENTRYPOINT ["/var/runtime/init"]
