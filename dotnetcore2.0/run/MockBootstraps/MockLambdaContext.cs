using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MockLambdaRuntime.Attributes;

namespace MockLambdaRuntime
{
    /// Lambda context definition
    public class MockLambdaContext
    {
        /// Creates a mock context from a given Lambda event
        public MockLambdaContext(string eventBody)
        {
            RequestId = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            InputStream = new MemoryStream();
            OutputStream = new MemoryStream();

            var eventData = Encoding.UTF8.GetBytes(eventBody);
            InputStream.Write(eventData, 0, eventData.Length);
            InputStream.Position = 0;

            var env = Environment.GetEnvironmentVariables();
            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                var attributes = propertyInfo.GetCustomAttributes(typeof(EnvMappingAttribute), false);
                foreach (var mappingAttribute in attributes.Cast<EnvMappingAttribute>())
                {
                    var value = env[mappingAttribute.Key] ?? mappingAttribute.DefaultValue;
                    propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType));
                }
            }
        }

        /// Calculates the remaining time using current time and timeout
        public TimeSpan RemainingTime()
        {
            return StartTime + TimeSpan.FromSeconds(Timeout) - DateTime.Now;
        }

        public long Duration => (long)(DateTime.Now - StartTime).TotalMilliseconds;
        public long BilledDuration => (long)(Math.Ceiling((DateTime.Now - StartTime).TotalMilliseconds / 100)) * 100;

        public long MemoryUsed => Process.GetCurrentProcess().WorkingSet64;

        /// The arn for the lambda function
        public string Arn => $"arn:aws:lambda:{Region}:{AccountId}:function:{FunctionName}";

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

        [EnvMapping("AWS_ACCOUNT_ID")]
        public string AccountId { get; private set; }

        [EnvMapping("AWS_REGION")]
        public string Region { get; private set; }

        [EnvMapping("AWS_ACCESS_KEY")]
        public string AccessKey { get; private set; }

        [EnvMapping("AWS_SECRET_KEY")]
        public string SecretKey { get; private set; }

        [EnvMapping("AWS_SESSION_TOKEN")]
        public string SessionToken { get; private set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_NAME")]
        public string FunctionName { get; private set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_VERSION")]
        public string FunctionVersion { get; private set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_TIMEOUT", "300")]
        public int Timeout { get; private set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536")]
        public int MemorySize { get; private set; }
    }
}
