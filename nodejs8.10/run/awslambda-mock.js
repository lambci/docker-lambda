var fs = require('fs')
var crypto = require('crypto')
var http = require('http')
var child_process = require('child_process')

var PING_RETRIES = 20

var LOGS = ''
var LOG_TAIL = false
var HAS_BUFFER_FROM = Buffer.from && Buffer.from !== Uint8Array.from

var STAY_OPEN = process.env.DOCKER_LAMBDA_STAY_OPEN

var HANDLER = process.argv[2] || process.env.AWS_LAMBDA_FUNCTION_HANDLER || process.env._HANDLER || 'index.handler'
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
var TRACE_ID = process.env._X_AMZN_TRACE_ID
var CLIENT_CONTEXT = process.env.AWS_LAMBDA_CLIENT_CONTEXT
var COGNITO_IDENTITY = process.env.AWS_LAMBDA_COGNITO_IDENTITY
var COGNITO_IDENTITY_ID = (tryParse(COGNITO_IDENTITY) || {}).identity_id
var COGNITO_IDENTITY_POOL_ID = (tryParse(COGNITO_IDENTITY) || {}).identity_pool_id
var DEADLINE_MS = Date.now() + (TIMEOUT * 1000)

process.on('SIGINT', () => process.exit(0))
process.on('SIGTERM', () => process.exit(0))

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

var mockServerProcess = child_process.spawn('/var/runtime/mockserver', {
  stdio: ['pipe', 'inherit', 'inherit'],
  env: Object.assign({
    DOCKER_LAMBDA_NO_BOOTSTRAP: 1,
    DOCKER_LAMBDA_USE_STDIN: 1,
  }, process.env)
})
mockServerProcess.on('error', console.error)
mockServerProcess.stdin.end(EVENT_BODY)
mockServerProcess.unref()

var OPTIONS = {
  invokeId: uuid(),
  handler: HANDLER,
  suppressInit: true,
  credentials: {
    key: ACCESS_KEY_ID,
    secret: SECRET_ACCESS_KEY,
    session: SESSION_TOKEN,
  },
  eventBody: EVENT_BODY,
  contextObjects: {
    clientContext: CLIENT_CONTEXT,
    cognitoIdentityId: COGNITO_IDENTITY_ID,
    cognitoPoolId: COGNITO_IDENTITY_POOL_ID,
  },
  invokedFunctionArn: INVOKED_ARN,
  'x-amzn-trace-id': TRACE_ID
}

// Some weird spelling error in the source?
OPTIONS.invokeid = OPTIONS.invokeId

var invoked = false
var errored = false
var initEndSent = false
var receivedInvokeAt
var initEnd
var pingPromise
var reportDonePromise

module.exports = {
  initRuntime: function () {
    pingPromise = new Promise(resolve => ping(Date.now() + 1000, resolve))
    reportDonePromise = new Promise(resolve => resolve())
    return OPTIONS
  },
  waitForInvoke: function (cb) {
    Promise.all([pingPromise, reportDonePromise]).then(() => {
      if (!invoked) {
        receivedInvokeAt = Date.now()
        invoked = true
      } else {
        LOGS = ''
      }
      http.get({
        hostname: '127.0.0.1',
        port: 9001,
        path: '/2018-06-01/runtime/invocation/next',
      }, res => {
        if (res.statusCode !== 200) {
          console.error(`Mock server invocation/next returned a ${res.statusCode} response`)
          return process.exit(1)
        }
        OPTIONS.invokeId = OPTIONS.initInvokeId = OPTIONS.invokeid = res.headers['lambda-runtime-aws-request-id']
        OPTIONS.invokedFunctionArn = res.headers['lambda-runtime-invoked-function-arn']
        OPTIONS['x-amzn-trace-id'] = res.headers['lambda-runtime-trace-id']
        DEADLINE_MS = +res.headers['lambda-runtime-deadline-ms']

        OPTIONS.contextObjects.clientContext = res.headers['lambda-runtime-client-context']
        var cognitoIdentity = tryParse(res.headers['lambda-runtime-cognito-identity']) || {}
        OPTIONS.contextObjects.cognitoIdentityId = cognitoIdentity.identity_id
        OPTIONS.contextObjects.cognitoPoolId = cognitoIdentity.identity_pool_id

        LOG_TAIL = res.headers['docker-lambda-log-type'] === 'Tail'

        OPTIONS.eventBody = ''
        res.setEncoding('utf8')
          .on('data', data => OPTIONS.eventBody += data)
          .on('end', () => cb(OPTIONS))
          .on('error', function (err) {
            console.error(err)
            process.exit(1)
          })
      }).on('error', err => {
        if (err.code === 'ECONNRESET') {
          return process.exit(errored ? 1 : 0)
        }
        console.error(err)
        process.exit(1)
      })
    })
  },
  reportRunning: function (invokeId) { }, // eslint-disable-line no-unused-vars
  reportDone: function (invokeId, errType, resultStr) {
    if (!invoked) return
    if (errType) errored = true
    reportDonePromise = new Promise(resolve => {
      var headers = {}
      if (LOG_TAIL) {
        headers['Docker-Lambda-Log-Result'] = newBuffer(LOGS).slice(-4096).toString('base64')
      }
      if (!initEndSent) {
        headers['Docker-Lambda-Invoke-Wait'] = receivedInvokeAt
        headers['Docker-Lambda-Init-End'] = initEnd
        initEndSent = true
      }
      http.request({
        method: 'POST',
        hostname: '127.0.0.1',
        port: 9001,
        path: '/2018-06-01/runtime/invocation/' + invokeId + (errType == null ? '/response' : '/error'),
        headers,
      }, res => {
        if (res.statusCode !== 202) {
          console.error(err || 'Got status code: ' + res.statusCode)
          process.exit(1)
        }
        resolve()
      }).on('error', err => {
        console.error(err)
        process.exit(1)
      }).end(resultStr)
    })
  },
  reportFault: function (invokeId, msg, errName, errStack) {
    errored = true
    systemErr(msg + (errName ? ': ' + errName : ''))
    if (errStack) systemErr(errStack)
  },
  reportUserInitStart: function () { },
  reportUserInitEnd: function () { initEnd = Date.now() },
  reportUserInvokeStart: function () { },
  reportUserInvokeEnd: function () { },
  reportException: function () { },
  getRemainingTime: function () { return DEADLINE_MS - Date.now() },
  sendConsoleLogs: consoleLog,
  maxLoggerErrorSize: 256 * 1024,
}

function ping(timeout, cb) {
  http.get({ hostname: '127.0.0.1', port: 9001, path: '/2018-06-01/runtime/ping' }, cb).on('error', () => {
    if (Date.now() > timeout) {
      console.error('Mock server did not respond to pings in time')
      process.exit(1)
    }
    setTimeout(ping, 5, timeout, cb)
  })
}

function tryParse(cognitoIdentity) {
  try {
    return JSON.parse(cognitoIdentity)
  } catch (e) {
    return null
  }
}

function consoleLog(str) {
  if (STAY_OPEN) {
    if (LOG_TAIL) {
      LOGS += str
    }
    process.stdout.write(str)
  } else {
    process.stderr.write(formatConsole(str))
  }
}

function systemErr(str) {
  process.stderr.write(formatErr(str) + '\n')
}

function formatConsole(str) {
  return str.replace(/^[0-9TZ:.-]+\t[0-9a-f-]+\t/, '\u001b[34m$&\u001b[0m')
}

function formatErr(str) {
  return '\u001b[31m' + str + '\u001b[0m'
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

function newBuffer(str) {
  return HAS_BUFFER_FROM ? Buffer.from(str) : new Buffer(str)
}
