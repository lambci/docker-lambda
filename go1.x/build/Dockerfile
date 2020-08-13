FROM lambci/lambda-base:build

# https://golang.org/doc/devel/release.html
ENV GOLANG_VERSION=1.15 \
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
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.0.0 && \
  pipx install aws-sam-cli==1.1.0

CMD ["dep", "ensure"]
