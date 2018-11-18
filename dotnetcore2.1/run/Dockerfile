FROM microsoft/dotnet:2.1-sdk
WORKDIR /source

# cache restore result
COPY MockBootstraps/*.csproj .
RUN dotnet restore

# copy the rest of the code
COPY MockBootstraps/ .
RUN dotnet publish --output /app/ --configuration Release


FROM lambci/lambda-base

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_dotnetcore2.1

RUN rm -rf /var/runtime /var/lang && \
  curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.1.tgz | tar -zx -C /

COPY --from=0 /app/MockBootstraps.* /var/runtime/

ENTRYPOINT ["/var/lang/bin/dotnet", "/var/runtime/MockBootstraps.dll"]
