// Run with:
// docker run --rm -v "$PWD/build/docker":/var/task lambci/lambda:java8 org.lambci.lambda.ExampleHandler

package org.lambci.lambda;

import java.io.File;
import java.lang.management.ManagementFactory;
import java.util.Map;

import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.LambdaLogger;
import com.amazonaws.services.lambda.runtime.RequestHandler;

public class ExampleHandler implements RequestHandler<Object, String> {

    @Override
    public String handleRequest(Object input, Context context) {
        // throw new RuntimeException("whatever");
        LambdaLogger logger = context.getLogger();
        logger.log(ManagementFactory.getRuntimeMXBean().getInputArguments().toString() + "\n");
        logger.log(System.getProperty("sun.java.command") + "\n");
        logger.log(System.getProperty("java.home") + "\n");
        logger.log(System.getProperty("java.library.path") + "\n");
        logger.log(System.getProperty("user.dir") + "\n");
        logger.log(System.getProperty("user.home") + "\n");
        logger.log(System.getProperty("user.name") + "\n");
        logger.log(new File(".").getAbsolutePath() + "\n");
        Map<String, String> env = System.getenv();
        for (String envName : env.keySet()) {
            logger.log(envName + "=" + env.get(envName) + "\n");
        }
        logger.log(context.getAwsRequestId() + "\n");
        logger.log(context.getFunctionName() + "\n");
        logger.log(context.getFunctionVersion() + "\n");
        logger.log(context.getInvokedFunctionArn() + "\n");
        logger.log(context.getLogGroupName() + "\n");
        logger.log(context.getLogStreamName() + "\n");
        logger.log(context.getIdentity().getIdentityId() + "\n");
        logger.log(context.getIdentity().getIdentityPoolId() + "\n");
        logger.log(context.getClientContext() + "\n");
        logger.log(context.getMemoryLimitInMB() + "\n");
        logger.log(context.getRemainingTimeInMillis() + "\n");
        logger.log(input + "\n");
        return "Hello World!";
    }
}
