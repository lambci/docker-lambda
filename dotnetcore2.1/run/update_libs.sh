#!/bin/sh

curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.1.tgz | \
  tar -xz var/runtime/Amazon.Lambda.Core.dll var/runtime/Bootstrap.dll

mv ./var/runtime/*.dll ./MockBootstraps/lib/
rm -rf ./var
