package org.lambci.lambda;

import java.io.File;
import java.lang.management.ManagementFactory;
import java.util.Map;
import java.util.Scanner;

import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestHandler;
import com.amazonaws.services.s3.AmazonS3;
import com.amazonaws.services.s3.AmazonS3ClientBuilder;
import com.amazonaws.services.s3.model.CannedAccessControlList;
import com.amazonaws.services.s3.model.PutObjectRequest;
import com.amazonaws.services.s3.model.PutObjectResult;

public class DumpJava11 implements RequestHandler<Object, PutObjectResult> {

    @Override
    public PutObjectResult handleRequest(Object input, Context context) {
        String filename = "java11.tgz";
        String cmd = "tar -cpzf /tmp/" + filename + " --numeric-owner --ignore-failed-read /var/runtime /var/lang";
        AmazonS3 s3client = AmazonS3ClientBuilder.standard().withRegion("us-east-1").build();

        System.out.println(ManagementFactory.getRuntimeMXBean().getInputArguments().toString());
        System.out.println(System.getProperty("sun.java.command"));
        System.out.println(System.getProperty("java.home"));
        System.out.println(System.getProperty("java.library.path"));
        System.out.println(System.getProperty("java.class.path"));
        System.out.println(System.getProperty("user.dir"));
        System.out.println(System.getProperty("user.home"));
        System.out.println(System.getProperty("user.name"));
        System.out.println(new File(".").getAbsolutePath());
        Map<String, String> env = System.getenv();
        for (String envName : env.keySet()) {
            System.out.println(envName + "=" + env.get(envName));
        }

        try {
            Process process = Runtime.getRuntime().exec(new String[] { "sh", "-c", cmd });

            try (Scanner stdoutScanner = new Scanner(process.getInputStream());
                    Scanner stderrScanner = new Scanner(process.getErrorStream())) {
                // Echo all stdout first
                while (stdoutScanner.hasNextLine()) {
                    System.out.println(stdoutScanner.nextLine());
                }
                // Then echo stderr
                while (stderrScanner.hasNextLine()) {
                    System.err.println(stderrScanner.nextLine());
                }
            }

            process.waitFor();
            if (process.exitValue() != 0) {
                return null;
            }

            System.out.println("Zipping done! Uploading...");

            return s3client.putObject(new PutObjectRequest("lambci", "fs/" + filename, new File("/tmp/" + filename))
                    .withCannedAcl(CannedAccessControlList.PublicRead));
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
