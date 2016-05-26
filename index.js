var spawnSync = require('child_process').spawnSync

var ENV_VARS = [
  'AWS_REGION',
  'AWS_DEFAULT_REGION',
  'AWS_ACCOUNT_ID',
  'AWS_ACCESS_KEY_ID',
  'AWS_SECRET_ACCESS_KEY',
  'AWS_SESSION_TOKEN',
  'AWS_LAMBDA_FUNCTION_NAME',
  'AWS_LAMBDA_FUNCTION_VERSION',
  'AWS_LAMBDA_FUNCTION_MEMORY_SIZE',
  'AWS_LAMBDA_FUNCTION_TIMEOUT',
  'AWS_LAMBDA_FUNCTION_HANDLER',
  'AWS_LAMBDA_EVENT_BODY',
]
var ENV_ARGS = [].concat.apply([], ENV_VARS.map(function(x) { return ['-e', x] }))

// Will spawn `docker run` synchronously and return stdout
module.exports = function runSync(options) {
  options = options || {}
  var dockerImage = options.dockerImage || 'lambci/lambda'
  var handler = options.handler || 'index.handler'
  var event = options.event || {}
  var taskDir = options.taskDir == null ? process.cwd() : options.taskDir
  var cleanUp = options.cleanUp == null ? true : options.cleanUp
  var addEnvVars = options.addEnvVars || false
  var dockerArgs = options.dockerArgs || []
  var spawnOptions = options.spawnOptions || {encoding: 'utf8'}
  var returnSpawnResult = options.returnSpawnResult || false

  var args = ['run']
    .concat(taskDir ? ['-v', taskDir + ':/var/task'] : [])
    .concat(cleanUp ? ['--rm'] : [])
    .concat(addEnvVars ? ENV_ARGS : [])
    .concat(dockerArgs)
    .concat([dockerImage, handler, JSON.stringify(event)])

  var spawnResult = spawnSync('docker', args, spawnOptions)

  if (returnSpawnResult) {
    return spawnResult
  }

  if (spawnResult.error || spawnResult.status !== 0) {
    var err = spawnResult.error
    if (!err) {
      err = new Error(spawnResult.stdout || spawnResult.stderr)
      err.code = spawnResult.status
      err.stdout = spawnResult.stdout
      err.stderr = spawnResult.stderr
    }
    throw err
  }

  return JSON.parse(spawnResult.stdout)
}
