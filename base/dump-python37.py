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

    filename = 'python3.7.tgz'

    subprocess.call(['sh', '-c', f'tar -cpzf /tmp/{filename} --numeric-owner --ignore-failed-read ' +
                     '/var/runtime /var/lang /var/rapid'])

    print('Zipping done! Uploading...')

    TRANSFER.upload_file(f'/tmp/{filename}', 'lambci', f'fs/{filename}', extra_args={'ACL': 'public-read'})

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

#  "sys.executable": "/var/lang/bin/python3.7",
#  "sys.argv": [
  #  "/var/runtime/bootstrap"
#  ],
#  "sys.path": [
  #  "/var/task",
  #  "/opt/python/lib/python3.7/site-packages",
  #  "/opt/python",
  #  "/var/runtime",
  #  "/var/lang/lib/python37.zip",
  #  "/var/lang/lib/python3.7",
  #  "/var/lang/lib/python3.7/lib-dynload",
  #  "/var/lang/lib/python3.7/site-packages",
  #  "/opt/python/lib/python3.7/site-packages",
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
  #  "AWS_LAMBDA_LOG_GROUP_NAME": "/aws/lambda/dump-python37",
  #  "AWS_LAMBDA_LOG_STREAM_NAME": "2018/11/20/[$LATEST]ac1dfb2ddf5a4ce8ae56fb4f8bbef79e",
  #  "AWS_LAMBDA_FUNCTION_NAME": "dump-python37",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE": "3008",
  #  "AWS_LAMBDA_FUNCTION_VERSION": "$LATEST",
  #  "_AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2",
  #  "_AWS_XRAY_DAEMON_PORT": "2000",
  #  "AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2:2000",
  #  "AWS_XRAY_CONTEXT_MISSING": "LOG_ERROR",
  #  "AWS_EXECUTION_ENV": "AWS_Lambda_python3.7",
  #  "_HANDLER": "lambda_function.lambda_handler",
  #  "AWS_ACCESS_KEY_ID": "ASIAYBQ3XNZICRJLMSWK",
  #  "AWS_SECRET_ACCESS_KEY": "zVGU...",
  #  "AWS_SESSION_TOKEN": "FQoG...",
  #  "_X_AMZN_TRACE_ID": "Root=1-5bf3752e-f6ac2142f8303c52aaab2628;Parent=08d2682547d140dd;Sampled=0"
#  },
#  "context": {
  #  "aws_request_id": "475d3ab2-ec6e-11e8-acea-69ef4368db5c",
  #  "log_group_name": "/aws/lambda/dump-python37",
  #  "log_stream_name": "2018/11/20/[$LATEST]ac1dfb2ddf5a4ce8ae56fb4f8bbef79e",
  #  "function_name": "dump-python37",
  #  "memory_limit_in_mb": "3008",
  #  "function_version": "$LATEST",
  #  "invoked_function_arn": "arn:aws:lambda:us-east-1:999999999999:function:dump-python37",
  #  "client_context": "None",
  #  "identity": "<bootstrap.CognitoIdentity object at 0x7f0a8b6dbe48>",
  #  "_epoch_deadline_time_in_ms": "1542682202288"
#  },
#  "ps aux": [
  #  "USER PID %CPU %MEM    VSZ   RSS TTY STAT START TIME COMMAND",
  #  "473    1  0.0  0.1 205576  6896 ?   Ssl  02:38 0:00 /var/rapid/init --bootstrap /var/runtime/bootstrap",
  #  "473    7  0.0  0.7 230312 28936 ?   S    02:38 0:00 /var/lang/bin/python3.7 /var/runtime/bootstrap",
  #  "473   40  0.0  0.0 117224  2512 ?   R    02:45 0:00 ps aux"
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
  #  "_LAMBDA_SB_ID=23",
  #  "_LAMBDA_SHARED_MEM_FD=12",
  #  "AWS_REGION=us-east-1",
  #  "AWS_DEFAULT_REGION=us-east-1",
  #  "AWS_LAMBDA_LOG_GROUP_NAME=/aws/lambda/dump-python37",
  #  "AWS_LAMBDA_LOG_STREAM_NAME=2018/11/20/[$LATEST]ac1dfb2ddf5a4ce8ae56fb4f8bbef79e",
  #  "AWS_LAMBDA_FUNCTION_NAME=dump-python37",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE=3008",
  #  "AWS_LAMBDA_FUNCTION_VERSION=$LATEST",
  #  "_AWS_XRAY_DAEMON_ADDRESS=169.254.79.2",
  #  "_AWS_XRAY_DAEMON_PORT=2000",
  #  "AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000",
  #  "AWS_XRAY_CONTEXT_MISSING=LOG_ERROR",
  #  "_X_AMZN_TRACE_ID=Parent=22afbb5e10233b0d",
  #  "AWS_EXECUTION_ENV=AWS_Lambda_python3.7",
  #  "_HANDLER=lambda_function.lambda_handler",
  #  "_LAMBDA_RUNTIME_LOAD_TIME=1534336339261"
#  ]
