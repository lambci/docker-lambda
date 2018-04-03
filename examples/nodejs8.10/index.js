// Just a test lambda, run with:
// docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10

exports.handler = async(event, context) => {
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
}
