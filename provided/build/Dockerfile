FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
RUN pip install -U aws-lambda-builders==0.9.0 aws-sam-cli==0.53.0 awscli boto3 --no-cache-dir
