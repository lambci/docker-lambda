FROM lambci/lambda-base:build

# Add these as a separate layer as they get updated frequently
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.1.0 && \
  pipx install aws-sam-cli==1.3.0
