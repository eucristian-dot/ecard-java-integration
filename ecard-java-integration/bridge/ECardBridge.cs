using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Novensys.eCard.SDK;
using Novensys.eCard.SDK.Entities;
using Novensys.eCard.SDK.Entities.SmartCard;
using Novensys.eCard.SDK.PCSC;

namespace RoSiui.ECardBridge
{
    public static class ECardBridge
    {
        private static string sdkDirectory;
        private static TextWriter jsonOutput;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            jsonOutput = Console.Out;
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            ParsedArgs parsed = ParseArgs(args);
            sdkDirectory = GetOption(parsed.Options, "sdk-dir", null);
            RegisterAssemblyResolver();

            try
            {
                string command = parsed.Positionals.Count == 0 ? "help" : parsed.Positionals[0].ToLowerInvariant();

                if (command == "help" || command == "--help" || command == "-h")
                {
                    PrintJson(new Dictionary<string, object>
                    {
                        { "ok", true },
                        { "commands", new[] { "terminals", "readers", "status", "token", "read", "activate" } },
                        { "examples", new[]
                            {
                                "ECardBridge.exe terminals",
                                "ECardBridge.exe readers",
                                "ECardBridge.exe status --reader \"ACS ACR39U ICC Reader\"",
                                "ECardBridge.exe token --reader \"ACS ACR83U\" --um-host 213.177.18.123 --um-port 443 --cui 4283325 --contract AOPSNAJ --contract-date 2014-06-29 --cas A45 --tip-furnizor CMG",
                                "ECardBridge.exe read --pin-terminal --fields A1,A2,A3",
                                "ECardBridge.exe read --pin-env ECARD_PIN --fields A1,A2,A3",
                                "ECardBridge.exe activate --pin-env ECARD_PIN"
                            }
                        }
                    });
                    return 0;
                }

                if (command == "terminals")
                {
                    return RunTerminals();
                }

                if (command == "readers")
                {
                    return RunReaders();
                }

                if (command == "status")
                {
                    return WithCardSession(parsed.Options, delegate(ISesiuneCard session)
                    {
                        PrintJson(new Dictionary<string, object>
                        {
                            { "ok", true },
                            { "status", DescribeSession(session) }
                        });
                        return 0;
                    });
                }

                if (command == "read")
                {
                    return WithCardSession(parsed.Options, delegate(ISesiuneCard session)
                    {
                        Dictionary<string, object> authorization = PrepareSessionAuthorization(session, parsed.Options);
                        string pin = ResolvePin(parsed.Options);
                        if (String.IsNullOrEmpty(pin))
                        {
                            if (!UseTerminalPin(parsed.Options))
                            {
                                throw new ArgumentException("Missing PIN. Use --pin-terminal, --pin-env ECARD_PIN or --pin 1234.");
                            }
                        }

                        List<CoduriCampuriCard> fields = ParseFields(GetOption(parsed.Options, "fields", null));
                        CardData cardData = new CardData();
                        Dictionary<CoduriCampuriCard, CoduriRaspunsOperatieCamp> fieldResponses =
                            new Dictionary<CoduriCampuriCard, CoduriRaspunsOperatieCamp>();

                        int responseCode = session.CitesteDate(pin, fields, ref cardData, ref fieldResponses);
                        bool ok = responseCode == (int)CoduriRaspunsOperatieCard.OK;

                        PrintJson(new Dictionary<string, object>
                        {
                            { "ok", ok },
                            { "responseCode", responseCode },
                            { "responseName", EnumName(typeof(CoduriRaspunsOperatieCard), responseCode) },
                            { "authorization", authorization },
                            { "fieldsRequested", EnumNames(fields) },
                            { "fieldResponses", DescribeFieldResponses(fieldResponses) },
                            { "cardData", FlattenCardData(cardData) }
                        });

                        return ok ? 0 : 2;
                    });
                }

                if (command == "token")
                {
                    return WithCardSession(parsed.Options, delegate(ISesiuneCard session)
                    {
                        Dictionary<string, object> authorization = PrepareSessionAuthorization(session, parsed.Options);
                        PrintJson(new Dictionary<string, object>
                        {
                            { "ok", true },
                            { "authorization", authorization },
                            { "status", DescribeSession(session) }
                        });
                        return 0;
                    });
                }

                if (command == "activate")
                {
                    return WithCardSession(parsed.Options, delegate(ISesiuneCard session)
                    {
                        Dictionary<string, object> authorization = PrepareSessionAuthorization(session, parsed.Options);
                        string pin = ResolvePin(parsed.Options);
                        if (String.IsNullOrEmpty(pin))
                        {
                            if (!UseTerminalPin(parsed.Options))
                            {
                                throw new ArgumentException("Missing PIN. Use --pin-terminal, --pin-env ECARD_PIN or --pin 1234.");
                            }
                        }

                        int responseCode = session.ActiveazaCard(pin);
                        bool ok = responseCode == (int)CoduriRaspunsOperatieCard.OK;

                        PrintJson(new Dictionary<string, object>
                        {
                            { "ok", ok },
                            { "responseCode", responseCode },
                            { "responseName", EnumName(typeof(CoduriRaspunsOperatieCard), responseCode) },
                            { "authorization", authorization }
                        });

                        return ok ? 0 : 2;
                    });
                }

                throw new ArgumentException("Unknown command: " + command);
            }
            catch (TargetInvocationException ex)
            {
                return Fail(ex.InnerException == null ? ex : ex.InnerException);
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }

        private static int RunTerminals()
        {
            PrintJson(new Dictionary<string, object>
            {
                { "ok", true },
                { "supportedTerminals", ManagerSesiuniCard.GetSupportedTerminalNames() }
            });
            return 0;
        }

        private static int RunReaders()
        {
            List<Dictionary<string, object>> supportedReaders = new List<Dictionary<string, object>>();
            List<string> availableReaders = new List<string>();
            WinSCardContextJob context = WinSCardContextJob.Instance;

            try
            {
                context.Start();
                foreach (WinSCardReaderInfo reader in WinSCardReaderInfo.Instances)
                {
                    string availableFullName = context.FindAvailableReader(reader.Name);
                    supportedReaders.Add(new Dictionary<string, object>
                    {
                        { "name", reader.Name },
                        { "fullName", reader.FullName },
                        { "availableFullName", availableFullName }
                    });

                    if (!String.IsNullOrWhiteSpace(availableFullName))
                    {
                        availableReaders.Add(availableFullName);
                    }
                }
            }
            finally
            {
                Try(delegate { context.Stop(); });
            }

            PrintJson(new Dictionary<string, object>
            {
                { "ok", true },
                { "availableReaders", availableReaders },
                { "supportedReaders", supportedReaders }
            });
            return 0;
        }

        private static int WithCardSession(Dictionary<string, string> options, Func<ISesiuneCard, int> action)
        {
            ISesiuneCard session = null;
            try
            {
                ConfigureManagementUnit(options);
                string reader = GetOption(options, "reader", null);
                session = String.IsNullOrWhiteSpace(reader)
                    ? ManagerSesiuniCard.StartSesiuneNoua()
                    : ManagerSesiuniCard.StartSesiuneNoua(reader);

                return action(session);
            }
            finally
            {
                if (session != null)
                {
                    Try(delegate { session.Stop(); });
                }
                Try(delegate { ManagerSesiuniCard.StopSesiuneCurenta(); });
            }
        }

        private static void ConfigureManagementUnit(Dictionary<string, string> options)
        {
            string host = GetOption(options, "um-host", null);
            string port = GetOption(options, "um-port", null);
            if (!String.IsNullOrWhiteSpace(host) || !String.IsNullOrWhiteSpace(port))
            {
                if (String.IsNullOrWhiteSpace(host) || String.IsNullOrWhiteSpace(port))
                {
                    throw new ArgumentException("Both --um-host and --um-port are required when configuring UM.");
                }

                ManagerSesiuniCard.SetAdresaUnitateManagement(host, Int32.Parse(port));
            }
        }

        private static Dictionary<string, object> PrepareSessionAuthorization(ISesiuneCard session, Dictionary<string, string> options)
        {
            string token = GetOption(options, "token", null);
            if (!String.IsNullOrWhiteSpace(token))
            {
                session.Token = token;
                return new Dictionary<string, object>
                {
                    { "mode", "provided-token" },
                    { "tokenLength", token.Length }
                };
            }

            if (!HasAuthorizationOptions(options))
            {
                return null;
            }

            IdentificatorDrepturi rights = new IdentificatorDrepturi();
            rights.CUI = RequiredOption(options, "cui");
            rights.NumarContract = RequiredOption(options, "contract");
            rights.DataContract = DateTime.Parse(RequiredOption(options, "contract-date"), CultureInfo.InvariantCulture);
            rights.CasaAsigurare = RequiredOption(options, "cas");
            rights.TipFurnizor = RequiredOption(options, "tip-furnizor");

            string obtainedToken = session.ObtineToken(rights.CUI, rights);
            return new Dictionary<string, object>
            {
                { "mode", "obtine-token" },
                { "cui", rights.CUI },
                { "contract", rights.NumarContract },
                { "contractDate", rights.DataContract.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                { "cas", rights.CasaAsigurare },
                { "tipFurnizor", rights.TipFurnizor },
                { "tokenReceived", !String.IsNullOrWhiteSpace(obtainedToken) },
                { "tokenLength", String.IsNullOrWhiteSpace(obtainedToken) ? 0 : obtainedToken.Length }
            };
        }

        private static bool HasAuthorizationOptions(Dictionary<string, string> options)
        {
            return GetOption(options, "cui", null) != null
                || GetOption(options, "contract", null) != null
                || GetOption(options, "contract-date", null) != null
                || GetOption(options, "cas", null) != null
                || GetOption(options, "tip-furnizor", null) != null
                || GetOption(options, "token", null) != null;
        }

        private static string RequiredOption(Dictionary<string, string> options, string key)
        {
            string value = GetOption(options, key, null);
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Missing --" + key + " for card session authorization.");
            }
            return value;
        }
        private static Dictionary<string, object> DescribeSession(ISesiuneCard session)
        {
            Dictionary<string, object> status = new Dictionary<string, object>();
            status["terminalCurent"] = Safe(delegate { return session.TerminalCurent; });
            status["terminalId"] = Safe(delegate { return session.TerminalId; });
            status["terminalConectat"] = Safe(delegate { return session.TerminalConectat; });
            status["terminalCuTastatura"] = Safe(delegate { return session.TerminalCuTastatura; });
            status["stareCardInTerminal"] = Safe(delegate { return EnumText(session.StareCardInTerminal); });
            status["stareCard"] = Safe(delegate { return EnumText(session.StareCard); });
            status["stareAutentificare"] = Safe(delegate { return EnumText(session.StareAutentificare); });
            status["stareComunicatieCuUM"] = Safe(delegate { return EnumText(session.StareComunicatieCuUM); });
            status["necesitaActualizare"] = Safe(delegate { return session.NecesitaActualizare; });
            status["numarIncercariRamase"] = Safe(delegate { return session.NumarIncercariRamase; });
            status["profilId"] = Safe(delegate { return session.ProfilId.HasValue ? (object)session.ProfilId.Value : null; });
            return status;
        }

        private static List<CoduriCampuriCard> ParseFields(string fieldsText)
        {
            if (String.IsNullOrWhiteSpace(fieldsText))
            {
                throw new ArgumentException("Missing fields. Use --fields A1,A2,A3 or another comma-separated list from CoduriCampuriCard.");
            }

            List<CoduriCampuriCard> fields = new List<CoduriCampuriCard>();
            string[] parts = fieldsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                fields.Add((CoduriCampuriCard)Enum.Parse(typeof(CoduriCampuriCard), token, true));
            }

            return fields;
        }

        private static string[] EnumNames(List<CoduriCampuriCard> fields)
        {
            string[] names = new string[fields.Count];
            for (int i = 0; i < fields.Count; i++)
            {
                names[i] = fields[i].ToString();
            }

            return names;
        }

        private static Dictionary<string, object> DescribeFieldResponses(
            Dictionary<CoduriCampuriCard, CoduriRaspunsOperatieCamp> responses)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (responses == null)
            {
                return result;
            }

            foreach (KeyValuePair<CoduriCampuriCard, CoduriRaspunsOperatieCamp> response in responses)
            {
                result[response.Key.ToString()] = response.Value.ToString();
            }

            return result;
        }

        private static Dictionary<string, object> FlattenCardData(CardData cardData)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (cardData == null)
            {
                return result;
            }

            PropertyInfo[] sections = cardData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < sections.Length; i++)
            {
                PropertyInfo sectionProperty = sections[i];
                object sectionValue = GetPropertyValue(sectionProperty, cardData);
                if (sectionValue == null)
                {
                    continue;
                }

                if (IsSimple(sectionValue.GetType()))
                {
                    result[sectionProperty.Name] = SimpleValue(sectionValue);
                    continue;
                }

                PropertyInfo[] fields = sectionValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int j = 0; j < fields.Length; j++)
                {
                    PropertyInfo fieldProperty = fields[j];
                    object value = GetPropertyValue(fieldProperty, sectionValue);
                    string key = sectionProperty.Name + "." + fieldProperty.Name;

                    Camp camp = value as Camp;
                    if (camp != null)
                    {
                        result[key] = DescribeCamp(camp);
                    }
                    else if (value == null || IsSimple(value.GetType()))
                    {
                        result[key] = SimpleValue(value);
                    }
                    else
                    {
                        result[key] = value.ToString();
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, object> DescribeCamp(Camp camp)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["cod"] = Safe(delegate { return camp.Cod; });
            result["denumire"] = Safe(delegate { return camp.Denumire; });
            result["partitie"] = Safe(delegate { return camp.Partitie; });
            result["valoare"] = Safe(delegate { return SimpleValue(camp.Valoare); });
            result["isChanged"] = Safe(delegate { return camp.IsChanged; });
            return result;
        }

        private static object GetPropertyValue(PropertyInfo property, object instance)
        {
            try
            {
                return property.GetValue(instance, null);
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "type", ex.GetType().FullName }
                };
            }
        }

        private static object Safe(Func<object> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "type", ex.GetType().FullName }
                };
            }
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid);
        }

        private static object SimpleValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            Type type = value.GetType();
            if (type.IsEnum)
            {
                return value.ToString();
            }

            byte[] bytes = value as byte[];
            if (bytes != null)
            {
                return Convert.ToBase64String(bytes);
            }

            return value;
        }

        private static string EnumText(object enumValue)
        {
            return enumValue == null ? null : enumValue.ToString();
        }

        private static string EnumName(Type enumType, int value)
        {
            return Enum.IsDefined(enumType, value) ? Enum.GetName(enumType, value) : value.ToString();
        }

        private static string ResolvePin(Dictionary<string, string> options)
        {
            if (UseTerminalPin(options))
            {
                return null;
            }

            string pinEnv = GetOption(options, "pin-env", null);
            if (!String.IsNullOrWhiteSpace(pinEnv))
            {
                return Environment.GetEnvironmentVariable(pinEnv);
            }

            return GetOption(options, "pin", null);
        }

        private static bool UseTerminalPin(Dictionary<string, string> options)
        {
            string value = GetOption(options, "pin-terminal", null);
            return value != null && !String.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintJson(object payload)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffK";
            jsonOutput.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented, settings));
            jsonOutput.Flush();
        }

        private static int Fail(Exception ex)
        {
            PrintJson(new Dictionary<string, object>
            {
                { "ok", false },
                { "error", ex.Message },
                { "type", ex.GetType().FullName },
                { "detail", ex.ToString() }
            });
            return 1;
        }

        private static ParsedArgs ParseArgs(string[] args)
        {
            ParsedArgs parsed = new ParsedArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    string keyValue = arg.Substring(2);
                    int equals = keyValue.IndexOf('=');
                    if (equals >= 0)
                    {
                        parsed.Options[keyValue.Substring(0, equals)] = keyValue.Substring(equals + 1);
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        parsed.Options[keyValue] = args[++i];
                    }
                    else
                    {
                        parsed.Options[keyValue] = "true";
                    }
                }
                else
                {
                    parsed.Positionals.Add(arg);
                }
            }

            return parsed;
        }

        private static string GetOption(Dictionary<string, string> options, string key, string defaultValue)
        {
            string value;
            return options.TryGetValue(key, out value) ? value : defaultValue;
        }

        private static void RegisterAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
            {
                string fileName = new AssemblyName(args.Name).Name + ".dll";
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                string localPath = Path.Combine(baseDirectory, fileName);
                if (File.Exists(localPath))
                {
                    return Assembly.LoadFrom(localPath);
                }

                if (!String.IsNullOrWhiteSpace(sdkDirectory))
                {
                    string sdkPath = Path.Combine(sdkDirectory, fileName);
                    if (File.Exists(sdkPath))
                    {
                        return Assembly.LoadFrom(sdkPath);
                    }
                }

                return null;
            };
        }

        private static void Try(Action action)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }

        private sealed class ParsedArgs
        {
            public readonly List<string> Positionals = new List<string>();
            public readonly Dictionary<string, string> Options =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

