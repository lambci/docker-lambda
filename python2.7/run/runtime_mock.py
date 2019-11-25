# pylint: disable=missing-docstring, global-statement, unused-argument, broad-except

from __future__ import print_function
import sys
import os
import random
import uuid
import time
import datetime
import subprocess
import json
import traceback
import base64
import signal
try:
    # for python 3
    from http.client import HTTPConnection
except ImportError:
    # for python 2
    from httplib import HTTPConnection


signal.signal(signal.SIGINT, lambda x, y: sys.exit(0))
signal.signal(signal.SIGTERM, lambda x, y: sys.exit(0))

ORIG_STDOUT = sys.stdout
ORIG_STDERR = sys.stderr

LOGS = ''
LOG_TAIL = False

STAY_OPEN = os.environ.get('DOCKER_LAMBDA_STAY_OPEN', '')

HANDLER = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('AWS_LAMBDA_FUNCTION_HANDLER', \
        os.environ.get('_HANDLER', 'lambda_function.lambda_handler'))
EVENT_BODY = sys.argv[2] if len(sys.argv) > 2 else os.environ.get('AWS_LAMBDA_EVENT_BODY', \
        (sys.stdin.read() if os.environ.get('DOCKER_LAMBDA_USE_STDIN', False) else '{}'))
FUNCTION_NAME = os.environ.get('AWS_LAMBDA_FUNCTION_NAME', 'test')
FUNCTION_VERSION = os.environ.get('AWS_LAMBDA_FUNCTION_VERSION', '$LATEST')
MEM_SIZE = os.environ.get('AWS_LAMBDA_FUNCTION_MEMORY_SIZE', '1536')
DEADLINE_MS = int(time.time() * 1000) + int(os.environ.get('AWS_LAMBDA_FUNCTION_TIMEOUT', '300'))
REGION = os.environ.get('AWS_REGION', os.environ.get('AWS_DEFAULT_REGION', 'us-east-1'))
ACCOUNT_ID = os.environ.get('AWS_ACCOUNT_ID', random.randint(100000000000, 999999999999))
ACCESS_KEY_ID = os.environ.get('AWS_ACCESS_KEY_ID', 'SOME_ACCESS_KEY_ID')
SECRET_ACCESS_KEY = os.environ.get('AWS_SECRET_ACCESS_KEY', 'SOME_SECRET_ACCESS_KEY')
SESSION_TOKEN = os.environ.get('AWS_SESSION_TOKEN', None)

INVOKEID = str(uuid.uuid4())
INVOKE_MODE = 'event'  # Either 'http' or 'event'
SUPPRESS_INIT = True  # Forces calling _get_handlers_delayed()
THROTTLED = False
DATA_SOCK = -1
CONTEXT_OBJS = {
    'clientcontext': None,
    'cognitoidentityid': None,
    'cognitopoolid': None,
}
CREDENTIALS = {
    'key': ACCESS_KEY_ID,
    'secret': SECRET_ACCESS_KEY,
    'session': SESSION_TOKEN
}
INVOKED_FUNCTION_ARN = os.environ.get('AWS_LAMBDA_FUNCTION_INVOKED_ARN', \
        'arn:aws:lambda:%s:%s:function:%s' % (REGION, ACCOUNT_ID, FUNCTION_NAME))
XRAY_TRACE_ID = os.environ.get('_X_AMZN_TRACE_ID', None)
XRAY_PARENT_ID = None
XRAY_SAMPLED = None
TRACE_ID = None
INVOKED = False
ERRORED = False
INIT_END_SENT = False
INIT_END = time.time()
RECEIVED_INVOKE_AT = time.time()
TODAY = datetime.date.today()
# export needed stuff
os.environ['AWS_LAMBDA_LOG_GROUP_NAME'] = '/aws/lambda/%s' % FUNCTION_NAME
os.environ['AWS_LAMBDA_LOG_STREAM_NAME'] = "%s/%s/%s/[%s]%s" % (
    TODAY.year,
    TODAY.month,
    TODAY.day,
    FUNCTION_VERSION,
    '%016x' % random.randrange(16**16)
)
os.environ["AWS_LAMBDA_FUNCTION_NAME"] = FUNCTION_NAME
os.environ['AWS_LAMBDA_FUNCTION_MEMORY_SIZE'] = MEM_SIZE
os.environ['AWS_LAMBDA_FUNCTION_VERSION'] = FUNCTION_VERSION
os.environ['AWS_REGION'] = REGION
os.environ['AWS_DEFAULT_REGION'] = REGION
os.environ['_HANDLER'] = HANDLER

MOCKSERVER_ENV = os.environ.copy()
MOCKSERVER_ENV['DOCKER_LAMBDA_NO_BOOTSTRAP'] = '1'
MOCKSERVER_ENV['DOCKER_LAMBDA_USE_STDIN'] = '1'

MOCKSERVER_PROCESS = subprocess.Popen(
    '/var/runtime/mockserver', stdin=subprocess.PIPE, env=MOCKSERVER_ENV)
MOCKSERVER_PROCESS.stdin.write(EVENT_BODY.encode())
MOCKSERVER_PROCESS.stdin.close()

MOCKSERVER_CONN = HTTPConnection("127.0.0.1", 9001)


def eprint(*args, **kwargs):
    print(*args, file=ORIG_STDERR, **kwargs)


def report_user_init_start():
    return


def report_user_init_end():
    global INIT_END
    INIT_END = time.time()


def report_user_invoke_start():
    return


def report_user_invoke_end():
    return


def receive_start():
    global MOCKSERVER_CONN

    ping_timeout = time.time() + 1
    while True:
        try:
            MOCKSERVER_CONN = HTTPConnection("127.0.0.1", 9001)
            MOCKSERVER_CONN.request("GET", "/2018-06-01/ping")
            resp = MOCKSERVER_CONN.getresponse()
            if resp.status != 200:
                raise Exception("Mock server returned %d" % resp.status)
            resp.read()
            break
        except Exception:
            if time.time() > ping_timeout:
                raise
            else:
                time.sleep(.005)
                continue
    return (
        INVOKEID,
        INVOKE_MODE,
        HANDLER,
        SUPPRESS_INIT,
        THROTTLED,
        CREDENTIALS
    )


def report_running(invokeid):
    return


def receive_invoke():
    global INVOKED
    global INVOKEID
    global DEADLINE_MS
    global INVOKED_FUNCTION_ARN
    global XRAY_TRACE_ID
    global EVENT_BODY
    global CONTEXT_OBJS
    global LOGS
    global LOG_TAIL
    global RECEIVED_INVOKE_AT

    ORIG_STDOUT.flush()
    ORIG_STDERR.flush()

    if not INVOKED:
        RECEIVED_INVOKE_AT = time.time()
        INVOKED = True
    else:
        LOGS = ""

    try:
        MOCKSERVER_CONN.request("GET", "/2018-06-01/runtime/invocation/next")
        resp = MOCKSERVER_CONN.getresponse()
        if resp.status != 200:
            raise Exception("/invocation/next return status %d" % resp.status)
    except Exception:
        if INVOKED and not STAY_OPEN:
            sys.exit(1 if ERRORED else 0)
            return ()
        raise

    INVOKEID = resp.getheader('Lambda-Runtime-Aws-Request-Id')
    DEADLINE_MS = int(resp.getheader('Lambda-Runtime-Deadline-Ms'))
    INVOKED_FUNCTION_ARN = resp.getheader(
        'Lambda-Runtime-Invoked-Function-Arn')
    XRAY_TRACE_ID = resp.getheader('Lambda-Runtime-Trace-Id')
    cognito_identity = json.loads(resp.getheader(
        'Lambda-Runtime-Cognito-Identity', '{}'))
    CONTEXT_OBJS['cognitoidentityid'] = cognito_identity.get('identity_id')
    CONTEXT_OBJS['cognitopoolid'] = cognito_identity.get('identity_pool_id')
    CONTEXT_OBJS['clientcontext'] = resp.getheader(
        'Lambda-Runtime-Client-Context')

    LOG_TAIL = resp.getheader('docker-lambda-log-type') == 'Tail'

    EVENT_BODY = resp.read()

    return (
        INVOKEID,
        DATA_SOCK,
        CREDENTIALS,
        EVENT_BODY,
        CONTEXT_OBJS,
        INVOKED_FUNCTION_ARN,
        XRAY_TRACE_ID,
    )


def report_fault(invokeid, msg, except_value, trace):
    global ERRORED

    ERRORED = True

    if msg and except_value:
        eprint('%s: %s' % (msg, except_value))
    if trace:
        eprint('%s' % trace)


def report_done(invokeid, errortype, result, is_fatal):
    global ERRORED
    global INIT_END_SENT

    if not INVOKED:
        return

    if errortype is not None:
        ERRORED = True
        result_obj = json.loads(result)
        stack_trace = result_obj.get('stackTrace')
        if stack_trace is not None:
            result_obj['stackTrace'] = traceback.format_list(stack_trace)
            result = json.dumps(result_obj)

    headers = {}
    if LOG_TAIL:
        headers['Docker-Lambda-Log-Result'] = base64.b64encode(LOGS.encode())
    if not INIT_END_SENT:
        headers['Docker-Lambda-Invoke-Wait'] = int(RECEIVED_INVOKE_AT * 1000)
        headers['Docker-Lambda-Init-End'] = int(INIT_END * 1000)
        INIT_END_SENT = True

    MOCKSERVER_CONN.request("POST", "/2018-06-01/runtime/invocation/%s/%s" % \
            (invokeid, "response" if errortype is None else "error"), result, headers)
    resp = MOCKSERVER_CONN.getresponse()
    if resp.status != 202:
        raise Exception("/invocation/response return status %d" % resp.status)
    resp.read()


def report_xray_exception(xray_json):
    return


def log_bytes(msg, fileno):
    global LOGS

    if STAY_OPEN:
        if LOG_TAIL:
            LOGS += msg
        (ORIG_STDOUT if fileno == 1 else ORIG_STDERR).write(msg)
    else:
        ORIG_STDERR.write(msg)


def log_sb(msg):
    return


def get_remaining_time():
    return DEADLINE_MS - int(time.time() * 1000)


def send_console_message(msg, byte_length):
    log_bytes(msg + '\n', 1)
