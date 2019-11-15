FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
RUN pip install -U awscli boto3 aws-sam-cli==0.31.1 aws-lambda-builders==0.5.0 --no-cache-dir
