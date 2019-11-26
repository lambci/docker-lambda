FROM lambci/lambda-base:build

# Run: docker run --rm --entrypoint dotnet lambci/lambda:dotnetcore2.1 --info
# Check https://dotnet.microsoft.com/download/dotnet-core/2.0 for versions
ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore2.0 \
    DOTNET_SDK_VERSION=2.1.202 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.0.tgz | tar -zx -C / && \
  yum install -y libunwind && \
  curl -L https://dot.net/v1/dotnet-install.sh | bash -s -- -v $DOTNET_SDK_VERSION -i /var/lang/bin && \
  mkdir /tmp/warmup && \
  cd /tmp/warmup && \
  dotnet new && \
  cd / && \
  rm -rf /tmp/warmup /tmp/NuGetScratch

# Add these as a separate layer as they get updated frequently
RUN pip install -U virtualenv pipenv awscli boto3 aws-sam-cli==0.33.1 aws-lambda-builders==0.6.0 --no-cache-dir

CMD ["dotnet", "build"]
