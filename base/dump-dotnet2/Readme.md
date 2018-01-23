# AWS Lambda Dump Runtime Project
This functions dumps the runtime and uplaods it to s3.


## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Restore dependencies
```
    cd "dumpdotnet2"
    dotnet restore
```

Deploy function to AWS Lambda
```
    cd "dumpdotnet2/src/dumpdotnet2"
    dotnet lambda deploy-function
```
