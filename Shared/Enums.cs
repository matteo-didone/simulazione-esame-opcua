namespace Shared
{
    /// <summary>
    /// Stati possibili per i nastri trasportatori
    /// </summary>
    public enum StatoNastro
    {
        Spento = 0,
        InFunzione = 1,
        InAllarme = 2
    }

    /// <summary>
    /// Direzione di marcia dei nastri
    /// </summary>
    public enum StatoMarcia
    {
        Avanti = 0,
        Indietro = 1
    }

    /// <summary>
    /// Modalità di funzionamento dei nastri
    /// </summary>
    public enum Modalita
    {
        Automatico = 0,
        Manuale = 1
    }

    /// <summary>
    /// Stati possibili per la riempitrice
    /// </summary>
    public enum StatoRiempitrice
    {
        Spenta = 0,
        Accesa = 1,
        InFunzione = 2,
        InAllarme = 3
    }

    /// <summary>
    /// Stato complessivo del sistema
    /// </summary>
    public enum StatoSistema
    {
        Spento = 0,
        Operativo = 1,
        ParzialeAllarme = 2,
        AllarmeGenerale = 3
    }
}