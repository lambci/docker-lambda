from __future__ import print_function
import os
import sys
import subprocess

# Just a test lambda, run with:
# docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7
# OR
# docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6
# OR
# docker run --rm -v "$PWD":/var/task lambci/lambda:python3.7 lambda_function.lambda_handler

def lambda_handler(event, context):
    context.log('Hello!')
    context.log('Hmmm, does not add newlines in 3.7?')
    context.log('\n')

    print(sys.executable)
    print(sys.argv)
    print(os.getcwd())
    print(__file__)
    print(os.environ)
    print(context.__dict__)

    return {
        "executable": str(sys.executable),
        "sys.argv": str(sys.argv),
        "os.getcwd": str(os.getcwd()),
        "__file__": str(__file__),
        "os.environ": str(os.environ),
        "context.__dict__": str(context.__dict__),
        "ps aux": str(subprocess.check_output(['ps', 'aux'])),
        "event": event
    }
