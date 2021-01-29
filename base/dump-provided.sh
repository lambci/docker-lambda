#!/bin/sh

set -euo pipefail

export HOME=/tmp
export PATH=/tmp/.local/bin:$PATH

cd /tmp
curl -sSL https://bootstrap.pypa.io/2.7/get-pip.py -o get-pip.py
python get-pip.py --user
pip install --user awscli

while true
do
  HEADERS="$(mktemp)"

  EVENT_DATA=$(curl -v -sS -LD "$HEADERS" -X GET "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/next")
  INVOCATION_ID=$(grep -Fi Lambda-Runtime-Aws-Request-Id "$HEADERS" | tr -d '[:space:]' | cut -d: -f2)

  tar -cpzf /tmp/provided.tgz --numeric-owner --ignore-failed-read /var/runtime /var/lang

  echo 'Zipping done! Uploading...'

  aws s3 cp /tmp/provided.tgz s3://lambci/fs/ --acl public-read

  echo 'Uploading done!'

  RESPONSE="
$(env)
$(ps aux)
$(xargs -n 1 -0 < /proc/1/environ)
"

  echo $RESPONSE

  curl -v "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/$INVOCATION_ID/response" -d "$RESPONSE"
done

