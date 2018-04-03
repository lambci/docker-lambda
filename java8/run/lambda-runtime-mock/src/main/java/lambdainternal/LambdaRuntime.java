package lambdainternal;

import java.lang.reflect.Field;
import java.math.BigInteger;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.Collections;
import java.util.Date;
import java.util.Map;
import java.util.Random;
import java.util.Scanner;
import java.util.UUID;

import sun.misc.Unsafe;

public class LambdaRuntime {
    private static Unsafe unsafe;

    private static final String INVOKE_ID = UUID.randomUUID().toString();
    private static final String AWS_ACCESS_KEY_ID;
    private static final String AWS_SECRET_ACCESS_KEY;
    private static final String AWS_SESSION_TOKEN;
    private static final String AWS_REGION;
    private static final String HANDLER;
    private static final String EVENT_BODY;
    private static final int TIMEOUT;
    private static final String X_AMZN_TRACE_ID;
    private static final String CLIENT_CONTEXT = null;
    private static final String COGNITO_IDENTITY_ID = "";
    private static final String COGNITO_IDENTITY_POOL_ID = "";
    private static final String FUNCTION_ARN;
    private static final String ACCOUNT_ID;
    private static boolean alreadyInvoked = false;
    private static long invokeStart;

    public static final int MEMORY_LIMIT;
    public static final String LOG_GROUP_NAME;
    public static final String LOG_STREAM_NAME;
    public static final String FUNCTION_NAME;
    public static final String FUNCTION_VERSION;
    public static volatile boolean needsDebugLogs;

    static {
        try {
            Field field = Unsafe.class.getDeclaredField("theUnsafe");
            field.setAccessible(true);
            unsafe = (Unsafe) field.get(null);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }

        TIMEOUT = Integer.parseInt(getEnvOrDefault("AWS_LAMBDA_FUNCTION_TIMEOUT", "300"));
        MEMORY_LIMIT = Integer.parseInt(getEnvOrDefault("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"));
        FUNCTION_NAME = getEnvOrDefault("AWS_LAMBDA_FUNCTION_NAME", "test");
        FUNCTION_VERSION = getEnvOrDefault("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST");
        LOG_GROUP_NAME = getEnvOrDefault("AWS_LAMBDA_LOG_GROUP_NAME", "/aws/lambda/" + FUNCTION_NAME);
        LOG_STREAM_NAME = getEnvOrDefault("AWS_LAMBDA_LOG_STREAM_NAME", randomLogStreamName(FUNCTION_VERSION));
        AWS_ACCESS_KEY_ID = getEnvOrDefault("AWS_ACCESS_KEY_ID", "SOME_ACCESS_KEY_ID");
        AWS_SECRET_ACCESS_KEY = getEnvOrDefault("AWS_SECRET_ACCESS_KEY", "SOME_SECRET_ACCESS_KEY");
        AWS_SESSION_TOKEN = getEnv("AWS_SESSION_TOKEN");
        AWS_REGION = getEnvOrDefault("AWS_REGION", getEnvOrDefault("AWS_DEFAULT_REGION", "us-east-1"));
        ACCOUNT_ID = getEnvOrDefault("AWS_ACCOUNT_ID", "000000000000");
        FUNCTION_ARN = "arn:aws:lambda:" + AWS_REGION + ":" + ACCOUNT_ID + ":function:" + FUNCTION_NAME;
        X_AMZN_TRACE_ID = getEnvOrDefault("_X_AMZN_TRACE_ID", "");

        String[] args = getCmdLineArgs();
        HANDLER = args.length > 1 ? args[1] : getEnvOrDefault("AWS_LAMBDA_FUNCTION_HANDLER", getEnvOrDefault("_HANDLER", "index.Handler"));
        EVENT_BODY = args.length > 2 ? args[2] : getEventBody();

        LambdaRuntime.needsDebugLogs = false;

        setenv("AWS_LAMBDA_FUNCTION_NAME", FUNCTION_NAME, 1);
        setenv("AWS_LAMBDA_FUNCTION_VERSION", FUNCTION_VERSION, 1);
        setenv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", Integer.toString(MEMORY_LIMIT), 1);
        setenv("AWS_LAMBDA_LOG_GROUP_NAME", LOG_GROUP_NAME, 1);
        setenv("AWS_LAMBDA_LOG_STREAM_NAME", LOG_STREAM_NAME, 1);
        setenv("AWS_REGION", AWS_REGION, 1);
        setenv("AWS_DEFAULT_REGION", AWS_REGION, 1);
        setenv("_HANDLER", HANDLER, 1);
    }

    private static String getEventBody() {
        String eventBody = getEnv("AWS_LAMBDA_EVENT_BODY");
        if (eventBody == null) {
            eventBody = getEnv("DOCKER_LAMBDA_USE_STDIN") != null ?
                new Scanner(System.in).useDelimiter("\\A").next() : "{}";
        }
        return eventBody;
    }

    private static String getEnvOrDefault(String key, String defaultVal) {
        String envVal = getEnv(key);
        return envVal != null ? envVal : defaultVal;
    }

    private static String randomLogStreamName(String functionVersion) {
        byte[] randomBuf = new byte[16];
        new Random().nextBytes(randomBuf);
        return String.format("%s/[%s]%016x", new SimpleDateFormat("yyyy/MM/dd").format(new Date()), functionVersion,
                new BigInteger(1, randomBuf));
    }

    private static void systemLog(String str) {
        System.err.println("\033[32m" + str + "\033[0m");
    }

    private static void systemErr(String str) {
        System.err.println("\033[31m" + str + "\033[0m");
    }

    public static String getEnv(final String envVariableName) {
        return System.getenv(envVariableName);
    }

    public static void initRuntime() {
    }

    public static void reportRunning(final String p0) {
    }

    public static void reportDone(final String invokeid, final byte[] result, final int resultLength, final int p3) {
        if (!alreadyInvoked) {
            return;
        }
        double durationMs = (System.nanoTime() - invokeStart) / 1_000_000d;
        long billedMs = Math.min(100 * ((long) Math.floor(durationMs / 100) + 1), TIMEOUT * 1000);
        long maxMemory = Math.round((Runtime.getRuntime().totalMemory() -
                Runtime.getRuntime().freeMemory()) / (1024 * 1024));
        systemLog("END RequestId: " + invokeid);
        systemLog(String.join("\t",
                "REPORT RequestId: " + invokeid,
                "Duration: " + String.format("%.2f", durationMs) + " ms",
                "Billed Duration: " + billedMs + " ms",
                "Memory Size: " + MEMORY_LIMIT + " MB",
                "Max Memory Used: " + maxMemory + " MB",
                ""));
        if (result != null) {
            System.out.println("\n" + new String(result, 0, resultLength));
        }
    }

    public static void reportException(final String p0) {
    }

    public static void reportUserInitStart() {
    }

    public static void reportUserInitEnd() {
    }

    public static void reportUserInvokeStart() {
    }

    public static void reportUserInvokeEnd() {
    }

    public static void reportFault(final String invokeid, final String msg, final String exceptionClass,
            final String stack) {
        systemErr(stack);
    }

    public static void setenv(final String key, final String val, final int p2) {
        getMutableEnv().put(key, val);
    }

    public static void unsetenv(final String key) {
        getMutableEnv().remove(key);
    }

    public static WaitForStartResult waitForStart() {
        return new WaitForStartResult(INVOKE_ID, HANDLER, "event", AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY,
                AWS_SESSION_TOKEN, false);
    }

    public static InvokeRequest waitForInvoke() {
        if (alreadyInvoked) {
            System.exit(0);
        }
        alreadyInvoked = true;
        long address = 0;
        byte[] eventBodyBytes = EVENT_BODY.getBytes(StandardCharsets.UTF_8);
        try {
            address = unsafe.allocateMemory(eventBodyBytes.length);
            for (int i = 0; i < eventBodyBytes.length; i++) {
                unsafe.putByte(address + i, eventBodyBytes[i]);
            }
        } catch (Exception e) {
            // Not sure, could happen if memory is exhausted?
            throw new RuntimeException(e);
        }
        invokeStart = System.nanoTime();
        systemLog("START RequestId: " + INVOKE_ID + " Version: " + FUNCTION_VERSION);
        return new InvokeRequest(-1, INVOKE_ID, X_AMZN_TRACE_ID, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY,
                AWS_SESSION_TOKEN, CLIENT_CONTEXT, COGNITO_IDENTITY_ID, COGNITO_IDENTITY_POOL_ID, address,
                eventBodyBytes.length, false, FUNCTION_ARN);
    }

    public static int getRemainingTime() {
        return (int) ((TIMEOUT * 1000) - Math.round((System.nanoTime() - invokeStart) / 1_000_000d));
    }

    public static void sendContextLogs(final byte[] msg, final int length) {
        System.err.print(new String(msg, 0, length, StandardCharsets.UTF_8));
    }

    public static synchronized void streamLogsToSlicer(final byte[] p0, final int p1, final int p2) {
    }

    private static String[] getCmdLineArgs() {
        return System.getProperty("sun.java.command").split(" ", 3);
    }

    private static Map<String, String> getMutableEnv() {
        Class[] classes = Collections.class.getDeclaredClasses();
        Map<String, String> env = System.getenv();
        for (Class cl : classes) {
            if ("java.util.Collections$UnmodifiableMap".equals(cl.getName())) {
                try {
                    Field field = cl.getDeclaredField("m");
                    field.setAccessible(true);
                    Object obj = field.get(env);
                    return (Map<String, String>) obj;
                } catch (Exception e) {
                    // Should never happen on Lambda
                    throw new RuntimeException(e);
                }
            }
        }
        // Should never happen on Lambda
        throw new RuntimeException("Could not find java.util.Collections$UnmodifiableMap class");
    }

    public static class AWSCredentials {
        public final String key;
        public final String secret;
        public final String session;

        public AWSCredentials(final String key, final String secret, final String session) {
            this.key = key;
            this.secret = secret;
            this.session = session;
        }
    }

    public static class InvokeRequest {
        public final int sockfd;
        public final String invokeid;
        public final String xAmznTraceId;
        public final AWSCredentials credentials;
        public final String clientContext;
        public final String cognitoIdentityId;
        public final String cognitoPoolId;
        public final long eventBodyAddr;
        public final int eventBodyLen;
        public final boolean needsDebugLogs;
        public final String invokedFunctionArn;

        public InvokeRequest(final int sockfd, final String invokeid, final String xAmznTraceId, final String awskey,
                final String awssecret, final String awssession, final String clientcontext,
                final String cognitoidentityid, final String cognitopoolid, final long addr, final int len,
                final boolean needsDebugLogs, final String invokedFunctionArn) {
            this.sockfd = sockfd;
            this.invokeid = invokeid;
            this.xAmznTraceId = xAmznTraceId;
            this.eventBodyAddr = addr;
            this.eventBodyLen = len;
            this.clientContext = clientcontext;
            this.cognitoIdentityId = cognitoidentityid;
            this.cognitoPoolId = cognitopoolid;
            this.credentials = new AWSCredentials(awskey, awssecret, awssession);
            this.needsDebugLogs = needsDebugLogs;
            this.invokedFunctionArn = invokedFunctionArn;
        }
    }

    public static class WaitForStartResult {
        public final String invokeid;
        public final String handler;
        public final String mode;
        public final AWSCredentials credentials;
        public final boolean suppressInit;

        public WaitForStartResult(final String invokeid, final String handler, final String mode, final String awskey,
                final String awssecret, final String awssession, final boolean suppressInit) {
            this.invokeid = invokeid;
            this.handler = handler;
            this.mode = mode;
            this.credentials = new AWSCredentials(awskey, awssecret, awssession);
            this.suppressInit = suppressInit;
        }
    }
}
