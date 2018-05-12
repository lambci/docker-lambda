#!/bin/bash

RUNTIMES="node43 node610 node810 python27 python36 java8 go1x dotnetcore20"

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  aws lambda invoke --function-name "dump-${RUNTIME}" /dev/stdout
  aws logs filter-log-events --log-group-name "/aws/lambda/dump-${RUNTIME}" \
    --start-time $(node -p 'Date.now() - 5*60*1000') --query 'events[].message' --output text
done
