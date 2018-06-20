var fs = require('fs')
var crypto = require('crypto')

var HANDLER = process.argv[2] || process.env.AWS_LAMBDA_FUNCTION_HANDLER || process.env._HANDLER ||  'index.handler'
var EVENT_BODY = process.argv[3] || process.env.AWS_LAMBDA_EVENT_BODY ||
  (process.env.DOCKER_LAMBDA_USE_STDIN && fs.readFileSync('/dev/stdin', 'utf8')) || '{}'

var FN_NAME = process.env.AWS_LAMBDA_FUNCTION_NAME || 'test'
var VERSION = process.env.AWS_LAMBDA_FUNCTION_VERSION || '$LATEST'
var MEM_SIZE = process.env.AWS_LAMBDA_FUNCTION_MEMORY_SIZE || '1536'
var TIMEOUT = process.env.AWS_LAMBDA_FUNCTION_TIMEOUT || '300'
var REGION = process.env.AWS_REGION || process.env.AWS_DEFAULT_REGION || 'us-east-1'
var ACCOUNT_ID = process.env.AWS_ACCOUNT_ID || randomAccountId()
var ACCESS_KEY_ID = process.env.AWS_ACCESS_KEY_ID || 'SOME_ACCESS_KEY_ID'
var SECRET_ACCESS_KEY = process.env.AWS_SECRET_ACCESS_KEY || 'SOME_SECRET_ACCESS_KEY'
var SESSION_TOKEN = process.env.AWS_SESSION_TOKEN
var INVOKED_ARN = process.env.AWS_LAMBDA_FUNCTION_INVOKED_ARN || arn(REGION, ACCOUNT_ID, FN_NAME)

function consoleLog(str) {
  process.stderr.write(formatConsole(str))
}

function systemLog(str) {
  process.stderr.write(formatSystem(str) + '\n')
}

function systemErr(str) {
  process.stderr.write(formatErr(str) + '\n')
}

function handleResult(resultStr, cb) {
  if (!process.stdout.write('\n' + resultStr + '\n')) {
    process.stdout.once('drain', cb)
  } else {
    process.nextTick(cb)
  }
}

// Don't think this can be done in the Docker image
process.umask(2)

process.env.AWS_LAMBDA_FUNCTION_NAME = FN_NAME
process.env.AWS_LAMBDA_FUNCTION_VERSION = VERSION
process.env.AWS_LAMBDA_FUNCTION_MEMORY_SIZE = MEM_SIZE
process.env.AWS_LAMBDA_LOG_GROUP_NAME = '/aws/lambda/' + FN_NAME
process.env.AWS_LAMBDA_LOG_STREAM_NAME = new Date().toISOString().slice(0, 10).replace(/-/g, '/') +
  '/[' + VERSION + ']' + crypto.randomBytes(16).toString('hex')
process.env.AWS_REGION = REGION
process.env.AWS_DEFAULT_REGION = REGION
process.env._HANDLER = HANDLER

var OPTIONS = {
  invokeid: uuid(),
  handler: HANDLER,
  suppress_init: true,
  mode: 'event',
  sockfd: -1,
  credentials: {
    key: ACCESS_KEY_ID,
    secret: SECRET_ACCESS_KEY,
    session: SESSION_TOKEN,
  },
  eventbody: EVENT_BODY,
  contextobjects: {
    clientcontext: undefined, // JSON string
    cognitoidentityid: undefined,
    cognitopoolid: undefined,
  },
  invokedfunctionarn: INVOKED_ARN,
}

var invoked = false
var errored = false
var start = null

module.exports = {
  init_runtime: function() { return OPTIONS },
  wait_for_invoke_nb: function(fn) {
    if (invoked) return
    systemLog('START RequestId: ' + OPTIONS.invokeid + ' Version: ' + VERSION)
    start = process.hrtime()
    invoked = true
    fn(OPTIONS)
  },
  report_running: function(invokeId) {}, // eslint-disable-line no-unused-vars
  report_done: function(invokeId, errType, resultStr) {
    if (!invoked) return
    var diffMs = hrTimeMs(process.hrtime(start))
    var billedMs = Math.min(100 * (Math.floor(diffMs / 100) + 1), TIMEOUT * 1000)
    systemLog('END RequestId: ' + invokeId)
    systemLog([
      'REPORT RequestId: ' + invokeId,
      'Duration: ' + diffMs.toFixed(2) + ' ms',
      'Billed Duration: ' + billedMs + ' ms',
      'Memory Size: ' + MEM_SIZE + ' MB',
      'Max Memory Used: ' + Math.round(process.memoryUsage().rss / (1024 * 1024)) + ' MB',
      '',
    ].join('\t'))
    if (typeof resultStr == 'string') {
      handleResult(resultStr)
    }
    process.exit(errored || errType ? 1 : 0)
  },
  report_fault: function(invokeId, msg, errName, errStack) {
    errored = true
    systemErr(msg + (errName ? ': ' + errName : ''))
    if (errStack) systemErr(errStack)
  },
  get_remaining_time: function() {
    return (TIMEOUT * 1000) - Math.floor(hrTimeMs(process.hrtime(start)))
  },
  send_console_logs: consoleLog,
  max_logger_error_size: 256 * 1024,
}

function formatConsole(str) {
  return str.replace(/^[0-9TZ:\.\-]+\t[0-9a-f\-]+\t/, '\033[34m$&\u001b[0m')
}

function formatSystem(str) {
  return '\033[32m' + str + '\033[0m'
}

function formatErr(str) {
  return '\033[31m' + str + '\033[0m'
}

function hrTimeMs(hrtime) {
  return (hrtime[0] * 1e9 + hrtime[1]) / 1e6
}

// Approximates the look of a v1 UUID
function uuid() {
  return crypto.randomBytes(4).toString('hex') + '-' +
    crypto.randomBytes(2).toString('hex') + '-' +
    crypto.randomBytes(2).toString('hex').replace(/^./, '1') + '-' +
    crypto.randomBytes(2).toString('hex') + '-' +
    crypto.randomBytes(6).toString('hex')
}

function randomAccountId() {
  return String(0x100000000 * Math.random())
}

function arn(region, accountId, fnName) {
  return 'arn:aws:lambda:' + region + ':' + accountId.replace(/[^\d]/g, '') + ':function:' + fnName
}

