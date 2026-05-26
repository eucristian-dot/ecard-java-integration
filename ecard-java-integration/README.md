# Integrare Java pentru Ceas.eCard.SDK

`Ceas.eCard.SDK.dll` este un assembly .NET Framework 4.x, nu o librarie Java/JAR. In Java nu poate fi importat direct cu Maven/Gradle; varianta stabila este un bridge local .NET care incarca SDK-ul oficial si returneaza JSON catre aplicatia Java.

DLL-urile SDK (`Ceas.eCard.SDK.dll`, `Newtonsoft.Json.dll`, `BouncyCastle.Crypto.dll`) sunt incluse in repo in `eCard.SDK.1.3.0.4`, ca dependinte vendor necesare la build/runtime. Arhiva ZIP originala si artefactele generate raman ignorate.

## Structura

- `bridge/ECardBridge.cs` - wrapper .NET peste `Novensys.eCard.SDK`.
- `bridge/build.ps1` - compileaza `ECardBridge.exe` si copiaza DLL-urile SDK langa executabil.
- `java-client/` - exemplu Java care apeleaza bridge-ul prin `ProcessBuilder`.

## Build bridge

```powershell
cd "C:\Users\LC\Documents\SIUI - eCard SDK\ecard-java-integration\bridge"
.\build.ps1
```

Executabilul rezultat va fi:

```text
C:\Users\LC\Documents\SIUI - eCard SDK\ecard-java-integration\bridge\bin\ECardBridge.exe
```

## Comenzi bridge

```powershell
.\bin\ECardBridge.exe terminals
.\bin\ECardBridge.exe readers
.\bin\ECardBridge.exe status
.\bin\ECardBridge.exe status --reader "ACS ACR39U ICC Reader"
$env:ECARD_PIN = "1234"
.\bin\ECardBridge.exe read --pin-env ECARD_PIN --fields A1,A2,A3
.\bin\ECardBridge.exe activate --pin-env ECARD_PIN
```

Daca trebuie configurata Unitatea de Management:

```powershell
.\bin\ECardBridge.exe status --um-host "host.um.local" --um-port 12345
```

Pentru productie, evita `--pin 1234`, fiindca PIN-ul apare in lista de procese. Foloseste `--pin-env ECARD_PIN` sau adapteaza bridge-ul sa citeasca PIN-ul din stdin.

`terminals` listeaza modelele suportate de SDK. `readers` include si `availableReaders`, adica cititoarele detectate efectiv prin PC/SC.

## Rulare exemplu Java cu javac

```powershell
cd "C:\Users\LC\Documents\SIUI - eCard SDK\ecard-java-integration\java-client"
mkdir target\classes
javac --release 7 -encoding UTF-8 -d target\classes src\main\java\ro\siui\ecard\ECardBridgeClient.java src\main\java\ro\siui\ecard\ECardDemo.java
java -cp target\classes ro.siui.ecard.ECardDemo "..\bridge\bin\ECardBridge.exe" terminals
```

Diagnostic PC/SC direct din Java:

```powershell
javac -source 1.7 -target 1.7 -encoding UTF-8 -d target\classes src\main\java\ro\siui\ecard\PcscProbe.java
java -cp target\classes ro.siui.ecard.PcscProbe
```

Cu JDK 9+ local, daca `javax.smartcardio` nu apare la compilare, foloseste:

```powershell
javac --add-modules java.smartcardio -encoding UTF-8 -d target\classes src\main\java\ro\siui\ecard\PcscProbe.java
java --add-modules java.smartcardio -cp target\classes ro.siui.ecard.PcscProbe
```

Codul Java este compatibil cu Java 7. `pom.xml` este inclus pentru proiecte Maven, dar pe aceasta statie Maven nu este instalat. Daca ai deja Maven in proiectul tau, poti copia clasa `ECardBridgeClient` sau adauga acest modul ca submodul.

## Integrare in proiectul Java existent

1. Compileaza bridge-ul si livreaza folderul `bridge\bin` impreuna cu aplicatia Java.
2. In aplicatia Java, configureaza o cale absoluta catre `ECardBridge.exe`.
3. Apeleaza `ECardBridgeClient.terminals()`, `status(...)`, `read(...)` sau `activate(...)`.
4. Parseaza JSON-ul returnat cu Jackson/Gson daca ai deja una dintre librarii in proiect.
5. Pastreaza driverele cititorului smartcard si serviciul PC/SC active pe statia unde ruleaza aplicatia.

Codurile de camp sunt cele expuse de enum-ul SDK `CoduriCampuriCard`: `A1` ... `T4`. Fara documentatia oficiala a profilului de card, bridge-ul lasa aplicatia Java sa specifice explicit lista de campuri citite.
