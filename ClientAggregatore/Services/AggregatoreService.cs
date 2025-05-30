using Opc.Ua;
using Opc.Ua.Client;
using ClientAggregatore.Models;
using Shared;

namespace ClientAggregatore.Services
{
    public class AggregatoreService
    {
        private Session? sessionNastri;
        private Session? sessionRiempitrice;
        private Timer? timerAggregazione;
        private DatiAggregati ultimiDati = new();
        private ApplicationConfiguration applicationConfiguration;

        private const string ENDPOINT_NASTRI = "opc.tcp://localhost:4841/NastriServer";
        private const string ENDPOINT_RIEMPITRICE = "opc.tcp://localhost:4843/RiempitriceServer";

        public AggregatoreService(ApplicationConfiguration config)
        {
            applicationConfiguration = config;
        }

        public async Task AvviaAggregazione()
        {
            try
            {
                // Connetti ai server
                await ConnettereAlServer();

                // Avvia il timer di aggregazione ogni 5 secondi
                timerAggregazione = new Timer(AggregaDati, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

                Console.WriteLine("✅ Aggregazione Enhanced avviata - lettura dati ogni 5 secondi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore avvio aggregazione Enhanced: {ex.Message}");
                throw;
            }
        }

        private async Task ConnettereAlServer()
        {
            var endpointConfiguration = EndpointConfiguration.Create();

            try
            {
                // Connessione al server nastri
                Console.WriteLine($"🔌 Connessione Enhanced a {ENDPOINT_NASTRI}...");
                
                var endpointNastri = CoreClientUtils.SelectEndpoint(applicationConfiguration, ENDPOINT_NASTRI, false, 15000);
                sessionNastri = await Session.Create(
                    applicationConfiguration,
                    new ConfiguredEndpoint(null, endpointNastri, endpointConfiguration),
                    false,
                    "Client Aggregatore Enhanced - Nastri",
                    60000,
                    new UserIdentity(),
                    null
                );

                Console.WriteLine("✅ Connesso al server nastri Enhanced");

                // Connessione al server riempitrice
                Console.WriteLine($"🔌 Connessione Enhanced a {ENDPOINT_RIEMPITRICE}...");
                
                var endpointRiempitrice = CoreClientUtils.SelectEndpoint(applicationConfiguration, ENDPOINT_RIEMPITRICE, false, 15000);
                sessionRiempitrice = await Session.Create(
                    applicationConfiguration,
                    new ConfiguredEndpoint(null, endpointRiempitrice, endpointConfiguration),
                    false,
                    "Client Aggregatore Enhanced - Riempitrice",
                    60000,
                    new UserIdentity(),
                    null
                );

                Console.WriteLine("✅ Connesso al server riempitrice Enhanced");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore connessione Enhanced: {ex.Message}");
                throw;
            }
        }

        private async void AggregaDati(object? state)
        {
            try
            {
                if (sessionNastri == null || sessionRiempitrice == null)
                {
                    Console.WriteLine("⚠️ Sessioni Enhanced non inizializzate");
                    return;
                }

                // Leggi dati dai nastri Enhanced
                var datiNastri = await LeggiDatiNastriEnhanced();
                
                // Leggi dati dalla riempitrice Enhanced
                var datiRiempitrice = await LeggiDatiRiempitriceEnhanced();

                // Aggrega i dati
                ultimiDati = new DatiAggregati
                {
                    Nastri = datiNastri,
                    Riempitrice = datiRiempitrice,
                    UltimoAggiornamento = DateTime.Now
                };

                // Calcola aggregazioni
                CalcolaAggregazioni();

                // Mostra risultati
                MostraRisultatiAggregazione();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore aggregazione Enhanced: {ex.Message}");
            }
        }

        private Task<List<StatoNastroDto>> LeggiDatiNastriEnhanced()
        {
            var nastri = new List<StatoNastroDto>();

            try
            {
                // Prepara i nodi da leggere per tutti i 6 nastri con la nuova struttura Enhanced
                var nodesToRead = new ReadValueIdCollection();

                for (int i = 1; i <= 6; i++)
                {
                    // Nuova struttura Enhanced: Nastro{i}/Parametri/...
                    nodesToRead.Add(new ReadValueId { NodeId = new NodeId($"Stato{i}", 2), AttributeId = Attributes.Value });
                    nodesToRead.Add(new ReadValueId { NodeId = new NodeId($"Consumo{i}", 2), AttributeId = Attributes.Value });
                    nodesToRead.Add(new ReadValueId { NodeId = new NodeId($"Contatore{i}", 2), AttributeId = Attributes.Value });
                }

                // Leggi tutti i valori in una volta
                sessionNastri!.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out var results, out var diagnostics);

                // Processa i risultati (3 valori per nastro)
                for (int i = 0; i < 6; i++)
                {
                    var baseIndex = i * 3;
                    
                    var nastro = new StatoNastroDto
                    {
                        Id = i + 1,
                        Nome = $"Nastro {i + 1}",
                        Stato = (StatoNastro)Convert.ToInt32(results[baseIndex].Value ?? 0),
                        ConsumoElettrico = Convert.ToSingle(results[baseIndex + 1].Value ?? 0.0f),
                        ContatoreBottiglie = Convert.ToUInt32(results[baseIndex + 2].Value ?? 0u)
                    };

                    nastri.Add(nastro);
                }

                Console.WriteLine($"📊 Letti {nastri.Count} nastri Enhanced");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore lettura nastri Enhanced: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return Task.FromResult(nastri);
        }

        private Task<StatoRiempitriceDto?> LeggiDatiRiempitriceEnhanced()
        {
            try
            {
                // Prepara i nodi da leggere per la riempitrice Enhanced
                var nodesToRead = new ReadValueIdCollection
                {
                    // Struttura Enhanced per riempitrice
                    new ReadValueId { NodeId = new NodeId("Stato", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("RicettaInUso", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("ConsumoElettrico", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("ContatoreBottiglieRiempite", 2), AttributeId = Attributes.Value }
                };

                // Leggi i valori
                sessionRiempitrice!.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out var results, out var diagnostics);

                var riempitrice = new StatoRiempitriceDto
                {
                    Nome = "Riempitrice Enhanced",
                    Stato = (StatoRiempitrice)Convert.ToInt32(results[0].Value ?? 0),
                    RicettaInUso = results[1].Value?.ToString() ?? "Nessuna",
                    ConsumoElettrico = Convert.ToSingle(results[2].Value ?? 0.0f),
                    ContatoreBottiglieRiempite = Convert.ToUInt32(results[3].Value ?? 0u)
                };

                Console.WriteLine($"🏭 Letta riempitrice Enhanced: {riempitrice.Stato}");

                return Task.FromResult<StatoRiempitriceDto?>(riempitrice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore lettura riempitrice Enhanced: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Task.FromResult<StatoRiempitriceDto?>(null);
            }
        }

        private void CalcolaAggregazioni()
        {
            // Calcola consumo complessivo
            var consumoNastri = ultimiDati.Nastri.Sum(n => n.ConsumoElettrico);
            var consumoRiempitrice = ultimiDati.Riempitrice?.ConsumoElettrico ?? 0.0f;
            ultimiDati.ConsumoComplessivo = consumoNastri + consumoRiempitrice;

            // Calcola numero bottiglie complessivo  
            var bottiglieNastri = ultimiDati.Nastri.Sum(n => (long)n.ContatoreBottiglie);
            var bottiglieRiempitrice = (long)(ultimiDati.Riempitrice?.ContatoreBottiglieRiempite ?? 0u);
            ultimiDati.NumeroBottiglieComplessivo = (uint)(bottiglieNastri + bottiglieRiempitrice);

            // Determina stato del sistema
            var nastriInAllarme = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InAllarme);
            var riempitriceInAllarme = ultimiDati.Riempitrice?.Stato == StatoRiempitrice.InAllarme;

            if (nastriInAllarme > 0 || riempitriceInAllarme)
            {
                ultimiDati.StatoSistema = nastriInAllarme >= 3 || riempitriceInAllarme ? 
                    StatoSistema.AllarmeGenerale : StatoSistema.ParzialeAllarme;
            }
            else
            {
                var nastriOperativi = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InFunzione);
                var riempitriceOperativa = ultimiDati.Riempitrice?.Stato == StatoRiempitrice.InFunzione;
                
                ultimiDati.StatoSistema = (nastriOperativi > 0 || riempitriceOperativa) ? 
                    StatoSistema.Operativo : StatoSistema.Spento;
            }

            // Rileva anomalie contatore bottiglie (logica semplice)
            // Se la riempitrice ha riempito più bottiglie di quelle passate dai nastri
            ultimiDati.AnomaliaContatoreBottiglie = bottiglieRiempitrice > bottiglieNastri + 10; // Tolleranza di 10
        }

        private void MostraRisultatiAggregazione()
        {
            Console.Clear();
            Console.WriteLine("=== 🏭 DASHBOARD AGGREGAZIONE IMPIANTO ENHANCED ===");
            Console.WriteLine($"🕒 Ultimo aggiornamento: {ultimiDati.UltimoAggiornamento:HH:mm:ss}");
            Console.WriteLine();

            // Stato sistema
            var coloreStato = ultimiDati.StatoSistema switch
            {
                StatoSistema.Operativo => "🟢",
                StatoSistema.ParzialeAllarme => "🟡", 
                StatoSistema.AllarmeGenerale => "🔴",
                _ => "⚪"
            };
            Console.WriteLine($"{coloreStato} STATO SISTEMA: {ultimiDati.StatoSistema}");

            // Dati aggregati
            Console.WriteLine($"⚡ CONSUMO COMPLESSIVO: {ultimiDati.ConsumoComplessivo:F2} kW");
            Console.WriteLine($"🍾 BOTTIGLIE PROCESSATE: {ultimiDati.NumeroBottiglieComplessivo}");
            Console.WriteLine($"⚠️  ANOMALIE CONTATORI: {(ultimiDati.AnomaliaContatoreBottiglie ? "SÌ" : "NO")}");
            Console.WriteLine();

            // Riassunto nastri Enhanced
            Console.WriteLine("📊 NASTRI ENHANCED:");
            var nastriOperativi = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InFunzione);
            var nastriSpenti = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.Spento);
            var nastriAllarme = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InAllarme);
            
            Console.WriteLine($"   Operativi: {nastriOperativi}/6 | Spenti: {nastriSpenti}/6 | In allarme: {nastriAllarme}/6");

            // Dettaglio nastri con consumo
            foreach (var nastro in ultimiDati.Nastri)
            {
                var icona = nastro.Stato switch
                {
                    StatoNastro.InFunzione => "🟢",
                    StatoNastro.InAllarme => "🔴",
                    _ => "⚪"
                };
                Console.WriteLine($"   {icona} {nastro.Nome}: {nastro.ConsumoElettrico:F1}kW | {nastro.ContatoreBottiglie} bot");
            }

            Console.WriteLine();

            // Stato riempitrice Enhanced
            if (ultimiDati.Riempitrice != null)
            {
                var iconaRiempitrice = ultimiDati.Riempitrice.Stato switch
                {
                    StatoRiempitrice.InFunzione => "🟢",
                    StatoRiempitrice.InAllarme => "🔴",
                    StatoRiempitrice.Accesa => "🟡",
                    _ => "⚪"
                };
                Console.WriteLine($"🏭 RIEMPITRICE ENHANCED:");
                Console.WriteLine($"   {iconaRiempitrice} Stato: {ultimiDati.Riempitrice.Stato} | Ricetta: {ultimiDati.Riempitrice.RicettaInUso}");
                Console.WriteLine($"   ⚡ Consumo: {ultimiDati.Riempitrice.ConsumoElettrico:F2}kW | 🍾 Riempite: {ultimiDati.Riempitrice.ContatoreBottiglieRiempite}");
            }

            Console.WriteLine("\n💡 Premere 'r' per report dettagliato, 'q' per uscire");
        }

        public async Task MostraReportDettagliato()
        {
            Console.Clear();
            Console.WriteLine("=== 📊 REPORT DETTAGLIATO IMPIANTO ENHANCED ===");
            Console.WriteLine($"🕒 Generato: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            // KPI principali
            Console.WriteLine("📈 KPI PRINCIPALI:");
            var efficienzaNastri = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InFunzione) / 6.0 * 100;
            var produttivitaOraria = ultimiDati.NumeroBottiglieComplessivo > 0 ? 
                (ultimiDati.NumeroBottiglieComplessivo * 12) : 0; // Stima bottiglie/ora
            
            Console.WriteLine($"   🎯 Efficienza nastri: {efficienzaNastri:F1}%");
            Console.WriteLine($"   🚀 Produttività stimata: {produttivitaOraria} bot/h");
            Console.WriteLine($"   ⚡ Intensità energetica: {(ultimiDati.ConsumoComplessivo / Math.Max(ultimiDati.NumeroBottiglieComplessivo, 1)):F3} kW/bot");
            Console.WriteLine();

            // Dettaglio nastri Enhanced
            Console.WriteLine("🎚️  DETTAGLIO NASTRI ENHANCED:");
            foreach (var nastro in ultimiDati.Nastri)
            {
                var icona = nastro.Stato switch
                {
                    StatoNastro.InFunzione => "🟢",
                    StatoNastro.InAllarme => "🔴",
                    _ => "⚪"
                };
                var percentualeCarico = nastro.ConsumoElettrico / 5.0 * 100; // Assumendo 5kW max per nastro
                Console.WriteLine($"   {icona} {nastro.Nome}:");
                Console.WriteLine($"      Stato: {nastro.Stato} | Carico: {percentualeCarico:F1}%");
                Console.WriteLine($"      Consumo: {nastro.ConsumoElettrico:F2}kW | Bottiglie: {nastro.ContatoreBottiglie}");
            }

            Console.WriteLine();
            
            // Dettaglio riempitrice Enhanced
            if (ultimiDati.Riempitrice != null)
            {
                var iconaRiempitrice = ultimiDati.Riempitrice.Stato switch
                {
                    StatoRiempitrice.InFunzione => "🟢",
                    StatoRiempitrice.InAllarme => "🔴",
                    StatoRiempitrice.Accesa => "🟡",
                    _ => "⚪"
                };
                var percentualeCarico = ultimiDati.Riempitrice.ConsumoElettrico / 20.0 * 100; // Assumendo 20kW max
                
                Console.WriteLine("🏭 DETTAGLIO RIEMPITRICE ENHANCED:");
                Console.WriteLine($"   {iconaRiempitrice} {ultimiDati.Riempitrice.Nome}:");
                Console.WriteLine($"      Stato: {ultimiDati.Riempitrice.Stato} | Carico: {percentualeCarico:F1}%");
                Console.WriteLine($"      📋 Ricetta attiva: {ultimiDati.Riempitrice.RicettaInUso}");
                Console.WriteLine($"      ⚡ Consumo: {ultimiDati.Riempitrice.ConsumoElettrico:F2}kW");
                Console.WriteLine($"      🍾 Bottiglie riempite: {ultimiDati.Riempitrice.ContatoreBottiglieRiempite}");
            }

            // Analisi performance
            Console.WriteLine("\n📊 ANALISI PERFORMANCE:");
            var bilanciamentoCarico = ultimiDati.Nastri.Max(n => n.ConsumoElettrico) - 
                                     ultimiDati.Nastri.Min(n => n.ConsumoElettrico);
            Console.WriteLine($"   ⚖️  Bilanciamento carico nastri: {bilanciamentoCarico:F2}kW (differenza max-min)");
            
            if (ultimiDati.AnomaliaContatoreBottiglie)
            {
                Console.WriteLine("   ⚠️  ATTENZIONE: Rilevata anomalia nei contatori bottiglie!");
            }

            Console.WriteLine("\n💡 Premere un tasto per tornare alla dashboard...");
            Console.ReadKey(true);
        }

        public async Task FermaAggregazione()
        {
            timerAggregazione?.Dispose();
            
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

            Console.WriteLine("🔌 Connessioni Enhanced chiuse");
        }
    }
}