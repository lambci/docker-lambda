# .NET Core 3.1 docker-lambda example

```sh
# Will place the compiled code in `./pub`
docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore3.1 dotnet publish -c Release -o pub

# Then you can run using that as the task directory
docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore3.1 test::test.Function::FunctionHandler '{"some": "event"}'
```
