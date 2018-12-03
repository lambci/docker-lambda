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
        private const string WaitForDebuggerFlag = "-d";
        private const bool WaitForDebuggerFlagDefaultValue = false;

        /// Task root of lambda task
        static string lambdaTaskRoot = EnvHelper.GetOrDefault("LAMBDA_TASK_ROOT", "/var/task");

        private static readonly TimeSpan _debuggerStatusQueryInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan _debuggerStatusQueryTimeout = TimeSpan.FromMinutes(10);

        /// Program entry point
        static void Main(string[] args)
        {
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;

            try
            {
                var shouldWaitForDebugger = GetShouldWaitForDebuggerFlag(args, out var positionalArgs);

                var handler = GetFunctionHandler(positionalArgs);
                var body = GetEventBody(positionalArgs);

                if (shouldWaitForDebugger)
                {
                    Console.Error.WriteLine("Waiting for the debugger to attach...");

                    if (!DebuggerExtensions.TryWaitForAttaching(
                        _debuggerStatusQueryInterval,
                        _debuggerStatusQueryTimeout))
                    {
                        Console.Error.WriteLine("Timeout. Proceeding without debugger.");
                    }
                }

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

            // Catch all unhandled exceptions from runtime, to prevent user from hanging on them while debugging
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nUnhandled exception occured in runner:\n{ex}");
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

        /// <summary>
        /// Extracts "waitForDebugger" flag from args. Returns other unprocessed arguments.
        /// </summary>
        /// <param name="args">Args to look through</param>
        /// <param name="unprocessed">Arguments except for the "waitForDebugger" ones</param>
        /// <returns>"waitForDebugger" flag value</returns>
        private static bool GetShouldWaitForDebuggerFlag(string[] args, out string[] unprocessed)
        {
            var flagValue = WaitForDebuggerFlagDefaultValue;

            var unprocessedList = new List<string>();

            foreach (var argument in args)
            {
                if (argument == WaitForDebuggerFlag)
                {
                    flagValue = true;
                    continue;
                }

                unprocessedList.Add(argument);
            }

            unprocessed = unprocessedList.ToArray();
            return flagValue;
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
            return args.Length > 1 ? args[1] : (Environment.GetEnvironmentVariable("AWS_LAMBDA_EVENT_BODY") ??
              (Environment.GetEnvironmentVariable("DOCKER_LAMBDA_USE_STDIN") != null ? Console.In.ReadToEnd() : "{}"));
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
