BUILD: docker run --rm -v "$PWD"/test:/app microsoft/dotnet:sdk /usr/share/dotnet/dotnet publish /app -c Release -o pub
RUN: docker run --rm -v "$PWD"/test/pub:/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler "some"
