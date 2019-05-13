#!/bin/bash

RUNTIMES="node43 node610 node810 node10x python27 python36 python37 ruby25 java8 go1x dotnetcore20 dotnetcore21 provided"

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  aws --cli-read-timeout 0 --cli-connect-timeout 0 lambda invoke --function-name "dump-${RUNTIME}" /dev/stdout
  aws logs filter-log-events --log-group-name "/aws/lambda/dump-${RUNTIME}" \
    --start-time $(node -p 'Date.now() - 5*60*1000') --query 'events[].message' --output text
done
