// Compile with:
// docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.0 dotnet publish -c Release -o pub

// Run with:
// docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.0 test::test.Function::FunctionHandler "some"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using System.IO;

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
