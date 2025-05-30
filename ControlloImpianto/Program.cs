using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace ControlloImpianto
{
    class Program
    {
        private static Session? sessionNastri;
        private static Session? sessionRiempitrice;
        private static Dictionary<string, NodeId> nodeNastri = new();
        private static Dictionary<string, NodeId> nodeRiempitrice = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== CONTROLLO IMPIANTO OPC-UA ENHANCED ===");
            Console.WriteLine("Client per controllare nastri e riempitrice Enhanced");

            try
            {
                await ConnettereAiServer();
                await ScoprireStrutture();
                await MenuPrincipale();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
            }
            finally
            {
                await ChiudiConnessioni();
            }
        }

        private static async Task ConnettereAiServer()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "Controllo Impianto Enhanced",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true,
                    ApplicationCertificate = new CertificateIdentifier(),
                    TrustedPeerCertificates = new CertificateTrustList(),
                    TrustedIssuerCertificates = new CertificateTrustList(),
                    RejectedCertificateStore = new CertificateTrustList()
                },
                ClientConfiguration = new ClientConfiguration()
                {
                    DefaultSessionTimeout = 60000,
                    WellKnownDiscoveryUrls = new StringCollection()
                },
                TransportQuotas = new TransportQuotas()
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65536,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65536,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                }
            };

            var endpointConfig = EndpointConfiguration.Create();

            Console.WriteLine("🔌 Connessione ai server Enhanced...");

            // Connetti al server nastri
            var endpointNastri = CoreClientUtils.SelectEndpoint(config, "opc.tcp://localhost:4841/NastriServer", false, 15000);
            sessionNastri = await Session.Create(config, new ConfiguredEndpoint(null, endpointNastri, endpointConfig),
                false, "Controllo Nastri Enhanced", 60000, new UserIdentity(), null);

            // Connetti al server riempitrice  
            var endpointRiempitrice = CoreClientUtils.SelectEndpoint(config, "opc.tcp://localhost:4843/RiempitriceServer", false, 15000);
            sessionRiempitrice = await Session.Create(config, new ConfiguredEndpoint(null, endpointRiempitrice, endpointConfig),
                false, "Controllo Riempitrice Enhanced", 60000, new UserIdentity(), null);

            Console.WriteLine("✅ Connesso ai server Enhanced!");
        }

        private static async Task ScoprireStrutture()
        {
            Console.WriteLine("\n🔍 Scoperta struttura server Enhanced...");
            
            // Scopri struttura server nastri
            nodeNastri = await DebugClient.ScoprireStruttura(sessionNastri!, "Server Nastri Enhanced");
            
            // Scopri struttura server riempitrice
            nodeRiempitrice = await DebugClient.ScoprireStruttura(sessionRiempitrice!, "Server Riempitrice Enhanced");
            
            Console.WriteLine($"\n✅ Trovati {nodeNastri.Count} nodi nastri e {nodeRiempitrice.Count} nodi riempitrice");
            Console.WriteLine("\n🎯 Nodi di controllo trovati:");
            
            // Mostra nodi di controllo Enhanced
            foreach (var kvp in nodeNastri.Where(n => n.Key.Contains("Controllo") && n.Key.Contains("Acceso")))
            {
                Console.WriteLine($"  📝 NASTRO: {kvp.Key}");
            }
            
            foreach (var kvp in nodeRiempitrice.Where(n => n.Key.Contains("Controllo") && (n.Key.Contains("Accesa") || n.Key.Contains("CambiaRicetta"))))
            {
                Console.WriteLine($"  📝 RIEMPITRICE: {kvp.Key}");
            }
        }

        private static async Task MenuPrincipale()
        {
            while (true)
            {
                Console.WriteLine("\n=== MENU CONTROLLO ENHANCED ===");
                Console.WriteLine("1. Accendi tutti i nastri");
                Console.WriteLine("2. Spegni tutti i nastri");
                Console.WriteLine("3. Accendi nastri 1-3");
                Console.WriteLine("4. Accendi riempitrice");
                Console.WriteLine("5. Spegni riempitrice");
                Console.WriteLine("6. Controllo singolo nastro");
                Console.WriteLine("7. Cambia ricetta riempitrice");
                Console.WriteLine("8. Stato attuale Enhanced");
                Console.WriteLine("9. Test controllo Enhanced");
                Console.WriteLine("0. Mostra struttura completa");
                Console.WriteLine("q. Esci");
                
                Console.Write("\nScegli opzione: ");
                var scelta = Console.ReadLine();

                switch (scelta?.ToLower())
                {
                    case "1":
                        await AccendiTuttiNastriEnhanced();
                        break;
                    case "2":
                        await SpegniTuttiNastriEnhanced();
                        break;
                    case "3":
                        await AccendiNastri123Enhanced();
                        break;
                    case "4":
                        await AccendiRiempitriceEnhanced();
                        break;
                    case "5":
                        await SpegniRiempitriceEnhanced();
                        break;
                    case "6":
                        await ControlloSingoloNastroEnhanced();
                        break;
                    case "7":
                        await CambiaRicettaEnhanced();
                        break;
                    case "8":
                        await MostraStatoEnhanced();
                        break;
                    case "9":
                        await TestControlloEnhanced();
                        break;
                    case "0":
                        await MostraStrutturaCompleta();
                        break;
                    case "q":
                        return;
                    default:
                        Console.WriteLine("Opzione non valida!");
                        break;
                }
            }
        }

        private static async Task AccendiTuttiNastriEnhanced()
        {
            Console.WriteLine("🔌 Accendendo tutti i nastri Enhanced...");
            
            for (int i = 1; i <= 6; i++)
            {
                // Prova diverse possibili strutture Enhanced
                var possibiliNodi = new string[]
                {
                    $"Objects/2:Impianto/2:LineaProduzione/2:Nastri/2:Nastro{i}/2:Controllo/2:Acceso",
                    $"Objects/2:Nastri/2:Nastro{i}/2:Controllo/2:Acceso",
                    $"Objects/2:Nastro{i}/2:Controllo/2:Acceso",
                    $"Nastro{i}_Controllo_Acceso",
                    $"Acceso{i}"
                };

                bool trovato = false;
                foreach (var nodoPath in possibiliNodi)
                {
                    if (nodeNastri.TryGetValue(nodoPath, out var nodeId))
                    {
                        await ScriviNodo(nodeId, true, sessionNastri!);
                        Console.WriteLine($"  ✅ Nastro {i} acceso (nodo: {nodoPath})");
                        trovato = true;
                        break;
                    }
                }

                if (!trovato)
                {
                    Console.WriteLine($"  ❌ Non trovato nodo Acceso per Nastro {i}");
                    // Debug: mostra nodi disponibili per questo nastro
                    var nodiNastro = nodeNastri.Where(n => n.Key.Contains($"Nastro{i}")).ToList();
                    if (nodiNastro.Any())
                    {
                        Console.WriteLine($"      Nodi disponibili per Nastro{i}:");
                        foreach (var nodo in nodiNastro.Take(3))
                        {
                            Console.WriteLine($"        {nodo.Key}");
                        }
                    }
                }
            }
            Console.WriteLine("✅ Operazione completata!");
        }

        private static async Task SpegniTuttiNastriEnhanced()
        {
            Console.WriteLine("⚪ Spegnendo tutti i nastri Enhanced...");
            
            for (int i = 1; i <= 6; i++)
            {
                var possibiliNodi = new string[]
                {
                    $"Objects/2:Nastri/2:Nastro{i}/2:Acceso",
                    $"Objects/2:Nastri/2:Nastro{i}/2:Controllo/2:Acceso",
                    $"Nastro{i}_Controllo_Acceso",
                    $"Acceso{i}"
                };

                bool trovato = false;
                foreach (var nodoPath in possibiliNodi)
                {
                    if (nodeNastri.TryGetValue(nodoPath, out var nodeId))
                    {
                        await ScriviNodo(nodeId, false, sessionNastri!);
                        Console.WriteLine($"  ✅ Nastro {i} spento");
                        trovato = true;
                        break;
                    }
                }

                // Fallback con NodeId diretto
                if (!trovato)
                {
                    try
                    {
                        var nodeIdDiretto = new NodeId($"Acceso{i}", 2);
                        await ScriviNodo(nodeIdDiretto, false, sessionNastri!);
                        Console.WriteLine($"  ✅ Nastro {i} spento (NodeId diretto)");
                        trovato = true;
                    }
                    catch
                    {
                        Console.WriteLine($"  ❌ Non trovato nodo Acceso per Nastro {i}");
                    }
                }
            }
            Console.WriteLine("✅ Operazione completata!");
        }

        private static async Task AccendiNastri123Enhanced()
        {
            Console.WriteLine("🔌 Accendendo nastri 1, 2, 3 Enhanced...");
            
            for (int i = 1; i <= 3; i++)
            {
                bool trovato = false;
                
                // Prima prova con NodeId diretto (più probabile che funzioni)
                try
                {
                    var nodeIdDiretto = new NodeId($"Acceso{i}", 2);
                    await ScriviNodo(nodeIdDiretto, true, sessionNastri!);
                    Console.WriteLine($"  ✅ Nastro {i} acceso (NodeId: ns=2;s=Acceso{i})");
                    trovato = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠️ Fallimento NodeId diretto per Nastro {i}: {ex.Message}");
                }

                if (!trovato)
                {
                    Console.WriteLine($"  ❌ Non trovato nodo per Nastro {i}");
                }
            }
            Console.WriteLine("✅ Nastri 1-3 accesi!");
        }

        private static async Task AccendiRiempitriceEnhanced()
        {
            Console.WriteLine("🏭 Accendendo riempitrice Enhanced...");
            
            var possibiliNodi = new string[]
            {
                "Objects/2:Riempitrice/2:Controllo/2:Accesa",
                "Riempitrice_Controllo_Accesa",
                "Accesa"
            };

            bool trovato = false;
            foreach (var nodoPath in possibiliNodi)
            {
                if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                {
                    await ScriviNodo(nodeId, true, sessionRiempitrice!);
                    Console.WriteLine($"✅ Riempitrice Enhanced accesa! (nodo: {nodoPath})");
                    trovato = true;
                    break;
                }
            }

            if (!trovato)
            {
                Console.WriteLine("❌ Non trovato nodo Accesa per riempitrice Enhanced");
            }
        }

        private static async Task SpegniRiempitriceEnhanced()
        {
            Console.WriteLine("⚪ Spegnendo riempitrice Enhanced...");
            
            var possibiliNodi = new string[]
            {
                "Objects/2:Riempitrice/2:Controllo/2:Accesa",
                "Riempitrice_Controllo_Accesa",
                "Accesa"
            };

            bool trovato = false;
            foreach (var nodoPath in possibiliNodi)
            {
                if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                {
                    await ScriviNodo(nodeId, false, sessionRiempitrice!);
                    Console.WriteLine("✅ Riempitrice Enhanced spenta!");
                    trovato = true;
                    break;
                }
            }

            if (!trovato)
            {
                Console.WriteLine("❌ Non trovato nodo Accesa per riempitrice Enhanced");
            }
        }

        private static async Task ControlloSingoloNastroEnhanced()
        {
            Console.Write("Quale nastro vuoi controllare (1-6)? ");
            if (int.TryParse(Console.ReadLine(), out int nastroId) && nastroId >= 1 && nastroId <= 6)
            {
                Console.Write($"Accendere il nastro {nastroId}? (s/n): ");
                var risposta = Console.ReadLine()?.ToLower();
                bool accendi = risposta == "s" || risposta == "si";
                
                var possibiliNodi = new string[]
                {
                    $"Objects/2:Impianto/2:LineaProduzione/2:Nastri/2:Nastro{nastroId}/2:Controllo/2:Acceso",
                    $"Nastro{nastroId}_Controllo_Acceso",
                    $"Acceso{nastroId}"
                };

                bool trovato = false;
                foreach (var nodoPath in possibiliNodi)
                {
                    if (nodeNastri.TryGetValue(nodoPath, out var nodeId))
                    {
                        await ScriviNodo(nodeId, accendi, sessionNastri!);
                        Console.WriteLine($"✅ Nastro {nastroId} {(accendi ? "acceso" : "spento")} Enhanced!");
                        trovato = true;
                        break;
                    }
                }

                if (!trovato)
                {
                    Console.WriteLine($"❌ Non trovato nodo per Nastro {nastroId}");
                }
            }
            else
            {
                Console.WriteLine("❌ Numero nastro non valido!");
            }
        }

        private static async Task CambiaRicettaEnhanced()
        {
            Console.WriteLine("🧪 Ricette disponibili Enhanced:");
            Console.WriteLine("1. Acqua Naturale");
            Console.WriteLine("2. Acqua Frizzante");
            Console.WriteLine("3. Coca Cola");
            Console.WriteLine("4. Succo Arancia");
            Console.WriteLine("5. Energy Drink");
            
            Console.Write("Scegli ricetta (1-5): ");
            if (int.TryParse(Console.ReadLine(), out int scelta) && scelta >= 1 && scelta <= 5)
            {
                var ricette = new[] { "Acqua Naturale", "Acqua Frizzante", "Coca Cola", "Succo Arancia", "Energy Drink" };
                var ricetta = ricette[scelta - 1];
                
                var possibiliNodi = new string[]
                {
                    "Objects/2:Riempitrice/2:Controllo/2:CambiaRicetta",
                    "Riempitrice_Controllo_CambiaRicetta",
                    "CambiaRicetta"
                };

                bool trovato = false;
                foreach (var nodoPath in possibiliNodi)
                {
                    if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                    {
                        await ScriviNodo(nodeId, ricetta, sessionRiempitrice!);
                        Console.WriteLine($"✅ Ricetta Enhanced cambiata a: {ricetta}");
                        trovato = true;
                        break;
                    }
                }

                if (!trovato)
                {
                    Console.WriteLine("❌ Non trovato nodo CambiaRicetta Enhanced");
                }
            }
            else
            {
                Console.WriteLine("❌ Scelta non valida!");
            }
        }

        private static async Task MostraStatoEnhanced()
        {
            Console.WriteLine("\n=== STATO ATTUALE ENHANCED ===");
            
            // Leggi stato nastri Enhanced (con fallback)
            for (int i = 1; i <= 6; i++)
            {
                var statoNastro = await LeggiStatoNastro(i);
                Console.WriteLine($"Nastro {i}: {statoNastro}");
            }
            
            // Leggi stato riempitrice Enhanced
            var statoRiempitrice = await LeggiStatoRiempitrice();
            Console.WriteLine($"Riempitrice Enhanced: {statoRiempitrice}");
        }

        private static async Task<string> LeggiStatoNastro(int nastroId)
        {
            try
            {
                var possibiliNodiStato = new string[]
                {
                    $"Objects/2:Impianto/2:LineaProduzione/2:Nastri/2:Nastro{nastroId}/2:Parametri/2:Stato",
                    $"Nastro{nastroId}_Parametri_Stato",
                    $"Stato{nastroId}"
                };

                var possibiliNodiAcceso = new string[]
                {
                    $"Objects/2:Impianto/2:LineaProduzione/2:Nastri/2:Nastro{nastroId}/2:Controllo/2:Acceso",
                    $"Nastro{nastroId}_Controllo_Acceso",
                    $"Acceso{nastroId}"
                };

                object? stato = null;
                object? acceso = null;

                foreach (var nodoPath in possibiliNodiStato)
                {
                    if (nodeNastri.TryGetValue(nodoPath, out var nodeId))
                    {
                        stato = await LeggiNodo(nodeId, sessionNastri!);
                        break;
                    }
                }

                foreach (var nodoPath in possibiliNodiAcceso)
                {
                    if (nodeNastri.TryGetValue(nodoPath, out var nodeId))
                    {
                        acceso = await LeggiNodo(nodeId, sessionNastri!);
                        break;
                    }
                }

                var statoStr = stato != null ? ((StatoNastro)Convert.ToInt32(stato)).ToString() : "N/A";
                var accesoStr = acceso != null ? (Convert.ToBoolean(acceso) ? "🟢 Acceso" : "⚪ Spento") : "N/A";

                return $"{accesoStr} | Stato: {statoStr}";
            }
            catch (Exception ex)
            {
                return $"❌ Errore: {ex.Message}";
            }
        }

        private static async Task<string> LeggiStatoRiempitrice()
        {
            try
            {
                var possibiliNodiStato = new string[]
                {
                    "Objects/2:Riempitrice/2:Parametri/2:Stato",
                    "Riempitrice_Parametri_Stato",
                    "Stato"
                };

                var possibiliNodiAccesa = new string[]
                {
                    "Objects/2:Riempitrice/2:Controllo/2:Accesa",
                    "Riempitrice_Controllo_Accesa",
                    "Accesa"
                };

                var possibiliNodiRicetta = new string[]
                {
                    "Objects/2:Riempitrice/2:Parametri/2:RicettaInUso",
                    "Riempitrice_Parametri_RicettaInUso",
                    "RicettaInUso"
                };

                object? stato = null;
                object? accesa = null;
                object? ricetta = null;

                foreach (var nodoPath in possibiliNodiStato)
                {
                    if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                    {
                        stato = await LeggiNodo(nodeId, sessionRiempitrice!);
                        break;
                    }
                }

                foreach (var nodoPath in possibiliNodiAccesa)
                {
                    if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                    {
                        accesa = await LeggiNodo(nodeId, sessionRiempitrice!);
                        break;
                    }
                }

                foreach (var nodoPath in possibiliNodiRicetta)
                {
                    if (nodeRiempitrice.TryGetValue(nodoPath, out var nodeId))
                    {
                        ricetta = await LeggiNodo(nodeId, sessionRiempitrice!);
                        break;
                    }
                }

                var statoStr = stato != null ? ((StatoRiempitrice)Convert.ToInt32(stato)).ToString() : "N/A";
                var accesaStr = accesa != null ? (Convert.ToBoolean(accesa) ? "🟢 Accesa" : "⚪ Spenta") : "N/A";
                var ricettaStr = ricetta?.ToString() ?? "N/A";

                return $"{accesaStr} | Stato: {statoStr} | Ricetta: {ricettaStr}";
            }
            catch (Exception ex)
            {
                return $"❌ Errore: {ex.Message}";
            }
        }

        private static async Task TestControlloEnhanced()
        {
            Console.WriteLine("\n🧪 TEST CONTROLLO ENHANCED");
            Console.WriteLine("Testando controllo riempitrice Enhanced...");

            // Test riempitrice
            await AccendiRiempitriceEnhanced();
            await Task.Delay(2000);
            
            Console.WriteLine("Cambio ricetta...");
            var nodeRicetta = nodeRiempitrice.FirstOrDefault(n => n.Key.Contains("CambiaRicetta"));
            if (nodeRicetta.Value != null)
            {
                await ScriviNodo(nodeRicetta.Value, "Coca Cola", sessionRiempitrice!);
                Console.WriteLine("✅ Ricetta cambiata in test");
            }

            await Task.Delay(2000);
            await SpegniRiempitriceEnhanced();
            
            Console.WriteLine("✅ Test completato!");
        }

        private static Task MostraStrutturaCompleta()
        {
            Console.WriteLine("\n=== STRUTTURA COMPLETA ENHANCED ===");
            Console.WriteLine("\n📊 NODI NASTRI ENHANCED:");
            foreach (var kvp in nodeNastri.OrderBy(n => n.Key))
            {
                Console.WriteLine($"  {kvp.Key} → {kvp.Value}");
            }
            
            Console.WriteLine("\n🏭 NODI RIEMPITRICE ENHANCED:");
            foreach (var kvp in nodeRiempitrice.OrderBy(n => n.Key))
            {
                Console.WriteLine($"  {kvp.Key} → {kvp.Value}");
            }
            return Task.CompletedTask;
        }

        private static async Task ScriviNodo(NodeId nodeId, object value, Session session)
        {
            try
            {
                var writeValue = new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(value))
                };

                var writeValues = new WriteValueCollection { writeValue };
                session.Write(null, writeValues, out var results, out var diagnostics);

                if (StatusCode.IsBad(results[0]))
                {
                    Console.WriteLine($"❌ Errore scrittura Enhanced {nodeId}: {results[0]}");
                }
                else
                {
                    Console.WriteLine($"✅ Scrittura Enhanced OK {nodeId}: {value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Eccezione scrittura Enhanced {nodeId}: {ex.Message}");
            }
        }

        private static async Task<object?> LeggiNodo(NodeId nodeId, Session session)
        {
            try
            {
                var readValue = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                };

                var readValues = new ReadValueIdCollection { readValue };
                session.Read(null, 0, TimestampsToReturn.Both, readValues, out var results, out var diagnostics);

                if (StatusCode.IsBad(results[0].StatusCode))
                {
                    Console.WriteLine($"❌ Errore lettura Enhanced {nodeId}: {results[0].StatusCode}");
                    return null;
                }

                return results[0].Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Eccezione lettura Enhanced {nodeId}: {ex.Message}");
                return null;
            }
        }

        private static async Task ChiudiConnessioni()
        {
            if (sessionNastri != null)
            {
                await sessionNastri.CloseAsync();
                sessionNastri.Dispose();
            }
            
            if (sessionRiempitrice != null)
            {
                await sessionRiempitrice.CloseAsync();
                sessionRiempitrice.Dispose();
            }
        }
    }

    // Enum per il display
    enum StatoNastro { Spento = 0, InFunzione = 1, InAllarme = 2 }
    enum StatoRiempitrice { Spenta = 0, Accesa = 1, InFunzione = 2, InAllarme = 3 }
}