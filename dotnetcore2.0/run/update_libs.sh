#!/bin/sh

curl https://lambci.s3.amazonaws.com/fs/dotnetcore2.0.tgz | \
  tar -xz var/runtime/Amazon.Lambda.Core.dll var/runtime/Bootstrap.dll var/runtime/Bootstrap.pdb

mv ./var/runtime/*.dll ./var/runtime/*.pdb ./MockBootstraps/lib/
rm -rf ./var
