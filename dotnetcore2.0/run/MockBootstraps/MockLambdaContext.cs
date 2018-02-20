using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MockLambdaRuntime.Attributes;

namespace MockLambdaRuntime
{
    /// <summary>
    /// Lambda context definition
    /// </summary>
    public class MockLambdaContext
    {
        private MockLambdaContext()
        {
            RequestId = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            OutputStream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockLambdaContext"/> class.
        /// Uses the data from the environment variables
        /// </summary>
        public MockLambdaContext(string eventBody, IDictionary environment) : this()
        {
            EventBody = eventBody;
            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                var attributes = propertyInfo.GetCustomAttributes(typeof(EnvMappingAttribute), false);
                foreach (var mappingAttribute in attributes.Cast<EnvMappingAttribute>())
                {
                    var value = environment[mappingAttribute.Key] ?? mappingAttribute.DefaultValue;
                    propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType));
                }
            }
            InputStream = new MemoryStream();
            var contextData = Encoding.UTF8.GetBytes(EventBody);
            InputStream.Write(contextData, 0, contextData.Length);
            InputStream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Calculates the remaining time using current time and timeout
        /// </summary>
        public TimeSpan RemainingTime()
        {
            return StartTime + TimeSpan.FromSeconds(Timeout) - DateTime.Now;
        }

        public long Duration => (long)(DateTime.Now - StartTime).TotalMilliseconds;
        public long BilledDuration => (long)(Math.Ceiling((DateTime.Now - StartTime).TotalMilliseconds / 100)) * 100;

        public long MemoryUsed => Process.GetCurrentProcess().WorkingSet64;

        /// <summary>
        /// Gets the arn for the lambda function
        /// </summary>
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

        public string RequestId { get; set; }
        public string EventBody { get; set; }
        public DateTime StartTime { get; set; }

        [EnvMapping("AWS_ACCOUNT_ID")]
        public string AccountId { get; set; }

        [EnvMapping("AWS_REGION")]
        public string Region { get; set; }

        [EnvMapping("AWS_ACCESS_KEY")]
        public string AccessKey { get; set; }

        [EnvMapping("AWS_SECRET_KEY")]
        public string SecretKey { get; set; }

        [EnvMapping("AWS_SESSION_TOKEN")]
        public string SessionToken { get; set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_NAME")]
        public string FunctionName { get; set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_VERSION")]
        public string FunctionVersion { get; set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_TIMEOUT", "300")]
        public int Timeout { get; set; }

        [EnvMapping("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536")]
        public int MemorySize { get; set; }
    }
}
