FROM golang:1
WORKDIR /go/src/github.com/lambci/docker-lambda
RUN curl -sSL -o /usr/local/bin/dep https://github.com/golang/dep/releases/download/v0.3.2/dep-linux-amd64 && chmod +x /usr/local/bin/dep
COPY aws-lambda-mock.go Gopkg.toml Gopkg.lock ./
RUN dep ensure
RUN GOARCH=amd64 GOOS=linux go build aws-lambda-mock.go


FROM lambci/lambda-base

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/go1.x.tgz | tar -zx -C /

COPY --from=0 /go/src/github.com/lambci/docker-lambda/aws-lambda-mock /var/runtime/aws-lambda-go

USER sbx_user1051

ENTRYPOINT ["/var/runtime/aws-lambda-go"]
