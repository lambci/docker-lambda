FROM microsoft/aspnetcore-build AS builder
WORKDIR /source

# caches restore result by copying csproj file separately
COPY MockBootstraps/*.csproj .
RUN dotnet restore

# copies the rest of your code
COPY MockBootstraps/ .
RUN dotnet publish --output /app/ --configuration Release


FROM lambci/lambda-base

RUN rm -rf /var/runtime /var/lang && \
  curl https://s3.eu-central-1.amazonaws.com/dockerlambda/dotnet2.tgz | tar -zx -C /


COPY --from=0 /app/MockBootstraps.* /var/runtime/

ENTRYPOINT ["/var/lang/bin/dotnet", "/var/runtime/MockBootstraps.dll"]