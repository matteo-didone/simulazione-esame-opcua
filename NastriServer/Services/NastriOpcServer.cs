using Opc.Ua;
using Opc.Ua.Server;
using NastriServer.Models;
using Shared;

namespace NastriServer.Services
{
    /// <summary>
    /// Server OPC-UA semplificato per i nastri
    /// </summary>
    public class NastriOpcServer : StandardServer
    {
        private List<Nastro> nastri = new();
        private Timer? updateTimer;
        private NastriNodeManager? nodeManager;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creazione del NodeManager...");

            // Crea i 6 nastri
            for (int i = 1; i <= 6; i++)
            {
                nastri.Add(new Nastro(i, $"Nastro {i}"));
            }

            // Crea il node manager personalizzato
            nodeManager = new NastriNodeManager(server, configuration, nastri);
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
                Console.WriteLine("=== Stato Nastri ===");
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
    /// NodeManager semplificato per gestire i nodi dei nastri
    /// </summary>
    public class NastriNodeManager : CustomNodeManager2
    {
        private List<Nastro> nastri;
        private Dictionary<string, BaseDataVariableState> variables = new();

        public NastriNodeManager(IServerInternal server, ApplicationConfiguration configuration, List<Nastro> nastri)
            : base(server, configuration, "http://mvlabs.it/nastri")
        {
            this.nastri = nastri;

            // Imposta i namespace
            SetNamespaces("http://mvlabs.it/nastri");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Crea il nodo radice "Nastri"
                FolderState nastriFolder = new FolderState(null);
                nastriFolder.NodeId = new NodeId("Nastri", NamespaceIndex);
                nastriFolder.BrowseName = new QualifiedName("Nastri", NamespaceIndex);
                nastriFolder.DisplayName = new LocalizedText("en", "Nastri");
                nastriFolder.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Aggiungi riferimento al nodo Objects
                IList<IReference>? references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                nastriFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, nastriFolder.NodeId));

                // Crea i nodi per ogni nastro
                foreach (var nastro in nastri)
                {
                    CreateNastroNodes(nastriFolder, nastro);
                }

                // Aggiungi tutto all'address space
                AddPredefinedNode(SystemContext, nastriFolder);
            }
        }

        private void CreateNastroNodes(FolderState parent, Nastro nastro)
        {
            // Crea cartella per il nastro
            FolderState nastroFolder = new FolderState(parent);
            nastroFolder.NodeId = new NodeId($"Nastro{nastro.Id}", NamespaceIndex);
            nastroFolder.BrowseName = new QualifiedName($"Nastro{nastro.Id}", NamespaceIndex);
            nastroFolder.DisplayName = new LocalizedText("en", nastro.Nome);
            nastroFolder.TypeDefinitionId = ObjectTypeIds.FolderType;

            parent.AddChild(nastroFolder);

            // Variabili di stato (read-only)
            CreateNastroVariable(nastroFolder, $"Stato{nastro.Id}", "Stato", DataTypeIds.Int32, nastro.Id);
            CreateNastroVariable(nastroFolder, $"StatoMarcia{nastro.Id}", "StatoMarcia", DataTypeIds.Int32, nastro.Id);
            CreateNastroVariable(nastroFolder, $"Modalita{nastro.Id}", "Modalita", DataTypeIds.Int32, nastro.Id);
            CreateNastroVariable(nastroFolder, $"Consumo{nastro.Id}", "ConsumoElettrico", DataTypeIds.Float, nastro.Id);
            CreateNastroVariable(nastroFolder, $"Contatore{nastro.Id}", "ContatoreBottiglie", DataTypeIds.UInt32, nastro.Id);

            // Variabili di controllo (read/write)
            CreateNastroVariable(nastroFolder, $"Acceso{nastro.Id}", "Acceso", DataTypeIds.Boolean, nastro.Id, true);
            CreateNastroVariable(nastroFolder, $"Automatico{nastro.Id}", "Automatico", DataTypeIds.Boolean, nastro.Id, true);
        }

        private BaseDataVariableState CreateNastroVariable(NodeState parent, string nodeId, string name, NodeId dataType, int nastroId, bool writable = false)
        {
            var variable = new BaseDataVariableState(parent);
            variable.NodeId = new NodeId(nodeId, NamespaceIndex);
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.DataType = dataType;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.UserAccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.Historizing = false;
            variable.Value = GetDefaultValue(dataType);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (writable)
            {
                variable.OnWriteValue = OnWriteValue;
            }

            parent.AddChild(variable);

            // Salva la variabile per gli aggiornamenti
            variables[$"{nastroId}_{name}"] = variable;

            return variable;
        }

        private object GetDefaultValue(NodeId dataType)
        {
            if (dataType == DataTypeIds.Boolean) return false;
            if (dataType == DataTypeIds.Int32) return 0;
            if (dataType == DataTypeIds.UInt32) return (uint)0;
            if (dataType == DataTypeIds.Float) return 0.0f;
            return 0;
        }

        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            try
            {
                var variable = node as BaseDataVariableState;
                if (variable == null) return StatusCodes.BadInternalError;

                // Trova il nastro corrispondente dal NodeId
                var nodeIdStr = variable.NodeId.ToString();
                var browseName = variable.BrowseName.Name;

                Console.WriteLine($"DEBUG: Scrittura su {nodeIdStr}, BrowseName: {browseName}");

                // Estrai l'ID del nastro dal NodeId string
                int nastroId = -1;

                // Il NodeId è del tipo "ns=2;s=Acceso3" - estrai il numero finale
                if (nodeIdStr.Contains(";s="))
                {
                    var stringPart = nodeIdStr.Split(";s=")[1]; // "Acceso3"

                    // Estrai il numero finale
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
                            if (int.TryParse(numberStr, out nastroId))
                            {
                                break;
                            }
                        }
                    }
                }

                Console.WriteLine($"DEBUG: Estratto nastroId = {nastroId}");

                if (nastroId == -1 || nastroId < 1 || nastroId > 6)
                {
                    Console.WriteLine($"ERROR: nastroId non valido: {nastroId}");
                    return StatusCodes.BadNodeIdUnknown;
                }

                var nastroTarget = nastri.FirstOrDefault(n => n.Id == nastroId);
                if (nastroTarget == null)
                {
                    Console.WriteLine($"ERROR: Nastro con ID {nastroId} non trovato");
                    return StatusCodes.BadNodeIdUnknown;
                }

                Console.WriteLine($"DEBUG: Trovato {nastroTarget.Nome}");

                // Applica la modifica in base al tipo di variabile
                switch (browseName)
                {
                    case "Acceso":
                        if (value is bool acceso)
                        {
                            nastroTarget.Acceso = acceso;
                            Console.WriteLine($"{nastroTarget.Nome} - Accensione: {acceso}");
                        }
                        break;

                    case "Automatico":
                        if (value is bool automatico)
                        {
                            nastroTarget.ModoAutomatico = automatico;
                            nastroTarget.Modalita = automatico ? Modalita.Automatico : Modalita.Manuale;
                            Console.WriteLine($"{nastroTarget.Nome} - Modalità: {nastroTarget.Modalita}");
                        }
                        break;
                }

                statusCode = StatusCodes.Good;
                timestamp = DateTime.UtcNow;
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore scrittura: {ex.Message}");
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
                    foreach (var nastro in nastri)
                    {
                        UpdateNastroVariable($"{nastro.Id}_Stato", (int)nastro.Stato);
                        UpdateNastroVariable($"{nastro.Id}_StatoMarcia", (int)nastro.StatoMarcia);
                        UpdateNastroVariable($"{nastro.Id}_Modalita", (int)nastro.Modalita);
                        UpdateNastroVariable($"{nastro.Id}_ConsumoElettrico", nastro.ConsumoElettrico);
                        UpdateNastroVariable($"{nastro.Id}_ContatoreBottiglie", nastro.ContatoreBottiglie);
                        UpdateNastroVariable($"{nastro.Id}_Acceso", nastro.Acceso);
                        UpdateNastroVariable($"{nastro.Id}_Automatico", nastro.ModoAutomatico);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore aggiornamento nodi: {ex.Message}");
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
    }
}