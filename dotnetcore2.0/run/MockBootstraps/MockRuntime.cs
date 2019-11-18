using AWSLambda.Internal.Bootstrap.Context;
using AWSLambda.Internal.Bootstrap.Interop.Structures;
using MockLambdaRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AWSLambda.Internal.Bootstrap
{
    internal class MockRuntime : ILambdaRuntime
    {
        private const string STACK_TRACE_INDENT = "   ";

        private bool invoked;

        private bool logTail;

        private bool initTimeSent;

        private DateTimeOffset receivedInvokeAt = DateTimeOffset.MinValue;

        private string logs;

        private Exception invokeError;

        private readonly IntPtr sharedMem = Marshal.AllocHGlobal(SBSharedMem.UnmanagedStructSize);

        private SBSharedMem curSBSharedMem;

        private readonly MockLambdaContext context;

        private static readonly HttpClient client = new HttpClient();

        private readonly MockXRayProfiler xRayProfiler = new MockXRayProfiler();

        public IEnvironment Environment { get; } = new SystemEnvironment();

        public IXRayProfiler XRayProfiler { get { return xRayProfiler; } }

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
                SuppressUserCodeInit = true,
                ErrorCode = null
            };
            invoked = false;
            client.Timeout = Timeout.InfiniteTimeSpan;
        }

        public bool KeepInvokeLoopRunning()
        {
            return true;
        }

        public void Init()
        {
            var timeout = DateTimeOffset.Now.AddSeconds(1);
            while (true)
            {
                try
                {
                    var result = client.GetAsync("http://127.0.0.1:9001/2018-06-01/ping").Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("Got a bad response from the bootstrap");
                    }
                    break;
                }
                catch (Exception e)
                {
                    if (DateTimeOffset.Now > timeout)
                    {
                        throw e;
                    }
                }
                Thread.Sleep(5);
            }
        }

        unsafe InvokeData ILambdaRuntime.ReceiveInvoke(IDictionary initialEnvironmentVariables, RuntimeReceiveInvokeBuffers buffers)
        {
            if (!invoked)
            {
                receivedInvokeAt = DateTimeOffset.Now;
                invoked = true;
            }
            else
            {
                logs = "";
            }
            var result = client.GetAsync("http://127.0.0.1:9001/2018-06-01/runtime/invocation/next").Result;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Got a bad response from the bootstrap");
            }

            var requestId = result.Headers.GetValues("Lambda-Runtime-Aws-Request-Id").First();
            var deadlineMs = result.Headers.GetValues("Lambda-Runtime-Deadline-Ms").First();
            var functionArn = result.Headers.GetValues("Lambda-Runtime-Invoked-Function-Arn").First();
            var xAmznTraceId = result.Headers.GetValues("Lambda-Runtime-Trace-Id").First();
            var clientContext = HeaderHelper.GetFirstOrDefault(result.Headers, "Lambda-Runtime-Client-Context");
            var cognitoIdentity = HeaderHelper.GetFirstOrDefault(result.Headers, "Lambda-Runtime-Cognito-Identity");

            logTail = HeaderHelper.GetFirstOrDefault(result.Headers, "Docker-Lambda-Log-Type") == "Tail";

            var body = result.Content.ReadAsStringAsync().Result;

            context.RequestId = requestId;
            context.DeadlineMs = long.Parse(deadlineMs);
            context.Body = body;

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
                XAmznTraceId = xAmznTraceId,
                InputStream = context.InputStream,
                OutputStream = new UnmanagedMemoryStream(curSBSharedMem.EventBody, 0, SBSharedMem.SizeOfEventBody, FileAccess.Write),
                LambdaContextInternal = new LambdaContextInternal(
                    context.RemainingTime,
                    SendCustomerLogMessage,
                    GetCognitoClientContextInternalLazy(clientContext),
                    context.RequestId,
                    new Lazy<string>(context.Arn),
                    GetCognitoIdentityIdLazy(cognitoIdentity),
                    GetCognitoIdentityPoolIdLazy(cognitoIdentity),
                    initialEnvironmentVariables
                )
            };
        }

        public unsafe void ReportDone(string invokeId, string errorType, bool waitForExit)
        {
            if (!invoked && invokeError == null) return;

            var output = Interop.InteropUtils.ReadUTF8String(curSBSharedMem.EventBody, curSBSharedMem.ResponseBodyLen);

            var suffix = errorType == null ? "response" : "error";

            Task<HttpResponseMessage> task;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:9001/2018-06-01/runtime/invocation/{context.RequestId}/{suffix}"))
            {
                if (logTail)
                {
                    requestMessage.Headers.Add("Docker-Lambda-Log-Result", Convert.ToBase64String(LogsTail4k()));
                }
                if (!initTimeSent)
                {
                    requestMessage.Headers.Add("Docker-Lambda-Invoke-Wait", receivedInvokeAt.ToUnixTimeMilliseconds().ToString());
                    requestMessage.Headers.Add("Docker-Lambda-Init-End", xRayProfiler.InitEnd.ToUnixTimeMilliseconds().ToString());
                    initTimeSent = true;
                }
                requestMessage.Content = new StringContent(output);
                task = client.SendAsync(requestMessage);
                try
                {
                    task.Wait();
                }
                catch (AggregateException ae)
                {
                    if (!context.StayOpen && ae.InnerException is HttpRequestException && ae.InnerException.InnerException != null &&
                        (ae.InnerException.InnerException is SocketException ||
                            // happens on dotnetcore2.0
                            ae.InnerException.InnerException.GetType().ToString().Equals("System.Net.Http.CurlException")))
                    {
                        System.Environment.Exit(string.IsNullOrEmpty(errorType) && invokeError == null ? 0 : 1);
                    }
                    else
                    {
                        throw ae;
                    }
                }
                var response = task.Result;
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new Exception($"Unknown response from invocation: {response.StatusCode}");
                }
            }
            if (invokeError != null)
            {
                Console.Error.WriteLine(invokeError);
                return;
            }
        }

        private byte[] LogsTail4k()
        {
            var logBuf = Encoding.UTF8.GetBytes(logs);
            if (logBuf.Length <= 4096)
            {
                return logBuf;
            }
            var slicedLogBuf = new byte[4096];
            Array.Copy(logBuf, logBuf.Length - 4096, slicedLogBuf, 0, 4096);
            return slicedLogBuf;
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

        /// Try to log everything to stderr except the function result
        public void SendCustomerLogMessage(string message)
        {
            if (context.StayOpen)
            {
                if (logTail)
                {
                    logs += message + System.Environment.NewLine;
                }
                Console.WriteLine(message);
            }
            else
            {
                Console.Error.WriteLine(message);
            }
        }

        private static void AppendStackTraceToStringBuilder(StringBuilder builder, ExceptionResponse ex)
        {
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                foreach (var line in ex.StackTrace
                    .Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => STACK_TRACE_INDENT + s))
                {
                    builder.AppendLine(line);
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

        internal static Lazy<string> GetCognitoIdentityIdLazy(string jsonStr)
        {
            return new Lazy<string>(() =>
            {
                var match = Regex.Match(jsonStr ?? "", "\"identity_id\":\"([^\"]+)\"");
                return match.Success ? match.Groups[1].ToString() : string.Empty;
            });
        }

        internal static Lazy<string> GetCognitoIdentityPoolIdLazy(string jsonStr)
        {
            return new Lazy<string>(() =>
            {
                var match = Regex.Match(jsonStr ?? "", "\"identity_pool_id\":\"([^\"]+)\"");
                return match.Success ? match.Groups[1].ToString() : string.Empty;
            });
        }

        internal static Lazy<CognitoClientContextInternal> GetCognitoClientContextInternalLazy(string text)
        {
            return new Lazy<CognitoClientContextInternal>(() =>
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
        public DateTimeOffset InitEnd { get; private set; }

        public void ReportUserInitStart()
        {
        }

        public void ReportUserInitEnd()
        {
            InitEnd = DateTimeOffset.Now;
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

    class HeaderHelper
    {
        /// Gets the given environment variable with a fallback if it doesn't exist
        public static string GetFirstOrDefault(HttpHeaders headers, string name)
        {
            if (headers.TryGetValues(name, out IEnumerable<string> values))
            {
                return values.FirstOrDefault();
            }
            return null;
        }
    }

}
