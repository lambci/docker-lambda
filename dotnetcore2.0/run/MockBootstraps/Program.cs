using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AWSLambda.Internal.Bootstrap;
using AWSLambda.Internal.Bootstrap.Context;

namespace MockLambdaRuntime
{
    class Program
    {
        /// <summary>
        /// Task root of lambda task
        /// </summary>
        private static string lambdaTaskRoot;

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            //add resolving hook
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;
            lambdaTaskRoot = GetEnvironmentVariable("LAMBDA_TASK_ROOT", "/var/task");

            string handler = GetFunctionHandler(args);
            string body = GetContext(args);

            var lambdaContext = new MockLambdaContext(body, Environment.GetEnvironmentVariables());
            
            
            var userCodeLoader = new UserCodeLoader(handler,InternalLogger.NO_OP_LOGGER);
            userCodeLoader.Init(x => Console.WriteLine(x));

            var lambdaContextInternal = new LambdaContextInternal(lambdaContext.RemainingTime,
                                                                  LogAction,new Lazy<CognitoClientContextInternal>(), 
                                                                  lambdaContext.RequestId,
                                                                  new Lazy<string>(lambdaContext.Arn),
                                                                  new Lazy<string>(string.Empty),
                                                                  new Lazy<string>(string.Empty),
                                                                  Environment.GetEnvironmentVariables());           
            
            LogStartRequest(lambdaContext);
            try
            {
                userCodeLoader.Invoke(lambdaContext.InputStream,lambdaContext.OutputStream,lambdaContextInternal);
                Console.WriteLine(lambdaContext.OutputText);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            LogEndRequest(lambdaContext);
        }

        /// <summary>
        /// Called when an assembly could not be resolved
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        private static Assembly OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assembly)
        {
            return context.LoadFromAssemblyPath(Path.Combine(lambdaTaskRoot, assembly.Name) + ".dll");
        }

        /// <summary>
        /// Logs the given text
        /// </summary>
        /// <param name="text">The text.</param>
        private static void LogAction(string text)
        {
            Console.Error.WriteLine(text);
        }

        /// <summary>
        /// Logs the start request.
        /// </summary>
        /// <param name="context"></param>
        static void LogStartRequest(MockLambdaContext context)
        {
            Console.Error.WriteLine($"START RequestId: {context.RequestId} Version: {context.FunctionVersion}");
        }

        /// <summary>
        /// Logs the end request.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        static void LogEndRequest(MockLambdaContext context)
        {
            Console.Error.WriteLine($"END  RequestId: {context.RequestId}");
            
            /*'REPORT RequestId: ' + invokeId,
            'Duration: ' + diffMs.toFixed(2) + ' ms',
            'Billed Duration: ' + billedMs + ' ms',
            'Memory Size: ' + MEM_SIZE + ' MB',
            'Max Memory Used: ' + Math.round(process.memoryUsage().rss / (1024 * 1024)) + ' MB',
            '',
                ].join('\t'))*/
            Console.Error.WriteLine($"REPORT RequestId {context.RequestId}\t" +
                                    $"Duration: {context.Duration} ms\t" +
                                    $"Billed Duration: {context.BilledDuration} ms\t" +
                                    $"Memory Size {context.MemorySize} MB\t" +
                                    $"Max Memory Used: {context.MemoryUsed/(1024*1024)} MB");
        }

        /// <summary>
        /// Gets the function handler from arguments or environment
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        static string GetFunctionHandler(string[] args){
            return args.Length > 0 ? args[0] : GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_HANDLER",string.Empty);
        }

        /// <summary>
        /// Gets the context from arguments or environment
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        static string GetContext(string[] args){
            return args.Length > 1 ? args[1] :GetEnvironmentVariable("AWS_LAMBDA_CONTEXT","{}"); ;   
        }


        /// <summary>
        /// Gets the environment variable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fallback">The fallback.</param>
        /// <returns></returns>
        static string GetEnvironmentVariable(string name, string fallback)
        {
            var res = Environment.GetEnvironmentVariable(name);
            if(res != null)
                return res;
            return fallback;

        }

       
    }
}
