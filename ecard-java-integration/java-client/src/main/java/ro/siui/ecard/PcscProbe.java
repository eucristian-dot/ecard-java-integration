package ro.siui.ecard;

import java.util.List;
import javax.smartcardio.Card;
import javax.smartcardio.CardException;
import javax.smartcardio.CardTerminal;
import javax.smartcardio.TerminalFactory;

public final class PcscProbe {
    private PcscProbe() {
    }

    public static void main(String[] args) throws Exception {
        TerminalFactory factory = TerminalFactory.getDefault();
        List<CardTerminal> terminals = factory.terminals().list();

        System.out.println("{");
        System.out.println("  \"ok\": true,");
        System.out.println("  \"terminalCount\": " + terminals.size() + ",");
        System.out.println("  \"terminals\": [");

        for (int i = 0; i < terminals.size(); i++) {
            CardTerminal terminal = terminals.get(i);
            boolean present = safeIsCardPresent(terminal);
            String atr = null;

            if (present) {
                Card card = null;
                try {
                    card = terminal.connect("*");
                    atr = toHex(card.getATR().getBytes());
                } catch (CardException ex) {
                    atr = "ERROR: " + escape(ex.getMessage());
                } finally {
                    if (card != null) {
                        try {
                            card.disconnect(false);
                        } catch (CardException ignored) {
                        }
                    }
                }
            }

            System.out.println("    {");
            System.out.println("      \"name\": \"" + escape(terminal.getName()) + "\",");
            System.out.println("      \"cardPresent\": " + present + (atr == null ? "" : ","));
            if (atr != null) {
                System.out.println("      \"atr\": \"" + escape(atr) + "\"");
            }
            System.out.println("    }" + (i + 1 == terminals.size() ? "" : ","));
        }

        System.out.println("  ]");
        System.out.println("}");
    }

    private static boolean safeIsCardPresent(CardTerminal terminal) {
        try {
            return terminal.isCardPresent();
        } catch (CardException ex) {
            return false;
        }
    }

    private static String toHex(byte[] bytes) {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.length; i++) {
            if (i > 0) {
                builder.append(' ');
            }
            int value = bytes[i] & 0xff;
            if (value < 16) {
                builder.append('0');
            }
            builder.append(Integer.toHexString(value).toUpperCase());
        }
        return builder.toString();
    }

    private static String escape(String value) {
        if (value == null) {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < value.length(); i++) {
            char ch = value.charAt(i);
            if (ch == '\\' || ch == '"') {
                builder.append('\\').append(ch);
            } else if (ch == '\n') {
                builder.append("\\n");
            } else if (ch == '\r') {
                builder.append("\\r");
            } else if (ch == '\t') {
                builder.append("\\t");
            } else {
                builder.append(ch);
            }
        }
        return builder.toString();
    }
}
