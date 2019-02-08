FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C / && \
  yum install -y --releasever=latest java-1.8.0-openjdk-devel-1.8.0.181 && \
  mkdir /opt/gradle && curl -L -o gradle.zip https://services.gradle.org/distributions/gradle-5.2-bin.zip && \
  unzip -d /opt/gradle gradle.zip && rm gradle.zip && mkdir /opt/maven && \
  curl -L http://mirror.metrocast.net/apache/maven/maven-3/3.6.0/binaries/apache-maven-3.6.0-bin.tar.gz -o maven.tar.gz && \
  tar -C /opt/maven -zxf maven.tar.gz && rm maven.tar.gz

ENV PATH="${PATH}:/opt/gradle/gradle-5.2/bin:/opt/maven/apache-maven-3.6.0/bin/"

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U awscli boto3 aws-sam-cli==0.10.0 aws-lambda-builders==0.1.0 --no-cache-dir
