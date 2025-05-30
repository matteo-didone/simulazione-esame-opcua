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

                Console.WriteLine("✅ Aggregazione avviata - lettura dati ogni 5 secondi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore avvio aggregazione: {ex.Message}");
                throw;
            }
        }

        private async Task ConnettereAlServer()
        {
            var endpointConfiguration = EndpointConfiguration.Create();

            try
            {
                // Connessione al server nastri
                Console.WriteLine($"🔌 Connessione a {ENDPOINT_NASTRI}...");
                
                var endpointNastri = CoreClientUtils.SelectEndpoint(applicationConfiguration, ENDPOINT_NASTRI, false, 15000);
                sessionNastri = await Session.Create(
                    applicationConfiguration,
                    new ConfiguredEndpoint(null, endpointNastri, endpointConfiguration),
                    false,
                    "Client Aggregatore - Nastri",
                    60000,
                    new UserIdentity(),
                    null
                );

                Console.WriteLine("✅ Connesso al server nastri");

                // Connessione al server riempitrice
                Console.WriteLine($"🔌 Connessione a {ENDPOINT_RIEMPITRICE}...");
                
                var endpointRiempitrice = CoreClientUtils.SelectEndpoint(applicationConfiguration, ENDPOINT_RIEMPITRICE, false, 15000);
                sessionRiempitrice = await Session.Create(
                    applicationConfiguration,
                    new ConfiguredEndpoint(null, endpointRiempitrice, endpointConfiguration),
                    false,
                    "Client Aggregatore - Riempitrice",
                    60000,
                    new UserIdentity(),
                    null
                );

                Console.WriteLine("✅ Connesso al server riempitrice");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore connessione: {ex.Message}");
                throw;
            }
        }

        private async void AggregaDati(object? state)
        {
            try
            {
                if (sessionNastri == null || sessionRiempitrice == null)
                {
                    Console.WriteLine("⚠️ Sessioni non inizializzate");
                    return;
                }

                // Leggi dati dai nastri
                var datiNastri = await LeggiDatiNastri();
                
                // Leggi dati dalla riempitrice  
                var datiRiempitrice = await LeggiDatiRiempitrice();

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
                Console.WriteLine($"❌ Errore aggregazione: {ex.Message}");
            }
        }

        private Task<List<StatoNastroDto>> LeggiDatiNastri()
        {
            var nastri = new List<StatoNastroDto>();

            try
            {
                // Prepara i nodi da leggere per tutti i 6 nastri
                var nodesToRead = new ReadValueIdCollection();

                for (int i = 1; i <= 6; i++)
                {
                    // CORRETTO: Namespace 2 e string-based NodeId
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore lettura nastri: {ex.Message}");
                
                // Debug: Mostra dettagli errore
                Console.WriteLine($"Dettagli errore: {ex.StackTrace}");
            }

            return Task.FromResult(nastri);
        }

        private Task<StatoRiempitriceDto?> LeggiDatiRiempitrice()
        {
            try
            {
                // Prepara i nodi da leggere per la riempitrice
                var nodesToRead = new ReadValueIdCollection
                {
                    // CORRETTO: Namespace 2 e string-based NodeId
                    new ReadValueId { NodeId = new NodeId("Stato", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("RicettaInUso", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("ConsumoElettrico", 2), AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = new NodeId("ContatoreBottiglieRiempite", 2), AttributeId = Attributes.Value }
                };

                // Leggi i valori
                sessionRiempitrice!.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out var results, out var diagnostics);

                var riempitrice = new StatoRiempitriceDto
                {
                    Nome = "Riempitrice",
                    Stato = (StatoRiempitrice)Convert.ToInt32(results[0].Value ?? 0),
                    RicettaInUso = results[1].Value?.ToString() ?? "Nessuna",
                    ConsumoElettrico = Convert.ToSingle(results[2].Value ?? 0.0f),
                    ContatoreBottiglieRiempite = Convert.ToUInt32(results[3].Value ?? 0u)
                };

                return Task.FromResult<StatoRiempitriceDto?>(riempitrice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore lettura riempitrice: {ex.Message}");
                Console.WriteLine($"Dettagli errore: {ex.StackTrace}");
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
            Console.WriteLine("=== DASHBOARD AGGREGAZIONE IMPIANTO ===");
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

            // Riassunto nastri
            Console.WriteLine("📊 RIASSUNTO NASTRI:");
            var nastriOperativi = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InFunzione);
            var nastriSpenti = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.Spento);
            var nastriAllarme = ultimiDati.Nastri.Count(n => n.Stato == StatoNastro.InAllarme);
            
            Console.WriteLine($"   Operativi: {nastriOperativi}/6 | Spenti: {nastriSpenti}/6 | In allarme: {nastriAllarme}/6");

            // Stato riempitrice
            if (ultimiDati.Riempitrice != null)
            {
                Console.WriteLine($"🏭 RIEMPITRICE: {ultimiDati.Riempitrice.Stato} | Ricetta: {ultimiDati.Riempitrice.RicettaInUso}");
            }

            Console.WriteLine("\n💡 Premere 'r' per report dettagliato, 'q' per uscire");
        }

        public async Task MostraReportDettagliato()
        {
            Console.Clear();
            Console.WriteLine("=== REPORT DETTAGLIATO IMPIANTO ===");
            Console.WriteLine($"🕒 Generato: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            // Dettaglio nastri
            Console.WriteLine("🎚️  NASTRI TRASPORTATORI:");
            foreach (var nastro in ultimiDati.Nastri)
            {
                var icona = nastro.Stato switch
                {
                    StatoNastro.InFunzione => "🟢",
                    StatoNastro.InAllarme => "🔴",
                    _ => "⚪"
                };
                Console.WriteLine($"   {icona} {nastro.Nome}: {nastro.Stato} | {nastro.ConsumoElettrico:F2}kW | {nastro.ContatoreBottiglie} bottiglie");
            }

            Console.WriteLine();
            
            // Dettaglio riempitrice
            if (ultimiDati.Riempitrice != null)
            {
                var iconaRiempitrice = ultimiDati.Riempitrice.Stato switch
                {
                    StatoRiempitrice.InFunzione => "🟢",
                    StatoRiempitrice.InAllarme => "🔴",
                    StatoRiempitrice.Accesa => "🟡",
                    _ => "⚪"
                };
                Console.WriteLine("🏭 RIEMPITRICE:");
                Console.WriteLine($"   {iconaRiempitrice} {ultimiDati.Riempitrice.Nome}: {ultimiDati.Riempitrice.Stato}");
                Console.WriteLine($"   📋 Ricetta attiva: {ultimiDati.Riempitrice.RicettaInUso}");
                Console.WriteLine($"   ⚡ Consumo: {ultimiDati.Riempitrice.ConsumoElettrico:F2}kW");
                Console.WriteLine($"   🍾 Bottiglie riempite: {ultimiDati.Riempitrice.ContatoreBottiglieRiempite}");
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

            Console.WriteLine("🔌 Connessioni chiuse");
        }
    }
}