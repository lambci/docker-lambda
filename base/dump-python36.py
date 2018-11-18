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
        return print(subprocess.check_output(['sh', '-c', event['cmd']]))

    filename = 'python3.6.tgz'
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

#  /var/lang/bin/python3.6
#  ['/var/runtime/awslambda/bootstrap.py']
#  /var/task
#  /var/task/lambda_function.py
#  environ({
#  'PATH': '/var/lang/bin:/usr/local/bin:/usr/bin/:/bin:/opt/bin',
#  'LANG': 'en_US.UTF-8',
#  'LD_LIBRARY_PATH': '/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib:/opt/lib',
#  'LAMBDA_TASK_ROOT': '/var/task',
#  'LAMBDA_RUNTIME_DIR': '/var/runtime',
#  'AWS_REGION': 'us-east-1',
#  'AWS_DEFAULT_REGION': 'us-east-1',
#  'AWS_LAMBDA_LOG_GROUP_NAME': '/aws/lambda/dump-python36',
#  'AWS_LAMBDA_LOG_STREAM_NAME': '2017/04/30/[$LATEST]55cf2f4a9b924101b800f0b9f10ab74c',
#  'AWS_LAMBDA_FUNCTION_NAME': 'dump-python36',
#  'AWS_LAMBDA_FUNCTION_MEMORY_SIZE': '1536',
#  'AWS_LAMBDA_FUNCTION_VERSION': '$LATEST',
#  '_AWS_XRAY_DAEMON_ADDRESS': '169.254.79.2',
#  '_AWS_XRAY_DAEMON_PORT': '2000',
#  'AWS_XRAY_DAEMON_ADDRESS': '169.254.79.2:2000',
#  'AWS_XRAY_CONTEXT_MISSING': 'LOG_ERROR',
#  '_X_AMZN_TRACE_ID': 'Root=1-5905f755-a3ea01f9783851b148e19f51;Parent=6b4656b818d2c094;Sampled=0',
#  'AWS_EXECUTION_ENV': 'AWS_Lambda_python3.6',
#  'PYTHONPATH': '/var/runtime',
#  'AWS_ACCESS_KEY_ID': 'ASIAJDXI4GKGONVRYCJA',
#  'AWS_SECRET_ACCESS_KEY': '0QFwsOM3a2q96n6Ddx6qp/e9qFAMTfRShXp51Rvb',
#  'AWS_SESSION_TOKEN': 'FQoDYXdzEHgaDAdBKrOATj5IDnzbdCLmAYPYsS4f+w9OQMpop0Bam28jBXR5IU0qglCtzh4wzMyWp8xHJTej3JACkO/yYWcooKjGaUJ7OaiZjPT9L2GmC+7zxTS7RpXsHDQGLX4f67TuMZ45DgQu43DyawwUWjQcYqJEr+v9NjpxtqB8h8EmIKNtApvieSLCUjdFVppmva17fm87tj4Ep9P3qHmNWpKyaiTBiIvYU1YASNIzhwa/0XBMynudAQhbqlr8UfmYTjEUZG7CjgCPgICl1LOIqzN8PZ5KFutLq1eBcRFpProhhqt4tNZsED97hoZPxb6aZCs//J5UcaL7KNXul8gF',
#  'AWS_SECURITY_TOKEN': 'FQoDYXdzEHgaDAdBKrOATj5IDnzbdCLmAYPYsS4f+w9OQMpop0Bam28jBXR5IU0qglCtzh4wzMyWp8xHJTej3JACkO/yYWcooKjGaUJ7OaiZjPT9L2GmC+7zxTS7RpXsHDQGLX4f67TuMZ45DgQu43DyawwUWjQcYqJEr+v9NjpxtqB8h8EmIKNtApvieSLCUjdFVppmva17fm87tj4Ep9P3qHmNWpKyaiTBiIvYU1YASNIzhwa/0XBMynudAQhbqlr8UfmYTjEUZG7CjgCPgICl1LOIqzN8PZ5KFutLq1eBcRFpProhhqt4tNZsED97hoZPxb6aZCs//J5UcaL7KNXul8gF'
#  })
#  {
#  'aws_request_id': '1fcdc383-a9e8-4228-bc1c-8db17629e183',
#  'log_group_name': '/aws/lambda/dump-python36',
#  'log_stream_name': '2017/03/23/[$LATEST]c079a84d433534434534ef0ddc99d00f',
#  'function_name': 'dump-python36',
#  'memory_limit_in_mb': '1536',
#  'function_version': '$LATEST',
#  'invoked_function_arn': 'arn:aws:lambda:us-east-1:879423879432:function:dump-python36',
#  'client_context': None,
#  'identity': <__main__.CognitoIdentity object at 0x7f79c5974eb8>
#  }
