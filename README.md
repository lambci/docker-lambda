docker-lambda
-------------

A sandboxed local environment that replicates the live [AWS Lambda](https://aws.amazon.com/lambda/)
environment almost identically – including installed software and libraries,
file structure and permissions, environment variables, context objects and
behaviors – even the user and running process are the same.

![Terminal Example](https://raw.githubusercontent.com/lambci/docker-lambda/master/examples/terminal.png "Example usage when index.js in current dir")

You can use it for testing your functions in the same strict Lambda environment,
knowing that they'll exhibit the same behavior when deployed live. You can
also use it to compile native dependencies knowing that you're linking to the
same library versions that exist on AWS Lambda and then deploy using
the [AWS CLI](https://aws.amazon.com/cli/).

This project consists of a set of Docker images for each of the supported Lambda runtimes
(Node.js 0.10, 4.3, 6.10 and 8.10, Python 2.7 and 3.6, Java 8, .NET Core 2.0, and Go 1.x).

There are also a set of build images that include packages like gcc-c++, git,
zip and the aws-cli for compiling and deploying.

There's also an npm module to make it convenient to invoke from Node.js

Prerequisites
-------------

You'll need [Docker](https://www.docker.com) installed

Example
-------

You can run your Lambdas from local directories using the `-v` arg with
`docker run` – logging goes to stderr and the callback result goes to stdout:

```sh
# Test an index.handler function from the current directory on Node.js v8.10
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10

# If using a function other than index.handler, with a custom event
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10 index.myHandler '{"some": "event"}'

# Use the Node.js v6.10 runtime
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs6.10

# Test a default function (lambda_function.lambda_handler) from the current directory on Python 2.7
docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7

# Test on Python 3.6 with a custom file named my_module.py containing a my_handler function
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6 my_module.my_handler

# Test on Go 1.x with a compiled handler named my_handler and a custom event
docker run --rm -v "$PWD":/var/task lambci/lambda:go1.x my_handler '{"some": "event"}'

# Test a function from the current directory on Java 8
# The directory must be laid out in the same way the Lambda zip file is,
# with top-level package source directories and a `lib` directory for third-party jars
# http://docs.aws.amazon.com/lambda/latest/dg/create-deployment-pkg-zip-java.html
# The default handler is "index.Handler", but you'll likely have your own package and class
docker run --rm -v "$PWD":/var/task lambci/lambda:java8 org.myorg.MyHandler

# Test on .NET Core 2.0 given a test.dll assembly in the current directory,
# a class named Function with a FunctionHandler method, and a custom event
docker run --rm -v "$PWD":/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler '{"some": "event"}'

# Run custom commands
docker run --rm --entrypoint node lambci/lambda:nodejs8.10 -v

# For large events you can pipe them into stdin if you set DOCKER_LAMBDA_USE_STDIN (on any runtime)
echo '{"some": "event"}' | docker run --rm -v "$PWD":/var/task -i -e DOCKER_LAMBDA_USE_STDIN=1 lambci/lambda:nodejs8.10
```

You can see more examples of how to build docker images and run different
runtimes in the [examples](./examples) directory.

To use the build images, for compilation, deployment, etc:

```sh
# To compile native deps in node_modules (runs `npm rebuild`)
docker run --rm -v "$PWD":/var/task lambci/lambda:build-nodejs8.10

# To resolve dependencies on go1.x (working directory is /go/src/handler, will run `dep ensure`)
docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x

# For .NET Core 2.0, this will publish the compiled code to `./pub`,
# which you can then use to run with `-v "$PWD"/pub:/var/task`
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.0 dotnet publish -c Release -o pub

# Run custom commands on a build container
docker run --rm lambci/lambda:build-python2.7 aws --version

# To run an interactive session on a build container
docker run -it lambci/lambda:build-python3.6 bash
```

Using the Node.js module (`npm install docker-lambda`) – for example in tests:

```js
var dockerLambda = require('docker-lambda')

// Spawns synchronously, uses current dir – will throw if it fails
var lambdaCallbackResult = dockerLambda({event: {some: 'event'}})

// Manually specify directory and custom args
lambdaCallbackResult = dockerLambda({taskDir: __dirname, dockerArgs: ['-m', '1.5G']})

// Use a different image from the default Node.js v4.3
lambdaCallbackResult = dockerLambda({dockerImage: 'lambci/lambda:nodejs6.10'})
```

Create your own Docker image for finer control:

```dockerfile
FROM lambci/lambda:build-nodejs8.10

ENV AWS_DEFAULT_REGION us-east-1

COPY . .

RUN npm install

CMD cat .lambdaignore | xargs zip -9qyr lambda.zip . -x && \
  aws lambda update-function-code --function-name mylambda --zip-file fileb://lambda.zip

# docker build -t mylambda .
# docker run --rm -e AWS_ACCESS_KEY_ID -e AWS_SECRET_ACCESS_KEY mylambda
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

  By [tarring the full filesystem in Lambda, uploading that to S3](./base/dump-nodejs43.js),
  and then [piping into Docker to create a new image from scratch](./base/create-base.sh) –
  then [creating mock modules](./nodejs4.3/run/awslambda-mock.js) that will be
  required/included in place of the actual native modules that communicate with
  the real Lambda coordinating services.  Only the native modules are mocked
  out – the actual parent JS/PY/Java runner files are left alone, so their behaviors
  don't need to be replicated (like the overriding of `console.log`, and custom
  defined properties like `callbackWaitsForEmptyEventLoop`)

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

Docker tags (follow the Lambda runtime names):
  - `nodejs4.3`
  - `nodejs6.10`
  - `nodejs8.10`
  - `python2.7`
  - `python3.6`
  - `java8`
  - `go1.x`
  - `dotnetcore2.0`
  - `build-nodejs4.3`
  - `build-nodejs6.10`
  - `build-nodejs8.10`
  - `build-python2.7`
  - `build-python3.6`
  - `build-java8`
  - `build-go1.x`
  - `build-dotnetcore2.0`

Env vars:
  - `AWS_LAMBDA_FUNCTION_NAME`
  - `AWS_LAMBDA_FUNCTION_VERSION`
  - `AWS_LAMBDA_FUNCTION_INVOKED_ARN`
  - `AWS_LAMBDA_FUNCTION_MEMORY_SIZE`
  - `AWS_LAMBDA_FUNCTION_TIMEOUT`
  - `AWS_LAMBDA_FUNCTION_HANDLER`
  - `AWS_LAMBDA_EVENT_BODY`
  - `AWS_REGION`
  - `AWS_DEFAULT_REGION`
  - `AWS_ACCOUNT_ID`
  - `AWS_ACCESS_KEY_ID`
  - `AWS_SECRET_ACCESS_KEY`
  - `AWS_SESSION_TOKEN`
  - `DOCKER_LAMBDA_USE_STDIN`

Options to pass to `dockerLambda()`:
  - `dockerImage`
  - `handler`
  - `event`
  - `taskDir`
  - `cleanUp`
  - `addEnvVars`
  - `dockerArgs`
  - `spawnOptions`
  - `returnSpawnResult`

Yum packages installed on build images:
  - `aws-cli`
  - `zip`
  - `git`
  - `vim`
  - `docker` (Docker in Docker!)
  - `gcc-c++`
  - `clang`
  - `openssl-devel`
  - `cmake`
  - `autoconf`
  - `automake`
  - `libtool`
  - `xz-libs`
  - `libffi-devel`
  - `python27-devel`
  - `libmpc-devel`
  - `mpfr-devel`
  - `gmp-devel`
