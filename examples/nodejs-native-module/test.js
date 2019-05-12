var dockerLambda = require('../..')

var match = dockerLambda({ event: { password: 'lambda-docker' }, dockerImage: 'lambci/lambda:nodejs6.10' })

console.log(match === 'Matches!' ? 'Match Passed' : 'Match Failed: ' + match)

var nonMatch = dockerLambda({ event: { password: 'lambda-mocker' }, dockerImage: 'lambci/lambda:nodejs6.10' })

console.log(nonMatch === 'NopeNopeNope' ? 'Non-Match Passed' : 'Non-Match Failed: ' + nonMatch)
