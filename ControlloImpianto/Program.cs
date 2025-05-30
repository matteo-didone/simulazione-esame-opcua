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
            Console.WriteLine("=== CONTROLLO IMPIANTO OPC-UA ===");
            Console.WriteLine("Client per accendere/spegnere nastri e riempitrice");

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
                ApplicationName = "Controllo Impianto",
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

            Console.WriteLine("🔌 Connessione ai server...");

            // Connetti al server nastri
            var endpointNastri = CoreClientUtils.SelectEndpoint(config, "opc.tcp://localhost:4841/NastriServer", false, 15000);
            sessionNastri = await Session.Create(config, new ConfiguredEndpoint(null, endpointNastri, endpointConfig),
                false, "Controllo Nastri", 60000, new UserIdentity(), null);

            // Connetti al server riempitrice  
            var endpointRiempitrice = CoreClientUtils.SelectEndpoint(config, "opc.tcp://localhost:4843/RiempitriceServer", false, 15000);
            sessionRiempitrice = await Session.Create(config, new ConfiguredEndpoint(null, endpointRiempitrice, endpointConfig),
                false, "Controllo Riempitrice", 60000, new UserIdentity(), null);

            Console.WriteLine("✅ Connesso ai server!");
        }

        private static async Task ScoprireStrutture()
        {
            Console.WriteLine("\n🔍 Scoperta struttura server...");
            
            // Scopri struttura server nastri
            nodeNastri = await DebugClient.ScoprireStruttura(sessionNastri!, "Server Nastri");
            
            // Scopri struttura server riempitrice
            nodeRiempitrice = await DebugClient.ScoprireStruttura(sessionRiempitrice!, "Server Riempitrice");
            
            Console.WriteLine($"\n✅ Trovati {nodeNastri.Count} nodi nastri e {nodeRiempitrice.Count} nodi riempitrice");
            Console.WriteLine("\n🎯 Nodi scrivibili trovati:");
            
            // Mostra nodi scrivibili
            foreach (var kvp in nodeNastri.Where(n => n.Key.Contains("Acceso") || n.Key.Contains("Automatico")))
            {
                Console.WriteLine($"  📝 {kvp.Key}");
            }
            
            foreach (var kvp in nodeRiempitrice.Where(n => n.Key.Contains("Accesa") || n.Key.Contains("CambiaRicetta")))
            {
                Console.WriteLine($"  📝 {kvp.Key}");
            }
        }

        private static async Task MenuPrincipale()
        {
            while (true)
            {
                Console.WriteLine("\n=== MENU CONTROLLO ===");
                Console.WriteLine("1. Accendi tutti i nastri");
                Console.WriteLine("2. Spegni tutti i nastri");
                Console.WriteLine("3. Accendi nastri 1-3");
                Console.WriteLine("4. Accendi riempitrice");
                Console.WriteLine("5. Spegni riempitrice");
                Console.WriteLine("6. Controllo singolo nastro");
                Console.WriteLine("7. Cambia ricetta riempitrice");
                Console.WriteLine("8. Stato attuale");
                Console.WriteLine("9. Mostra struttura completa");
                Console.WriteLine("q. Esci");
                
                Console.Write("\nScegli opzione: ");
                var scelta = Console.ReadLine();

                switch (scelta?.ToLower())
                {
                    case "1":
                        await AccendiTuttiNastri();
                        break;
                    case "2":
                        await SpegniTuttiNastri();
                        break;
                    case "3":
                        await AccendiNastri123();
                        break;
                    case "4":
                        await AccendiRiempitrice();
                        break;
                    case "5":
                        await SpegniRiempitrice();
                        break;
                    case "6":
                        await ControlloSingoloNastro();
                        break;
                    case "7":
                        await CambiaRicetta();
                        break;
                    case "8":
                        await MostraStato();
                        break;
                    case "9":
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

        private static async Task AccendiTuttiNastri()
        {
            Console.WriteLine("🔌 Accendendo tutti i nastri...");
            
            for (int i = 1; i <= 6; i++)
            {
                // Cerca i nodi per questo nastro
                var acceso = TrovaNodo(nodeNastri, $"Nastro{i}", "Acceso");
                var automatico = TrovaNodo(nodeNastri, $"Nastro{i}", "Automatico");
                
                if (acceso != null)
                {
                    await ScriviNodo(acceso, true, sessionNastri!);
                    Console.WriteLine($"  ✅ Nastro {i} acceso");
                }
                else
                {
                    Console.WriteLine($"  ❌ Non trovato nodo Acceso per Nastro {i}");
                }
                
                if (automatico != null)
                {
                    await ScriviNodo(automatico, true, sessionNastri!);
                }
            }
            Console.WriteLine("✅ Operazione completata!");
        }

        private static async Task SpegniTuttiNastri()
        {
            Console.WriteLine("⚪ Spegnendo tutti i nastri...");
            
            for (int i = 1; i <= 6; i++)
            {
                var acceso = TrovaNodo(nodeNastri, $"Nastro{i}", "Acceso");
                
                if (acceso != null)
                {
                    await ScriviNodo(acceso, false, sessionNastri!);
                    Console.WriteLine($"  ✅ Nastro {i} spento");
                }
                else
                {
                    Console.WriteLine($"  ❌ Non trovato nodo Acceso per Nastro {i}");
                }
            }
            Console.WriteLine("✅ Operazione completata!");
        }

        private static async Task AccendiNastri123()
        {
            Console.WriteLine("🔌 Accendendo nastri 1, 2, 3...");
            
            for (int i = 1; i <= 3; i++)
            {
                var acceso = TrovaNodo(nodeNastri, $"Nastro{i}", "Acceso");
                var automatico = TrovaNodo(nodeNastri, $"Nastro{i}", "Automatico");
                
                if (acceso != null)
                {
                    await ScriviNodo(acceso, true, sessionNastri!);
                    Console.WriteLine($"  ✅ Nastro {i} acceso");
                }
                
                if (automatico != null)
                {
                    await ScriviNodo(automatico, true, sessionNastri!);
                }
            }
            Console.WriteLine("✅ Nastri 1-3 accesi!");
        }

        private static async Task AccendiRiempitrice()
        {
            Console.WriteLine("🏭 Accendendo riempitrice...");
            
            var accesa = TrovaNodo(nodeRiempitrice, "Riempitrice", "Accesa");
            
            if (accesa != null)
            {
                await ScriviNodo(accesa, true, sessionRiempitrice!);
                Console.WriteLine("✅ Riempitrice accesa!");
            }
            else
            {
                Console.WriteLine("❌ Non trovato nodo Accesa per riempitrice");
            }
        }

        private static async Task SpegniRiempitrice()
        {
            Console.WriteLine("⚪ Spegnendo riempitrice...");
            
            var accesa = TrovaNodo(nodeRiempitrice, "Riempitrice", "Accesa");
            
            if (accesa != null)
            {
                await ScriviNodo(accesa, false, sessionRiempitrice!);
                Console.WriteLine("✅ Riempitrice spenta!");
            }
            else
            {
                Console.WriteLine("❌ Non trovato nodo Accesa per riempitrice");
            }
        }

        private static async Task ControlloSingoloNastro()
        {
            Console.Write("Quale nastro vuoi controllare (1-6)? ");
            if (int.TryParse(Console.ReadLine(), out int nastroId) && nastroId >= 1 && nastroId <= 6)
            {
                Console.Write($"Accendere il nastro {nastroId}? (s/n): ");
                var risposta = Console.ReadLine()?.ToLower();
                bool accendi = risposta == "s" || risposta == "si";
                
                var acceso = TrovaNodo(nodeNastri, $"Nastro{nastroId}", "Acceso");
                var automatico = TrovaNodo(nodeNastri, $"Nastro{nastroId}", "Automatico");
                
                if (acceso != null)
                {
                    await ScriviNodo(acceso, accendi, sessionNastri!);
                    if (accendi && automatico != null)
                    {
                        await ScriviNodo(automatico, true, sessionNastri!);
                    }
                    Console.WriteLine($"✅ Nastro {nastroId} {(accendi ? "acceso" : "spento")}!");
                }
                else
                {
                    Console.WriteLine($"❌ Non trovato nodo per Nastro {nastroId}");
                }
            }
            else
            {
                Console.WriteLine("❌ Numero nastro non valido!");
            }
        }

        private static async Task CambiaRicetta()
        {
            Console.WriteLine("Ricette disponibili:");
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
                
                var cambiaRicetta = TrovaNodo(nodeRiempitrice, "Riempitrice", "CambiaRicetta");
                
                if (cambiaRicetta != null)
                {
                    await ScriviNodo(cambiaRicetta, ricetta, sessionRiempitrice!);
                    Console.WriteLine($"✅ Ricetta cambiata a: {ricetta}");
                }
                else
                {
                    Console.WriteLine("❌ Non trovato nodo CambiaRicetta");
                }
            }
            else
            {
                Console.WriteLine("❌ Scelta non valida!");
            }
        }

        private static async Task MostraStato()
        {
            Console.WriteLine("\n=== STATO ATTUALE ===");
            
            // Leggi stato nastri
            for (int i = 1; i <= 6; i++)
            {
                var acceso = TrovaNodo(nodeNastri, $"Nastro{i}", "Acceso");
                var stato = TrovaNodo(nodeNastri, $"Nastro{i}", "Stato");
                var consumo = TrovaNodo(nodeNastri, $"Nastro{i}", "ConsumoElettrico");
                
                var valueAcceso = acceso != null ? await LeggiNodo(acceso, sessionNastri!) : false;
                var valueStato = stato != null ? await LeggiNodo(stato, sessionNastri!) : 0;
                var valueConsumo = consumo != null ? await LeggiNodo(consumo, sessionNastri!) : 0.0f;
                
                Console.WriteLine($"Nastro {i}: {(Convert.ToBoolean(valueAcceso) ? "🟢 Acceso" : "⚪ Spento")} " +
                                 $"| Stato: {(StatoNastro)Convert.ToInt32(valueStato)} | Consumo: {Convert.ToSingle(valueConsumo):F2}kW");
            }
            
            // Leggi stato riempitrice
            var riempitriceAccesa = TrovaNodo(nodeRiempitrice, "Riempitrice", "Accesa");
            var riempitriceStato = TrovaNodo(nodeRiempitrice, "Riempitrice", "Stato");
            var ricetta = TrovaNodo(nodeRiempitrice, "Riempitrice", "RicettaInUso");
            
            var valueRiempitriceAccesa = riempitriceAccesa != null ? await LeggiNodo(riempitriceAccesa, sessionRiempitrice!) : false;
            var valueRiempitriceStato = riempitriceStato != null ? await LeggiNodo(riempitriceStato, sessionRiempitrice!) : 0;
            var valueRicetta = ricetta != null ? await LeggiNodo(ricetta, sessionRiempitrice!) : "";
            
            Console.WriteLine($"Riempitrice: {(Convert.ToBoolean(valueRiempitriceAccesa) ? "🟢 Accesa" : "⚪ Spenta")} " +
                             $"| Stato: {(StatoRiempitrice)Convert.ToInt32(valueRiempitriceStato)} | Ricetta: {valueRicetta}");
        }

        private static Task MostraStrutturaCompleta()
        {
            Console.WriteLine("\n=== STRUTTURA COMPLETA ===");
            Console.WriteLine("\n📊 NODI NASTRI:");
            foreach (var kvp in nodeNastri.OrderBy(n => n.Key))
            {
                Console.WriteLine($"  {kvp.Key} → {kvp.Value}");
            }
            
            Console.WriteLine("\n🏭 NODI RIEMPITRICE:");
            foreach (var kvp in nodeRiempitrice.OrderBy(n => n.Key))
            {
                Console.WriteLine($"  {kvp.Key} → {kvp.Value}");
            }
            return Task.CompletedTask;
        }

        private static NodeId? TrovaNodo(Dictionary<string, NodeId> nodes, string parent, string variableName)
        {
            // Per i nastri: cerca esatto match "Objects/2:Nastri/2:Nastro1/2:Acceso"
            if (parent.StartsWith("Nastro"))
            {
                var exactKey = $"Objects/2:Nastri/2:{parent}/2:{variableName}";
                if (nodes.TryGetValue(exactKey, out var nodeId))
                {
                    return nodeId;
                }
            }
            
            // Per la riempitrice: cerca esatto match "Objects/2:Riempitrice/2:Accesa"
            if (parent == "Riempitrice")
            {
                var exactKey = $"Objects/2:Riempitrice/2:{variableName}";
                if (nodes.TryGetValue(exactKey, out var nodeId))
                {
                    return nodeId;
                }
            }
            
            return null;
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
                    Console.WriteLine($"❌ Errore scrittura {nodeId}: {results[0]}");
                }
                else
                {
                    Console.WriteLine($"✅ Scrittura OK {nodeId}: {value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Eccezione scrittura {nodeId}: {ex.Message}");
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
                    Console.WriteLine($"❌ Errore lettura {nodeId}: {results[0].StatusCode}");
                    return null;
                }

                return results[0].Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Eccezione lettura {nodeId}: {ex.Message}");
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