package ro.siui.ecard;

import java.io.IOException;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Objects;

public final class ECardBridgeClient {
    private final Path bridgeExe;

    public ECardBridgeClient(Path bridgeExe) {
        this.bridgeExe = Objects.requireNonNull(bridgeExe, "bridgeExe");
    }

    public BridgeResult terminals() throws IOException, InterruptedException {
        return run("terminals", Collections.<String>emptyList());
    }

    public BridgeResult readers() throws IOException, InterruptedException {
        return run("readers", Collections.<String>emptyList());
    }

    public BridgeResult status(String reader) throws IOException, InterruptedException {
        List<String> args = new ArrayList<>();
        if (hasText(reader)) {
            args.add("--reader");
            args.add(reader);
        }
        return run("status", args);
    }

    public BridgeResult read(String pinEnv, List<String> fields, String reader)
            throws IOException, InterruptedException {
        List<String> args = new ArrayList<>();
        args.add("--pin-env");
        args.add(pinEnv);
        args.add("--fields");
        args.add(join(",", fields));

        if (hasText(reader)) {
            args.add("--reader");
            args.add(reader);
        }

        return run("read", args);
    }

    public BridgeResult activate(String pinEnv, String reader) throws IOException, InterruptedException {
        List<String> args = new ArrayList<>();
        args.add("--pin-env");
        args.add(pinEnv);

        if (hasText(reader)) {
            args.add("--reader");
            args.add(reader);
        }

        return run("activate", args);
    }

    public BridgeResult run(String command, List<String> args) throws IOException, InterruptedException {
        List<String> processCommand = new ArrayList<>();
        processCommand.add(bridgeExe.toAbsolutePath().toString());
        processCommand.add(command);
        processCommand.addAll(args);

        ProcessBuilder processBuilder = new ProcessBuilder(processCommand);
        processBuilder.redirectErrorStream(true);

        Process process = processBuilder.start();
        byte[] output = readAll(process.getInputStream());
        int exitCode = process.waitFor();

        return new BridgeResult(exitCode, new String(output, StandardCharsets.UTF_8), processCommand);
    }

    private static boolean hasText(String value) {
        return value != null && value.trim().length() > 0;
    }

    private static byte[] readAll(InputStream inputStream) throws IOException {
        ByteArrayOutputStream buffer = new ByteArrayOutputStream();
        byte[] chunk = new byte[8192];
        int read;
        while ((read = inputStream.read(chunk)) != -1) {
            buffer.write(chunk, 0, read);
        }
        return buffer.toByteArray();
    }

    private static String join(String delimiter, List<String> values) {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < values.size(); i++) {
            if (i > 0) {
                builder.append(delimiter);
            }
            builder.append(values.get(i));
        }
        return builder.toString();
    }

    public static final class BridgeResult {
        private final int exitCode;
        private final String output;
        private final List<String> command;

        private BridgeResult(int exitCode, String output, List<String> command) {
            this.exitCode = exitCode;
            this.output = output;
            this.command = Collections.unmodifiableList(new ArrayList<String>(command));
        }

        public int exitCode() {
            return exitCode;
        }

        public String output() {
            return output;
        }

        public List<String> command() {
            return command;
        }

        public boolean isOk() {
            return exitCode == 0;
        }
    }
}
