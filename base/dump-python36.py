from __future__ import print_function

import os
import sys
import subprocess
import json
import boto3
from boto3.s3.transfer import S3Transfer

TRANSFER = S3Transfer(boto3.client('s3'))


def lambda_handler(event, context):
    if 'cmd' in event:
        return print(subprocess.check_output(['sh', '-c', event['cmd']]))

    subprocess.call(['sh', '-c', 'tar -cpzf /tmp/python3.6.tgz --numeric-owner --ignore-failed-read ' +
                     '/var/runtime /var/lang /var/rapid'])

    print('Zipping done! Uploading...')

    TRANSFER.upload_file('/tmp/python3.6.tgz', 'lambci', 'fs/python3.6.tgz', extra_args={'ACL': 'public-read'})

    print('Uploading done!')

    info = {'sys.executable': sys.executable,
            'sys.argv': sys.argv,
            'sys.path': sys.path,
            'os.getcwd': os.getcwd(),
            '__file__': __file__,
            'os.environ': {k: str(v) for k, v in os.environ.items()},
            'context': {k: str(v) for k, v in context.__dict__.items()},
            'ps aux': subprocess.check_output(['ps', 'aux']).decode('utf-8').splitlines(),
            'proc environ': subprocess.check_output(
                ['sh', '-c', 'xargs -n 1 -0 < /proc/1/environ']).decode('utf-8').splitlines()}

    print(json.dumps(info, indent=2))

    return info

#  "sys.executable": "/var/lang/bin/python3.6",
#  "sys.argv": [
  #  "/var/runtime/awslambda/bootstrap.py"
#  ],
#  "sys.path": [
  #  "/var/task",
  #  "/opt/python/lib/python3.6/site-packages",
  #  "/opt/python",
  #  "/var/runtime",
  #  "/var/runtime/awslambda",
  #  "/var/lang/lib/python36.zip",
  #  "/var/lang/lib/python3.6",
  #  "/var/lang/lib/python3.6/lib-dynload",
  #  "/var/lang/lib/python3.6/site-packages",
  #  "/opt/python/lib/python3.6/site-packages",
  #  "/opt/python"
#  ],
#  "os.getcwd": "/var/task",
#  "__file__": "/var/task/lambda_function.py",
#  "os.environ": {
  #  "PATH": "/var/lang/bin:/usr/local/bin:/usr/bin/:/bin:/opt/bin",
  #  "LD_LIBRARY_PATH": "/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib",
  #  "LANG": "en_US.UTF-8",
  #  "TZ": ":UTC",
  #  "LAMBDA_TASK_ROOT": "/var/task",
  #  "LAMBDA_RUNTIME_DIR": "/var/runtime",
  #  "AWS_REGION": "us-east-1",
  #  "AWS_DEFAULT_REGION": "us-east-1",
  #  "AWS_LAMBDA_LOG_GROUP_NAME": "/aws/lambda/dump-python36",
  #  "AWS_LAMBDA_LOG_STREAM_NAME": "2018/11/20/[$LATEST]bed1311f89d44a4ca0153b20afe94ed5",
  #  "AWS_LAMBDA_FUNCTION_NAME": "dump-python36",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE": "1536",
  #  "AWS_LAMBDA_FUNCTION_VERSION": "$LATEST",
  #  "_AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2",
  #  "_AWS_XRAY_DAEMON_PORT": "2000",
  #  "AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2:2000",
  #  "AWS_XRAY_CONTEXT_MISSING": "LOG_ERROR",
  #  "_X_AMZN_TRACE_ID": "Root=1-5bf37642-635d5f0017b99334b0dc0a0a;Parent=68f7884d5be55d38;Sampled=0",
  #  "AWS_EXECUTION_ENV": "AWS_Lambda_python3.6",
  #  "_HANDLER": "lambda_function.lambda_handler",
  #  "AWS_ACCESS_KEY_ID": "ASIAYBQ3XNZIETZUEWMI",
  #  "AWS_SECRET_ACCESS_KEY": "84k...",
  #  "AWS_SESSION_TOKEN": "FQoGZ...",
  #  "AWS_SECURITY_TOKEN": "FQoGZ...",
  #  "PYTHONPATH": "/var/runtime"
#  },
#  "context": {
  #  "aws_request_id": "ec20cbf4-ec6e-11e8-ae2f-1900152991b2",
  #  "log_group_name": "/aws/lambda/dump-python36",
  #  "log_stream_name": "2018/11/20/[$LATEST]bed1311f89d44a4ca0153b20afe94ed5",
  #  "function_name": "dump-python36",
  #  "memory_limit_in_mb": "1536",
  #  "function_version": "$LATEST",
  #  "invoked_function_arn": "arn:aws:lambda:us-east-1:999999999999:function:dump-python36",
  #  "client_context": "None",
  #  "identity": "<__main__.CognitoIdentity object at 0x7f157ab6bd68>"
#  },
#  "ps aux": [
  #  "USER PID %CPU %MEM    VSZ   RSS TTY STAT START TIME COMMAND",
  #  "488    1 13.5  0.7 183460 30112 ?   Ss   02:49 0:00 /var/lang/bin/python3.6 /var/runtime/awslambda/bootstrap.py",
  #  "488    5  0.0  0.0 117224  2492 ?   R    02:49 0:00 ps aux"
#  ],
#  "proc environ": [
  #  "PATH=/var/lang/bin:/usr/local/bin:/usr/bin/:/bin:/opt/bin",
  #  "LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib",
  #  "LANG=en_US.UTF-8",
  #  "TZ=:UTC",
  #  "_LAMBDA_CONTROL_SOCKET=15",
  #  "_LAMBDA_CONSOLE_SOCKET=17",
  #  "LAMBDA_TASK_ROOT=/var/task",
  #  "LAMBDA_RUNTIME_DIR=/var/runtime",
  #  "_LAMBDA_LOG_FD=24",
  #  "_LAMBDA_SB_ID=8",
  #  "_LAMBDA_SHARED_MEM_FD=12",
  #  "AWS_REGION=us-east-1",
  #  "AWS_DEFAULT_REGION=us-east-1",
  #  "AWS_LAMBDA_LOG_GROUP_NAME=/aws/lambda/dump-python36",
  #  "AWS_LAMBDA_LOG_STREAM_NAME=2018/11/20/[$LATEST]bed1311f89d44a4ca0153b20afe94ed5",
  #  "AWS_LAMBDA_FUNCTION_NAME=dump-python36",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE=1536",
  #  "AWS_LAMBDA_FUNCTION_VERSION=$LATEST",
  #  "_AWS_XRAY_DAEMON_ADDRESS=169.254.79.2",
  #  "_AWS_XRAY_DAEMON_PORT=2000",
  #  "AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000",
  #  "AWS_XRAY_CONTEXT_MISSING=LOG_ERROR",
  #  "_X_AMZN_TRACE_ID=Parent=3f4f839064ce33c1",
  #  "AWS_EXECUTION_ENV=AWS_Lambda_python3.6",
  #  "_HANDLER=lambda_function.lambda_handler",
  #  "_LAMBDA_RUNTIME_LOAD_TIME=39954026786"
#  ]
