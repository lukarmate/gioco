using System.Collections.Generic;

namespace Engine.Core
{
    // Definizione carta usata dall'engine (disaccoppiata dal parser).
    // Campi opzionali popolati in base al tipo: Costo per giocabili, Produzione per avamposti,
    // Atk/Def per creature.
    public sealed record DefCarta(
        string DefId,
        string Tipo,
        ManaCosto? Costo = null,
        ManaProdotto? Produzione = null,
        int? Atk = null,
        int? Def = null);

    // Comodità di lettura per i tipi compositi dei TS.
    //   DefinizioniCarte = IReadOnlyDictionary<string, DefCarta>
    //   DeckList         = IReadOnlyList<string>   (lista di defId)

    // Discriminated union delle azioni → gerarchia di record sigillati.
    public abstract record Azione;

    public sealed record IniziaPartita(
        int Seed,
        ConfigPartita Config,
        IReadOnlyList<IReadOnlyList<string>> Mazzi,
        IReadOnlyDictionary<string, DefCarta> Carte) : Azione;

    public sealed record AvanzaFase : Azione;

    public sealed record Scarta(IReadOnlyList<string> Iids) : Azione;

    // E2 — gioco di permanenti + mana.
    public sealed record GiocaAvamposto(string Iid) : Azione;

    // Tappa un avamposto per produrre mana. Scelte = colore scelto per ogni unità di mana
    // (usato solo quando l'avamposto ha Produzione.Scelta == true).
    public sealed record AttivaAvamposto(string Iid, IReadOnlyList<string>? Scelte = null) : Azione;

    public sealed record GiocaCreatura(string Iid) : Azione;

    // E4 — combattimento.
    public sealed record DichiaraAttacco(IReadOnlyList<string> Attaccanti) : Azione;

    // Blocchi: mappa attaccante -> bloccante. Attaccanti non presenti = non bloccati.
    // Dichiarare i blocchi risolve il combattimento.
    public sealed record DichiaraBlocchi(IReadOnlyDictionary<string, string> Assegnazioni) : Azione;
}
