using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Configuration;
using RiempitriceServer.Services;

namespace RiempitriceServer
{
    class Program
    {
        private static ApplicationInstance? application;
        private static RiempitriceOpcServer? server;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Server OPC-UA Riempitrice ===");
            Console.WriteLine("Premere Ctrl+C per terminare");

            try
            {
                // Configurazione dell'applicazione OPC-UA
                application = new ApplicationInstance
                {
                    ApplicationName = "Server Riempitrice",
                    ApplicationType = ApplicationType.Server
                };

                // Crea configurazione programmaticamente
                var config = new ApplicationConfiguration()
                {
                    ApplicationName = "Server Riempitrice",
                    ApplicationUri = "urn:localhost:MVLabs:RiempitriceServer",
                    ProductUri = "http://mvlabs.it/RiempitriceServer",
                    ApplicationType = ApplicationType.Server,
                    
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier()
                        {
                            StoreType = "Directory",
                            StorePath = "%LocalApplicationData%/OPC Foundation/pki/own",
                            SubjectName = "CN=Server Riempitrice"
                        },
                        TrustedPeerCertificates = new CertificateTrustList()
                        {
                            StoreType = "Directory",
                            StorePath = "%LocalApplicationData%/OPC Foundation/pki/trusted"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList()
                        {
                            StoreType = "Directory",
                            StorePath = "%LocalApplicationData%/OPC Foundation/pki/issuers"
                        },
                        RejectedCertificateStore = new CertificateTrustList()
                        {
                            StoreType = "Directory",
                            StorePath = "%LocalApplicationData%/OPC Foundation/pki/rejected"
                        },
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        MinimumCertificateKeySize = 1024
                    },
                    
                    ServerConfiguration = new ServerConfiguration()
                    {
                        BaseAddresses = { "opc.tcp://localhost:4843/RiempitriceServer" },
                        SecurityPolicies = new ServerSecurityPolicyCollection()
                        {
                            new ServerSecurityPolicy()
                            {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection()
                        {
                            new UserTokenPolicy()
                            {
                                TokenType = UserTokenType.Anonymous
                            }
                        },
                        MaxSessionCount = 100,
                        MinSessionTimeout = 10000,
                        MaxSessionTimeout = 3600000,
                        MaxBrowseContinuationPoints = 10,
                        MaxQueryContinuationPoints = 10,
                        MaxHistoryContinuationPoints = 100,
                        MaxRequestAge = 600000,
                        MinPublishingInterval = 100,
                        MaxPublishingInterval = 3600000,
                        PublishingResolution = 50,
                        MaxSubscriptionLifetime = 3600000,
                        MaxMessageQueueSize = 10,
                        MaxNotificationQueueSize = 100,
                        MaxNotificationsPerPublish = 1000
                    },
                    
                    TransportQuotas = new TransportQuotas()
                    {
                        OperationTimeout = 600000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65536,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65536,
                        ChannelLifetime = 300000,
                        SecurityTokenLifetime = 3600000
                    }
                };

                // Imposta la configurazione
                application.ApplicationConfiguration = config;

                // Salta il controllo dei certificati e crea automaticamente se necessario
                try 
                {
                    await application.CheckApplicationInstanceCertificate(false, CertificateFactory.DefaultKeySize);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Creazione automatica certificato: {ex.Message}");
                    // Continua comunque - per sviluppo va bene
                }

                // Crea il server
                server = new RiempitriceOpcServer();
                await application.Start(server);

                Console.WriteLine("Server avviato!");
                Console.WriteLine($"Endpoint: opc.tcp://localhost:4843/RiempitriceServer");
                
                // Gestione dell'arresto graceful
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\nArresto del server in corso...");
                    server?.Stop();
                };

                // Mantieni il server in esecuzione
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                server?.Stop();
                Console.WriteLine("Server arrestato.");
            }
        }
    }
}