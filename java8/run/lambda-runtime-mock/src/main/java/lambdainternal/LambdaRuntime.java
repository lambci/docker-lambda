package lambdainternal;

import java.io.ByteArrayOutputStream;
import java.io.OutputStream;
import java.io.PrintStream;
import java.lang.ProcessBuilder.Redirect;
import java.lang.reflect.Field;
import java.math.BigInteger;
import java.net.ConnectException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Base64;
import java.util.Date;
import java.util.Map;
import java.util.Random;
import java.util.Scanner;
import java.util.UUID;

import com.google.gson.Gson;

import sun.misc.Unsafe;

@SuppressWarnings("restriction")
public class LambdaRuntime {
    private static Unsafe unsafe;

    private static final String API_BASE = "http://127.0.0.1:9001/2018-06-01";
    private static final boolean STAY_OPEN = !isNullOrEmpty(getEnv("DOCKER_LAMBDA_STAY_OPEN"));
    private static final String INVOKE_ID = UUID.randomUUID().toString();
    private static final String AWS_ACCESS_KEY_ID;
    private static final String AWS_SECRET_ACCESS_KEY;
    private static final String AWS_SESSION_TOKEN;
    private static final String AWS_REGION;
    private static final String HANDLER;
    private static final String EVENT_BODY;
    private static final PrintStream ORIG_STDERR = System.err;
    private static final ByteArrayOutputStream LOGS = new ByteArrayOutputStream();
    private static long deadlineMs;
    private static boolean invoked = false;
    private static boolean errored = false;

    public static final int MEMORY_LIMIT;
    public static final String LOG_GROUP_NAME;
    public static final String LOG_STREAM_NAME;
    public static final String FUNCTION_NAME;
    public static final String FUNCTION_VERSION;
    public static volatile boolean needsDebugLogs = false;

    static {
        try {
            Field field = Unsafe.class.getDeclaredField("theUnsafe");
            field.setAccessible(true);
            unsafe = (Unsafe) field.get(null);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }

        deadlineMs = System.currentTimeMillis()
                + (1000 * Long.parseLong(getEnvOrDefault("AWS_LAMBDA_FUNCTION_TIMEOUT", "300")));
        MEMORY_LIMIT = Integer.parseInt(getEnvOrDefault("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", "1536"));
        FUNCTION_NAME = getEnvOrDefault("AWS_LAMBDA_FUNCTION_NAME", "test");
        FUNCTION_VERSION = getEnvOrDefault("AWS_LAMBDA_FUNCTION_VERSION", "$LATEST");
        LOG_GROUP_NAME = getEnvOrDefault("AWS_LAMBDA_LOG_GROUP_NAME", "/aws/lambda/" + FUNCTION_NAME);
        LOG_STREAM_NAME = getEnvOrDefault("AWS_LAMBDA_LOG_STREAM_NAME", randomLogStreamName(FUNCTION_VERSION));
        AWS_ACCESS_KEY_ID = getEnvOrDefault("AWS_ACCESS_KEY_ID", "SOME_ACCESS_KEY_ID");
        AWS_SECRET_ACCESS_KEY = getEnvOrDefault("AWS_SECRET_ACCESS_KEY", "SOME_SECRET_ACCESS_KEY");
        AWS_SESSION_TOKEN = getEnv("AWS_SESSION_TOKEN");
        AWS_REGION = getEnvOrDefault("AWS_REGION", getEnvOrDefault("AWS_DEFAULT_REGION", "us-east-1"));

        String[] args = getCmdLineArgs();
        HANDLER = args.length > 1 ? args[1]
                : getEnvOrDefault("AWS_LAMBDA_FUNCTION_HANDLER", getEnvOrDefault("_HANDLER", "index.Handler"));
        EVENT_BODY = args.length > 2 ? args[2] : getEventBody();

        setenv("AWS_LAMBDA_FUNCTION_NAME", FUNCTION_NAME, 1);
        setenv("AWS_LAMBDA_FUNCTION_VERSION", FUNCTION_VERSION, 1);
        setenv("AWS_LAMBDA_FUNCTION_MEMORY_SIZE", Integer.toString(MEMORY_LIMIT), 1);
        setenv("AWS_LAMBDA_LOG_GROUP_NAME", LOG_GROUP_NAME, 1);
        setenv("AWS_LAMBDA_LOG_STREAM_NAME", LOG_STREAM_NAME, 1);
        setenv("AWS_REGION", AWS_REGION, 1);
        setenv("AWS_DEFAULT_REGION", AWS_REGION, 1);
        setenv("_HANDLER", HANDLER, 1);

        try {
            ProcessBuilder pb = new ProcessBuilder("/var/runtime/mockserver").redirectInput(Redirect.PIPE)
                    .redirectOutput(Redirect.INHERIT).redirectError(Redirect.INHERIT);
            Map<String, String> mockEnv = pb.environment();
            mockEnv.put("DOCKER_LAMBDA_NO_BOOTSTRAP", "1");
            mockEnv.put("DOCKER_LAMBDA_USE_STDIN", "1");
            Process mockServer = pb.start();
            mockServer.getOutputStream().write(EVENT_BODY.getBytes(StandardCharsets.UTF_8));
            mockServer.getOutputStream().close();
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    public static void initRuntime() {
        for (int i = 0; i < 20; i++) {
            try {
                HttpURLConnection conn = (HttpURLConnection) new URL(API_BASE + "/ping").openConnection();
                int responseCode = conn.getResponseCode();
                if (responseCode != 200) {
                    throw new RuntimeException("Unexpected status code from ping: " + responseCode);
                }
                break;
            } catch (Exception e) {
                if (i < 19)
                    continue;
                throw new RuntimeException(e);
            }
        }
    }

    public static WaitForStartResult waitForStart() {
        if (!STAY_OPEN) {
            System.setOut(ORIG_STDERR);
            System.setErr(ORIG_STDERR);
        }
        return new WaitForStartResult(INVOKE_ID, HANDLER, "event", AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY,
                AWS_SESSION_TOKEN, true);
    }

    public static InvokeRequest waitForInvoke() {
        invoked = true;
        try {
            HttpURLConnection conn = (HttpURLConnection) new URL(API_BASE + "/runtime/invocation/next")
                    .openConnection();
            try {
                int responseCode = conn.getResponseCode();
                if (responseCode != 200) {
                    throw new RuntimeException("Unexpected status code from invocation/next: " + responseCode);
                }
            } catch (ConnectException e) {
                System.exit(errored ? 1 : 0);
            }
            String requestId = conn.getHeaderField("Lambda-Runtime-Aws-Request-Id");
            deadlineMs = Long.parseLong(conn.getHeaderField("Lambda-Runtime-Deadline-Ms"));
            String functionArn = conn.getHeaderField("Lambda-Runtime-Invoked-Function-Arn");
            String xAmznTraceId = conn.getHeaderField("Lambda-Runtime-Trace-Id");
            String clientContext = conn.getHeaderField("Lambda-Runtime-Client-Context");
            String cognitoIdentity = conn.getHeaderField("Lambda-Runtime-Cognito-Identity");

            CognitoIdentity cognitoIdentityObj = new CognitoIdentity();
            if (!isNullOrEmpty(cognitoIdentity)) {
                cognitoIdentityObj = new Gson().fromJson(cognitoIdentity, CognitoIdentity.class);
            }

            needsDebugLogs = "Tail".equals(conn.getHeaderField("Docker-Lambda-Log-Type"));
            LOGS.reset();

            String responseBody = "";
            try (Scanner scanner = new Scanner(conn.getInputStream())) {
                responseBody = scanner.useDelimiter("\\A").next();
            }
            long eventBodyAddress = 0;
            byte[] eventBodyBytes = responseBody.getBytes(StandardCharsets.UTF_8);
            eventBodyAddress = unsafe.allocateMemory(eventBodyBytes.length);
            for (int i = 0; i < eventBodyBytes.length; i++) {
                unsafe.putByte(eventBodyAddress + i, eventBodyBytes[i]);
            }

            return new InvokeRequest(-1, requestId, xAmznTraceId, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY,
                    AWS_SESSION_TOKEN, clientContext, cognitoIdentityObj.identity_id,
                    cognitoIdentityObj.identity_pool_id, eventBodyAddress, eventBodyBytes.length, needsDebugLogs,
                    functionArn);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    public static void reportDone(final String invokeid, final byte[] result, final int resultLength,
            final int waitForExitFlag) {
        if (!invoked) {
            return;
        }
        String invokeType = errored ? "/error" : "/response";
        try {
            HttpURLConnection conn = (HttpURLConnection) new URL(
                    API_BASE + "/runtime/invocation/" + invokeid + invokeType).openConnection();
            conn.setRequestMethod("POST");
            conn.setDoOutput(true);

            byte[] logs = LOGS.toByteArray();
            if (logs.length > 0) {
                if (logs.length > 4096) {
                    logs = Arrays.copyOfRange(logs, logs.length - 4096, logs.length);
                }
                conn.setRequestProperty("Docker-Lambda-Log-Result", Base64.getEncoder().encodeToString(logs));
            }

            byte[] resultCopy = result == null ? new byte[0]
                    : new String(result, 0, resultLength).getBytes(StandardCharsets.UTF_8);
            try (OutputStream os = conn.getOutputStream()) {
                os.write(resultCopy);
            }
            int responseCode = conn.getResponseCode();
            if (responseCode != 202) {
                throw new RuntimeException("Unexpected status code from invocation/response: " + responseCode);
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    public static void reportFault(final String invokeid, final String msg, final String exceptionClass,
            final String stack) {
        errored = true;
        systemErr(stack);
    }

    public static int getRemainingTime() {
        return (int) (deadlineMs - System.currentTimeMillis());
    }

    public static void sendContextLogs(final byte[] msg, final int length) {
        (STAY_OPEN ? System.out : System.err).print(new String(msg, 0, length, StandardCharsets.UTF_8));
    }

    public static synchronized void streamLogsToSlicer(final byte[] msg, final int offset, final int length) {
        LOGS.write(msg, offset, length);
    }

    public static void reportRunning(final String invokeId) {
    }

    public static void reportException(final String xrayJsonException) {
    }

    public static void reportUserInitStart() {
    }

    public static void reportUserInitEnd() {
    }

    public static void reportUserInvokeStart() {
    }

    public static void reportUserInvokeEnd() {
    }

    public static void writeSandboxLog(String msg) {
    }

    public static String getEnv(final String key) {
        return System.getenv(key);
    }

    @SuppressWarnings("unchecked")
    public static void setenv(final String key, final String val, final int flag) {
        try {
            Map<String, String> env = System.getenv();
            Field field = env.getClass().getDeclaredField("m");
            field.setAccessible(true);
            ((Map<String, String>) field.get(env)).put(key, val);
            field.setAccessible(false);
        } catch (Exception e) {
            // Should never happen on Lambda
            throw new RuntimeException(e);
        }
    }

    private static String getEventBody() {
        String eventBody = getEnv("AWS_LAMBDA_EVENT_BODY");
        if (eventBody == null) {
            eventBody = getEnv("DOCKER_LAMBDA_USE_STDIN") != null ? new Scanner(System.in).useDelimiter("\\A").next()
                    : "{}";
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

    private static void systemErr(String str) {
        ORIG_STDERR.println("\033[31m" + str + "\033[0m");
    }

    private static String[] getCmdLineArgs() {
        return System.getProperty("sun.java.command").split(" ", 3);
    }

    private static boolean isNullOrEmpty(String str) {
        return str == null || str.isEmpty();
    }

    private static class CognitoIdentity {
        private final String identity_id = null;
        private final String identity_pool_id = null;

        private CognitoIdentity() {
        }
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
