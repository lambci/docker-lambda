from __future__ import print_function
import os
import sys
import subprocess

# Just a test lambda, run with:
# docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7
# OR
# docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6

def lambda_handler(event, context):
    for arg in sys.argv:
        print(arg)
    print(os.getcwd())
    print(os.path.basename(__file__))
    for key in os.environ.keys():
        print('{0}={1}'.format(key,os.environ[key]))
    print(subprocess.check_output(['ps', 'aux']).decode('utf-8'))
    print(subprocess.check_output(['sh', '-c', 'xargs -n 1 -0 < /proc/1/environ']).decode('utf-8'))
    print(sys.path)
    print(os.getuid())
    print(os.getgid())
    print(os.geteuid())
    print(os.getegid())
    print(os.getgroups())
    print(os.umask(0o222))

    print(event)
    print(context.get_remaining_time_in_millis())
    print(context.aws_request_id)
    if context.client_context:
        print(context.client_context)
    print(context.function_name)
    print(context.function_version)
    print(context.identity.cognito_identity_id)
    print(context.identity.cognito_identity_pool_id)
    print(context.invoked_function_arn)
    print(context.log('Log this for me please'))
    print(context.log_group_name)
    print(context.log_stream_name)
    print(context.memory_limit_in_mb)

    return 'It works!'
