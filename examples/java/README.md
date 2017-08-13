# Java 8 docker-lambda example

This example requires [Gradle](https://gradle.org/) to be installed to build
and layout the classes and jars correctly.

Run with:

```sh
# Will place the compiled code in `./build/docker`
gradle build

# Then you can run using that directory as the task directory
docker run -v "$PWD/build/docker":/var/task lambci/lambda:java8 org.lambci.lambda.ExampleHandler '{"some": "event"}'
```
