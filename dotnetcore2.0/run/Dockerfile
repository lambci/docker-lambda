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
  curl 'https://hm0s1q-db3pap001.files.1drv.com/y4mUBe_7LPKlEdvZRR8JKW44PuCG75m3w0N8eIHQy-SjfHdHlNCejlhjZE10BNL1aA-HT7I0-JDXB4w4XMQ5IHKHgT2oLCUjDw4itIcZBVb6VYEjdzVurzlRGgz0YsPOPnZMwQjxDX7EVShce6WXyMZSM6KblGfxg8FF62uZgtfd-2_hY2co3_OFVAhpQD2UaNrSh8GqESiaqEGrrnDxHJaQg/dotnet2.tgz?download&psid=1' | tar -zx -C /


COPY --from=0 /app/MockBootstraps.* /var/runtime/

ENTRYPOINT ["/var/lang/bin/dotnet", "/var/runtime/MockBootstraps.dll"]