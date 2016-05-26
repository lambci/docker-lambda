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
    cmd = 'tar -cvpzf /tmp/{} --numeric-owner --ignore-failed-read /var/runtime'.format(filename)

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

    return data

# /usr/bin/python2.7
# ['/var/runtime/awslambda/bootstrap.py']
# /var/task
# /var/task/lambda_function.py
# {
# 'PATH': '/usr/local/bin:/usr/bin/:/bin',
# 'LD_LIBRARY_PATH': '/lib64:/usr/lib64:/var/runtime:/var/task:/var/task/lib',
# 'PYTHONPATH': '/var/runtime',
# 'AWS_REGION': 'us-east-1',
# 'AWS_DEFAULT_REGION': 'us-east-1',
# 'AWS_ACCESS_KEY_ID': 'ASIA...C37A',
# 'AWS_SECRET_ACCESS_KEY': 'JZvD...BDZ4L',
# 'AWS_SESSION_TOKEN': 'FQoDYXdzEMb//////////...0oog7bzuQU=',
# 'AWS_SECURITY_TOKEN': 'FQoDYXdzEMb//////////...0oog7bzuQU=',
# 'LAMBDA_CONSOLE_SOCKET': '16',
# 'LAMBDA_SHARED_MEM_FD': '11',
# 'LAMBDA_LOG_FD': '9',
# 'LAMBDA_CONTROL_SOCKET': '14',
# 'LAMBDA_RUNTIME_DIR': '/var/runtime',
# 'LAMBDA_RUNTIME_LOAD_TIME': '1530232235231',
# 'LAMBDA_TASK_ROOT': '/var/task',
# 'AWS_LAMBDA_LOG_GROUP_NAME': '/aws/lambda/dump-python27',
# 'AWS_LAMBDA_LOG_STREAM_NAME': '2016/05/18/[$LATEST]27e5a905...392c2c0b',
# 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE': '1536',
# 'AWS_LAMBDA_FUNCTION_VERSION': '$LATEST',
# 'AWS_LAMBDA_FUNCTION_NAME': 'dump-python27'
# }
