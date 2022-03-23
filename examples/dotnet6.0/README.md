# .NET 6.0 docker-lambda example

```sh
# Will place the compiled code in `./pub`
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnet6.0 dotnet publish -c Release -o pub

# Then you can run using that as the task directory
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnet6.0 test::test.Function::FunctionHandler '{"some": "event"}'
```
