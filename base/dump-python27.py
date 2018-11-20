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

    subprocess.call(['sh', '-c', 'tar -cpzf /tmp/base.tgz -C / ' +
                     '--exclude=/proc --exclude=/sys --exclude=/dev --exclude=/tmp ' +
                     '--exclude=/var/task/* --exclude=/var/runtime/* --exclude=/var/lang/* --exclude=/var/rapid/* ' +
                     '--numeric-owner --ignore-failed-read /'])

    subprocess.call(['sh', '-c', 'tar -cpzf /tmp/python2.7.tgz --numeric-owner --ignore-failed-read ' +
                     '/var/runtime /var/lang /var/rapid'])

    print('Zipping done! Uploading...')

    TRANSFER.upload_file('/tmp/base.tgz', 'lambci', 'fs/base.tgz', extra_args={'ACL': 'public-read'})

    TRANSFER.upload_file('/tmp/python2.7.tgz', 'lambci', 'fs/python2.7.tgz', extra_args={'ACL': 'public-read'})

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

#  "sys.executable": "/usr/bin/python2.7",
#  "os.environ": {
  #  "AWS_LAMBDA_FUNCTION_VERSION": "$LATEST",
  #  "LAMBDA_TASK_ROOT": "/var/task",
  #  "PATH": "/usr/local/bin:/usr/bin/:/bin:/opt/bin",
  #  "LD_LIBRARY_PATH": "/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib",
  #  "LANG": "en_US.UTF-8",
  #  "TZ": ":UTC",
  #  "AWS_REGION": "us-east-1",
  #  "AWS_XRAY_CONTEXT_MISSING": "LOG_ERROR",
  #  "AWS_SECURITY_TOKEN": "FQoG...",
  #  "LAMBDA_RUNTIME_DIR": "/var/runtime",
  #  "_AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2",
  #  "_HANDLER": "lambda_function.lambda_handler",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE": "1536",
  #  "_AWS_XRAY_DAEMON_PORT": "2000",
  #  "AWS_ACCESS_KEY_ID": "ASIAYBQ3XNZIEDGGFLW2",
  #  "PYTHONPATH": "/var/runtime",
  #  "AWS_LAMBDA_LOG_GROUP_NAME": "/aws/lambda/dump-python27",
  #  "AWS_LAMBDA_LOG_STREAM_NAME": "2018/11/20/[$LATEST]5c455c57ebf1498baa655038b81887bb",
  #  "AWS_SESSION_TOKEN": "FQoG...",
  #  "_X_AMZN_TRACE_ID": "Root=1-5bf37490-36e3602a997859d0e6cb7a7e;Parent=69511ba109ff016c;Sampled=0",
  #  "AWS_DEFAULT_REGION": "us-east-1",
  #  "AWS_SECRET_ACCESS_KEY": "k/ri...",
  #  "AWS_EXECUTION_ENV": "AWS_Lambda_python2.7",
  #  "AWS_XRAY_DAEMON_ADDRESS": "169.254.79.2:2000",
  #  "AWS_LAMBDA_FUNCTION_NAME": "dump-python27"
#  },
#  "context": {
  #  "memory_limit_in_mb": "1536",
  #  "aws_request_id": "e9958263-ec6d-11e8-96d7-15e8d92f88e2",
  #  "log_stream_name": "2018/11/20/[$LATEST]5c455c57ebf1498baa655038b81887bb",
  #  "invoked_function_arn": "arn:aws:lambda:us-east-1:999999999999:function:dump-python27",
  #  "log_group_name": "/aws/lambda/dump-python27",
  #  "function_name": "dump-python27",
  #  "function_version": "$LATEST",
  #  "identity": "<__main__.CognitoIdentity object at 0x7f5d65a02610>",
  #  "client_context": "None"
#  },
#  "proc environ": [
  #  "PATH=/usr/local/bin:/usr/bin/:/bin:/opt/bin",
  #  "LD_LIBRARY_PATH=/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib",
  #  "LANG=en_US.UTF-8",
  #  "TZ=:UTC",
  #  "_LAMBDA_CONTROL_SOCKET=17",
  #  "_LAMBDA_CONSOLE_SOCKET=21",
  #  "LAMBDA_TASK_ROOT=/var/task",
  #  "LAMBDA_RUNTIME_DIR=/var/runtime",
  #  "_LAMBDA_LOG_FD=31",
  #  "_LAMBDA_SB_ID=9",
  #  "_LAMBDA_SHARED_MEM_FD=12",
  #  "AWS_REGION=us-east-1",
  #  "AWS_DEFAULT_REGION=us-east-1",
  #  "AWS_LAMBDA_LOG_GROUP_NAME=/aws/lambda/dump-python27",
  #  "AWS_LAMBDA_LOG_STREAM_NAME=2018/11/20/[$LATEST]5c455c57ebf1498baa655038b81887bb",
  #  "AWS_LAMBDA_FUNCTION_NAME=dump-python27",
  #  "AWS_LAMBDA_FUNCTION_MEMORY_SIZE=1536",
  #  "AWS_LAMBDA_FUNCTION_VERSION=$LATEST",
  #  "_AWS_XRAY_DAEMON_ADDRESS=169.254.79.2",
  #  "_AWS_XRAY_DAEMON_PORT=2000",
  #  "AWS_XRAY_DAEMON_ADDRESS=169.254.79.2:2000",
  #  "AWS_XRAY_CONTEXT_MISSING=LOG_ERROR",
  #  "_X_AMZN_TRACE_ID=Parent=6863b4d91ed32479",
  #  "AWS_EXECUTION_ENV=AWS_Lambda_python2.7",
  #  "_HANDLER=lambda_function.lambda_handler",
  #  "_LAMBDA_RUNTIME_LOAD_TIME=150756847494"
#  ],
#  "sys.argv": [
  #  "/var/runtime/awslambda/bootstrap.py"
#  ],
#  "sys.path": [
  #  "/var/task",
  #  "/opt/python/lib/python2.7/site-packages",
  #  "/opt/python",
  #  "/var/runtime",
  #  "/var/runtime/awslambda",
  #  "/usr/lib/python27.zip",
  #  "/usr/lib64/python2.7",
  #  "/usr/lib64/python2.7/plat-linux2",
  #  "/usr/lib64/python2.7/lib-tk",
  #  "/usr/lib64/python2.7/lib-old",
  #  "/usr/lib64/python2.7/lib-dynload",
  #  "/usr/local/lib64/python2.7/site-packages",
  #  "/usr/local/lib/python2.7/site-packages",
  #  "/usr/lib64/python2.7/site-packages",
  #  "/usr/lib/python2.7/site-packages",
  #  "/usr/lib64/python2.7/dist-packages",
  #  "/usr/lib/python2.7/dist-packages",
  #  "/opt/python/lib/python2.7/site-packages",
  #  "/opt/python"
#  ],
#  "ps aux": [
  #  "USER PID %CPU %MEM    VSZ   RSS TTY STAT START TIME COMMAND",
  #  "487    1 20.0  0.7 250180 30040 ?   Ss   02:42 0:00 /usr/bin/python2.7 /var/runtime/awslambda/bootstrap.py",
  #  "487    6  0.0  0.0 117220  2496 ?   R    02:42 0:00 ps aux"
#  ],
#  "__file__": "/var/task/lambda_function.py",
#  "os.getcwd": "/var/task"
