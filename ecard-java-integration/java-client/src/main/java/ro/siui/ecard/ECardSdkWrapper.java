package ro.siui.ecard;

import java.io.IOException;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.List;

/**
 * Java 7 friendly wrapper over the local .NET eCard bridge.
 *
 * The Java application should depend on this class instead of invoking
 * ECardBridge.exe directly.
 */
public final class ECardSdkWrapper {
    private final ECardBridgeClient bridgeClient;

    public ECardSdkWrapper(Path bridgeExe) {
        this.bridgeClient = new ECardBridgeClient(bridgeExe);
    }

    public String getSupportedTerminalsJson() throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.terminals());
    }

    public String getReadersJson() throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.readers());
    }

    public String getStatusJson() throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.status(null));
    }

    public String getStatusJson(String reader) throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.status(reader));
    }

    public String readCardJson(String pinEnvironmentVariable, String reader, String... fields)
            throws IOException, InterruptedException, ECardSdkException {
        return readCardJson(pinEnvironmentVariable, reader, Arrays.asList(fields));
    }

    public String readCardJson(String pinEnvironmentVariable, String reader, List<String> fields)
            throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.read(pinEnvironmentVariable, fields, reader));
    }

    public String activateCardJson(String pinEnvironmentVariable, String reader)
            throws IOException, InterruptedException, ECardSdkException {
        return requireOk(bridgeClient.activate(pinEnvironmentVariable, reader));
    }

    private String requireOk(ECardBridgeClient.BridgeResult result) throws ECardSdkException {
        if (result.isOk()) {
            return result.output();
        }

        throw new ECardSdkException(
                "ECard bridge command failed with exit code " + result.exitCode(),
                result.exitCode(),
                result.output());
    }
}
