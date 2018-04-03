FROM lambci/lambda:nodejs8.10

ENV AWS_LAMBDA_FUNCTION_NAME=docker-build \
    AWS_LAMBDA_FUNCTION_VERSION=2 \
    AWS_LAMBDA_FUNCTION_MEMORY_SIZE=384 \
    AWS_LAMBDA_FUNCTION_TIMEOUT=60 \
    AWS_REGION=us-east-1

COPY . .

# If we want to match permissions in /var/task exactly...
USER root
RUN chown -R slicer:497 .
USER sbx_user1051

