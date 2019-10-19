#!/bin/sh

cd $(dirname "$0")

curl -s https://lambci.s3.amazonaws.com/fs/java8.tgz | tar -zx -- var/runtime/lib

mv var/runtime/lib/LambdaSandboxJava-1.0.jar var/runtime/lib/gson-*.jar ./

mkdir -p ./target/classes

javac -target 1.8 -cp ./gson-*.jar -d ./target/classes ./src/main/java/lambdainternal/LambdaRuntime.java

cp -R ./target/classes/lambdainternal ./

jar uf LambdaSandboxJava-1.0.jar lambdainternal/LambdaRuntime*.class

rm -rf ./var ./lambdainternal
