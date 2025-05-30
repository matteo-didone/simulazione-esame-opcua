using Opc.Ua;
using Opc.Ua.Server;
using RiempitriceServer.Models;
using Shared;

namespace RiempitriceServer.Services
{
    /// <summary>
    /// Server OPC-UA per la riempitrice
    /// </summary>
    public class RiempitriceOpcServer : StandardServer
    {
        private Riempitrice riempitrice = new();
        private Timer? updateTimer;
        private RiempitriceNodeManager? nodeManager;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creazione del NodeManager per Riempitrice...");

            // Crea il node manager personalizzato
            nodeManager = new RiempitriceNodeManager(server, configuration, riempitrice);
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
                Console.WriteLine("=== Stato Riempitrice ===");
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
    /// NodeManager per gestire i nodi della riempitrice
    /// </summary>
    public class RiempitriceNodeManager : CustomNodeManager2
    {
        private Riempitrice riempitrice;
        private Dictionary<string, BaseDataVariableState> variables = new();

        public RiempitriceNodeManager(IServerInternal server, ApplicationConfiguration configuration, Riempitrice riempitrice)
            : base(server, configuration, "http://mvlabs.it/riempitrice")
        {
            this.riempitrice = riempitrice;
            
            // Imposta i namespace
            SetNamespaces("http://mvlabs.it/riempitrice");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Crea il nodo radice "Riempitrice"
                FolderState riempitriceFolder = new FolderState(null);
                riempitriceFolder.NodeId = new NodeId("Riempitrice", NamespaceIndex);
                riempitriceFolder.BrowseName = new QualifiedName("Riempitrice", NamespaceIndex);
                riempitriceFolder.DisplayName = new LocalizedText("en", "Riempitrice");
                riempitriceFolder.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Aggiungi riferimento al nodo Objects
                IList<IReference>? references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                riempitriceFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, riempitriceFolder.NodeId));

                // Crea i nodi per la riempitrice
                CreateRiempitriceNodes(riempitriceFolder);

                // Aggiungi tutto all'address space
                AddPredefinedNode(SystemContext, riempitriceFolder);
            }
        }

        private void CreateRiempitriceNodes(FolderState parent)
        {
            // Variabili di stato (read-only)
            CreateRiempitriceVariable(parent, "Stato", "Stato", DataTypeIds.Int32);
            CreateRiempitriceVariable(parent, "RicettaInUso", "RicettaInUso", DataTypeIds.String);
            CreateRiempitriceVariable(parent, "ConsumoElettrico", "ConsumoElettrico", DataTypeIds.Float);
            CreateRiempitriceVariable(parent, "ContatoreBottiglieRiempite", "ContatoreBottiglieRiempite", DataTypeIds.UInt32);

            // Variabili di controllo (read/write)
            CreateRiempitriceVariable(parent, "Accesa", "Accesa", DataTypeIds.Boolean, true);

            // Array delle ricette disponibili (read-only)
            CreateRiempitriceVariable(parent, "RicetteDisponibili", "RicetteDisponibili", DataTypeIds.String, false, true);

            // Metodo per cambiare ricetta (write-only)
            CreateRiempitriceVariable(parent, "CambiaRicetta", "CambiaRicetta", DataTypeIds.String, true);
        }

        private BaseDataVariableState CreateRiempitriceVariable(NodeState parent, string nodeId, string name, NodeId dataType, bool writable = false, bool isArray = false)
        {
            var variable = new BaseDataVariableState(parent);
            variable.NodeId = new NodeId(nodeId, NamespaceIndex);
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.DataType = dataType;
            variable.ValueRank = isArray ? ValueRanks.OneDimension : ValueRanks.Scalar;
            variable.AccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.UserAccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.Historizing = false;
            variable.Value = GetDefaultValue(dataType, isArray);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (writable)
            {
                variable.OnWriteValue = OnWriteValue;
            }

            parent.AddChild(variable);

            // Salva la variabile per gli aggiornamenti
            variables[name] = variable;
            
            return variable;
        }

        private object GetDefaultValue(NodeId dataType, bool isArray = false)
        {
            if (isArray && dataType == DataTypeIds.String)
            {
                return riempitrice.RicetteDisponibili.ToArray();
            }
            
            if (dataType == DataTypeIds.Boolean) return false;
            if (dataType == DataTypeIds.Int32) return 0;
            if (dataType == DataTypeIds.UInt32) return (uint)0;
            if (dataType == DataTypeIds.Float) return 0.0f;
            if (dataType == DataTypeIds.String) return string.Empty;
            return 0;
        }

        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            try
            {
                var variable = node as BaseDataVariableState;
                if (variable == null) return StatusCodes.BadInternalError;

                var browseName = variable.BrowseName.Name;

                // Applica la modifica in base al tipo di variabile
                switch (browseName)
                {
                    case "Accesa":
                        if (value is bool accesa)
                        {
                            riempitrice.Accesa = accesa;
                            Console.WriteLine($"{riempitrice.Nome} - Accensione: {accesa}");
                        }
                        break;

                    case "CambiaRicetta":
                        if (value is string nuovaRicetta && !string.IsNullOrEmpty(nuovaRicetta))
                        {
                            riempitrice.CambiaRicetta(nuovaRicetta);
                            Console.WriteLine($"{riempitrice.Nome} - Nuova ricetta: {nuovaRicetta}");
                        }
                        break;

                    default:
                        return StatusCodes.BadNotWritable;
                }

                statusCode = StatusCodes.Good;
                timestamp = DateTime.UtcNow;
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore scrittura: {ex.Message}");
                return StatusCodes.BadInternalError;
            }
        }

        public void UpdateNodes()
        {
            try
            {
                lock (Lock)
                {
                    // Aggiorna i valori di tutti i nodi
                    UpdateRiempitriceVariable("Stato", (int)riempitrice.Stato);
                    UpdateRiempitriceVariable("RicettaInUso", riempitrice.RicettaInUso);
                    UpdateRiempitriceVariable("ConsumoElettrico", riempitrice.ConsumoElettrico);
                    UpdateRiempitriceVariable("ContatoreBottiglieRiempite", riempitrice.ContatoreBottiglieRiempite);
                    UpdateRiempitriceVariable("Accesa", riempitrice.Accesa);
                    UpdateRiempitriceVariable("RicetteDisponibili", riempitrice.RicetteDisponibili.ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore aggiornamento nodi: {ex.Message}");
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
    }
}