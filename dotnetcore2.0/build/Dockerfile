FROM lambci/lambda:dotnetcore2.0

FROM lambci/lambda-base:build

# Run: docker run --rm --entrypoint dotnet lambci/lambda:dotnetcore2.1 --info
# Check https://dotnet.microsoft.com/download/dotnet-core/2.0 for versions
ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore2.0 \
    DOTNET_SDK_VERSION=2.1.202 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

COPY --from=0 /var/runtime /var/runtime
COPY --from=0 /var/lang /var/lang
COPY --from=0 /var/rapid /var/rapid

RUN yum install -y libunwind && \
  curl -L https://dot.net/v1/dotnet-install.sh | bash -s -- -v $DOTNET_SDK_VERSION -i /var/lang/bin && \
  mkdir /tmp/warmup && \
  cd /tmp/warmup && \
  dotnet new && \
  cd / && \
  rm -rf /tmp/warmup /tmp/NuGetScratch

# Add these as a separate layer as they get updated frequently
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install virtualenv && \
  pipx install pipenv && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.1.0 && \
  pipx install aws-sam-cli==1.11.0

CMD ["dotnet", "build"]
