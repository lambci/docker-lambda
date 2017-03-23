var fs = require('fs')
var childProcess = require('child_process')
var AWS = require('aws-sdk')
var s3 = new AWS.S3()

exports.handler = function(event, context, cb) {
  var filename = 'nodejs4.3.tgz'
  var cmd = 'tar -cpzf /tmp/' + filename + ' -C / ' +
    '--exclude=/proc --exclude=/sys --exclude=/tmp --exclude=/var/task/* ' +
    '--numeric-owner --ignore-failed-read /'

  var child = childProcess.spawn('sh', ['-c', event.cmd || cmd])
  child.stdout.setEncoding('utf8')
  child.stderr.setEncoding('utf8')
  child.stdout.on('data', console.log.bind(console))
  child.stderr.on('data', console.error.bind(console))
  child.on('error', cb)

  child.on('close', function() {
    if (event.cmd) return cb()

    console.log('Zipping done! Uploading...')

    s3.upload({
      Bucket: 'lambci',
      Key: 'fs/' + filename,
      Body: fs.createReadStream('/tmp/' + filename),
      ACL: 'public-read',
    }, function(err, data) {
      if (err) return cb(err)

      console.log('Uploading done!')

      console.log(process.execPath)
      console.log(process.execArgv)
      console.log(process.argv)
      console.log(process.cwd())
      console.log(__filename)
      console.log(process.env)
      console.log(childProcess.execSync('ls -la /dev', {encoding: 'utf8'}))

      cb(null, data)
    })
  })
}

// /usr/local/lib64/node-v4.3.x/bin/node
// [ '--max-old-space-size=1229', '--max-semi-space-size=76', '--max-executable-size=153', '--expose-gc' ]
// [ '/usr/local/lib64/node-v4.3.x/bin/node', '/var/runtime/node_modules/awslambda/index.js' ]
// /var/task
// /var/task/index.js
// {
// PATH: '/usr/local/lib64/node-v4.3.x/bin:/usr/local/bin:/usr/bin/:/bin',
// LANG: 'en_US.UTF-8',
// LD_LIBRARY_PATH: '/usr/local/lib64/node-v4.3.x/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib',
// LAMBDA_TASK_ROOT: '/var/task',
// LAMBDA_RUNTIME_DIR: '/var/runtime',
// AWS_REGION: 'us-east-1',
// AWS_DEFAULT_REGION: 'us-east-1',
// AWS_LAMBDA_LOG_GROUP_NAME: '/aws/lambda/dump-node43',
// AWS_LAMBDA_LOG_STREAM_NAME: '2017/03/23/[$LATEST]c079a84d433534434534ef0ddc99d00f',
// AWS_LAMBDA_FUNCTION_NAME: 'dump-node43',
// AWS_LAMBDA_FUNCTION_MEMORY_SIZE: '1536',
// AWS_LAMBDA_FUNCTION_VERSION: '$LATEST',
// AWS_EXECUTION_ENV: 'AWS_Lambda_nodejs4.3',
// NODE_PATH: '/var/runtime:/var/task:/var/runtime/node_modules',
// AWS_ACCESS_KEY_ID: 'ASIA...C37A',
// AWS_SECRET_ACCESS_KEY: 'JZvD...BDZ4L',
// AWS_SESSION_TOKEN: 'FQoDYXdzEMb//////////...0oog7bzuQU='
// }
// {
// callbackWaitsForEmptyEventLoop: [Getter/Setter],
// done: [Function],
// succeed: [Function],
// fail: [Function],
// logGroupName: '/aws/lambda/dump-node43',
// logStreamName: '2017/03/23/[$LATEST]c079a84d433534434534ef0ddc99d00f',
// functionName: 'dump-node43',
// memoryLimitInMB: '1536',
// functionVersion: '$LATEST',
// getRemainingTimeInMillis: [Function],
// invokeid: '1fcdc383-a9e8-4228-bc1c-8db17629e183',
// awsRequestId: '1fcdc383-a9e8-4228-bc1c-8db17629e183',
// invokedFunctionArn: 'arn:aws:lambda:us-east-1:879423879432:function:dump-node43'
// }
