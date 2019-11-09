using AWSLambda.Internal.Bootstrap.Context;
using AWSLambda.Internal.Bootstrap.Interop.Structures;
using MockLambdaRuntime;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AWSLambda.Internal.Bootstrap
{
    internal class MockRuntime : ILambdaRuntime
    {
        private const string STACK_TRACE_INDENT = "   ";

        private bool invoked;

        private Exception invokeError;

        private readonly byte[] outputBuffer = new byte[SBSharedMem.SizeOfEventBody];

        private readonly IntPtr sharedMem = Marshal.AllocHGlobal(SBSharedMem.UnmanagedStructSize);

        private SBSharedMem curSBSharedMem;

        private readonly MockLambdaContext context;

        public IEnvironment Environment { get; } = new SystemEnvironment();

        public IXRayProfiler XRayProfiler { get; } = new MockXRayProfiler();

        public InitData InitData
        {
            get;
            private set;
        }

        public MockRuntime(string handler, string body)
        {
            context = new MockLambdaContext(handler, body);
            InitData = new InitData
            {
                Handler = handler,
                InvokeId = context.RequestId,
                SuppressUserCodeInit = false,
                ErrorCode = null
            };
            invoked = false;
        }

        public bool KeepInvokeLoopRunning()
        {
            return true;
        }

        public void Init()
        {
        }

        public InvokeData ReceiveInvoke(IDictionary initialEnvironmentVariables, RuntimeReceiveInvokeBuffers buffers)
        {
            Console.Error.WriteLine($"START RequestId: {context.RequestId} Version: {context.FunctionVersion}");

            invoked = true;

            curSBSharedMem = new SBSharedMem(sharedMem);
            return new InvokeData(curSBSharedMem)
            {
                RequestId = context.RequestId,
                AwsCredentials = new AwsCredentials
                {
                    AccessKeyId = EnvHelper.GetOrDefault("AWS_ACCESS_KEY_ID", "SOME_ACCESS_KEY_ID"),
                    SecretAccessKey = EnvHelper.GetOrDefault("AWS_SECRET_ACCESS_KEY", "SOME_SECRET_ACCESS_KEY"),
                    SessionToken = System.Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN")
                },
                XAmznTraceId = EnvHelper.GetOrDefault("_X_AMZN_TRACE_ID", ""),
                InputStream = context.InputStream,
                OutputStream = new MemoryStream(outputBuffer),
                LambdaContextInternal = new LambdaContextInternal(
                    context.RemainingTime,
                    SendCustomerLogMessage,
                    new Lazy<CognitoClientContextInternal>(),
                    context.RequestId,
                    new Lazy<string>(context.Arn),
                    new Lazy<string>(string.Empty),
                    new Lazy<string>(string.Empty),
                    initialEnvironmentVariables
                )
            };
        }

        /// Try to log everything to stderr except the function result
        public void SendCustomerLogMessage(string message)
        {
            Console.Error.WriteLine(message);
        }

        public void ReportDone(string invokeId, string errorType, bool waitForExit)
        {
            if (!invoked && invokeError == null) return;

            Console.Error.WriteLine($"END  RequestId: {context.RequestId}");

            Console.Error.WriteLine($"REPORT RequestId {context.RequestId}\t" +
                                    $"Duration: {context.Duration} ms\t" +
                                    $"Billed Duration: {context.BilledDuration} ms\t" +
                                    $"Memory Size {context.MemorySize} MB\t" +
                                    $"Max Memory Used: {context.MemoryUsed / (1024 * 1024)} MB");

            if (invokeError != null)
            {
                Console.Error.WriteLine(invokeError);
                System.Environment.Exit(1);
                return;
            }

            var output = Encoding.UTF8.GetString(outputBuffer, 0, curSBSharedMem.ResponseBodyLen);

            Console.WriteLine(output);

            System.Environment.Exit(string.IsNullOrEmpty(errorType) ? 0 : 1);
        }

        public void ReportError(string invokeId, ExceptionResponse exceptionResponse)
        {
            invokeError = exceptionResponse.OriginalException;

            // XXX: For the future perhaps:
            /*
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(exceptionResponse.StackTrace))
            {
                stringBuilder.AppendLine(exceptionResponse.StackTrace);
            }
            if (exceptionResponse.InnerException != null)
            {
                AppendStackTraceToStringBuilder(stringBuilder, exceptionResponse.InnerException);
            }
            */
        }

        private static void AppendStackTraceToStringBuilder(StringBuilder builder, ExceptionResponse ex)
        {
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                string[] array = (from s in ex.StackTrace.Split(new string[]
                    {
                        System.Environment.NewLine
                    }, StringSplitOptions.None)
                                  select s.Trim() into s
                                  where !string.IsNullOrWhiteSpace(s)
                                  select STACK_TRACE_INDENT + s).ToArray();
                foreach (string value in array)
                {
                    builder.AppendLine(value);
                }
            }
            if (ex.InnerException != null)
            {
                string errorMessage = ex.InnerException.ErrorMessage;
                string errorType = ex.InnerException.ErrorType;
                if (errorMessage != null)
                {
                    builder.Append(errorMessage.Trim());
                    builder.Append(": ");
                }
                builder.AppendLine(errorType);
                AppendStackTraceToStringBuilder(builder, ex.InnerException);
            }
        }

        internal static Lazy<CognitoClientContextInternal> GetCognitoClientContextInternalLazy(string text)
        {
            return new Lazy<CognitoClientContextInternal>(delegate
            {
                CognitoClientContextInternal result = null;
                if (!string.IsNullOrEmpty(text))
                {
                    try
                    {
                        return CognitoClientContextInternal.FromJson(text);
                    }
                    catch (Exception innerException)
                    {
                        throw LambdaExceptions.ValidationException(innerException, "Unable to parse client context JSON string '{0}'.", text);
                    }
                }
                return result;
            });
        }
    }

    internal class MockXRayProfiler : IXRayProfiler
    {
        public void ReportUserInitStart()
        {
        }

        public void ReportUserInitEnd()
        {
        }

        public void ReportUserInvokeStart()
        {
        }

        public void ReportUserInvokeEnd()
        {
        }

        public void ReportError(ExceptionResponse exceptionResponse)
        {
        }
    }

}
