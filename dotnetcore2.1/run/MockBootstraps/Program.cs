using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AWSLambda.Internal.Bootstrap;
using AWSLambda.Internal.Bootstrap.Context;

namespace MockLambdaRuntime
{
    internal static class Program
    {
        private const string WaitForDebuggerFlagName = "d";

        private const bool WaitForDebuggerFlagDefaultValue = false;

        /// Task root of lambda task
        private static readonly string lambdaTaskRoot = EnvHelper.GetOrDefault("LAMBDA_TASK_ROOT", "/var/task");

        private static readonly TimeSpan _debuggerStatusQueryInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan _debuggerStatusQueryTimeout = TimeSpan.FromMinutes(10);

        /// Program entry point
        public static void Main(string[] args)
        {
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;

            if (args.Length > 3)
            {
                Console.Error.WriteLine("Too many arguments");
                return;
            }

            var shouldWaitForDebugger = GetShouldWaitForDebuggerFlag(args, out var positionalArgs);

            if (!TryGetFunctionHandler(positionalArgs, out var handler))
            {
                Console.Error.WriteLine("Handler was not specified");
                return;
            }
            
            var body = GetEventBody(positionalArgs);

            if (shouldWaitForDebugger)
            {
                TryDisplayProcessId();
                Console.Error.WriteLine("Waiting for the debugger to attach...");

                if (!DebuggerExtensions.TryWaitForAttaching(_debuggerStatusQueryInterval, _debuggerStatusQueryTimeout))
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

        /// <summary>
        /// Tries to display PID of the started program to simplify attaching.
        /// </summary>
        private static void TryDisplayProcessId()
        {
            try
            {
                var processId = Process.GetCurrentProcess().Id;
                Console.Error.WriteLine($"Attach to processId: {processId}.");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is PlatformNotSupportedException)
            {
                Console.Error.WriteLine($"Failed to get process ID: {ex.Message}.");
            }
        }

        /// <summary>
        /// Extracts "waitForDebugger" flag from args. Returns other unprocessed arguments.
        /// </summary>
        /// <param name="args">Args to look through</param>
        /// <param name="unprocessed">Arguments except for the "waitForDebugger" ones</param>
        /// <returns>"waitForDebugger" flag value</returns>
        private static bool GetShouldWaitForDebuggerFlag(IReadOnlyList<string> args, out IReadOnlyList<string> unprocessed)
        {
            var flagValue = WaitForDebuggerFlagDefaultValue;

            var unprocessedList = new List<string>();
            foreach (var argument in args)
            {
                if (argument.StartsWith('-'))
                {
                    var flag = argument.TrimStart('-');
                    if (flag == WaitForDebuggerFlagName)
                    {
                        flagValue = true;
                        continue;
                    }
                }

                unprocessedList.Add(argument);
            }

            if (!flagValue)
            {
                flagValue = Environment.GetEnvironmentVariable("_SHOULD_WAIT_FOR_DEBUGGER") != null;                
            }

            unprocessed = unprocessedList.AsReadOnly();
            return flagValue;
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

        private static void LogRequestStart(MockLambdaContext context)
        {
            Console.Error.WriteLine($"START RequestId: {context.RequestId} Version: {context.FunctionVersion}");
        }

        private static void LogRequestEnd(MockLambdaContext context)
        {
            Console.Error.WriteLine($"END  RequestId: {context.RequestId}");

            Console.Error.WriteLine($"REPORT RequestId {context.RequestId}\t" +
                                    $"Duration: {context.Duration} ms\t" +
                                    $"Billed Duration: {context.BilledDuration} ms\t" +
                                    $"Memory Size {context.MemorySize} MB\t" +
                                    $"Max Memory Used: {context.MemoryUsed / (1024 * 1024)} MB");
        }

        /// Gets the function handler from arguments or environment
        private static bool TryGetFunctionHandler(IReadOnlyList<string> args, out string handler)
        {
            handler = args.Count > 0 ? args[0] : EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_HANDLER", string.Empty);
            return !string.IsNullOrWhiteSpace(handler);
        }

        /// Gets the event body from arguments or environment
        private static string GetEventBody(IReadOnlyList<string> args)
        {
            return args.Count > 1 ? args[1] : (Environment.GetEnvironmentVariable("AWS_LAMBDA_EVENT_BODY") ??
              (Environment.GetEnvironmentVariable("DOCKER_LAMBDA_USE_STDIN") != null ? Console.In.ReadToEnd() : "'{}'"));
        }
    }

    internal static class EnvHelper
    {
        /// Gets the given environment variable with a fallback if it doesn't exist
        public static string GetOrDefault(string name, string fallback)
        {
            return Environment.GetEnvironmentVariable(name) ?? fallback;
        }
    }
}