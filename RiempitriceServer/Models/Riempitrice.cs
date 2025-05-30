using Shared;

namespace RiempitriceServer.Models
{
    /// <summary>
    /// Modello dati per la riempitrice
    /// </summary>
    public class Riempitrice
    {
        public string Nome { get; set; } = "Riempitrice";

        // Variabili di stato (read-only)
        public StatoRiempitrice Stato { get; set; } = StatoRiempitrice.Spenta;
        public string RicettaInUso { get; set; } = "Nessuna";
        public float ConsumoElettrico { get; set; } = 0.0f; // kW
        public uint ContatoreBottiglieRiempite { get; set; } = 0;

        // Variabili di controllo (write)
        public bool Accesa { get; set; } = false;

        // Ricette disponibili
        public List<string> RicetteDisponibili { get; set; } = new()
        {
            "Acqua Naturale",
            "Acqua Frizzante", 
            "Coca Cola",
            "Succo Arancia",
            "Energy Drink"
        };

        /// <summary>
        /// Costruttore vuoto
        /// </summary>
        public Riempitrice() { }

        /// <summary>
        /// Simula il funzionamento della riempitrice
        /// </summary>
        public void Aggiorna()
        {
            if (!Accesa)
            {
                Stato = StatoRiempitrice.Spenta;
                ConsumoElettrico = 0.0f;
                RicettaInUso = "Nessuna";
                return;
            }

            // Simulazione logica
            if (Random.Shared.NextDouble() < 0.03) // 3% probabilità allarme
            {
                Stato = StatoRiempitrice.InAllarme;
                ConsumoElettrico = 0.0f;
            }
            else if (Random.Shared.NextDouble() < 0.7) // 70% probabilità in funzione
            {
                Stato = StatoRiempitrice.InFunzione;
                ConsumoElettrico = Random.Shared.NextSingle() * 5.0f + 3.0f; // 3-8 kW
                
                // Seleziona ricetta casuale se non ne è stata impostata una
                if (RicettaInUso == "Nessuna")
                {
                    RicettaInUso = RicetteDisponibili[Random.Shared.Next(RicetteDisponibili.Count)];
                }

                // Incrementa contatore bottiglie
                if (Random.Shared.NextDouble() < 0.4) // 40% probabilità
                {
                    ContatoreBottiglieRiempite++;
                }
            }
            else
            {
                Stato = StatoRiempitrice.Accesa;
                ConsumoElettrico = 0.5f; // Consumo stand-by
            }
        }

        /// <summary>
        /// Accende o spegne la riempitrice
        /// </summary>
        public void ToggleAccensione()
        {
            Accesa = !Accesa;
        }

        /// <summary>
        /// Cambia la ricetta in uso
        /// </summary>
        public void CambiaRicetta(string nuovaRicetta)
        {
            if (RicetteDisponibili.Contains(nuovaRicetta))
            {
                RicettaInUso = nuovaRicetta;
            }
        }

        public override string ToString()
        {
            return $"{Nome} - Stato: {Stato}, Ricetta: {RicettaInUso}, Consumo: {ConsumoElettrico:F2}kW, Riempite: {ContatoreBottiglieRiempite}";
        }
    }
}