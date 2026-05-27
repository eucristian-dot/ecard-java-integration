package ro.siui.ecard;

import java.nio.file.Path;
import java.nio.file.Paths;

public final class ECardWrapperDemo {
    private ECardWrapperDemo() {
    }

    public static void main(String[] args) throws Exception {
        Path bridgeExe = args.length > 0
                ? Paths.get(args[0])
                : Paths.get("..", "bridge", "bin", "ECardBridge.exe");

        ECardSdkWrapper ecard = new ECardSdkWrapper(bridgeExe);

        System.out.println(ecard.getReadersJson());
        System.out.println(ecard.getStatusJson());

        // Pentru citire reala:
        // set ECARD_PIN in mediu si decomenteaza linia de mai jos.
        // System.out.println(ecard.readCardJson("ECARD_PIN", null, "A1", "A2", "A3"));
    }
}
