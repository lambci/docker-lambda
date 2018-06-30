FROM openjdk:8-alpine
WORKDIR /src
COPY ./lambda-runtime-mock /src
RUN apk add --no-cache curl && ./build.sh


FROM lambci/lambda-base

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C /

COPY --from=0 /src/LambdaSandboxJava-1.0.jar /var/runtime/lib/

WORKDIR /

USER sbx_user1051

ENTRYPOINT ["/usr/bin/java", "-XX:MaxHeapSize=2834432k", "-XX:MaxMetaspaceSize=163840k", "-XX:ReservedCodeCacheSize=81920k", \
  "-XX:+UseSerialGC", "-Xshare:on", "-XX:-TieredCompilation", "-Djava.net.preferIPv4Stack=true", \
  "-jar", "/var/runtime/lib/LambdaJavaRTEntry-1.0.jar"]
