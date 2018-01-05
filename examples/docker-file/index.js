var execSync = require('child_process').execSync

// Intended to show that a built image will have the correct permissions in /var/task
// docker build -t build-test . && docker run --rm build-test

exports.handler = function(event, context, cb) {

  console.log(process.execPath)
  console.log(process.execArgv)
  console.log(process.argv)
  console.log(process.cwd())
  console.log(process.mainModule.filename)
  console.log(__filename)
  console.log(process.env)
  console.log(process.getuid())
  console.log(process.getgid())
  console.log(process.geteuid())
  console.log(process.getegid())
  console.log(process.getgroups())
  console.log(process.umask())

  console.log(event)

  console.log(context)

  context.callbackWaitsForEmptyEventLoop = false

  console.log(context.getRemainingTimeInMillis())

  console.log(execSync('ls -l /var/task', {encoding: 'utf8'}))

  cb()
}


