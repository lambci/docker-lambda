# Java 8 Build Instructions

As the Java 8 Lambda libraries are statically compiled into jars, it's not
possible to just swap in a mock interface source file, as it is in the other
dynamic runtimes, without compiling first.

The `Dockerfile` here will build using a patched `LambdaSandboxJava-1.0.jar`,
which is checked into git. This jar was built using the jar from the Lambda runtime,
`/var/runtime/lib/LambdaSandboxJava-1.0.jar`, with a single class
(`lambdainternal/LambdaRuntime.class`) updated to use local/mock methods
instead of native ones.

The build script to perform this patch/update is at
[./lambda-runtime-mock/build.sh](./lambda-runtime-mock/build.sh)
