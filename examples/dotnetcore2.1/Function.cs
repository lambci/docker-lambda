// Compile with:
// docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore2.1 dotnet publish -c Release -o pub

// Run with:
// docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore2.1 test::test.Function::FunctionHandler '{"some": "event"}'

using System;
using System.Collections;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace test
{
    public class Function
    {
        public string FunctionHandler(object inputEvent, ILambdaContext context)
        {
            context.Logger.Log($"inputEvent: {inputEvent}");
            LambdaLogger.Log($"RemainingTime: {context.RemainingTime}");

            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                context.Logger.Log($"{kv.Key}={kv.Value}");
            }

            return "Hello World!";
        }
    }
}
