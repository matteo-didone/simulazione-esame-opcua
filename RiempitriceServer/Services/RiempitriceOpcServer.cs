using Opc.Ua;
using Opc.Ua.Server;
using RiempitriceServer.Models;
using Shared;
using Shared.CustomTypes;

namespace RiempitriceServer.Services
{
    /// <summary>
    /// Server OPC-UA Enhanced per la riempitrice con template professionali
    /// </summary>
    public class RiempitriceOpcServer : StandardServer
    {
        private Riempitrice riempitrice = new();
        private Timer? updateTimer;
        private EnhancedRiempitriceNodeManager? nodeManager;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creazione del NodeManager Enhanced per Riempitrice...");

            // Crea il node manager enhanced
            nodeManager = new EnhancedRiempitriceNodeManager(server, configuration, riempitrice);
            var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManager);

            // Avvia il timer per aggiornare i dati ogni 2 secondi
            updateTimer = new Timer(UpdateRiempitriceData, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            return masterNodeManager;
        }

        private void UpdateRiempitriceData(object? state)
        {
            // Aggiorna i dati della riempitrice
            riempitrice.Aggiorna();

            // Aggiorna i nodi OPC-UA
            nodeManager?.UpdateNodes();

            // Log periodico dello stato
            if (DateTime.Now.Second % 10 == 0) // Ogni 10 secondi
            {
                Console.WriteLine("=== Stato Riempitrice Enhanced ===");
                Console.WriteLine($"  {riempitrice}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// NodeManager Enhanced con Template Professionali per la riempitrice
    /// </summary>
    public class EnhancedRiempitriceNodeManager : CustomNodeManager2
    {
        private Riempitrice riempitrice;
        private Dictionary<string, BaseDataVariableState> variables = new();
        private BaseObjectState? riempitriceInstance;

        public EnhancedRiempitriceNodeManager(IServerInternal server, ApplicationConfiguration configuration, Riempitrice riempitrice)
            : base(server, configuration, "http://mvlabs.it/riempitrice")
        {
            this.riempitrice = riempitrice;
            SetNamespaces("http://mvlabs.it/riempitrice");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Crea l'istanza della riempitrice usando i template professionali
                CreateRiempitriceInstance(externalReferences);
            }
        }

        private void CreateRiempitriceInstance(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            // Crea l'istanza principale della riempitrice usando il template professionale
            riempitriceInstance = IndustrialComponentTemplates.CreateProfessionalRiempitrice(null, riempitrice.Nome, NamespaceIndex);

            // Aggiungi riferimento al nodo Objects
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
            {
                externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
            }

            riempitriceInstance.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, riempitriceInstance.NodeId));

            // Registra le variabili per gli aggiornamenti
            RegisterRiempitriceVariables();

            // Aggiungi all'address space
            AddPredefinedNode(SystemContext, riempitriceInstance);
        }

        private void RegisterRiempitriceVariables()
        {
            if (riempitriceInstance == null) return;

            // Crea le variabili professionali direttamente e le registra
            CreateRiempitriceVariables();
        }

        private void CreateRiempitriceVariables()
        {
            if (riempitriceInstance == null) return;

            // === CARTELLA PARAMETRI ===
            var parametriFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceInstance,
                "Riempitrice_Parametri",
                "Parametri",
                "Parametri Operativi",
                "Parametri di stato e configurazione della riempitrice",
                NamespaceIndex
            );

            // Stato
            var statoVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "Stato",
                "Stato",
                "Stato Operativo",
                "Stato corrente: 0=Spenta, 1=Accesa, 2=InFunzione, 3=InAllarme",
                DataTypeIds.Int32,
                NamespaceIndex,
                writable: false,
                defaultValue: 0
            );
            variables["Stato"] = statoVar;

            // Ricetta in Uso
            var ricettaVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "RicettaInUso",
                "RicettaInUso",
                "Ricetta Attiva",
                "Nome della ricetta di produzione attualmente in uso",
                DataTypeIds.String,
                NamespaceIndex,
                writable: false,
                defaultValue: "Nessuna"
            );
            variables["RicettaInUso"] = ricettaVar;

            // Velocità Riempimento
            var velocitaRiempimentoVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "VelocitaRiempimento",
                "VelocitaRiempimento",
                "Velocità Riempimento",
                "Velocità di riempimento delle bottiglie",
                DataTypeIds.Float,
                NamespaceIndex,
                writable: true,
                unit: "bot/min",
                minValue: 0.0,
                maxValue: 200.0,
                defaultValue: 0.0f
            );
            variables["VelocitaRiempimento"] = velocitaRiempimentoVar;

            // Consumo Elettrico
            var consumoVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "ConsumoElettrico",
                "ConsumoElettrico",
                "Consumo Elettrico",
                "Consumo elettrico istantaneo della riempitrice",
                DataTypeIds.Float,
                NamespaceIndex,
                writable: false,
                unit: "kW",
                minValue: 0.0,
                maxValue: 100.0,
                defaultValue: 0.0f
            );
            variables["ConsumoElettrico"] = consumoVar;

            // Contatore Bottiglie Riempite
            var contatoreVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "ContatoreBottiglieRiempite",
                "ContatoreBottiglieRiempite",
                "Bottiglie Riempite",
                "Numero totale di bottiglie riempite",
                DataTypeIds.UInt32,
                NamespaceIndex,
                writable: false,
                unit: "pz",
                defaultValue: (uint)0
            );
            variables["ContatoreBottiglieRiempite"] = contatoreVar;

            // === CARTELLA RICETTE ===
            var ricetteFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceInstance,
                "Riempitrice_Ricette",
                "Ricette",
                "Gestione Ricette",
                "Configurazione e gestione delle ricette di produzione",
                NamespaceIndex
            );

            // Ricette Disponibili
            var ricetteDisponibiliVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                ricetteFolder,
                "RicetteDisponibili",
                "RicetteDisponibili",
                "Ricette Disponibili",
                "Elenco delle ricette di produzione disponibili",
                DataTypeIds.String,
                NamespaceIndex,
                writable: false,
                defaultValue: riempitrice.RicetteDisponibili.ToArray()
            );
            ricetteDisponibiliVar.ValueRank = ValueRanks.OneDimension;
            variables["RicetteDisponibili"] = ricetteDisponibiliVar;

            // === CARTELLA CONTROLLO ===
            var controlloFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceInstance,
                "Riempitrice_Controllo",
                "Controllo",
                "Comandi di Controllo",
                "Interfaccia per il controllo della riempitrice",
                NamespaceIndex
            );

            // Accesa (comando)
            var accesaVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                "Accesa",
                "Accesa",
                "Comando Accensione",
                "Comando per accendere/spegnere la riempitrice",
                DataTypeIds.Boolean,
                NamespaceIndex,
                writable: true,
                defaultValue: false
            );
            accesaVar.OnWriteValue = OnWriteValue;
            variables["Accesa"] = accesaVar;

            // Cambia Ricetta (comando)
            var cambiaRicettaVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                "CambiaRicetta",
                "CambiaRicetta",
                "Comando Cambio Ricetta",
                "Imposta una nuova ricetta di produzione",
                DataTypeIds.String,
                NamespaceIndex,
                writable: true,
                defaultValue: string.Empty
            );
            cambiaRicettaVar.OnWriteValue = OnWriteValue;
            variables["CambiaRicetta"] = cambiaRicettaVar;

            // === CARTELLA DIAGNOSTICA ===
            var diagnosticaFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceInstance,
                "Riempitrice_Diagnostica",
                "Diagnostica",
                "Informazioni Diagnostiche",
                "Dati per manutenzione e diagnostica della riempitrice",
                NamespaceIndex
            );

            // Tempo Funzionamento
            var tempoFunzionamentoVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                diagnosticaFolder,
                "TempoFunzionamento",
                "TempoFunzionamento",
                "Ore di Funzionamento",
                "Tempo totale di funzionamento della riempitrice",
                DataTypeIds.Double,
                NamespaceIndex,
                writable: false,
                unit: "h",
                defaultValue: 0.0
            );
            variables["TempoFunzionamento"] = tempoFunzionamentoVar;

            // Numero Avvii
            var numeroAvviiVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                diagnosticaFolder,
                "NumeroAvvii",
                "NumeroAvvii",
                "Numero Avvii",
                "Contatore del numero di avvii della riempitrice",
                DataTypeIds.UInt32,
                NamespaceIndex,
                writable: false,
                defaultValue: (uint)0
            );
            variables["NumeroAvvii"] = numeroAvviiVar;

            // Efficienza
            var efficienzaVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                diagnosticaFolder,
                "Efficienza",
                "Efficienza",
                "Efficienza Operativa",
                "Percentuale di efficienza della riempitrice",
                DataTypeIds.Float,
                NamespaceIndex,
                writable: false,
                unit: "%",
                minValue: 0.0,
                maxValue: 100.0,
                defaultValue: 0.0f
            );
            variables["Efficienza"] = efficienzaVar;
        }

        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, NumericRange indexRange, 
            QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            try
            {
                var variable = node as BaseDataVariableState;
                if (variable == null) return StatusCodes.BadInternalError;

                var browseName = variable.BrowseName.Name;

                Console.WriteLine($"🔧 Scrittura Riempitrice Enhanced: {browseName} = {value}");

                // Applica la modifica in base al tipo di variabile
                switch (browseName)
                {
                    case "Accesa":
                        if (value is bool accesa)
                        {
                            riempitrice.Accesa = accesa;
                            Console.WriteLine($"✅ {riempitrice.Nome} - Accensione: {accesa}");
                        }
                        break;

                    case "CambiaRicetta":
                        if (value is string nuovaRicetta && !string.IsNullOrEmpty(nuovaRicetta))
                        {
                            riempitrice.CambiaRicetta(nuovaRicetta);
                            Console.WriteLine($"✅ {riempitrice.Nome} - Nuova ricetta: {nuovaRicetta}");
                        }
                        break;

                    case "VelocitaRiempimento":
                        if (value is float velocita)
                        {
                            Console.WriteLine($"✅ {riempitrice.Nome} - Velocità riempimento impostata: {velocita} bot/min");
                        }
                        break;
                }

                statusCode = StatusCodes.Good;
                timestamp = DateTime.UtcNow;
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore scrittura Riempitrice Enhanced: {ex.Message}");
                return StatusCodes.BadInternalError;
            }
        }

        public void UpdateNodes()
        {
            try
            {
                lock (Lock)
                {
                    // Aggiorna i valori di tutti i nodi della riempitrice
                    UpdateRiempitriceVariable("Stato", (int)riempitrice.Stato);
                    UpdateRiempitriceVariable("RicettaInUso", riempitrice.RicettaInUso);
                    UpdateRiempitriceVariable("ConsumoElettrico", riempitrice.ConsumoElettrico);
                    UpdateRiempitriceVariable("ContatoreBottiglieRiempite", riempitrice.ContatoreBottiglieRiempite);
                    UpdateRiempitriceVariable("Accesa", riempitrice.Accesa);
                    UpdateRiempitriceVariable("RicetteDisponibili", riempitrice.RicetteDisponibili.ToArray());

                    // Aggiorna anche le informazioni diagnostiche
                    UpdateRiempitriceVariable("TempoFunzionamento", GetTempoFunzionamento());
                    UpdateRiempitriceVariable("NumeroAvvii", GetNumeroAvvii());
                    UpdateRiempitriceVariable("Efficienza", GetEfficienza());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore aggiornamento nodi Riempitrice Enhanced: {ex.Message}");
            }
        }

        private void UpdateRiempitriceVariable(string key, object value)
        {
            if (variables.TryGetValue(key, out var variable))
            {
                variable.Value = value;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
            }
        }

        private double GetTempoFunzionamento()
        {
            // Simulazione del tempo di funzionamento in ore
            return riempitrice.ContatoreBottiglieRiempite * 0.005; // 0.005 ore per bottiglia
        }

        private uint GetNumeroAvvii()
        {
            // Simulazione del numero di avvii
            return (uint)(riempitrice.ContatoreBottiglieRiempite / 50); // Un avvio ogni 50 bottiglie
        }

        private float GetEfficienza()
        {
            // Calcola efficienza basata sullo stato
            return riempitrice.Stato switch
            {
                StatoRiempitrice.InFunzione => Random.Shared.NextSingle() * 20.0f + 80.0f, // 80-100%
                StatoRiempitrice.Accesa => Random.Shared.NextSingle() * 30.0f + 50.0f, // 50-80%
                StatoRiempitrice.InAllarme => Random.Shared.NextSingle() * 20.0f, // 0-20%
                _ => 0.0f
            };
        }
    }
}