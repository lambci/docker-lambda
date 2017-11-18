FROM lambci/lambda-base

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C /

COPY lambda-runtime-mock/LambdaSandboxJava-1.0.jar /var/runtime/lib/

WORKDIR /

USER sbx_user1051

ENTRYPOINT ["/usr/bin/java", "-XX:MaxHeapSize=1336935k", "-XX:MaxMetaspaceSize=157286k", "-XX:ReservedCodeCacheSize=78643k", \
  "-XX:+UseSerialGC", "-Xshare:on", "-XX:-TieredCompilation", "-jar", "/var/runtime/lib/LambdaJavaRTEntry-1.0.jar"]
