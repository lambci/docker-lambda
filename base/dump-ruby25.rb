require 'json'
require 'aws-sdk-s3'

S3_CLIENT = Aws::S3::Client.new({region: "us-east-1"})

def lambda_handler(event:, context:)
  filename = 'ruby2.5.tgz'

  puts `tar -cpzf /tmp/#{filename} --numeric-owner --ignore-failed-read /var/runtime /var/lang /var/rapid`

  File.open("/tmp/#{filename}", 'rb') do |file|
    S3_CLIENT.put_object({
      body: file,
      bucket: 'lambci',
      key: "fs/#{filename}",
      acl: 'public-read',
    })
  end

  info = {
    'ENV' => ENV.to_hash,
    'context' => context.instance_variables.each_with_object({}) { |k, h| h[k] = context.instance_variable_get k },
    'ps aux' => `ps aux`,
    'proc environ' => `xargs -n 1 -0 < /proc/1/environ`,
  }

  print JSON.pretty_generate(info)

  return info
end

