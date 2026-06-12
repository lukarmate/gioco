using System.Collections.Generic;

namespace Engine.Core
{
    // v1: una sola fase di gioco (Hearthstone-style). Begin/end step sono automatici.
    public enum Fase { Azioni }

    public enum PenalitaMazzoVuoto { PerditaImmediata, DannoPerTurno }

    public sealed record ConfigPartita
    {
        public int HpIniziali { get; init; }
        public int ManoIniziale { get; init; }
        public int LimiteMano { get; init; }
        public bool PrimoNonPescaT1 { get; init; }
        public PenalitaMazzoVuoto PenalitaMazzoVuoto { get; init; }
        public int? DannoMazzoVuoto { get; init; } // usato se penalita = DannoPerTurno
    }

    public sealed record ManaPool
    {
        public int Nord { get; init; }
        public int Sud { get; init; }
        public int Est { get; init; }
        public int Ovest { get; init; }
        public int Centro { get; init; }
        public int Generico { get; init; }

        public static ManaPool Vuoto() => new ManaPool();
    }

    public sealed record CartaIstanza
    {
        public string Iid { get; init; } = "";
        public string DefId { get; init; } = "";
        public int Proprietario { get; init; }
        public bool Tappata { get; init; }
        // Summoning sickness: true nel turno in cui la creatura entra in campo.
        // Azzerato nell'untap del proprietario.
        public bool EntrataQuestoTurno { get; init; }
        // Danno accumulato (HS-style): PERSISTE finché non curato (nessun reset a fine turno).
        // La creatura muore quando Danno >= DEF effettiva.
        public int Danno { get; init; }
    }

    public sealed record Giocatore
    {
        public int Id { get; init; }
        public int Hp { get; init; }
        public IReadOnlyList<CartaIstanza> Mazzo { get; init; } = new List<CartaIstanza>();
        public IReadOnlyList<CartaIstanza> Mano { get; init; } = new List<CartaIstanza>();
        public IReadOnlyList<CartaIstanza> Campo { get; init; } = new List<CartaIstanza>();
        public IReadOnlyList<CartaIstanza> Cimitero { get; init; } = new List<CartaIstanza>();
        public IReadOnlyList<CartaIstanza> Esilio { get; init; } = new List<CartaIstanza>();
        public ManaPool ManaDisponibile { get; init; } = ManaPool.Vuoto();
        public int Morti { get; init; }
        // Regola: 1 avamposto giocabile per turno. Azzerato nell'untap del proprietario.
        public bool AvampostoGiocatoQuestoTurno { get; init; }
    }

    public sealed record StatoPartita
    {
        public ConfigPartita Config { get; init; } = new ConfigPartita();
        public int Rng { get; init; }
        public IReadOnlyList<Giocatore> Giocatori { get; init; } = new List<Giocatore>();
        public int TurnoDi { get; init; }
        public int NumeroTurno { get; init; }
        public Fase Fase { get; init; }
        public int PrimoGiocatore { get; init; }
        public bool Finita { get; init; }
        public int? Vincitore { get; init; }
        // Definizioni delle carte in gioco (defId -> DefCarta), popolate a iniziaPartita.
        // L'engine le consulta per costi/produzione/stat durante il gioco.
        public IReadOnlyDictionary<string, DefCarta> Carte { get; init; }
            = new Dictionary<string, DefCarta>();
    }
}
