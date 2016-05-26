docker-lambda
-------------

A sandboxed local environment that replicates the live [AWS Lambda](https://aws.amazon.com/lambda/)
environment almost identically – including installed software and libraries,
file structure and permissions, environment variables, context objects and
behaviors – even the user and running process are the same.

You can use it for testing your functions in the same strict Lambda environment,
knowing that they'll exhibit the same behavior when deployed live. You can
also use it to compile native dependencies knowing that you're linking to the
same library versions that exist on AWS Lambda and then deploy using
the [AWS CLI](https://aws.amazon.com/cli/).

This project consists of a set of Docker images for each of the supported Lambda runtimes
(Node.js 0.10 and 4.3, Python 2.7\* and Java 8\*) – as well as build
images that include packages like gcc-c++, git, zip and the aws-cli for
compiling and deploying.

There's also an npm module to make it convenient to invoke from Node.js

\* NB: Python 2.7 and Java 8 test runners are not yet complete, but both
languages are installed in the images so can be manually tested

Prerequisites
-------------

You'll need [Docker](https://www.docker.com) installed

Example
-------

You can perform actions with the current directory using the `-v` arg with
`docker run` – logging goes to stderr and the callback result goes to stdout:

```console
# Test an index.handler function from the current directory on Node.js v4.3
docker run -v "$PWD":/var/task lambci/lambda

# If using a function other than index.handler, with a custom event
docker run -v "$PWD":/var/task lambci/lambda index.myHandler '{"some": "event"}'

# Use the original Node.js v0.10 runtime
docker run -v "$PWD":/var/task lambci/lambda:nodejs

# To compile native deps in node_modules (runs `npm rebuild`)
docker run -v "$PWD":/var/task lambci/lambda:build

# Run custom commands on the build container
docker run lambci/lambda:build java -version

# To run an interactive session on the build container
docker run -it lambci/lambda:build bash
```

Using the Node.js module (`npm install docker-lambda`) – for example in tests:

```js
var dockerLambda = require('docker-lambda')

// Spawns synchronously, uses current dir – will throw if it fails
var lambdaCallbackResult = dockerLambda({event: {some: 'event'}})

// Manually specify directory and custom args
lambdaCallbackResult = dockerLambda({taskDir: __dirname, dockerArgs: ['-m', '1.5G']})
```

Create your own Docker image for finer control:

```dockerfile
FROM lambci/lambda:build

ENV AWS_DEFAULT_REGION us-east-1

ADD . .

RUN npm install

CMD cat .lambdaignore | xargs zip -9qyr lambda.zip . -x && \
  aws lambda update-function-code --function-name mylambda --zip-file fileb://lambda.zip

# docker build -t mylambda .
# docker run -e AWS_ACCESS_KEY_ID -e AWS_SECRET_ACCESS_KEY mylambda
```


Questions
---------

* *When should I use this?*

  When you want fast local reproducibility. When you don't want to spin up an
  Amazon Linux EC2 instance (indeed, network aside, this is closer to the real
  Lambda environment because there are a number of different files, permissions
  and libraries on a default Amazon Linux instance). When you don't want to
  invoke a live Lambda just to test your Lambda package – you can do it locally
  from your dev machine or run tests on your CI system (assuming it has Docker
  support!)


* *Wut, how?*

  By tarring the full filesystem in Lambda, uploading that to S3, and then
  piping into Docker to create a new image from scratch – then creating
  mock modules that will be required/included in place of the actual native
  modules that communicate with the real Lambda coordinating services. Only the
  native modules are mocked out – the actual parent JS/PY runner files are left
  alone, so their behaviors don't need to be replicated (like the
  overriding of `console.log`, and custom defined properties like
  `callbackWaitsForEmptyEventLoop`)

* *What's missing from the images?*

  Hard to tell – anything that's not readable – so at least `/root/*` –
  but probably a little more than that – hopefully nothing important, after all,
  it's not readable by Lambda, so how could it be!

* *Is it really necessary to replicate exactly to this degree?*

  Not for many scenarios – some compiled Linux binaries work out of the box
  and a CentOS Docker image can compile some binaries that work on Lambda too,
  for example – but for testing it's great to be able to reliably verify
  permissions issues, library linking issues, etc.

* *What's this got to do with LambCI?*

  Technically nothing – it's just been incredibly useful during the building
  and testing of LambCI.

Documentation
------------

TODO

lambci/lambda
  - uses ENTRYPOINT, override with `--entrypoint`
lambci/lambda:build
  - uses CMD

  'AWS_LAMBDA_FUNCTION_NAME',
  'AWS_LAMBDA_FUNCTION_VERSION',
  'AWS_LAMBDA_FUNCTION_MEMORY_SIZE',
  'AWS_LAMBDA_FUNCTION_TIMEOUT',
  'AWS_LAMBDA_FUNCTION_HANDLER',
  'AWS_LAMBDA_EVENT_BODY',

  'AWS_REGION',
  'AWS_DEFAULT_REGION',
  'AWS_ACCOUNT_ID',
  'AWS_ACCESS_KEY_ID',
  'AWS_SECRET_ACCESS_KEY',
  'AWS_SESSION_TOKEN',

