var bcrypt = require('bcrypt')

// Hashed password for "lamda-docker"
var HASHED_PASS = '$2a$10$w9.BRCsnWXv5f.eUGD2fieT.wfLV9.rSJFC/2bzz3sahJdCLaYs0K'

exports.handler = function(event, context, cb) {
  console.log('hello?')
  bcrypt.compare(event.password, HASHED_PASS, function(err, res) {
    cb(err, res ? 'Matches!' : 'NopeNopeNope')
  })
}

// Just to test this locally:
if (require.main === module) {
  exports.handler({password: 'lambda-docker'}, {}, console.log)
  exports.handler({password: 'lambda-mocker'}, {}, console.log)
}
