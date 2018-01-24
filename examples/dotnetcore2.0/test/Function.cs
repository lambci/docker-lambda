/*
BUILD: docker run --rm -v "$PWD"/test:/app microsoft/dotnet:sdk /usr/share/dotnet/dotnet publish /app -c Release -o pub
RUN: docker run --rm -v "$PWD"/test/pub:/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler "some"
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace test
{
    public class Function
    {
        
        public string FunctionHandler(Stream stream, ILambdaContext context)
        {
            context.Logger.Log("Log Hello world");
            return "Hallo world!";
        }
    }
}
