FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python3 && \
  pip install -U awscli boto3 aws-sam-cli==0.31.0 aws-lambda-builders==0.5.0 --no-cache-dir
