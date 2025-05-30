using Opc.Ua;
using Opc.Ua.Server;

namespace Shared.CustomTypes
{
    /// <summary>
    /// Helper per creare nodi con metadati professionali e semantica chiara
    /// </summary>
    public static class ProfessionalNodeCreator
    {
        /// <summary>
        /// Crea una variabile con metadati professionali completi
        /// </summary>
        public static BaseDataVariableState CreateProfessionalVariable(
            NodeState parent,
            string nodeId,
            string browseName,
            string displayName,
            string description,
            NodeId dataType,
            ushort namespaceIndex,
            bool writable = false,
            string? unit = null,
            double? minValue = null,
            double? maxValue = null,
            object? defaultValue = null)
        {
            var variable = new BaseDataVariableState(parent);
            variable.NodeId = new NodeId(nodeId, namespaceIndex);
            variable.BrowseName = new QualifiedName(browseName, namespaceIndex);
            variable.DisplayName = new LocalizedText("it", displayName);
            variable.Description = new LocalizedText("it", description);
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.DataType = dataType;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.UserAccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead;
            variable.Historizing = false;
            variable.Value = defaultValue ?? GetDefaultValue(dataType);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            // Aggiungi unità di misura se specificata
            if (!string.IsNullOrEmpty(unit))
            {
                AddEngineeringUnits(variable, unit, namespaceIndex);
            }

            // Aggiungi range se specificato
            if (minValue.HasValue && maxValue.HasValue)
            {
                AddEURange(variable, minValue.Value, maxValue.Value, namespaceIndex);
            }

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Crea un oggetto con metadati professionali
        /// </summary>
        public static BaseObjectState CreateProfessionalObject(
            NodeState parent,
            string nodeId,
            string browseName,
            string displayName,
            string description,
            ushort namespaceIndex,
            NodeId? typeDefinition = null)
        {
            var obj = new BaseObjectState(parent);
            obj.NodeId = new NodeId(nodeId, namespaceIndex);
            obj.BrowseName = new QualifiedName(browseName, namespaceIndex);
            obj.DisplayName = new LocalizedText("it", displayName);
            obj.Description = new LocalizedText("it", description);
            obj.TypeDefinitionId = typeDefinition ?? ObjectTypeIds.BaseObjectType;

            if (parent != null)
            {
                parent.AddChild(obj);
            }

            return obj;
        }

        /// <summary>
        /// Crea una cartella organizzata professionalmente
        /// </summary>
        public static FolderState CreateProfessionalFolder(
            NodeState parent,
            string nodeId,
            string browseName,
            string displayName,
            string description,
            ushort namespaceIndex)
        {
            var folder = new FolderState(parent);
            folder.NodeId = new NodeId(nodeId, namespaceIndex);
            folder.BrowseName = new QualifiedName(browseName, namespaceIndex);
            folder.DisplayName = new LocalizedText("it", displayName);
            folder.Description = new LocalizedText("it", description);
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }

        /// <summary>
        /// Crea un metodo professionale con documentazione completa
        /// </summary>
        public static MethodState CreateProfessionalMethod(
            NodeState parent,
            string nodeId,
            string browseName,
            string displayName,
            string description,
            ushort namespaceIndex,
            Argument[]? inputArguments = null,
            Argument[]? outputArguments = null)
        {
            var method = new MethodState(parent);
            method.NodeId = new NodeId(nodeId, namespaceIndex);
            method.BrowseName = new QualifiedName(browseName, namespaceIndex);
            method.DisplayName = new LocalizedText("it", displayName);
            method.Description = new LocalizedText("it", description);
            method.Executable = true;
            method.UserExecutable = true;

            // Aggiungi argomenti input se presenti
            if (inputArguments != null && inputArguments.Length > 0)
            {
                method.InputArguments = new PropertyState<Argument[]>(method);
                method.InputArguments.NodeId = new NodeId($"{nodeId}_InputArguments", namespaceIndex);
                method.InputArguments.BrowseName = BrowseNames.InputArguments;
                method.InputArguments.DisplayName = BrowseNames.InputArguments;
                method.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                method.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                method.InputArguments.DataType = DataTypeIds.Argument;
                method.InputArguments.ValueRank = ValueRanks.OneDimension;
                method.InputArguments.AccessLevel = AccessLevels.CurrentRead;
                method.InputArguments.UserAccessLevel = AccessLevels.CurrentRead;
                method.InputArguments.Value = inputArguments;
            }

            // Aggiungi argomenti output se presenti
            if (outputArguments != null && outputArguments.Length > 0)
            {
                method.OutputArguments = new PropertyState<Argument[]>(method);
                method.OutputArguments.NodeId = new NodeId($"{nodeId}_OutputArguments", namespaceIndex);
                method.OutputArguments.BrowseName = BrowseNames.OutputArguments;
                method.OutputArguments.DisplayName = BrowseNames.OutputArguments;
                method.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                method.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                method.OutputArguments.DataType = DataTypeIds.Argument;
                method.OutputArguments.ValueRank = ValueRanks.OneDimension;
                method.OutputArguments.AccessLevel = AccessLevels.CurrentRead;
                method.OutputArguments.UserAccessLevel = AccessLevels.CurrentRead;
                method.OutputArguments.Value = outputArguments;
            }

            if (parent != null)
            {
                parent.AddChild(method);
            }

            return method;
        }

        /// <summary>
        /// Aggiunge unità di misura a una variabile
        /// </summary>
        private static void AddEngineeringUnits(BaseDataVariableState variable, string unit, ushort namespaceIndex)
        {
            try
            {
                var engineeringUnits = new PropertyState<EUInformation>(variable);
                engineeringUnits.NodeId = new NodeId($"{variable.NodeId}_EU", namespaceIndex);
                engineeringUnits.BrowseName = BrowseNames.EngineeringUnits;
                engineeringUnits.DisplayName = BrowseNames.EngineeringUnits;
                engineeringUnits.TypeDefinitionId = VariableTypeIds.PropertyType;
                engineeringUnits.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                engineeringUnits.DataType = DataTypeIds.EUInformation;
                engineeringUnits.ValueRank = ValueRanks.Scalar;
                engineeringUnits.AccessLevel = AccessLevels.CurrentRead;
                engineeringUnits.UserAccessLevel = AccessLevels.CurrentRead;
                
                // Crea EUInformation semplificata
                var euInfo = new EUInformation();
                euInfo.DisplayName = new LocalizedText("it", unit);
                euInfo.Description = new LocalizedText("it", $"Unità di misura: {unit}");
                
                engineeringUnits.Value = euInfo;
                variable.AddChild(engineeringUnits);
            }
            catch (Exception ex)
            {
                // Log dell'errore ma continua l'esecuzione
                Console.WriteLine($"Errore aggiunta Engineering Units: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiunge range di valori a una variabile
        /// </summary>
        private static void AddEURange(BaseDataVariableState variable, double minValue, double maxValue, ushort namespaceIndex)
        {
            try
            {
                var euRange = new PropertyState<Opc.Ua.Range>(variable);
                euRange.NodeId = new NodeId($"{variable.NodeId}_Range", namespaceIndex);
                euRange.BrowseName = BrowseNames.EURange;
                euRange.DisplayName = BrowseNames.EURange;
                euRange.TypeDefinitionId = VariableTypeIds.PropertyType;
                euRange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                euRange.DataType = DataTypeIds.Range;
                euRange.ValueRank = ValueRanks.Scalar;
                euRange.AccessLevel = AccessLevels.CurrentRead;
                euRange.UserAccessLevel = AccessLevels.CurrentRead;
                euRange.Value = new Opc.Ua.Range(maxValue, minValue); // High, Low
                variable.AddChild(euRange);
            }
            catch (Exception ex)
            {
                // Log dell'errore ma continua l'esecuzione
                Console.WriteLine($"Errore aggiunta EU Range: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottiene il valore di default per un tipo di dato
        /// </summary>
        private static object GetDefaultValue(NodeId dataType)
        {
            if (dataType == DataTypeIds.Boolean) return false;
            if (dataType == DataTypeIds.Int32) return 0;
            if (dataType == DataTypeIds.UInt32) return (uint)0;
            if (dataType == DataTypeIds.Float) return 0.0f;
            if (dataType == DataTypeIds.Double) return 0.0;
            if (dataType == DataTypeIds.String) return string.Empty;
            if (dataType == DataTypeIds.DateTime) return DateTime.UtcNow;
            if (dataType == DataTypeIds.Duration) return 0.0;
            return 0;
        }
    }

    /// <summary>
    /// Template professionali per i componenti dell'impianto
    /// </summary>
    public static class IndustrialComponentTemplates
    {
        /// <summary>
        /// Crea un nastro completo con struttura professionale
        /// </summary>
        public static BaseObjectState CreateProfessionalNastro(
            NodeState parent,
            int nastroId,
            string nome,
            ushort namespaceIndex)
        {
            // Oggetto principale nastro
            var nastroObj = ProfessionalNodeCreator.CreateProfessionalObject(
                parent,
                $"Nastro{nastroId}",
                $"Nastro{nastroId}",
                nome,
                $"Nastro trasportatore #{nastroId} per linea di imbottigliamento",
                namespaceIndex
            );

            // === CARTELLA PARAMETRI ===
            var parametriFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                nastroObj,
                $"Nastro{nastroId}_Parametri",
                "Parametri",
                "Parametri Operativi",
                "Parametri di stato e configurazione del nastro",
                namespaceIndex
            );

            // Stato (Enum simulato con Int32)
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                $"Stato{nastroId}",
                "Stato",
                "Stato Operativo",
                "Stato corrente del nastro: 0=Spento, 1=InFunzione, 2=InAllarme",
                DataTypeIds.Int32,
                namespaceIndex,
                writable: false,
                defaultValue: 0
            );

            // Velocità
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                $"Velocita{nastroId}",
                "Velocita",
                "Velocità Nastro",
                "Velocità di movimento del nastro trasportatore",
                DataTypeIds.Float,
                namespaceIndex,
                writable: true,
                unit: "m/min",
                minValue: 0.0,
                maxValue: 100.0,
                defaultValue: 0.0f
            );

            // Consumo Elettrico
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                $"Consumo{nastroId}",
                "ConsumoElettrico",
                "Consumo Elettrico",
                "Consumo elettrico istantaneo del nastro",
                DataTypeIds.Float,
                namespaceIndex,
                writable: false,
                unit: "kW",
                minValue: 0.0,
                maxValue: 50.0,
                defaultValue: 0.0f
            );

            // Contatore Bottiglie
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                $"Contatore{nastroId}",
                "ContatoreBottiglie",
                "Contatore Bottiglie",
                "Numero totale di bottiglie processate dal nastro",
                DataTypeIds.UInt32,
                namespaceIndex,
                writable: false,
                unit: "pz",
                defaultValue: (uint)0
            );

            // === CARTELLA CONTROLLO ===
            var controlloFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                nastroObj,
                $"Nastro{nastroId}_Controllo",
                "Controllo",
                "Comandi di Controllo",
                "Interfaccia per il controllo del nastro",
                namespaceIndex
            );

            // Acceso (comando)
            ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                $"Acceso{nastroId}",
                "Acceso",
                "Comando Accensione",
                "Comando per accendere/spegnere il nastro",
                DataTypeIds.Boolean,
                namespaceIndex,
                writable: true,
                defaultValue: false
            );

            // Automatico (comando)
            ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                $"Automatico{nastroId}",
                "Automatico",
                "Modalità Automatica",
                "Imposta modalità automatica (true) o manuale (false)",
                DataTypeIds.Boolean,
                namespaceIndex,
                writable: true,
                defaultValue: true
            );

            // === CARTELLA DIAGNOSTICA ===
            var diagnosticaFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                nastroObj,
                $"Nastro{nastroId}_Diagnostica",
                "Diagnostica",
                "Informazioni Diagnostiche",
                "Dati per manutenzione e diagnostica del nastro",
                namespaceIndex
            );

            // Tempo Funzionamento
            ProfessionalNodeCreator.CreateProfessionalVariable(
                diagnosticaFolder,
                $"TempoFunzionamento{nastroId}",
                "TempoFunzionamento",
                "Ore di Funzionamento",
                "Tempo totale di funzionamento del nastro",
                DataTypeIds.Double,
                namespaceIndex,
                writable: false,
                unit: "h",
                defaultValue: 0.0
            );

            // Numero Avvii
            ProfessionalNodeCreator.CreateProfessionalVariable(
                diagnosticaFolder,
                $"NumeroAvvii{nastroId}",
                "NumeroAvvii",
                "Numero Avvii",
                "Contatore del numero di avvii del nastro",
                DataTypeIds.UInt32,
                namespaceIndex,
                writable: false,
                defaultValue: (uint)0
            );

            // === METODI DI CONTROLLO ===
            var metodiFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                nastroObj,
                $"Nastro{nastroId}_Metodi",
                "Metodi",
                "Metodi di Controllo",
                "Metodi per il controllo avanzato del nastro",
                namespaceIndex
            );

            // Metodo Avvia
            ProfessionalNodeCreator.CreateProfessionalMethod(
                metodiFolder,
                $"Nastro{nastroId}_Avvia",
                "Avvia",
                "Avvia Nastro",
                "Avvia il funzionamento del nastro trasportatore",
                namespaceIndex
            );

            // Metodo Arresta
            ProfessionalNodeCreator.CreateProfessionalMethod(
                metodiFolder,
                $"Nastro{nastroId}_Arresta",
                "Arresta",
                "Arresta Nastro",
                "Arresta il funzionamento del nastro trasportatore",
                namespaceIndex
            );

            // Metodo Imposta Velocità
            var velocitaArguments = new Argument[]
            {
                new Argument()
                {
                    Name = "NuovaVelocita",
                    DataType = DataTypeIds.Float,
                    ValueRank = ValueRanks.Scalar,
                    Description = new LocalizedText("it", "Nuova velocità in m/min (0-100)")
                }
            };

            ProfessionalNodeCreator.CreateProfessionalMethod(
                metodiFolder,
                $"Nastro{nastroId}_ImpostaVelocita",
                "ImpostaVelocita",
                "Imposta Velocità",
                "Imposta la velocità del nastro trasportatore",
                namespaceIndex,
                inputArguments: velocitaArguments
            );

            return nastroObj;
        }

        /// <summary>
        /// Crea una riempitrice completa con struttura professionale
        /// </summary>
        public static BaseObjectState CreateProfessionalRiempitrice(
            NodeState parent,
            string nome,
            ushort namespaceIndex)
        {
            // Oggetto principale riempitrice
            var riempitriceObj = ProfessionalNodeCreator.CreateProfessionalObject(
                parent,
                "Riempitrice",
                "Riempitrice",
                nome,
                "Macchina riempitrice per l'imbottigliamento automatico",
                namespaceIndex
            );

            // === CARTELLA PARAMETRI ===
            var parametriFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceObj,
                "Riempitrice_Parametri",
                "Parametri",
                "Parametri Operativi",
                "Parametri di stato e configurazione della riempitrice",
                namespaceIndex
            );

            // Stato
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "Stato",
                "Stato",
                "Stato Operativo",
                "Stato corrente: 0=Spenta, 1=Accesa, 2=InFunzione, 3=InAllarme",
                DataTypeIds.Int32,
                namespaceIndex,
                writable: false,
                defaultValue: 0
            );

            // Ricetta in Uso
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "RicettaInUso",
                "RicettaInUso",
                "Ricetta Attiva",
                "Nome della ricetta di produzione attualmente in uso",
                DataTypeIds.String,
                namespaceIndex,
                writable: false,
                defaultValue: "Nessuna"
            );

            // Velocità Riempimento
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "VelocitaRiempimento",
                "VelocitaRiempimento",
                "Velocità Riempimento",
                "Velocità di riempimento delle bottiglie",
                DataTypeIds.Float,
                namespaceIndex,
                writable: true,
                unit: "bot/min",
                minValue: 0.0,
                maxValue: 200.0,
                defaultValue: 0.0f
            );

            // Consumo Elettrico
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "ConsumoElettrico",
                "ConsumoElettrico",
                "Consumo Elettrico",
                "Consumo elettrico istantaneo della riempitrice",
                DataTypeIds.Float,
                namespaceIndex,
                writable: false,
                unit: "kW",
                minValue: 0.0,
                maxValue: 100.0,
                defaultValue: 0.0f
            );

            // Contatore Bottiglie Riempite
            ProfessionalNodeCreator.CreateProfessionalVariable(
                parametriFolder,
                "ContatoreBottiglieRiempite",
                "ContatoreBottiglieRiempite",
                "Bottiglie Riempite",
                "Numero totale di bottiglie riempite",
                DataTypeIds.UInt32,
                namespaceIndex,
                writable: false,
                unit: "pz",
                defaultValue: (uint)0
            );

            // === CARTELLA RICETTE ===
            var ricetteFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceObj,
                "Riempitrice_Ricette",
                "Ricette",
                "Gestione Ricette",
                "Configurazione e gestione delle ricette di produzione",
                namespaceIndex
            );

            // Ricette Disponibili
            var ricetteDisponibili = new string[] 
            { 
                "Acqua Naturale", 
                "Acqua Frizzante", 
                "Coca Cola", 
                "Succo Arancia", 
                "Energy Drink" 
            };

            var ricetteVar = ProfessionalNodeCreator.CreateProfessionalVariable(
                ricetteFolder,
                "RicetteDisponibili",
                "RicetteDisponibili",
                "Ricette Disponibili",
                "Elenco delle ricette di produzione disponibili",
                DataTypeIds.String,
                namespaceIndex,
                writable: false,
                defaultValue: ricetteDisponibili
            );
            ricetteVar.ValueRank = ValueRanks.OneDimension;

            // === CARTELLA CONTROLLO ===
            var controlloFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                riempitriceObj,
                "Riempitrice_Controllo",
                "Controllo",
                "Comandi di Controllo",
                "Interfaccia per il controllo della riempitrice",
                namespaceIndex
            );

            // Accesa (comando)
            ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                "Accesa",
                "Accesa",
                "Comando Accensione",
                "Comando per accendere/spegnere la riempitrice",
                DataTypeIds.Boolean,
                namespaceIndex,
                writable: true,
                defaultValue: false
            );

            // Cambia Ricetta (comando)
            ProfessionalNodeCreator.CreateProfessionalVariable(
                controlloFolder,
                "CambiaRicetta",
                "CambiaRicetta",
                "Comando Cambio Ricetta",
                "Imposta una nuova ricetta di produzione",
                DataTypeIds.String,
                namespaceIndex,
                writable: true,
                defaultValue: string.Empty
            );

            return riempitriceObj;
        }

        /// <summary>
        /// Crea l'impianto completo con struttura gerarchica professionale
        /// </summary>
        public static BaseObjectState CreateProfessionalImpianto(
            NodeState parent,
            ushort namespaceIndex)
        {
            // Oggetto principale impianto
            var impiantoObj = ProfessionalNodeCreator.CreateProfessionalObject(
                parent,
                "Impianto",
                "Impianto",
                "Impianto di Imbottigliamento",
                "Impianto completo per l'imbottigliamento industriale",
                namespaceIndex
            );

            // === DATI AGGREGATI ===
            var datiAggregatiFolder = ProfessionalNodeCreator.CreateProfessionalFolder(
                impiantoObj,
                "DatiAggregati",
                "DatiAggregati",
                "Dati Aggregati",
                "Informazioni aggregate di tutto l'impianto",
                namespaceIndex
            );

            // Stato Sistema
            ProfessionalNodeCreator.CreateProfessionalVariable(
                datiAggregatiFolder,
                "StatoSistema",
                "StatoSistema",
                "Stato del Sistema",
                "Stato operativo aggregato: 0=Spento, 1=Operativo, 2=ParzialeAllarme, 3=AllarmeGenerale",
                DataTypeIds.Int32,
                namespaceIndex,
                writable: false,
                defaultValue: 0
            );

            // Consumo Complessivo
            ProfessionalNodeCreator.CreateProfessionalVariable(
                datiAggregatiFolder,
                "ConsumoComplessivo",
                "ConsumoComplessivo",
                "Consumo Elettrico Totale",
                "Consumo elettrico aggregato di tutto l'impianto",
                DataTypeIds.Float,
                namespaceIndex,
                writable: false,
                unit: "kW",
                defaultValue: 0.0f
            );

            // Produzione Totale
            ProfessionalNodeCreator.CreateProfessionalVariable(
                datiAggregatiFolder,
                "ProduzioneComplessiva",
                "ProduzioneComplessiva",
                "Produzione Totale",
                "Numero totale di bottiglie processate",
                DataTypeIds.UInt32,
                namespaceIndex,
                writable: false,
                unit: "pz",
                defaultValue: (uint)0
            );

            // Efficienza Sistema
            ProfessionalNodeCreator.CreateProfessionalVariable(
                datiAggregatiFolder,
                "EfficienzaSistema",
                "EfficienzaSistema",
                "Efficienza del Sistema",
                "Efficienza operativa complessiva dell'impianto",
                DataTypeIds.Float,
                namespaceIndex,
                writable: false,
                unit: "%",
                minValue: 0.0,
                maxValue: 100.0,
                defaultValue: 0.0f
            );

            // Anomalia Contatori
            ProfessionalNodeCreator.CreateProfessionalVariable(
                datiAggregatiFolder,
                "AnomaliaContatoreBottiglie",
                "AnomaliaContatoreBottiglie",
                "Anomalia Contatori",
                "Indica presenza di anomalie nei contatori delle bottiglie",
                DataTypeIds.Boolean,
                namespaceIndex,
                writable: false,
                defaultValue: false
            );

            return impiantoObj;
        }
    }
}