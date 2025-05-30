using Shared;

namespace NastriServer.Models
{
    /// <summary>
    /// Modello dati per un nastro trasportatore
    /// </summary>
    public class Nastro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        // Variabili di stato (read-only)
        public StatoNastro Stato { get; set; } = StatoNastro.Spento;
        public StatoMarcia StatoMarcia { get; set; } = StatoMarcia.Avanti;
        public Modalita Modalita { get; set; } = Modalita.Automatico;
        public float ConsumoElettrico { get; set; } = 0.0f; // kW
        public uint ContatoreBottiglie { get; set; } = 0;

        // Variabili di controllo (write)
        public bool Acceso { get; set; } = false;
        public bool ModoAutomatico { get; set; } = true;

        /// <summary>
        /// Costruttore per inizializzare un nastro
        /// </summary>
        public Nastro(int id, string nome)
        {
            Id = id;
            Nome = nome;
        }

        /// <summary>
        /// Costruttore vuoto per serializzazione
        /// </summary>
        public Nastro() { }

        /// <summary>
        /// Simula il funzionamento del nastro
        /// </summary>
        public void Aggiorna()
        {
            if (!Acceso)
            {
                Stato = StatoNastro.Spento;
                ConsumoElettrico = 0.0f;
                return;
            }

            // Simulazione logica di base
            if (Random.Shared.NextDouble() < 0.05) // 5% probabilità allarme
            {
                Stato = StatoNastro.InAllarme;
                ConsumoElettrico = 0.0f;
            }
            else
            {
                Stato = StatoNastro.InFunzione;
                ConsumoElettrico = Random.Shared.NextSingle() * 2.0f + 1.0f; // 1-3 kW
                
                // Incrementa contatore bottiglie casualmente
                if (Random.Shared.NextDouble() < 0.3) // 30% probabilità
                {
                    ContatoreBottiglie++;
                }
            }
        }

        /// <summary>
        /// Accende o spegne il nastro
        /// </summary>
        public void ToggleAccensione()
        {
            Acceso = !Acceso;
        }

        /// <summary>
        /// Cambia modalità automatico/manuale
        /// </summary>
        public void CambiaModalita()
        {
            ModoAutomatico = !ModoAutomatico;
            Modalita = ModoAutomatico ? Modalita.Automatico : Modalita.Manuale;
        }

        public override string ToString()
        {
            return $"{Nome} - Stato: {Stato}, Consumo: {ConsumoElettrico:F2}kW, Bottiglie: {ContatoreBottiglie}";
        }
    }
}