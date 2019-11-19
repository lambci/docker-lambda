FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C / && \
  yum install -y java-1.8.0-openjdk-devel && \
  mkdir /usr/local/gradle && curl -L -o gradle.zip https://services.gradle.org/distributions/gradle-6.0-bin.zip && \
  unzip -d /usr/local/gradle gradle.zip && rm gradle.zip && mkdir /usr/local/maven && \
  curl -L http://mirror.metrocast.net/apache/maven/maven-3/3.6.2/binaries/apache-maven-3.6.2-bin.tar.gz | \
  tar -zx -C /usr/local/maven

ENV PATH="/usr/local/gradle/gradle-6.0/bin:/usr/local/maven/apache-maven-3.6.2/bin:${PATH}"

# Add these as a separate layer as they get updated frequently
RUN pip install -U awscli boto3 aws-sam-cli==0.31.1 aws-lambda-builders==0.6.0 --no-cache-dir
