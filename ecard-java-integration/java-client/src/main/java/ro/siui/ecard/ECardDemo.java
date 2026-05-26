package ro.siui.ecard;

import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.List;

public final class ECardDemo {
    private ECardDemo() {
    }

    public static void main(String[] args) throws Exception {
        Path bridgeExe = args.length > 0
                ? Paths.get(args[0])
                : Paths.get("..", "bridge", "bin", "ECardBridge.exe");

        ECardBridgeClient client = new ECardBridgeClient(bridgeExe);

        if (args.length <= 1 || "terminals".equalsIgnoreCase(args[1])) {
            print(client.terminals());
            return;
        }

        if ("readers".equalsIgnoreCase(args[1])) {
            print(client.readers());
            return;
        }

        if ("status".equalsIgnoreCase(args[1])) {
            String reader = args.length > 2 ? args[2] : null;
            print(client.status(reader));
            return;
        }

        if ("read".equalsIgnoreCase(args[1])) {
            String reader = args.length > 2 ? args[2] : null;
            List<String> fields = Arrays.asList("A1", "A2", "A3");
            print(client.read("ECARD_PIN", fields, reader));
            return;
        }

        if ("activate".equalsIgnoreCase(args[1])) {
            String reader = args.length > 2 ? args[2] : null;
            print(client.activate("ECARD_PIN", reader));
            return;
        }

        throw new IllegalArgumentException("Unknown demo command: " + args[1]);
    }

    private static void print(ECardBridgeClient.BridgeResult result) {
        System.out.print(result.output());
        if (!result.isOk()) {
            System.exit(result.exitCode());
        }
    }
}
