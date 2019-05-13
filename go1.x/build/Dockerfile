FROM lambci/lambda-base:build

ENV GOLANG_VERSION=1.12 \
    GOPATH=/go \
    PATH=/go/bin:/usr/local/go/bin:$PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_go1.x

WORKDIR /go/src/handler

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/go1.x.tgz | tar -zx -C / && \
  curl https://storage.googleapis.com/golang/go${GOLANG_VERSION}.linux-amd64.tar.gz | tar -zx -C /usr/local && \
  go get github.com/golang/dep/cmd/dep && \
  go install github.com/golang/dep/cmd/dep && \
  go get golang.org/x/vgo

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir

CMD ["dep", "ensure"]
