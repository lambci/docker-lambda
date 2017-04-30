from __future__ import print_function

import os
import sys
import subprocess
import boto3
from boto3.s3.transfer import S3Transfer

client = boto3.client('s3')
transfer = S3Transfer(client)

def lambda_handler(event, context):
    if ('cmd' in event):
        return subprocess.call(['sh', '-c', event['cmd']])

    filename = 'python2.7.tgz'
    cmd = 'tar -cpzf /tmp/{} --numeric-owner --ignore-failed-read /var/runtime /var/lang'.format(filename)

    subprocess.call(['sh', '-c', cmd])

    print('Zipping done! Uploading...')

    data = transfer.upload_file('/tmp/' + filename, 'lambci', 'fs/' + filename,
                                extra_args={'ACL': 'public-read'})

    print('Uploading done!')

    print(sys.executable)
    print(sys.argv)
    print(os.getcwd())
    print(__file__)
    print(os.environ)
    print(context.__dict__)

    return data

#  /usr/bin/python2.7
#  ['/var/runtime/awslambda/bootstrap.py']
#  /var/task
#  /var/task/lambda_function.py
#  {
#  'AWS_LAMBDA_FUNCTION_VERSION': '$LATEST',
#  'LAMBDA_TASK_ROOT': '/var/task',
#  'PATH': '/usr/local/bin:/usr/bin/:/bin',
#  'LD_LIBRARY_PATH': '/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib',
#  'LANG': 'en_US.UTF-8',
#  'AWS_LAMBDA_FUNCTION_NAME': 'dump-python27',
#  'AWS_REGION': 'us-east-1',
#  'AWS_XRAY_CONTEXT_MISSING': 'LOG_ERROR',
#  'AWS_SESSION_TOKEN': 'FQoDYXdzEMb//////////...0oog7bzuQU=',
#  'AWS_SECURITY_TOKEN': 'FQoDYXdzEMb//////////...0oog7bzuQU=',
#  'LAMBDA_RUNTIME_DIR': '/var/runtime',
#  'PYTHONPATH': '/var/runtime',
#  'AWS_LAMBDA_FUNCTION_MEMORY_SIZE': '1536',
#  '_AWS_XRAY_DAEMON_PORT': '2000',
#  '_AWS_XRAY_DAEMON_ADDRESS': '169.254.79.2',
#  'AWS_LAMBDA_LOG_GROUP_NAME': '/aws/lambda/dump-python27',
#  'AWS_LAMBDA_LOG_STREAM_NAME': '2017/03/23/[$LATEST]c079a84d433534434534ef0ddc99d00f',
#  'AWS_ACCESS_KEY_ID': 'ASIA...C37A',
#  '_X_AMZN_TRACE_ID': 'Root=1-dc99d00f-c079a84d433534434534ef0d;Parent=91ed514f1e5c03b2;Sampled=0',
#  'AWS_DEFAULT_REGION': 'us-east-1',
#  'AWS_SECRET_ACCESS_KEY': 'JZvD...BDZ4L',
#  'AWS_EXECUTION_ENV': 'AWS_Lambda_python2.7',
#  'AWS_XRAY_DAEMON_ADDRESS': '169.254.79.2:2000'
#  }
#  {
#  'aws_request_id': '1fcdc383-a9e8-4228-bc1c-8db17629e183',
#  'log_stream_name': '2017/03/23/[$LATEST]c079a84d433534434534ef0ddc99d00f',
#  'invoked_function_arn': 'arn:aws:lambda:us-east-1:879423879432:function:dump-python27',
#  'client_context': None,
#  'log_group_name': '/aws/lambda/dump-python27',
#  'function_name': 'dump-python27',
#  'function_version': '$LATEST',
#  'identity': <__main__.CognitoIdentity object at 0x7f5985a27fd0>,
#  'memory_limit_in_mb': '1536'
#  }
