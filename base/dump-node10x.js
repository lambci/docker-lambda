const fs = require('fs')
const { execSync } = require('child_process')
const AWS = require('aws-sdk')
const s3 = new AWS.S3()

// Depends on tar-find-layer for the tar/find/xargs binaries
exports.handler = async(event, context) => {
  const execOpts = { stdio: 'inherit', maxBuffer: 16 * 1024 * 1024 }

  let filename = 'base-2.tgz'
  let cmd = 'tar -cpzf /tmp/' + filename +
    ' -C / --exclude=/proc --exclude=/sys --exclude=/dev --exclude=/tmp ' +
    '--exclude=/var/task/* --exclude=/var/runtime/* --exclude=/var/lang/* --exclude=/var/rapid/* --exclude=/opt/* ' +
    '--numeric-owner --ignore-failed-read /'

  execSync(event.cmd || cmd, execOpts)
  if (event.cmd) return

  console.log('Zipping done! Uploading...')

  let data = await s3.upload({
    Bucket: 'lambci',
    Key: 'fs/' + filename,
    Body: fs.createReadStream('/tmp/' + filename),
    ACL: 'public-read',
  }).promise()

  filename = 'nodejs10.x.tgz'
  cmd = 'tar -cpzf /tmp/' + filename +
    ' --numeric-owner --ignore-failed-read /var/runtime /var/lang /var/rapid'

  execSync(cmd, execOpts)

  console.log('Zipping done! Uploading...')

  data = await s3.upload({
    Bucket: 'lambci',
    Key: 'fs/' + filename,
    Body: fs.createReadStream('/tmp/' + filename),
    ACL: 'public-read',
  }).promise()

  console.log('Uploading done!')

  console.log(process.execPath)
  console.log(process.execArgv)
  console.log(process.argv)
  console.log(process.cwd())
  console.log(__filename)
  console.log(process.env)
  execSync('echo /proc/1/environ; xargs -n 1 -0 < /proc/1/environ', execOpts)
  execSync("bash -O extglob -c 'for cmd in /proc/+([0-9])/cmdline; do echo $cmd; xargs -n 1 -0 < $cmd; done'", execOpts)
  console.log(context)

  return data
}
