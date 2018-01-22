FROM lambci/lambda-base:build

ENV GOLANG_VERSION=1.9.3 \
    GOPATH=/go \
    PATH=/go/bin:/usr/local/go/bin:$PATH

WORKDIR /go/src/handler

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/go1.x.tgz | tar -zx -C / && \
  curl https://dl.google.com/go/go${GOLANG_VERSION}.linux-amd64.tar.gz | tar -zx -C /usr/local && \
  go get github.com/golang/dep/cmd/dep && \
  go install github.com/golang/dep/cmd/dep

CMD ["dep", "ensure"]
