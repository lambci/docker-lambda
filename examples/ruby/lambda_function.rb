require 'pp'

# docker run --rm -v "$PWD":/var/task lambci/lambda:ruby2.5 lambda_function.lambda_handler

def lambda_handler(event:, context:)
  info = {
    'event' => event,
    'ENV' => ENV.to_hash,
    'context' => context.instance_variables.each_with_object({}) { |k, h| h[k] = context.instance_variable_get k },
    'ps aux' => `ps aux`,
    'proc environ' => `xargs -n 1 -0 < /proc/1/environ`,
  }

  pp info

  return info
end
