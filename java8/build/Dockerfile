FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -C / && \
  yum install -y java-1.8.0-openjdk-devel && \
  mkdir /usr/local/gradle && curl -L -o gradle.zip https://services.gradle.org/distributions/gradle-6.6-bin.zip && \
  unzip -d /usr/local/gradle gradle.zip && rm gradle.zip && mkdir /usr/local/maven && \
  curl -L http://mirror.cc.columbia.edu/pub/software/apache/maven/maven-3/3.6.3/binaries/apache-maven-3.6.3-bin.tar.gz | \
  tar -zx -C /usr/local/maven

ENV PATH="/usr/local/gradle/gradle-6.6/bin:/usr/local/maven/apache-maven-3.6.3/bin:${PATH}"

# Add these as a separate layer as they get updated frequently
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.0.0 && \
  pipx install aws-sam-cli==1.1.0
