#!/bin/bash

source ${PWD}/runtimes.sh

for RUNTIME in $RUNTIMES; do
  echo $RUNTIME
  aws --cli-read-timeout 0 --cli-connect-timeout 0 lambda invoke --function-name "dump-${RUNTIME}" /dev/stdout
  aws logs filter-log-events --log-group-name "/aws/lambda/dump-${RUNTIME}" \
    --start-time $(node -p 'Date.now() - 5*60*1000') --query 'events[].message' --output text
done
