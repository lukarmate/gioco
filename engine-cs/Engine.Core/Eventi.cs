using System.Collections.Generic;

namespace Engine.Core
{
    // Discriminated union dei TS portata su gerarchia di record sigillati.
    // Ogni evento è confrontabile per valore (record) e pattern-matchabile con `is`.
    public abstract record Evento;

    public sealed record PartitaIniziata(IReadOnlyList<int> Giocatori, int Primo) : Evento;
    public sealed record TurnoIniziato(int Giocatore, int NumeroTurno) : Evento;
    public sealed record FaseEntrata(Fase Fase) : Evento;
    public sealed record CartaStappata(string Iid) : Evento;
    public sealed record CartaPescata(int Giocatore, string Iid) : Evento;
    public sealed record MazzoVuoto(int Giocatore) : Evento;
    public sealed record ManaAzzerato(int Giocatore) : Evento;
    public sealed record RichiestaScarto(int Giocatore, int Quantita) : Evento;
    public sealed record CartaScartata(int Giocatore, string Iid) : Evento;
    public sealed record TurnoPassato(int Da, int A) : Evento;
    public sealed record PartitaFinita(int? Vincitore, string Motivo) : Evento;

    // E2 — gioco di permanenti + mana.
    public sealed record AvampostoGiocato(int Giocatore, string Iid) : Evento;
    public sealed record ManaGenerato(int Giocatore, string Iid, IReadOnlyDictionary<string, int> Mana) : Evento;
    public sealed record CreaturaGiocata(int Giocatore, string Iid) : Evento;

    // E3 — effetti.
    public sealed record CartaMacinata(int Giocatore, string Iid) : Evento;

    // E4 — combattimento.
    public sealed record CreaturaAttacca(int Giocatore, string Iid) : Evento;
    public sealed record CreaturaBlocca(int Giocatore, string Bloccante, string Attaccante) : Evento;
    public sealed record CreaturaDistrutta(string Iid, int Proprietario) : Evento;
    public sealed record DannoGiocatore(int Giocatore, int Danno) : Evento;
}
