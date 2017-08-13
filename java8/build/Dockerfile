FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C / && \
  yum install -y --releasever=2016.09 java-1.8.0-openjdk-devel
