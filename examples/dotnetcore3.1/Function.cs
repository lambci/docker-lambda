// Compile with:
// docker run --rm -v "$PWD":/var/task lambci/lambda:build-dotnetcore3.1 dotnet publish -c Release -o pub

// Run with:
// docker run --rm -v "$PWD"/pub:/var/task lambci/lambda:dotnetcore3.1 test::test.Function::FunctionHandler '{"some": "event"}'

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
            Console.WriteLine($"inputEvent: {inputEvent}");
            Console.WriteLine($"RemainingTime: {context.RemainingTime}");

            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                Console.WriteLine($"{kv.Key}={kv.Value}");
            }

            return "Hello World!";
        }
    }
}
