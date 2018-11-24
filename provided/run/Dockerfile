FROM golang:1
WORKDIR /app
COPY go.mod ./
RUN go mod download
COPY init.go ./
RUN GOARCH=amd64 GOOS=linux go build init.go


FROM lambci/lambda-base

COPY --from=0 /app/init /var/runtime/init

USER sbx_user1051

ENTRYPOINT ["/var/runtime/init"]
