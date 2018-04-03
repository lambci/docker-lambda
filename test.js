var should = require('should')
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
  'lambci/lambda:nodejs4.3',
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
  '-e',
  'DOCKER_LAMBDA_USE_STDIN',
  'lambci/lambda:nodejs4.3',
  'index.handler',
  '{}',
])
captured.options.should.eql({encoding: 'utf8'})
result.should.eql({})

// Should return spawn result if asked to
resetMock({status: 0, stdout: 'null'})
result = dockerLambda({returnSpawnResult: true})
result.should.eql({status: 0, stdout: 'null'})

// Should not fail if stdout contains logging
resetMock({status: 0, stdout: 'Test\nResult\n{"success":true}'})
result = dockerLambda()
result.should.eql({success: true})

// Should not fail if stdout contains extra newlines
resetMock({status: 0, stdout: 'Test\nResult\n\n{"success":true}\n\n'})
result = dockerLambda()
result.should.eql({success: true})

// Should return undefined if last stdout entry cannot be parsed
resetMock({status: 0, stdout: 'Test\nResult\nsuccess'})
result = dockerLambda()
should.not.exist(result)

// Should return undefined when function was successful but there is no stdout
resetMock({status: 0, stdout: ''})
result = dockerLambda()
should.not.exist(result)

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
