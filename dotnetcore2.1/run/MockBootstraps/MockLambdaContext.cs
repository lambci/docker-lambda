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
            Body = eventBody;
            Timeout = Convert.ToInt32(EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_TIMEOUT", "300"));
            MemorySize = Convert.ToInt32(EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"));
            FunctionName = EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_NAME", "test");
            FunctionVersion = EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST");
            LogGroup = EnvHelper.GetOrDefault("AWS_LAMBDA_LOG_GROUP_NAME", $"/aws/lambda/{FunctionName}");
            LogStream = EnvHelper.GetOrDefault("AWS_LAMBDA_LOG_STREAM_NAME", RandomLogStreamName);
            Region = EnvHelper.GetOrDefault("AWS_REGION", EnvHelper.GetOrDefault("AWS_DEFAULT_REGION", "us-east-1"));
            AccountId = EnvHelper.GetOrDefault("AWS_ACCOUNT_ID", "000000000000");
            Arn = EnvHelper.GetOrDefault("AWS_LAMBDA_FUNCTION_INVOKED_ARN", $"arn:aws:lambda:{Region}:{AccountId}:function:{FunctionName}");
            StayOpen = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_LAMBDA_STAY_OPEN"));

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

        public long DeadlineMs
        {
            set
            {
                Timeout = (int)DateTimeOffset.FromUnixTimeMilliseconds(value).Subtract(DateTime.Now).TotalSeconds;
            }
        }

        public string Body
        {
            set
            {
                InputStream = new MemoryStream();
                var eventData = Encoding.UTF8.GetBytes(value);
                InputStream.Write(eventData, 0, eventData.Length);
                InputStream.Position = 0;
            }
        }

        public Stream InputStream { get; set; }

        public string RequestId { get; set; }
        public DateTime StartTime { get; set; }

        public int Timeout { get; set; }

        public int MemorySize { get; set; }

        public string FunctionName { get; set; }

        public string FunctionVersion { get; set; }

        public string LogGroup { get; set; }

        public string LogStream { get; set; }

        public string Region { get; set; }

        public string AccountId { get; set; }

        public string Arn { get; set; }

        public bool StayOpen { get; }

        string RandomLogStreamName => $"{DateTime.Now.ToString("yyyy/MM/dd")}/[{FunctionVersion}]{random.Next().ToString("x") + random.Next().ToString("x")}";
    }
}
