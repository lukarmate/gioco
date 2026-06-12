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
        int? Def = null,
        IReadOnlyList<Effetto>? Effetti = null,
        IReadOnlyList<string>? Keyword = null);

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

    // v1: passa il turno (end step automatico + begin step del prossimo giocatore).
    public sealed record PassaTurno : Azione;

    public sealed record Scarta(IReadOnlyList<string> Iids) : Azione;

    // E2 — gioco di permanenti + mana.
    public sealed record GiocaAvamposto(string Iid) : Azione;

    // Tappa un avamposto per produrre mana. Scelte = colore scelto per ogni unità di mana
    // (usato solo quando l'avamposto ha Produzione.Scelta == true).
    public sealed record AttivaAvamposto(string Iid, IReadOnlyList<string>? Scelte = null) : Azione;

    public sealed record GiocaCreatura(string Iid) : Azione;

    // E3 — attiva l'abilità (trigger "attivata") di un permanente proprio in campo.
    // Tappa il permanente ed esegue i suoi effetti attivata. 1 uso/turno (finché stappato).
    public sealed record AttivaAbilita(string Iid) : Azione;

    // E4 — combattimento (attacco diretto stile Hearthstone).
    // Bersaglio = iid di una creatura avversaria, oppure null = HP del giocatore avversario.
    // Risoluzione immediata: danno reciproco (vs creatura) o agli HP (vs giocatore).
    public sealed record Attacca(string Attaccante, string? Bersaglio = null) : Azione;
}
