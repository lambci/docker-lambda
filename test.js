require('should')
require('child_process').spawnSync = mockSpawnSync

var dockerLambda = require('.')

var captured = {}
var mockReturn
function mockSpawnSync(cmd, args, options) {
  captured.cmd = cmd
  captured.args = args
  captured.options = options
  return mockReturn
}
function resetMock(returnVal) {
  mockReturn = returnVal || {status: 0, stdout: '{}'}
}

// Should return defaults if calling with no options
resetMock()
var result = dockerLambda()
captured.cmd.should.equal('docker')
captured.args.should.eql([
  'run',
  '-v',
  __dirname + ':/var/task',
  '--rm',
  'lambci/lambda',
  'index.handler',
  '{}',
])
captured.options.should.eql({encoding: 'utf8'})
result.should.eql({})

// Should use env vars if asked to
resetMock()
result = dockerLambda({addEnvVars: true})
captured.cmd.should.equal('docker')
captured.args.should.eql([
  'run',
  '-v',
  __dirname + ':/var/task',
  '--rm',
  '-e',
  'AWS_REGION',
  '-e',
  'AWS_DEFAULT_REGION',
  '-e',
  'AWS_ACCOUNT_ID',
  '-e',
  'AWS_ACCESS_KEY_ID',
  '-e',
  'AWS_SECRET_ACCESS_KEY',
  '-e',
  'AWS_SESSION_TOKEN',
  '-e',
  'AWS_LAMBDA_FUNCTION_NAME',
  '-e',
  'AWS_LAMBDA_FUNCTION_VERSION',
  '-e',
  'AWS_LAMBDA_FUNCTION_MEMORY_SIZE',
  '-e',
  'AWS_LAMBDA_FUNCTION_TIMEOUT',
  '-e',
  'AWS_LAMBDA_FUNCTION_HANDLER',
  '-e',
  'AWS_LAMBDA_EVENT_BODY',
  'lambci/lambda',
  'index.handler',
  '{}',
])
captured.options.should.eql({encoding: 'utf8'})
result.should.eql({})

// Should return spawn result if asked to
resetMock({status: 0, stdout: 'null'})
result = dockerLambda({returnSpawnResult: true})
result.should.eql({status: 0, stdout: 'null'})

// Should throw error if spawn returns error
resetMock({error: new Error('Something went wrong')})
var err
try {
  result = dockerLambda()
} catch (e) {
  err = e
}
err.should.eql(new Error('Something went wrong'))

// Should throw error if spawn process dies
resetMock({status: 1, stdout: 'wtf', stderr: 'ftw'})
try {
  result = dockerLambda()
} catch (e) {
  err = e
}
var expectedErr = new Error('wtf')
expectedErr.code = 1
expectedErr.stdout = 'wtf'
expectedErr.stderr = 'ftw'
err.should.eql(expectedErr)

console.log('All Passed!')
