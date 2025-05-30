using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace ControlloImpianto
{
    public class DebugClient
    {
        public static async Task<Dictionary<string, NodeId>> ScoprireStruttura(Session session, string serverName)
        {
            var nodeIds = new Dictionary<string, NodeId>();
            
            Console.WriteLine($"\n=== STRUTTURA {serverName.ToUpper()} ===");
            
            try
            {
                // Parti dal nodo Objects - AUMENTATO maxLevel da 3 a 4!
                var objectsNode = new NodeId(Objects.ObjectsFolder);
                await EsploraRicorsivo(session, objectsNode, "", nodeIds, 0, 4);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore esplorazione: {ex.Message}");
            }
            
            return nodeIds;
        }
        
        private static async Task EsploraRicorsivo(Session session, NodeId nodeId, string path, 
            Dictionary<string, NodeId> nodeIds, int level, int maxLevel)
        {
            if (level > maxLevel) return;
            
            try
            {
                // Leggi i metadati del nodo
                var readValues = new ReadValueIdCollection
                {
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.BrowseName },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.DisplayName },
                    new ReadValueId { NodeId = nodeId, AttributeId = Attributes.NodeClass }
                };
                
                session.Read(null, 0, TimestampsToReturn.Neither, readValues, out var results, out _);
                
                var browseName = results[0].Value?.ToString() ?? "Unknown";
                var displayName = results[1].Value?.ToString() ?? "Unknown";
                var nodeClass = (NodeClass)(results[2].Value ?? NodeClass.Unspecified);
                
                var indent = new string(' ', level * 2);
                var currentPath = string.IsNullOrEmpty(path) ? browseName : $"{path}/{browseName}";
                
                Console.WriteLine($"{indent}{browseName} ({displayName}) [{nodeClass}] - NodeId: {nodeId}");
                
                // Se è una variabile, salvala nel dizionario CON ENTRAMBE LE CHIAVI
                if (nodeClass == NodeClass.Variable)
                {
                    nodeIds[currentPath] = nodeId;
                    
                    // AGGIUNTO: Salva anche con NodeId semplificato per compatibilità
                    var nodeIdStr = nodeId.ToString();
                    if (nodeIdStr.Contains(";s="))
                    {
                        var simpleKey = nodeIdStr.Split(";s=")[1]; // Es: "Acceso1"
                        if (!nodeIds.ContainsKey(simpleKey))
                        {
                            nodeIds[simpleKey] = nodeId;
                            Console.WriteLine($"{indent}  🔑 Chiave aggiuntiva: {simpleKey}");
                        }
                    }
                    
                    // Prova a leggere il valore
                    try
                    {
                        var valueRead = new ReadValueIdCollection
                        {
                            new ReadValueId { NodeId = nodeId, AttributeId = Attributes.Value }
                        };
                        session.Read(null, 0, TimestampsToReturn.Neither, valueRead, out var valueResults, out _);
                        var value = valueResults[0].Value;
                        Console.WriteLine($"{indent}  Valore: {value} | Tipo: {value?.GetType().Name}");
                        
                        // Verifica se è scrivibile
                        var accessRead = new ReadValueIdCollection
                        {
                            new ReadValueId { NodeId = nodeId, AttributeId = Attributes.AccessLevel }
                        };
                        session.Read(null, 0, TimestampsToReturn.Neither, accessRead, out var accessResults, out _);
                        var accessLevelValue = Convert.ToByte(accessResults[0].Value ?? 0);
                        var isWritable = (accessLevelValue & AccessLevels.CurrentWrite) != 0;
                        Console.WriteLine($"{indent}  Scrivibile: {isWritable}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{indent}  ❌ Errore lettura valore: {ex.Message}");
                    }
                }
                
                // Esplora i figli
                var browseDesc = new BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = (uint)(BrowseResultMask.All)
                };
                
                session.Browse(null, null, 0, new BrowseDescriptionCollection { browseDesc },
                    out var browseResults, out _);
                
                if (browseResults?.Count > 0 && browseResults[0].References?.Count > 0)
                {
                    foreach (var reference in browseResults[0].References)
                    {
                        // Escludi nodi di sistema
                        var targetBrowseName = reference.BrowseName.Name;
                        if (!targetBrowseName.StartsWith("Server") && 
                            !targetBrowseName.StartsWith("Types") &&
                            !targetBrowseName.StartsWith("Views"))
                        {
                            await EsploraRicorsivo(session, ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                                currentPath, nodeIds, level + 1, maxLevel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{new string(' ', level * 2)}❌ Errore: {ex.Message}");
            }
        }
    }
}