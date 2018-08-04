FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C / && \
  yum install -y java-1.8.0-openjdk-devel && \
  curl -L http://repos.fedorapeople.org/repos/dchen/apache-maven/epel-apache-maven.repo -o /etc/yum.repos.d/epel-apache-maven.repo && \
  sed -i s/\$releasever/7/g /etc/yum.repos.d/epel-apache-maven.repo && \
  yum clean all && \
  yum install -y apache-maven