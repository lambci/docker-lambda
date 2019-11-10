FROM golang:1
WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY aws-lambda-mock.go ./
RUN GOARCH=amd64 GOOS=linux go build aws-lambda-mock.go


FROM lambci/lambda:provided


FROM lambci/lambda-base

ENV AWS_EXECUTION_ENV=AWS_Lambda_go1.x

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/go1.x.tgz | tar -zx -C /

COPY --from=0 /app/aws-lambda-mock /var/runtime/aws-lambda-go

COPY --from=1 /var/runtime/init /var/runtime/mockserver

USER sbx_user1051

ENTRYPOINT ["/var/runtime/aws-lambda-go"]
