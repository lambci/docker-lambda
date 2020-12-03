# docker-lambda

A sandboxed local environment that replicates the live [AWS Lambda](https://aws.amazon.com/lambda/)
environment almost identically – including installed software and libraries,
file structure and permissions, environment variables, context objects and
behaviors – even the user and running process are the same.

<img src="https://raw.githubusercontent.com/lambci/docker-lambda/master/examples/terminal3.png" width="969" alt="Example usage with java11 runtime">

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
`docker run`. You can run them in two modes: as a single execution, or as
[an API server that listens for invoke events](#running-in-stay-open-api-mode).
The default is single execution mode, which outputs all logging to stderr and the result of the handler to stdout.

You mount your (unzipped) lambda code at `/var/task` and any (unzipped) layer
code at `/opt`, and most runtimes take two arguments – the first for the
handler and the second for the event, ie:

```sh
docker run --rm \
  -v <code_dir>:/var/task:ro,delegated \
  [-v <layer_dir>:/opt:ro,delegated] \
  lambci/lambda:<runtime> \
  [<handler>] [<event>]
```

(the `--rm` flag will remove the docker container once it has run, which is usually what you want,
and the `ro,delegated` options ensure the directories are mounted read-only and have the highest performance)

You can pass environment variables (eg `-e AWS_ACCESS_KEY_ID=abcd`) to talk to live AWS services,
or modify aspects of the runtime. See [below](#environment-variables) for a list.

#### Running in "stay-open" API mode

If you pass the environment variable `DOCKER_LAMBDA_STAY_OPEN=1` to the container, then instead of
executing the event and shutting down, it will start an API server (on port 9001 by default), which
you can then call with HTTP following the [Lambda Invoke API](https://docs.aws.amazon.com/lambda/latest/dg/API_Invoke.html).
This allows you to make fast subsequent calls to your handler without paying the "cold start" penalty each time.

```sh
docker run --rm [-d] \
  -e DOCKER_LAMBDA_STAY_OPEN=1 \
  -p 9001:9001 \
  -v <code_dir>:/var/task:ro,delegated \
  [-v <layer_dir>:/opt:ro,delegated] \
  lambci/lambda:<runtime> \
  [<handler>]
```

(the `-d` flag will start the container in detached mode, in the background)

You should then see:

```sh
Lambda API listening on port 9001...
```

Then, in another terminal shell/window you can invoke your function using the [AWS CLI](https://aws.amazon.com/cli/)
(or any http client, like `curl`):

```sh
aws lambda invoke --endpoint http://localhost:9001 --no-sign-request \
  --function-name myfunction --payload '{}' output.json
```

(if you're using [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/cliv2-migration.html#cliv2-migration-binaryparam), you'll need to add `--cli-binary-format raw-in-base64-out` to the above command)

Or just:

```sh
curl -d '{}' http://localhost:9001/2015-03-31/functions/myfunction/invocations
```

It also supports the [documented Lambda API headers](https://docs.aws.amazon.com/lambda/latest/dg/API_Invoke.html)
`X-Amz-Invocation-Type`, `X-Amz-Log-Type` and `X-Amz-Client-Context`.

If you want to change the exposed port, eg run on port 3000 on the host, use `-p 3000:9001` (then query `http://localhost:3000`).

You can change the internal Lambda API port from `9001` by passing `-e DOCKER_LAMBDA_API_PORT=<port>`.
You can also change the [custom runtime](https://docs.aws.amazon.com/lambda/latest/dg/runtimes-custom.html#runtimes-custom-build)
port from `9001` by passing `-e DOCKER_LAMBDA_RUNTIME_PORT=<port>`.

#### Developing in "stay-open" mode

docker-lambda can watch for changes to your handler (and layer) code and restart the internal bootstrap process
so you can always invoke the latest version of your code without needing to shutdown the container.

To enable this, pass `-e DOCKER_LAMBDA_WATCH=1` to `docker run`:

```
docker run --rm \
  -e DOCKER_LAMBDA_WATCH=1 -e DOCKER_LAMBDA_STAY_OPEN=1 -p 9001:9001 \
  -v "$PWD":/var/task:ro,delegated \
  lambci/lambda:java11 handler
```

Then when you make changes to any file in the mounted directory, you'll see:

```
Handler/layer file changed, restarting bootstrap...
```

And the next invoke will reload your handler with the latest version of your code.

NOTE: This doesn't work in exactly the same way with some of the older runtimes due to the way they're loaded. Specifically: `nodejs8.10` and earlier, `python3.6` and earlier, `dotnetcore2.1` and earlier, `java8` and `go1.x`. These runtimes will instead exit with error code 2
when they are in watch mode and files in the handler or layer are changed.

That way you can use the `--restart on-failure` capabilities of `docker run` to have the container automatically restart instead.

So, for `nodejs8.10`, `nodejs6.10`, `nodejs4.3`, `python3.6`, `python2.7`, `dotnetcore2.1`, `dotnetcore2.0`, `java8` and `go1.x`, you'll
need to run watch mode like this instead:

```
docker run --restart on-failure \
  -e DOCKER_LAMBDA_WATCH=1 -e DOCKER_LAMBDA_STAY_OPEN=1 -p 9001:9001 \
  -v "$PWD":/var/task:ro,delegated \
  lambci/lambda:java8 handler
```

When you make changes to any file in the mounted directory, you'll see:

```
Handler/layer file changed, restarting bootstrap...
```

And then the docker container will restart. See the [Docker documentation](https://docs.docker.com/engine/reference/commandline/run/#restart-policies---restart) for more details. Your terminal may get detached, but the container should still be running and the
API should have restarted. You can do `docker ps` to find the container ID and then `docker attach <container_id>` to reattach if you wish.

If none of the above strategies work for you, you can use a file-watching utility like [nodemon](https://nodemon.io/):

```sh
# npm install -g nodemon
nodemon -w ./ -e '' -s SIGINT -x docker -- run --rm \
  -e DOCKER_LAMBDA_STAY_OPEN=1 -p 9001:9001 \
  -v "$PWD":/var/task:ro,delegated \
  lambci/lambda:go1.x handler
```

### Building Lambda functions

The build images have a [number of extra system packages installed](#build-environment)
intended for building and packaging your Lambda functions. You can run your build commands (eg, `gradle` on the java image), and then package up your function using `zip` or the
[AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html),
all from within the image.

```sh
docker run [--rm] -v <code_dir>:/var/task [-v <layer_dir>:/opt] lambci/lambda:build-<runtime> <build-cmd>
```

You can also use [yumda](https://github.com/lambci/yumda) to install precompiled native dependencies using `yum install`.

## Run Examples

```sh
# Test a `handler` function from an `index.js` file in the current directory on Node.js v12.x
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:nodejs12.x index.handler

# Using a different file and handler, with a custom event
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:nodejs12.x app.myHandler '{"some": "event"}'

# Test a `lambda_handler` function in `lambda_function.py` with an empty event on Python 3.8
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:python3.8 lambda_function.lambda_handler

# Similarly with Ruby 2.7
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:ruby2.7 lambda_function.lambda_handler

# Test on Go 1.x with a compiled handler named my_handler and a custom event
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:go1.x my_handler '{"some": "event"}'

# Test a function from the current directory on Java 11
# The directory must be laid out in the same way the Lambda zip file is,
# with top-level package source directories and a `lib` directory for third-party jars
# https://docs.aws.amazon.com/lambda/latest/dg/java-package.html
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:java11 org.myorg.MyHandler

# Test on .NET Core 3.1 given a test.dll assembly in the current directory,
# a class named Function with a FunctionHandler method, and a custom event
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:dotnetcore3.1 test::test.Function::FunctionHandler '{"some": "event"}'

# Test with a provided runtime (assumes you have a `bootstrap` executable in the current directory)
docker run --rm -v "$PWD":/var/task:ro,delegated lambci/lambda:provided handler '{"some": "event"}'

# Test with layers (assumes your function code is in `./fn` and your layers in `./layer`)
docker run --rm -v "$PWD"/fn:/var/task:ro,delegated -v "$PWD"/layer:/opt:ro,delegated lambci/lambda:nodejs12.x

# Run custom commands
docker run --rm --entrypoint node lambci/lambda:nodejs12.x -v

# For large events you can pipe them into stdin if you set DOCKER_LAMBDA_USE_STDIN
echo '{"some": "event"}' | docker run --rm -v "$PWD":/var/task:ro,delegated -i -e DOCKER_LAMBDA_USE_STDIN=1 lambci/lambda:nodejs12.x
```

You can see more examples of how to build docker images and run different
runtimes in the [examples](./examples) directory.

## Build Examples

To use the build images, for compilation, deployment, etc:

```sh
# To compile native deps in node_modules
docker run --rm -v "$PWD":/var/task lambci/lambda:build-nodejs12.x npm rebuild --build-from-source

# To install defined poetry dependencies
docker run --rm -v "$PWD":/var/task lambci/lambda:build-python3.8 poetry install

# To resolve dependencies on go1.x (working directory is /go/src/handler)
docker run --rm -v "$PWD":/go/src/handler lambci/lambda:build-go1.x go mod download

# For .NET Core, this will publish the compiled code to `./pub`,
# which you can then use to run with `-v "$PWD"/pub:/var/task`
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore3.1 dotnet publish -c Release -o pub

# Run custom commands on a build container
docker run --rm lambci/lambda:build-python3.8 aws --version

# To run an interactive session on a build container
docker run -it lambci/lambda:build-python3.8 bash
```

## Using a Dockerfile to build

Create your own Docker image to build and deploy:

```dockerfile
FROM lambci/lambda:build-nodejs12.x

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
var lambdaCallbackResult = dockerLambda({event: {some: 'event'}, dockerImage: 'lambci/lambda:nodejs12.x'})

// Manually specify directory and custom args
lambdaCallbackResult = dockerLambda({taskDir: __dirname, dockerArgs: ['-m', '1.5G'], dockerImage: 'lambci/lambda:nodejs12.x'})
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
  - `ruby2.7`
  - `java8`
  - `java8.al2`
  - `java11`
  - `go1.x`
  - `dotnetcore2.0`
  - `dotnetcore2.1`
  - `dotnetcore3.1`
  - `provided`
  - `provided.al2`
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
  - `build-ruby2.7`
  - `build-java8`
  - `build-java8.al2`
  - `build-java11`
  - `build-go1.x`
  - `build-dotnetcore2.0`
  - `build-dotnetcore2.1`
  - `build-dotnetcore3.1`
  - `build-provided`
  - `build-provided.al2`

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
  - `DOCKER_LAMBDA_RUNTIME_PORT`
  - `DOCKER_LAMBDA_DEBUG`
  - `DOCKER_LAMBDA_NO_MODIFY_LOGS`

## Build environment

Yum packages installed on build images:

  - `development` (group, includes `gcc-c++`, `autoconf`, `automake`, `git`, `vim`, etc)
  - `aws-cli`
  - `aws-sam-cli`
  - `docker` (Docker in Docker!)
  - `clang`
  - `cmake`
  
The build image for older Amazon Linux 1 based runtimes also include:

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
