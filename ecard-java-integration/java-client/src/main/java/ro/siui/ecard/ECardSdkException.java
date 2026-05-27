package ro.siui.ecard;

public class ECardSdkException extends Exception {
    private final int exitCode;
    private final String bridgeOutput;

    public ECardSdkException(String message, int exitCode, String bridgeOutput) {
        super(message);
        this.exitCode = exitCode;
        this.bridgeOutput = bridgeOutput;
    }

    public int getExitCode() {
        return exitCode;
    }

    public String getBridgeOutput() {
        return bridgeOutput;
    }
}
