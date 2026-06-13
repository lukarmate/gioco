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
        IReadOnlyList<string>? Keyword = null,
        HeroPower? HeroPower = null);

    // Comodità di lettura per i tipi compositi dei TS.
    //   DefinizioniCarte = IReadOnlyDictionary<string, DefCarta>
    //   DeckList         = IReadOnlyList<string>   (lista di defId)

    // Discriminated union delle azioni → gerarchia di record sigillati.
    public abstract record Azione;

    public sealed record IniziaPartita(
        int Seed,
        ConfigPartita Config,
        IReadOnlyList<IReadOnlyList<string>> Mazzi,
        IReadOnlyDictionary<string, DefCarta> Carte,
        // Opzionali, per giocatore (indice = id). Leader[i] = defId del leader; Obiettivi[i] = id obiettivo.
        IReadOnlyList<string?>? Leader = null,
        IReadOnlyList<string?>? Obiettivi = null) : Azione;

    // v1: passa il turno (end step automatico + begin step del prossimo giocatore).
    public sealed record PassaTurno : Azione;

    public sealed record Scarta(IReadOnlyList<string> Iids) : Azione;

    // E2 — gioco di permanenti.
    public sealed record GiocaCreatura(string Iid) : Azione;

    // Gioca una Magia dalla mano: esegue gli effetti one-shot, poi va al cimitero.
    // Bersaglio = iid della creatura scelta (per gli effetti con quantificatore "una"). null = nessuna scelta.
    public sealed record GiocaMagia(string Iid, string? Bersaglio = null) : Azione;

    // Gioca il proprio Leader dalla Zona di Comando al campo (paga costo base + incremento per morte).
    public sealed record GiocaLeader : Azione;

    // Attiva l'Hero Power del proprio Leader (da Zona Comando o campo): paga energia, va in cooldown.
    public sealed record AttivaHeroPower : Azione;

    // E3 — attiva l'abilità (trigger "attivata") di un permanente proprio in campo.
    // Tappa il permanente ed esegue i suoi effetti attivata. 1 uso/turno (finché stappato).
    public sealed record AttivaAbilita(string Iid) : Azione;

    // E4 — combattimento (attacco diretto stile Hearthstone).
    // Bersaglio = iid di una creatura avversaria, oppure null = HP del giocatore avversario.
    // Risoluzione immediata: danno reciproco (vs creatura) o agli HP (vs giocatore).
    public sealed record Attacca(string Attaccante, string? Bersaglio = null) : Azione;
}
