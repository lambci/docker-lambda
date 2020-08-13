FROM lambci/lambda-base-2:build

# Run: docker run --rm --entrypoint dotnet lambci/lambda:dotnetcore3.1 --info
# Check https://dotnet.microsoft.com/download/dotnet-core/3.1 for versions
ENV DOTNET_ROOT=/var/lang/bin
ENV PATH=/root/.dotnet/tools:$DOTNET_ROOT:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore3.1 \
    DOTNET_SDK_VERSION=3.1.401 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/dotnetcore3.1.tgz | tar -zx -C / && \
  curl -L https://dot.net/v1/dotnet-install.sh | bash -s -- -v $DOTNET_SDK_VERSION -i $DOTNET_ROOT && \
  mkdir /tmp/warmup && \
  cd /tmp/warmup && \
  dotnet new && \
  cd / && \
  rm -rf /tmp/warmup /tmp/NuGetScratch /tmp/.dotnet

# Add these as a separate layer as they get updated frequently
RUN pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.0.0 && \
  pipx install aws-sam-cli==1.1.0 && \
  dotnet tool install --global Amazon.Lambda.Tools --version 4.0.0

CMD ["dotnet", "build"]
