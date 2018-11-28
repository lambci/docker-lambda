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

    subprocess.call(['sh', '-c', 'tar -cpzf /tmp/python3.7.tgz --numeric-owner --ignore-failed-read ' +
                     '/var/runtime /var/lang /var/rapid'])

    print('Zipping done! Uploading...')

    TRANSFER.upload_file('/tmp/python3.7.tgz', 'lambci', 'fs/python3.7.tgz', extra_args={'ACL': 'public-read'})

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
