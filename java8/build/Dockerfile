FROM lambci/lambda:java8

FROM lambci/lambda-base:build

ENV AWS_EXECUTION_ENV=AWS_Lambda_java8

WORKDIR /

COPY --from=0 /var/runtime /var/runtime
COPY --from=0 /var/lang /var/lang
COPY --from=0 /var/rapid /var/rapid

RUN yum install -y java-1.8.0-openjdk-devel && \
  mkdir /usr/local/gradle && curl -L -o gradle.zip https://services.gradle.org/distributions/gradle-6.8.1-bin.zip && \
  unzip -qd /usr/local/gradle gradle.zip && rm gradle.zip && mkdir /usr/local/maven && \
  curl -L http://mirror.cc.columbia.edu/pub/software/apache/maven/maven-3/3.6.3/binaries/apache-maven-3.6.3-bin.tar.gz | \
  tar -zx -C /usr/local/maven

ENV PATH="/usr/local/gradle/gradle-6.8.1/bin:/usr/local/maven/apache-maven-3.6.3/bin:${PATH}"

# Add these as a separate layer as they get updated frequently
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.1.0 && \
  pipx install aws-sam-cli==1.11.0
