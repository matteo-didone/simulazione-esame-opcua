using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using ClientAggregatore.Services;

namespace ClientAggregatore
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Client Aggregatore OPC-UA ===");
            Console.WriteLine("Connessione ai server Nastri e Riempitrice...");

            try
            {
                // Configurazione dell'applicazione client
                var application = new ApplicationInstance
                {
                    ApplicationName = "Client Aggregatore",
                    ApplicationType = ApplicationType.Client
                };

                // Configurazione client semplificata
                var config = new ApplicationConfiguration()
                {
                    ApplicationName = "Client Aggregatore",
                    ApplicationUri = "urn:localhost:MVLabs:ClientAggregatore",
                    ProductUri = "http://mvlabs.it/ClientAggregatore",
                    ApplicationType = ApplicationType.Client,
                    
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier(),
                        TrustedPeerCertificates = new CertificateTrustList(),
                        TrustedIssuerCertificates = new CertificateTrustList(),
                        RejectedCertificateStore = new CertificateTrustList(),
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        MinimumCertificateKeySize = 1024
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

                application.ApplicationConfiguration = config;

                // Ignora certificati per semplicità
                try 
                {
                    await application.CheckApplicationInstanceCertificate(false, CertificateFactory.DefaultKeySize);
                }
                catch 
                {
                    // Ignora errori certificati per sviluppo
                }

                // Crea il service aggregatore
                var aggregatore = new AggregatoreService(config);
                
                // Avvia l'aggregazione
                await aggregatore.AvviaAggregazione();

                Console.WriteLine("\nClient aggregatore avviato!");
                Console.WriteLine("Premere 'q' per uscire, 'r' per report dettagliato");

                // Loop principale
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        break;
                    }
                    else if (key.KeyChar == 'r' || key.KeyChar == 'R')
                    {
                        await aggregatore.MostraReportDettagliato();
                    }
                }

                // Arresta l'aggregazione
                await aggregatore.FermaAggregazione();
                Console.WriteLine("Client aggregatore arrestato.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}