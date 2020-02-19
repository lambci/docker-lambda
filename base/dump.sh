#!/bin/bash

RUNTIMES="node43 node610 node810 node10x node12x python27 python36 python37 python38 ruby25 ruby27 java8 java11 go1x dotnetcore20 dotnetcore21 provided"

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  aws --cli-read-timeout 0 --cli-connect-timeout 0 lambda invoke --function-name "dump-${RUNTIME}" /dev/stdout
  aws logs filter-log-events --log-group-name "/aws/lambda/dump-${RUNTIME}" \
    --start-time $(node -p 'Date.now() - 5*60*1000') --query 'events[].message' --output text
done
