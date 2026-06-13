using System.Collections.Generic;

namespace Engine.Core
{
    // Discriminated union dei TS portata su gerarchia di record sigillati.
    // Ogni evento è confrontabile per valore (record) e pattern-matchabile con `is`.
    public abstract record Evento;

    public sealed record PartitaIniziata(IReadOnlyList<int> Giocatori, int Primo) : Evento;
    public sealed record TurnoIniziato(int Giocatore, int NumeroTurno) : Evento;
    public sealed record CartaStappata(string Iid) : Evento;
    public sealed record CartaPescata(int Giocatore, string Iid) : Evento;
    public sealed record MazzoVuoto(int Giocatore) : Evento;
    public sealed record FatigueSubita(int Giocatore, int Danno) : Evento;
    public sealed record EnergiaRicaricata(int Giocatore, int Energia) : Evento;
    public sealed record RichiestaScarto(int Giocatore, int Quantita) : Evento;
    public sealed record CartaScartata(int Giocatore, string Iid) : Evento;
    public sealed record TurnoPassato(int Da, int A) : Evento;
    public sealed record PartitaFinita(int? Vincitore, string Motivo) : Evento;

    // E2 — gioco di permanenti.
    public sealed record CreaturaGiocata(int Giocatore, string Iid) : Evento;
    public sealed record MagiaGiocata(int Giocatore, string Iid) : Evento;
    // Leader.
    public sealed record LeaderGiocato(int Giocatore, string Iid) : Evento;
    public sealed record LeaderTornatoInComando(int Giocatore, int Morti) : Evento;
    // Energia generata da un effetto (verbo genera_mana → energia).
    public sealed record EnergiaGenerata(int Giocatore, int Quantita) : Evento;

    // Obiettivi segreti.
    public sealed record ObiettivoProgredito(int Giocatore, ProgressoObiettivo Progresso) : Evento;
    public sealed record ObiettivoCompletato(int Giocatore, string ObiettivoId) : Evento;

    // E3 — effetti.
    public sealed record CartaMacinata(int Giocatore, string Iid) : Evento;
    public sealed record TokenGenerato(int Giocatore, string Iid, string Nome) : Evento;

    // E4 — combattimento (attacco diretto stile Hearthstone).
    public sealed record CreaturaAttacca(int Giocatore, string Iid) : Evento;
    public sealed record DannoCreatura(string Iid, int Danno) : Evento;
    public sealed record CreaturaDistrutta(string Iid, int Proprietario) : Evento;
    public sealed record DannoGiocatore(int Giocatore, int Danno) : Evento;
}
