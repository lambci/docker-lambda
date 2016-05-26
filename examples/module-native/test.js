var dockerLambda = require('..')

var match = dockerLambda({event: {password: 'lambda-docker'}})

console.log(match == 'Matches!' ? 'Match Passed' : 'Match Failed: ' + match)


var nonMatch = dockerLambda({event: {password: 'lambda-mocker'}})

console.log(nonMatch == 'NopeNopeNope' ? 'Non-Match Passed' : 'Non-Match Failed: ' + nonMatch)
