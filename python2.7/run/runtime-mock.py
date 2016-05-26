from __future__ import print_function
import sys

orig_stdout = sys.stdout
orig_stderr = sys.stderr

# TODO: finish this

def recv_start(ctrl_sock):
    sys.stdout = orig_stdout
    sys.stderr = orig_stderr
    print("recv_start")
    return (invokeid, mode, handler, suppress_init, credentials)

def report_running(invokeid):
    print("report_running")
    return

def receive_invoke(ctrl_sock):
    print("receive_invoke")
    return (invokeid, data_sock, credentials, event_body, context_objs, invoked_function_arn)

def report_fault(invokeid, msg, except_value, trace):
    print("report_fault")
    return

def report_done(invokeid, errortype, result):
    print("report_done")
    return

def log_bytes(msg, fileno):
    print(msg)
    return

def get_remaining_time():
    print("get_remaining_time")
    return

def send_console_message(msg):
    print(msg)
    return
