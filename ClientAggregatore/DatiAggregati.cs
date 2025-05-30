using Shared;

namespace ClientAggregatore.Models
{
    /// <summary>
    /// Modello per i dati aggregati di tutto l'impianto
    /// </summary>
    public class DatiAggregati
    {
        public StatoSistema StatoSistema { get; set; } = StatoSistema.Spento;
        public float ConsumoComplessivo { get; set; } = 0.0f;
        public uint NumeroBottiglieComplessivo { get; set; } = 0;
        public bool AnomaliaContatoreBottiglie { get; set; } = false;
        public DateTime UltimoAggiornamento { get; set; } = DateTime.Now;

        // Dettagli per nastri
        public List<StatoNastroDto> Nastri { get; set; } = new();
        
        // Dettagli riempitrice  
        public StatoRiempitriceDto? Riempitrice { get; set; }

        public override string ToString()
        {
            return $"Sistema: {StatoSistema}, Consumo: {ConsumoComplessivo:F2}kW, " +
                   $"Bottiglie: {NumeroBottiglieComplessivo}, Anomalia: {AnomaliaContatoreBottiglie}";
        }
    }

    /// <summary>
    /// DTO per lo stato di un nastro (Data Transfer Object)
    /// </summary>
    public class StatoNastroDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public StatoNastro Stato { get; set; }
        public float ConsumoElettrico { get; set; }
        public uint ContatoreBottiglie { get; set; }
    }

    /// <summary>
    /// DTO per lo stato della riempitrice
    /// </summary>
    public class StatoRiempitriceDto
    {
        public string Nome { get; set; } = string.Empty;
        public StatoRiempitrice Stato { get; set; }
        public string RicettaInUso { get; set; } = string.Empty;
        public float ConsumoElettrico { get; set; }
        public uint ContatoreBottiglieRiempite { get; set; }
    }
}