FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U awscli boto3 aws-sam-cli aws-lambda-builders==0.0.4 --no-cache-dir
