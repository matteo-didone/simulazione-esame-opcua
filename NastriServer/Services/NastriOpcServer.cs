using Opc.Ua;
using Opc.Ua.Server;
using NastriServer.Models;
using Shared;
using Shared.CustomTypes;

namespace NastriServer.Services
{
    /// <summary>
    /// Server OPC-UA Enhanced per i nastri con template professionali
    /// </summary>
    public class NastriOpcServer : StandardServer
    {
        private List<Nastro> nastri = new();
        private Timer? updateTimer;
        private EnhancedNastriNodeManager? nodeManager;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creazione del NodeManager Enhanced...");

            // Crea i 6 nastri
            for (int i = 1; i <= 6; i++)
            {
                nastri.Add(new Nastro(i, $"Nastro {i}"));
            }

            // Crea il node manager enhanced
            nodeManager = new EnhancedNastriNodeManager(server, configuration, nastri);
            var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManager);

            // Avvia il timer per aggiornare i dati ogni 2 secondi
            updateTimer = new Timer(UpdateNastriData, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            return masterNodeManager;
        }

        private void UpdateNastriData(object? state)
        {
            // Aggiorna i dati di tutti i nastri
            foreach (var nastro in nastri)
            {
                nastro.Aggiorna();
            }

            // Aggiorna i nodi OPC-UA
            nodeManager?.UpdateNodes();

            // Log periodico dello stato
            if (DateTime.Now.Second % 10 == 0) // Ogni 10 secondi
            {
                Console.WriteLine("=== Stato Nastri Enhanced ===");
                foreach (var nastro in nastri)
                {
                    Console.WriteLine($"  {nastro}");
                }
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
    /// NodeManager Enhanced con Template Professionali per i nastri - VERSION SEMPLIFICATA
    /// </summary>
    public class EnhancedNastriNodeManager : CustomNodeManager2
    {
        private List<Nastro> nastri;
        private Dictionary<string, BaseDataVariableState> variables = new();

        public EnhancedNastriNodeManager(IServerInternal server, ApplicationConfiguration configuration, List<Nastro> nastri)
            : base(server, configuration, "http://mvlabs.it/nastri")
        {
            this.nastri = nastri;
            SetNamespaces("http://mvlabs.it/nastri");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // APPROCCIO SEMPLIFICATO: Crea direttamente la cartella nastri sotto Objects
                CreateNastriDirectly(externalReferences);
            }
        }

        private void CreateNastriDirectly(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            Console.WriteLine("🏗️ Creazione nastri direttamente sotto Objects...");

            // Crea la cartella Nastri direttamente sotto Objects
            var nastriFolder = new FolderState(null);
            nastriFolder.NodeId = new NodeId("Nastri", NamespaceIndex);
            nastriFolder.BrowseName = new QualifiedName("Nastri", NamespaceIndex);
            nastriFolder.DisplayName = new LocalizedText("it", "Nastri Trasportatori");
            nastriFolder.Description = new LocalizedText("it", "Sistema di nastri trasportatori Enhanced");
            nastriFolder.TypeDefinitionId = ObjectTypeIds.FolderType;

            // Aggiungi riferimento al nodo Objects
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
            {
                externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
            }

            nastriFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, nastriFolder.NodeId));

            // Crea ogni singolo nastro Enhanced
            foreach (var nastro in nastri)
            {
                Console.WriteLine($"🔧 Creando Nastro {nastro.Id} Enhanced...");
                CreateSingleNastroEnhanced(nastriFolder, nastro);
            }

            // Aggiungi all'address space
            AddPredefinedNode(SystemContext, nastriFolder);
            
            Console.WriteLine($"✅ Creati {nastri.Count} nastri Enhanced sotto Objects/Nastri");
        }

        private void CreateSingleNastroEnhanced(FolderState parent, Nastro nastro)
        {
            // Oggetto nastro principale
            var nastroObj = new BaseObjectState(parent);
            nastroObj.NodeId = new NodeId($"Nastro{nastro.Id}", NamespaceIndex);
            nastroObj.BrowseName = new QualifiedName($"Nastro{nastro.Id}", NamespaceIndex);
            nastroObj.DisplayName = new LocalizedText("it", nastro.Nome);
            nastroObj.Description = new LocalizedText("it", $"Nastro trasportatore #{nastro.Id} Enhanced");
            nastroObj.TypeDefinitionId = ObjectTypeIds.BaseObjectType;

            // PARAMETRI - Variabili di stato (read-only)
            var statoVar = CreateNastroVariable(nastroObj, $"Stato{nastro.Id}", "Stato", "Stato Operativo", 
                DataTypeIds.Int32, false, 0);
            variables[$"{nastro.Id}_Stato"] = statoVar;

            var velocitaVar = CreateNastroVariable(nastroObj, $"Velocita{nastro.Id}", "Velocita", "Velocità (m/min)", 
                DataTypeIds.Float, true, 0.0f);
            variables[$"{nastro.Id}_Velocita"] = velocitaVar;

            var consumoVar = CreateNastroVariable(nastroObj, $"Consumo{nastro.Id}", "ConsumoElettrico", "Consumo Elettrico (kW)", 
                DataTypeIds.Float, false, 0.0f);
            variables[$"{nastro.Id}_ConsumoElettrico"] = consumoVar;

            var contatoreVar = CreateNastroVariable(nastroObj, $"Contatore{nastro.Id}", "ContatoreBottiglie", "Contatore Bottiglie", 
                DataTypeIds.UInt32, false, (uint)0);
            variables[$"{nastro.Id}_ContatoreBottiglie"] = contatoreVar;

            // CONTROLLI - Variabili scrivibili
            var accesoVar = CreateNastroVariable(nastroObj, $"Acceso{nastro.Id}", "Acceso", "Comando Accensione", 
                DataTypeIds.Boolean, true, false);
            accesoVar.OnWriteValue = OnWriteValue;
            variables[$"{nastro.Id}_Acceso"] = accesoVar;

            var automaticoVar = CreateNastroVariable(nastroObj, $"Automatico{nastro.Id}", "Automatico", "Modalità Automatica", 
                DataTypeIds.Boolean, true, true);
            automaticoVar.OnWriteValue = OnWriteValue;
            variables[$"{nastro.Id}_Automatico"] = automaticoVar;

            // DIAGNOSTICA
            var tempoFunzVar = CreateNastroVariable(nastroObj, $"TempoFunzionamento{nastro.Id}", "TempoFunzionamento", "Tempo Funzionamento (h)", 
                DataTypeIds.Double, false, 0.0);
            variables[$"{nastro.Id}_TempoFunzionamento"] = tempoFunzVar;

            var numAvviiVar = CreateNastroVariable(nastroObj, $"NumeroAvvii{nastro.Id}", "NumeroAvvii", "Numero Avvii", 
                DataTypeIds.UInt32, false, (uint)0);
            variables[$"{nastro.Id}_NumeroAvvii"] = numAvviiVar;

            parent.AddChild(nastroObj);
            
            Console.WriteLine($"  ✅ Nastro {nastro.Id} Enhanced creato con {variables.Count(v => v.Key.StartsWith($"{nastro.Id}_"))} variabili");
        }

        private BaseDataVariableState CreateNastroVariable(NodeState parent, string nodeId, string browseName, 
            string displayName, NodeId dataType, bool writable, object defaultValue)
        {
            var variable = new BaseDataVariableState(parent);
            variable.NodeId = new NodeId(nodeId, NamespaceIndex);
            variable.BrowseName = new QualifiedName(browseName, NamespaceIndex);
            variable.DisplayName = new LocalizedText("it", displayName);
            variable.Description = new LocalizedText("it", $"{displayName} del nastro");
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.DataType = dataType;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.UserAccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.Historizing = false;
            variable.Value = defaultValue;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            parent.AddChild(variable);
            return variable;
        }

        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, NumericRange indexRange, 
            QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            try
            {
                var variable = node as BaseDataVariableState;
                if (variable == null) return StatusCodes.BadInternalError;

                var nodeIdStr = variable.NodeId.ToString();
                var browseName = variable.BrowseName.Name;

                Console.WriteLine($"🔧 Scrittura Enhanced: {browseName} = {value} su {nodeIdStr}");

                // Estrai l'ID del nastro dal NodeId
                int nastroId = ExtractNastroIdFromNodeId(nodeIdStr);
                if (nastroId == -1) return StatusCodes.BadNodeIdUnknown;

                var nastroTarget = nastri.FirstOrDefault(n => n.Id == nastroId);
                if (nastroTarget == null) return StatusCodes.BadNodeIdUnknown;

                // Applica la modifica in base al tipo di variabile
                switch (browseName)
                {
                    case "Acceso":
                        if (value is bool acceso)
                        {
                            nastroTarget.Acceso = acceso;
                            Console.WriteLine($"✅ {nastroTarget.Nome} Enhanced - Accensione: {acceso}");
                        }
                        break;

                    case "Automatico":
                        if (value is bool automatico)
                        {
                            nastroTarget.ModoAutomatico = automatico;
                            nastroTarget.Modalita = automatico ? Modalita.Automatico : Modalita.Manuale;
                            Console.WriteLine($"✅ {nastroTarget.Nome} Enhanced - Modalità: {nastroTarget.Modalita}");
                        }
                        break;

                    case "Velocita":
                        if (value is float velocita)
                        {
                            Console.WriteLine($"✅ {nastroTarget.Nome} Enhanced - Velocità: {velocita} m/min");
                        }
                        break;
                }

                statusCode = StatusCodes.Good;
                timestamp = DateTime.UtcNow;
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore scrittura Enhanced: {ex.Message}");
                return StatusCodes.BadInternalError;
            }
        }

        private int ExtractNastroIdFromNodeId(string nodeIdStr)
        {
            try
            {
                // Il NodeId è del tipo "ns=2;s=Acceso3" - estrai il numero finale
                if (nodeIdStr.Contains(";s="))
                {
                    var stringPart = nodeIdStr.Split(";s=")[1];
                    
                    // Trova il numero alla fine
                    for (int i = stringPart.Length - 1; i >= 0; i--)
                    {
                        if (char.IsDigit(stringPart[i]))
                        {
                            // Trova tutti i digit consecutivi dalla fine
                            int digitStart = i;
                            while (digitStart > 0 && char.IsDigit(stringPart[digitStart - 1]))
                            {
                                digitStart--;
                            }

                            var numberStr = stringPart.Substring(digitStart, i - digitStart + 1);
                            if (int.TryParse(numberStr, out int nastroId))
                            {
                                return nastroId;
                            }
                        }
                    }
                }
                
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        public void UpdateNodes()
        {
            try
            {
                lock (Lock)
                {
                    // Aggiorna i valori di tutti i nodi dei nastri
                    foreach (var nastro in nastri)
                    {
                        UpdateNastroVariable($"{nastro.Id}_Stato", (int)nastro.Stato);
                        UpdateNastroVariable($"{nastro.Id}_ConsumoElettrico", nastro.ConsumoElettrico);
                        UpdateNastroVariable($"{nastro.Id}_ContatoreBottiglie", nastro.ContatoreBottiglie);
                        UpdateNastroVariable($"{nastro.Id}_Acceso", nastro.Acceso);
                        UpdateNastroVariable($"{nastro.Id}_Automatico", nastro.ModoAutomatico);

                        // Aggiorna diagnostica
                        UpdateNastroVariable($"{nastro.Id}_TempoFunzionamento", GetTempoFunzionamento(nastro));
                        UpdateNastroVariable($"{nastro.Id}_NumeroAvvii", GetNumeroAvvii(nastro));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore aggiornamento nodi Enhanced: {ex.Message}");
            }
        }

        private void UpdateNastroVariable(string key, object value)
        {
            if (variables.TryGetValue(key, out var variable))
            {
                variable.Value = value;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
            }
        }

        private double GetTempoFunzionamento(Nastro nastro)
        {
            return nastro.ContatoreBottiglie * 0.01; // 0.01 ore per bottiglia
        }

        private uint GetNumeroAvvii(Nastro nastro)
        {
            return (uint)(nastro.ContatoreBottiglie / 100); // Un avvio ogni 100 bottiglie
        }
    }
}