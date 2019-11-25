#!/bin/sh

curl https://lambci.s3.amazonaws.com/fs/base.tgz | tar -xz --strip-components=2 -- var/lib/rpm

docker pull amazonlinux:1
docker run -v "$PWD/rpm":/rpm --rm amazonlinux:1 rpm -qa --dbpath /rpm | grep -v ^gpg-pubkey- | sort > packages.txt
rm -rf rpm

docker run --rm amazonlinux:1 bash -c 'yum upgrade -y > /dev/null && rpm -qa' | grep -v ^gpg-pubkey- | sort > amazonlinux1.txt

diff -w -d amazonlinux1.txt packages.txt
