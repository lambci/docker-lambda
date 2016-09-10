var fs = require('fs')
var childProcess = require('child_process')
var AWS = require('aws-sdk')
var s3 = new AWS.S3()

exports.handler = function(event, context) {
  var filename = 'nodejs.tgz'
  var cmd = 'tar -cpzf /tmp/' + filename + ' --numeric-owner --ignore-failed-read /var/runtime'

  var child = childProcess.spawn('sh', ['-c', event.cmd || cmd])
  child.stdout.setEncoding('utf8')
  child.stderr.setEncoding('utf8')
  child.stdout.on('data', console.log.bind(console))
  child.stderr.on('data', console.error.bind(console))
  child.on('error', context.done.bind(context))

  child.on('close', function() {
    if (event.cmd) return context.done()

    console.log('Zipping done! Uploading...')

    s3.upload({
      Bucket: 'lambci',
      Key: 'fs/' + filename,
      Body: fs.createReadStream('/tmp/' + filename),
      ACL: 'public-read',
    }, function(err, data) {
      if (err) return context.done(err)

      console.log('Uploading done!')

      console.log(process.execPath)
      console.log(process.execArgv)
      console.log(process.argv)
      console.log(process.cwd())
      console.log(__filename)
      console.log(process.env)
      childProcess.exec('ls -la /dev', {encoding: 'utf8'}, function(err, stdout, stderr) {
        if (stdout) console.log(stdout)
        if (stderr) console.error(stderr)
        context.done(err, data)
      })

    })
  })
}

// /usr/bin/node
// [ '--max-old-space-size=1229', '--max-new-space-size=153', '--max-executable-size=153' ]
// [ 'node', '/var/runtime/node_modules/.bin/awslambda' ]
// /var/task
// /var/task/index.js
// {
// PATH: '/usr/local/bin:/usr/bin/:/bin',
// LAMBDA_TASK_ROOT: '/var/task',
// LAMBDA_RUNTIME_DIR: '/var/runtime',
// AWS_REGION: 'us-east-1',
// AWS_DEFAULT_REGION: 'us-east-1',
// AWS_LAMBDA_LOG_GROUP_NAME: '/aws/lambda/dump-nodejs',
// AWS_LAMBDA_LOG_STREAM_NAME: '2016/05/18/[$LATEST]85da517...0ec8b49e',
// AWS_LAMBDA_FUNCTION_NAME: 'dump-nodejs', AWS_LAMBDA_FUNCTION_MEMORY_SIZE: '1536',
// AWS_LAMBDA_FUNCTION_VERSION: '$LATEST',
// LD_LIBRARY_PATH: '/lib64:/usr/lib64:/var/runtime:/var/task:/var/task/lib',
// NODE_PATH: '/var/runtime:/var/task:/var/runtime/node_modules',
// AWS_ACCESS_KEY_ID: 'ASIA...C37A',
// AWS_SECRET_ACCESS_KEY: 'JZvD...BDZ4L',
// AWS_SESSION_TOKEN: 'FQoDYXdzEMb//////////...0oog7bzuQU='
// }
