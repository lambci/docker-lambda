FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:/usr/local/bin:/usr/bin/:/bin \
    LD_LIBRARY_PATH=/var/lang/lib:/lib64:/usr/lib64:/var/runtime:/var/runtime/lib:/var/task:/var/task/lib \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore2.0 \
    DOTNET_SDK_VERSION=2.1.200 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.0.tgz | tar -zx -C / && \
  yum install -y libunwind && \
  curl https://dot.net/v1/dotnet-install.sh | bash -s -- -v $DOTNET_SDK_VERSION -i /var/lang/bin && \
  mkdir /tmp/warmup && \
  cd /tmp/warmup && \
  dotnet new && \
  cd / && \
  rm -rf /tmp/warmup /tmp/NuGetScratch

CMD ["dotnet", "build"]
