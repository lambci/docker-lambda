using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MockLambdaRuntime
{
    public class MockLambdaContext
    {
        static readonly Random random = new Random();

        /// Creates a mock context from a given Lambda handler and event
        public MockLambdaContext(string handler, string eventBody)
        {
            RequestId = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            InputStream = new MemoryStream();
            OutputStream = new MemoryStream();

            var eventData = Encoding.UTF8.GetBytes(eventBody);
            InputStream.Write(eventData, 0, eventData.Length);
            InputStream.Position = 0;

            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", FunctionName);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_VERSION", FunctionVersion);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", MemorySize.ToString());
            Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_GROUP_NAME", LogGroup);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_STREAM_NAME", LogStream);
            Environment.SetEnvironmentVariable("AWS_REGION", Region);
            Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", Region);
            Environment.SetEnvironmentVariable("_HANDLER", handler);
        }

        /// Calculates the remaining time using current time and timeout
        public TimeSpan RemainingTime()
        {
            return StartTime + TimeSpan.FromSeconds(Timeout) - DateTime.Now;
        }

        public long Duration => (long)(DateTime.Now - StartTime).TotalMilliseconds;
        public long BilledDuration => (long)(Math.Ceiling((DateTime.Now - StartTime).TotalMilliseconds / 100)) * 100;

        public long MemoryUsed => Process.GetCurrentProcess().WorkingSet64;

        public Stream InputStream { get; }

        public Stream OutputStream { get; }

        public string OutputText
        {
            get
            {
                OutputStream.Position = 0;
                using (TextReader reader = new StreamReader(OutputStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public string RequestId { get; }
        public DateTime StartTime { get; }

        public int Timeout => Convert.ToInt32(EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_TIMEOUT", "300"));

        public int MemorySize => Convert.ToInt32(EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"));

        public string FunctionName => EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_NAME", "test");

        public string FunctionVersion => EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST");

        public string LogGroup => EnvHelper.GetOrDefault("AWS_LAMBDA_LOG_GROUP_NAME", $"/aws/lambda/{FunctionName}");

        public string LogStream => EnvHelper.GetOrDefault("AWS_LAMBDA_LOG_STREAM_NAME", RandomLogStreamName);

        public string Region => EnvHelper.GetOrDefault("AWS_REGION", EnvHelper.GetOrDefault("AWS_DEFAULT_REGION", "us-east-1"));

        public string AccountId => EnvHelper.GetOrDefault("AWS_ACCOUNT_ID", "000000000000");

        public string Arn => EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_INVOKED_ARN", $"arn:aws:lambda:{Region}:{AccountId}:function:{FunctionName}");

        string RandomLogStreamName => $"{DateTime.Now.ToString("yyyy/MM/dd")}/[{FunctionVersion}]{random.Next().ToString("x") + random.Next().ToString("x")}";
    }
}
