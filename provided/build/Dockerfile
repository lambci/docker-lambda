FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
RUN pip install -U awscli boto3 aws-sam-cli==0.42.0 aws-lambda-builders==0.7.0 --no-cache-dir
