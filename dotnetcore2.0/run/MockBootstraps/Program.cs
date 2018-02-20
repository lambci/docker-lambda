using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AWSLambda.Internal.Bootstrap;
using AWSLambda.Internal.Bootstrap.Context;

namespace MockLambdaRuntime
{
    class Program
    {
        /// Task root of lambda task
        static string lambdaTaskRoot = EnvHelper.GetOrDefault("LAMBDA_TASK_ROOT", "/var/task");

        /// Program entry point
        static void Main(string[] args)
        {
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;

            var handler = GetFunctionHandler(args);
            var body = GetEventBody(args);

            var lambdaContext = new MockLambdaContext(handler, body);

            var userCodeLoader = new UserCodeLoader(handler, InternalLogger.NO_OP_LOGGER);
            userCodeLoader.Init(Console.Error.WriteLine);

            var lambdaContextInternal = new LambdaContextInternal(lambdaContext.RemainingTime,
                                                                  LogAction, new Lazy<CognitoClientContextInternal>(),
                                                                  lambdaContext.RequestId,
                                                                  new Lazy<string>(lambdaContext.Arn),
                                                                  new Lazy<string>(string.Empty),
                                                                  new Lazy<string>(string.Empty),
                                                                  Environment.GetEnvironmentVariables());

            Exception lambdaException = null;

            LogRequestStart(lambdaContext);
            try
            {
                userCodeLoader.Invoke(lambdaContext.InputStream, lambdaContext.OutputStream, lambdaContextInternal);
            }
            catch (Exception ex)
            {
                lambdaException = ex;
            }
            LogRequestEnd(lambdaContext);

            if (lambdaException == null)
            {
                Console.WriteLine(lambdaContext.OutputText);
            }
            else
            {
                Console.Error.WriteLine(lambdaException);
            }
        }

        /// Called when an assembly could not be resolved
        private static Assembly OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assembly)
        {
            return context.LoadFromAssemblyPath(Path.Combine(lambdaTaskRoot, $"{assembly.Name}.dll"));
        }

        /// Try to log everything to stderr except the function result
        private static void LogAction(string text)
        {
            Console.Error.WriteLine(text);
        }

        static void LogRequestStart(MockLambdaContext context)
        {
            Console.Error.WriteLine($"START RequestId: {context.RequestId} Version: {context.FunctionVersion}");
        }

        static void LogRequestEnd(MockLambdaContext context)
        {
            Console.Error.WriteLine($"END  RequestId: {context.RequestId}");

            Console.Error.WriteLine($"REPORT RequestId {context.RequestId}\t" +
                                    $"Duration: {context.Duration} ms\t" +
                                    $"Billed Duration: {context.BilledDuration} ms\t" +
                                    $"Memory Size {context.MemorySize} MB\t" +
                                    $"Max Memory Used: {context.MemoryUsed / (1024 * 1024)} MB");
        }

        /// Gets the function handler from arguments or environment
        static string GetFunctionHandler(string[] args)
        {
            return args.Length > 0 ? args[0] : EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_HANDLER", string.Empty);
        }

        /// Gets the event body from arguments or environment
        static string GetEventBody(string[] args)
        {
            return args.Length > 1 ? args[1] : EnvHelper.GetOrDefault("AWS_LAMBDA_EVENT_BODY", "{}");
        }
    }

    class EnvHelper
    {
        /// Gets the given environment variable with a fallback if it doesn't exist
        public static string GetOrDefault(string name, string fallback)
        {
            return Environment.GetEnvironmentVariable(name) ?? fallback;
        }
    }
}
