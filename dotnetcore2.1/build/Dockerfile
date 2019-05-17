FROM lambci/lambda-base:build

# Check https://dotnet.microsoft.com/download/dotnet-core/2.1 for versions
ENV DOTNET_ROOT=/var/lang/bin
ENV PATH=/root/.dotnet/tools:$DOTNET_ROOT:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore2.1 \
    DOTNET_SDK_VERSION=2.1.603 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.1.tgz | tar -zx -C / && \
  yum install -y libunwind && \
  curl https://dot.net/v1/dotnet-install.sh | bash -s -- -v $DOTNET_SDK_VERSION -i $DOTNET_ROOT && \
  mkdir /tmp/warmup && \
  cd /tmp/warmup && \
  dotnet new && \
  cd / && \
  rm -rf /tmp/warmup /tmp/NuGetScratch /tmp/.dotnet

# Add these as a separate layer as they get updated frequently
RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U virtualenv pipenv awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir && \
  dotnet tool install --global Amazon.Lambda.Tools --version 3.2.3

CMD ["dotnet", "build"]
