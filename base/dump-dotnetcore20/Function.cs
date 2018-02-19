using System;   
using System.Diagnostics;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.S3;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace dump_dotnetcore20
{
    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }

    public class Function
    {
        IAmazonS3 S3Client { get; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function() => S3Client = new AmazonS3Client();

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client) => S3Client = s3Client;

        /// <summary>
        /// Lambda function to dump the container directories /var/lang 
        /// and /var/runtime and upload the resulting archive to S3
        /// </summary>
        /// <returns></returns>
        public async Task<string> FunctionHandler()
        {
            var environment =  Environment.GetEnvironmentVariables();
            foreach(var env in environment.Keys){
                    Console.WriteLine($"{env}:{environment[env]}");
            }

            string filename = "dotnetcore2.0.tgz";
            string cmd = $"tar -cpzf /tmp/{filename} --numeric-owner --ignore-failed-read /var/runtime /var/lang";

            cmd.Bash();

            Console.WriteLine("Zipping done! Uploading...");
            await S3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest{
                BucketName="lambci",
                Key=$"fs/{filename}",
                FilePath=$"/tmp/{filename}"
            });
            return "OK";

        }
    }
}
