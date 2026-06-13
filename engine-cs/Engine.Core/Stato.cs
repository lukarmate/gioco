using System.Collections.Generic;

namespace Engine.Core
{
    // v1: una sola fase di gioco (Hearthstone-style). Begin/end step sono automatici.
    public enum Fase { Azioni }

    public enum PenalitaMazzoVuoto { PerditaImmediata, DannoPerTurno, Fatigue }

    public sealed record ConfigPartita
    {
        public int HpIniziali { get; init; }
        public int ManoIniziale { get; init; }
        public int LimiteMano { get; init; }
        public bool PrimoNonPescaT1 { get; init; }
        public PenalitaMazzoVuoto PenalitaMazzoVuoto { get; init; }
        public int? DannoMazzoVuoto { get; init; } // usato se penalita = DannoPerTurno
        // v1: cap dell'energia massima (DESIGN_V1 = 8). 0 => default 8.
        public int CapEnergia { get; init; }
        // v1: cap delle creature in campo per giocatore (DESIGN_V1 = 6). 0 => default 6.
        public int CapCampo { get; init; }
        // Incremento del costo del Leader per ogni sua morte (DESIGN_V1 = +2). 0 => default 2.
        public int IncrementoLeader { get; init; }
    }

    // Leader nella Zona di Comando. Quando entra in campo (InCampo) diventa una creatura (Iid).
    // Alla morte torna in Zona Comando e Morti cresce (costo di rientro incrementale).
    public sealed record StatoLeader
    {
        public string DefId { get; init; } = "";
        public bool InCampo { get; init; }
        public string? Iid { get; init; }
        public int Morti { get; init; }
        // Turni rimanenti prima di poter riusare l'Hero Power (0 = pronto).
        public int CooldownHeroPower { get; init; }
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
        // Modificatori PERSISTENTI (segnalini +X/+X) applicati da effetti one-shot. Restano
        // sulla creatura anche se la sorgente lascia il campo (diversi dalle aure continue).
        public int BonusAtk { get; init; }
        public int BonusDef { get; init; }
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
        // v1: energia automatica. EnergiaMax cresce di 1 a turno (cap config); Energia = corrente
        // disponibile, ricaricata a EnergiaMax all'inizio del proprio turno.
        public int Energia { get; init; }
        public int EnergiaMax { get; init; }
        // Fatigue: numero di pescate a mazzo vuoto subite (il danno cresce di 1 ogni volta).
        public int Fatigue { get; init; }
        public int Morti { get; init; }
        // Accumulatori per-turno (per gli obiettivi). Reset all'inizio del proprio turno.
        public int CarteGiocateQuestoTurno { get; init; }
        public int DanniAvversarioQuestoTurno { get; init; }
        public int AttacchiQuestoTurno { get; init; }
        public int EnergiaSpesaQuestoTurno { get; init; }
        // Creature avversarie distrutte da questo giocatore (cumulativo + per-turno).
        public int CreatureNemicheDistrutte { get; init; }
        public int CreatureNemicheDistrutteQuestoTurno { get; init; }
        // Danno subito nella finestra dall'ultimo fine-turno (per OB difensivi). Reset a fine turno.
        public int DannoSubitoFinestra { get; init; }
        // Iid distinti delle proprie creature che hanno colpito gli HP avversari questo turno.
        public IReadOnlyList<string> CreatureColpisconoFaccia { get; init; } = new List<string>();
        // Il Leader ha inflitto danno agli HP avversari (cumulativo, per OB-16).
        public bool LeaderHaColpitoFaccia { get; init; }
        // Numero di turni in cui ha attivato l'Hero Power (cumulativo, per OB-18).
        public int HeroPowerTurniUsati { get; init; }
        // Streak: turni consecutivi in cui la condizione dell'obiettivo è soddisfatta a fine turno.
        public int StreakObiettivo { get; init; }
        // Leader (Zona di Comando). null nei test che non lo usano.
        public StatoLeader? Leader { get; init; }
        // Obiettivo segreto assegnato (id nel registro Obiettivi). null = nessuno (es. nei test legacy).
        public string? ObiettivoId { get; init; }
        public ProgressoObiettivo ObiettivoProgresso { get; init; }
        public bool ObiettivoCompletato { get; init; }
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
