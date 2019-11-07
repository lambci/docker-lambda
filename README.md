# docker-lambda

A sandboxed local environment that replicates the live [AWS Lambda](https://aws.amazon.com/lambda/)
environment almost identically – including installed software and libraries,
file structure and permissions, environment variables, context objects and
behaviors – even the user and running process are the same.

![Terminal Example](https://raw.githubusercontent.com/lambci/docker-lambda/master/examples/terminal.png "Example usage when index.js in current dir")

You can use it for [running your functions](#run-examples) in the same strict Lambda environment,
knowing that they'll exhibit the same behavior when deployed live. You can
also use it to [compile native dependencies](#build-examples) knowing that you're linking to the
same library versions that exist on AWS Lambda and then deploy using
the [AWS CLI](https://aws.amazon.com/cli/).

---

## Contents

* [Usage](#usage)
* [Run Examples](#run-examples)
* [Build Examples](#build-examples)
* [Using a Dockerfile to build](#using-a-dockerfile-to-build)
* [Docker tags](#docker-tags)
* [Verifying images](#verifying-images)
* [Environment variables](#environment-variables)
* [Build environment](#build-environment)
* [Questions](#questions)

---

## Usage

### Running Lambda functions

You can run your Lambdas from local directories using the `-v` arg with
`docker run` – logging goes to stderr and the callback result goes to stdout.

You mount your (unzipped) lambda code at `/var/task` and any (unzipped) layer
code at `/opt`, and most runtimes take two arguments – the first for the
handler and the second for the event, ie:

```sh
docker run [--rm] -v <code_dir>:/var/task [-v <layer_dir>:/opt] lambci/lambda:<runtime> [<handler>] [<event>]
```

(the `--rm` flag will remove the docker container once it has run, which is usually what you want)

You can pass environment variables (eg `-e AWS_ACCESS_KEY_ID=abcd`) to talk to live AWS services,
or modify aspects of the runtime. See [below](#environment-variables) for a list.

### Building Lambda functions

```sh
docker run [--rm] -v <code_dir>:/var/task [-v <layer_dir>:/opt] lambci/lambda:build-<runtime> <build-cmd>
```

You can also use [yumda](https://github.com/lambci/yumda) to install precompiled native dependencies using `yum install`.

## Run Examples

```sh
# Test an index.handler function from the current directory on Node.js v12.x
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs12.x index.handler

# If using a function other than index.handler, with a custom event
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs12.x index.myHandler '{"some": "event"}'

# Use the Node.js v8.10 runtime in a similar fashion
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs8.10 index.myHandler '{}'

# Use the Node.js v6.10 runtime with the default handler (index.handler)
docker run --rm -v "$PWD":/var/task lambci/lambda:nodejs6.10

# Test a default function (lambda_function.lambda_handler) from the current directory on Python 2.7
docker run --rm -v "$PWD":/var/task lambci/lambda:python2.7

# Test on Python 3.6 with a custom file named my_module.py containing a my_handler function
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.6 my_module.my_handler

# Python 3.7/3.8 require the handler be given explicitly
docker run --rm -v "$PWD":/var/task lambci/lambda:python3.7 lambda_function.lambda_handler

# Similarly with Ruby 2.5
docker run --rm -v "$PWD":/var/task lambci/lambda:ruby2.5 lambda_function.lambda_handler

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

# Test on .NET Core 2.1 in the same way
docker run --rm -v "$PWD":/var/task lambci/lambda:dotnetcore2.1 test::test.Function::FunctionHandler '{"some": "event"}'

# Test with a provided runtime (assumes you have a bootstrap file in the current directory)
docker run --rm -v "$PWD":/var/task lambci/lambda:provided handler '{"some": "event"}'

# Test with layers (assumes all layers have been unzipped to ../opt)
docker run --rm -v "$PWD":/var/task -v "$PWD"/../opt:/opt lambci/lambda:nodejs8.10

# Run custom commands
docker run --rm --entrypoint node lambci/lambda:nodejs8.10 -v

# For large events you can pipe them into stdin if you set DOCKER_LAMBDA_USE_STDIN (on any runtime)
echo '{"some": "event"}' | docker run --rm -v "$PWD":/var/task -i -e DOCKER_LAMBDA_USE_STDIN=1 lambci/lambda:nodejs8.10
```

You can see more examples of how to build docker images and run different
runtimes in the [examples](./examples) directory.

## Build Examples

To use the build images, for compilation, deployment, etc:

```sh
# To compile native deps in node_modules
docker run --rm -v "$PWD":/var/task lambci/lambda:build-nodejs12.x npm rebuild

# To resolve dependencies on go1.x (working directory is /go/src/handler)
docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x go mod download

# For .NET Core 2.0, this will publish the compiled code to `./pub`,
# which you can then use to run with `-v "$PWD"/pub:/var/task`
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.0 dotnet publish -c Release -o pub

# Run custom commands on a build container
docker run --rm lambci/lambda:build-python2.7 aws --version

# To run an interactive session on a build container
docker run -it lambci/lambda:build-python3.6 bash
```

## Using a Dockerfile to build

Create your own Docker image to build and deploy:

```dockerfile
FROM lambci/lambda:build-nodejs8.10

ENV AWS_DEFAULT_REGION us-east-1

COPY . .

RUN npm install

RUN zip -9yr lambda.zip .

CMD aws lambda update-function-code --function-name mylambda --zip-file fileb://lambda.zip
```

And then:

```sh
docker build -t mylambda .
docker run --rm -e AWS_ACCESS_KEY_ID -e AWS_SECRET_ACCESS_KEY mylambda
```

## Node.js module

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

## Docker tags

These follow the Lambda runtime names:

  - `nodejs4.3`
  - `nodejs6.10`
  - `nodejs8.10`
  - `nodejs10.x`
  - `nodejs12.x`
  - `python2.7`
  - `python3.6`
  - `python3.7`
  - `python3.8`
  - `ruby2.5`
  - `java8`
  - `java11`
  - `go1.x`
  - `dotnetcore2.0`
  - `dotnetcore2.1`
  - `provided`
  - `build-nodejs4.3`
  - `build-nodejs6.10`
  - `build-nodejs8.10`
  - `build-nodejs10.x`
  - `build-nodejs12.x`
  - `build-python2.7`
  - `build-python3.6`
  - `build-python3.7`
  - `build-python3.8`
  - `build-ruby2.5`
  - `build-java8`
  - `build-java11`
  - `build-go1.x`
  - `build-dotnetcore2.0`
  - `build-dotnetcore2.1`
  - `build-provided`

## Verifying images

These images are signed using [Docker Content Trust](https://docs.docker.com/engine/security/trust/content_trust/),
with the following keys:

- Repository Key: `e966126aacd4be5fb92e0160212dd007fc16a9b4366ef86d28fc7eb49f4d0809`
- Root Key: `031d78bcdca4171be103da6ffb55e8ddfa9bd113e0ec481ade78d897d9e65c0e`

You can verify/inspect an image using `docker trust inspect`:

```sh
$ docker trust inspect --pretty lambci/lambda:provided

Signatures for lambci/lambda:provided

SIGNED TAG          DIGEST                                                             SIGNERS
provided            838c42079b5fcfd6640d486f13c1ceeb52ac661e19f9f1d240b63478e53d73f8   (Repo Admin)

Administrative keys for lambci/lambda:provided

  Repository Key:	e966126aacd4be5fb92e0160212dd007fc16a9b4366ef86d28fc7eb49f4d0809
  Root Key:	031d78bcdca4171be103da6ffb55e8ddfa9bd113e0ec481ade78d897d9e65c0e
```

(The `DIGEST` for a given tag may not match the example above, but the Repository and Root keys should match)

## Environment variables

  - `AWS_LAMBDA_FUNCTION_HANDLER` or `_HANDLER`
  - `AWS_LAMBDA_EVENT_BODY`
  - `AWS_LAMBDA_FUNCTION_NAME`
  - `AWS_LAMBDA_FUNCTION_VERSION`
  - `AWS_LAMBDA_FUNCTION_INVOKED_ARN`
  - `AWS_LAMBDA_FUNCTION_MEMORY_SIZE`
  - `AWS_LAMBDA_FUNCTION_TIMEOUT`
  - `_X_AMZN_TRACE_ID`
  - `AWS_REGION` or `AWS_DEFAULT_REGION`
  - `AWS_ACCOUNT_ID`
  - `AWS_ACCESS_KEY_ID`
  - `AWS_SECRET_ACCESS_KEY`
  - `AWS_SESSION_TOKEN`
  - `DOCKER_LAMBDA_USE_STDIN`
  - `DOCKER_LAMBDA_STAY_OPEN`
  - `DOCKER_LAMBDA_API_PORT`

## Build environment

Yum packages installed on build images:

  - `development` (group, includes `gcc-c++`, `autoconf`, `automake`, `git`, `vim`, etc)
  - `aws-cli`
  - `aws-sam-cli`
  - `docker` (Docker in Docker!)
  - `clang`
  - `cmake`
  - `python27-devel`
  - `python36-devel`
  - `ImageMagick-devel`
  - `cairo-devel`
  - `libssh2-devel`
  - `libxslt-devel`
  - `libmpc-devel`
  - `readline-devel`
  - `db4-devel`
  - `libffi-devel`
  - `expat-devel`
  - `libicu-devel`
  - `lua-devel`
  - `gdbm-devel`
  - `sqlite-devel`
  - `pcre-devel`
  - `libcurl-devel`
  - `yum-plugin-ovl`

## Questions

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
  and an Amazon Linux Docker image can compile some binaries that work on
  Lambda too, for example – but for testing it's great to be able to reliably
  verify permissions issues, library linking issues, etc.

* *What's this got to do with LambCI?*

  Technically nothing – it's just been incredibly useful during the building
  and testing of LambCI.
