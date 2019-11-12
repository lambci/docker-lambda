# Java 8 docker-lambda example

Run with:

```sh
# Will place the compiled code in `./build/docker`
docker run --rm -v "$PWD":/app -w /app gradle:6.0 gradle build

# Then you can run using that directory as the task directory
docker run --rm -v "$PWD/build/docker":/var/task lambci/lambda:java8 org.lambci.lambda.ExampleHandler '{"some": "event"}'
```
