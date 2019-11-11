using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AWSLambda.Internal.Bootstrap;
using AWSLambda.Internal.Bootstrap.ErrorHandling;

namespace MockLambdaRuntime
{
    class Program
    {
        private const string WaitForDebuggerFlag = "--debugger-spin-wait";
        private const bool WaitForDebuggerFlagDefaultValue = false;

        /// Task root of lambda task
        static readonly string lambdaTaskRoot = EnvHelper.GetOrDefault("LAMBDA_TASK_ROOT", "/var/task");

        private static readonly TimeSpan _debuggerStatusQueryInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan _debuggerStatusQueryTimeout = TimeSpan.FromMinutes(10);

        private static readonly IList<string> assemblyDirs = new List<string> { lambdaTaskRoot };

        /// Program entry point
        static void Main(string[] args)
        {
            // Add all /var/lang/bin/shared/*/* directories to the search path
            foreach (var di in new DirectoryInfo("/var/lang/bin/shared").EnumerateDirectories().SelectMany(di => di.EnumerateDirectories().Select(di2 => di2)))
            {
                assemblyDirs.Add(di.FullName);
            }
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;

            //Console.CancelKeyPress += delegate {
            //    // call methods to clean up
            //};

            Process mockServer = null;

            try
            {
                var shouldWaitForDebugger = GetShouldWaitForDebuggerFlag(args, out var positionalArgs);

                var handler = GetFunctionHandler(positionalArgs);
                var body = GetEventBody(positionalArgs);

                var lambdaRuntime = new MockRuntime(handler, body);

                mockServer = new Process();
                mockServer.StartInfo.FileName = "/var/runtime/mockserver";
                mockServer.StartInfo.CreateNoWindow = true;
                mockServer.StartInfo.RedirectStandardInput = true;
                mockServer.StartInfo.Environment["DOCKER_LAMBDA_NO_BOOTSTRAP"] = "1";
                mockServer.StartInfo.Environment["DOCKER_LAMBDA_USE_STDIN"] = "1";
                mockServer.Start();
                mockServer.StandardInput.Write(body);
                mockServer.StandardInput.Close();

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

                var lambdaBootstrap = new LambdaBootstrap(lambdaRuntime, InternalLogger.NO_OP_LOGGER);
                UnhandledExceptionLogger.Register();
                lambdaBootstrap.Initialize();
                lambdaBootstrap.Invoke();
            }

            // Catch all unhandled exceptions from runtime, to prevent user from hanging on them while debugging
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nUnhandled exception occured in runner:\n{ex}");
            }
            finally
            {
                if (mockServer != null) mockServer.Dispose();
            }
        }

        /// Called when an assembly could not be resolved
        private static Assembly OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            foreach (var dir in assemblyDirs)
            {
                try
                {
                    return context.LoadFromAssemblyPath(Path.Combine(dir, $"{assemblyName.Name}.dll"));
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
            }
            throw new FileNotFoundException($"{assemblyName.Name}.dll");
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
