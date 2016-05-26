// Just a test lambda, run with:
// docker run -v "$PWD":/var/task lambci/lambda

exports.handler = function(event, context, cb) {

  console.log(process.execPath)
  console.log(process.execArgv)
  console.log(process.argv)
  console.log(process.cwd())
  console.log(__filename)
  console.log(process.env)

  console.log(context)

  console.log(context.getRemainingTimeInMillis())

  cb()
}

