FROM lambci/lambda-base:build

# https://golang.org/doc/devel/release.html
ENV GOLANG_VERSION=1.14.2 \
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
RUN pip install -U awscli boto3 aws-sam-cli==0.47.0 aws-lambda-builders==0.8.0 --no-cache-dir

CMD ["dep", "ensure"]
