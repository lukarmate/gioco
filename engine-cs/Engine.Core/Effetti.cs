using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    // E3 — interprete effetti. Modello (AST) + executor.
    //
    // Una carta porta una lista di Effetto; ogni Effetto ha un Trigger (quando scatta)
    // e una lista di AzioneEffetto (i verbi da eseguire, in ordine).

    public enum Trigger { Etb, Morte, Upkeep, Attacco, Attivata, Passiva }

    // Proprietario del bersaglio, relativo al CONTROLLORE dell'effetto.
    public enum Proprietario { Tue, Avversario, Tutti }

    // Quantificatore: Una = richiede scelta (E3b), Tutte/Ogni = deterministico.
    public enum Quantificatore { Una, Tutte, Ogni }

    // Selettore di bersagli. Tipo = "creatura" | "giocatore". Filtro non gestito in E3a.
    public sealed record Bersaglio(
        string Tipo,
        Proprietario Proprietario,
        Quantificatore Quantificatore = Quantificatore.Tutte,
        string? Filtro = null);

    // Verbi (discriminated union). Sottoinsieme deterministico in E3a.
    public abstract record AzioneEffetto;
    public sealed record Pesca(int Valore) : AzioneEffetto;
    public sealed record GeneraMana(int Valore, string Colore) : AzioneEffetto;
    public sealed record InfliggiDanno(Bersaglio Bersaglio, int Valore) : AzioneEffetto;
    public sealed record Distruggi(Bersaglio Bersaglio) : AzioneEffetto;
    public sealed record Mill(Bersaglio Bersaglio, int Valore) : AzioneEffetto;
    // Controllore: "tu" = chi controlla la sorgente, "avversario" = primo avversario.
    public sealed record GeneraToken(string Nome, int Atk, int Def, string Controllore = "tu") : AzioneEffetto;
    // Effetti STATICI (trigger Passiva): non mutano lo stato, sono letti da StatEffettive/KeywordEffettive.
    public sealed record ModificaStat(Bersaglio Bersaglio, int Atk, int Def) : AzioneEffetto;
    public sealed record ConcediKeyword(Bersaglio Bersaglio, string Keyword) : AzioneEffetto;
    // One-shot: applica un segnalino +X/+X PERSISTENTE alle creature bersaglio.
    public sealed record ApplicaStat(Bersaglio Bersaglio, int Atk, int Def) : AzioneEffetto;

    public sealed record Effetto(Trigger Trigger, IReadOnlyList<AzioneEffetto> Azioni);

    // Hero Power del Leader: azioni eseguite pagando Costo energia, riusabile dopo Cooldown turni.
    public sealed record HeroPower(int Costo, int Cooldown, IReadOnlyList<AzioneEffetto> Azioni);

    public static class Effetti
    {
        public sealed record Risultato(StatoPartita Stato, IReadOnlyList<Evento> Eventi);

        // Esegue, in ordine, tutti gli effetti della carta che corrispondono al trigger dato.
        // controllore = giocatore che controlla la sorgente; sorgenteIid = istanza che genera.
        public static Risultato EseguiTrigger(
            StatoPartita stato, DefCarta def, int controllore, string sorgenteIid, Trigger trigger)
        {
            var eventi = new List<Evento>();
            if (def.Effetti == null) return new Risultato(stato, eventi);

            foreach (Effetto eff in def.Effetti.Where(e => e.Trigger == trigger))
                foreach (AzioneEffetto az in eff.Azioni)
                    stato = ApplicaAzione(stato, controllore, sorgenteIid, az, eventi);

            return new Risultato(stato, eventi);
        }

        // Esegue una lista di azioni effetto (usata dall'Hero Power del Leader).
        public static Risultato EseguiAzioni(
            StatoPartita stato, int controllore, string sorgenteIid, IReadOnlyList<AzioneEffetto> azioni)
        {
            var eventi = new List<Evento>();
            foreach (AzioneEffetto az in azioni)
                stato = ApplicaAzione(stato, controllore, sorgenteIid, az, eventi);
            return new Risultato(stato, eventi);
        }

        private static StatoPartita ApplicaAzione(
            StatoPartita stato, int ctrl, string iid, AzioneEffetto az, List<Evento> ev)
        {
            switch (az)
            {
                case Pesca p: return EseguiPesca(stato, ctrl, p.Valore, ev);
                case GeneraMana g: return EseguiGeneraMana(stato, ctrl, g.Valore, ev);
                case InfliggiDanno d: return EseguiInfliggiDanno(stato, ctrl, d.Bersaglio, d.Valore, ev);
                case Distruggi ds: return EseguiDistruggi(stato, ctrl, ds.Bersaglio, ev);
                case Mill m: return EseguiMill(stato, ctrl, m.Bersaglio, m.Valore, ev);
                case GeneraToken t: return EseguiGeneraToken(stato, ctrl, iid, t, ev);
                case ApplicaStat ap: return EseguiApplicaStat(stato, ctrl, ap, ev);
                default: return stato;
            }
        }

        // --- effetti statici (passiva): stat e keyword effettive ---

        // Stat di una creatura = base (DefCarta) + somma dei modificatori passivi attivi sul campo.
        public static (int Atk, int Def) StatEffettive(StatoPartita stato, CartaIstanza carta)
        {
            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def)) return (0, 0);
            int atk = (def.Atk ?? 0) + carta.BonusAtk;   // base + segnalini persistenti
            int def2 = (def.Def ?? 0) + carta.BonusDef;
            foreach (var (srcCtrl, az) in PassiveAzioni(stato))
            {
                if (az is ModificaStat ms && Bersagliata(carta, srcCtrl, ms.Bersaglio))
                {
                    atk += ms.Atk;
                    def2 += ms.Def;
                }
            }
            return (atk, def2);
        }

        // Keyword effettive = base (DefCarta.Keyword) + keyword concesse da effetti passivi attivi.
        public static ISet<string> KeywordEffettive(StatoPartita stato, CartaIstanza carta)
        {
            var set = new HashSet<string>();
            if (stato.Carte.TryGetValue(carta.DefId, out DefCarta? def) && def.Keyword != null)
                foreach (string k in def.Keyword) set.Add(k);

            foreach (var (srcCtrl, az) in PassiveAzioni(stato))
                if (az is ConcediKeyword ck && Bersagliata(carta, srcCtrl, ck.Bersaglio))
                    set.Add(ck.Keyword);

            return set;
        }

        // Tutte le azioni passive attive sul campo, con il controllore della sorgente.
        private static IEnumerable<(int ctrl, AzioneEffetto az)> PassiveAzioni(StatoPartita stato)
        {
            for (int i = 0; i < stato.Giocatori.Count; i++)
            {
                foreach (CartaIstanza c in stato.Giocatori[i].Campo)
                {
                    if (!stato.Carte.TryGetValue(c.DefId, out DefCarta? def) || def.Effetti == null) continue;
                    foreach (Effetto e in def.Effetti)
                        if (e.Trigger == Trigger.Passiva)
                            foreach (AzioneEffetto az in e.Azioni)
                                yield return (i, az);
                }

                // La passiva del Leader è attiva anche dalla Zona di Comando (quando NON in campo,
                // per non contarla due volte: in campo è già scansionata sopra).
                StatoLeader? lead = stato.Giocatori[i].Leader;
                if (lead != null && !lead.InCampo
                    && stato.Carte.TryGetValue(lead.DefId, out DefCarta? ldef) && ldef.Effetti != null)
                {
                    foreach (Effetto e in ldef.Effetti)
                        if (e.Trigger == Trigger.Passiva)
                            foreach (AzioneEffetto az in e.Azioni)
                                yield return (i, az);
                }
            }
        }

        // La carta bersaglio è colpita dal Bersaglio di una sorgente controllata da srcCtrl?
        private static bool Bersagliata(CartaIstanza bersaglio, int srcCtrl, Bersaglio b)
        {
            if (b.Quantificatore == Quantificatore.Una) return false; // i passivi non hanno target singolo
            int owner = bersaglio.Proprietario;
            bool propOk = b.Proprietario switch
            {
                Proprietario.Tue => owner == srcCtrl,
                Proprietario.Avversario => owner != srcCtrl,
                _ => true,
            };
            return propOk;
        }

        // --- targeting ---

        private static IEnumerable<int> RisolviGiocatori(StatoPartita s, int ctrl, Proprietario p)
        {
            for (int i = 0; i < s.Giocatori.Count; i++)
            {
                bool sel = p switch
                {
                    Proprietario.Tue => i == ctrl,
                    Proprietario.Avversario => i != ctrl,
                    _ => true,
                };
                if (sel) yield return i;
            }
        }

        private static bool ECreatura(StatoPartita s, CartaIstanza c)
            => s.Carte.TryGetValue(c.DefId, out DefCarta? d) && d.Atk != null;

        private static Giocatore[] Sostituisci(StatoPartita s, int idx, Giocatore nuovo)
            => s.Giocatori.Select((g, i) => i == idx ? nuovo : g).ToArray();

        // --- verbi ---

        private static StatoPartita EseguiPesca(StatoPartita stato, int ctrl, int n, List<Evento> ev)
        {
            Giocatore g = stato.Giocatori[ctrl];
            var mazzo = g.Mazzo.ToList();
            var mano = g.Mano.ToList();
            for (int k = 0; k < n; k++)
            {
                if (mazzo.Count == 0) { ev.Add(new MazzoVuoto(ctrl)); break; }
                CartaIstanza cima = mazzo[0];
                mazzo.RemoveAt(0);
                mano.Add(cima);
                ev.Add(new CartaPescata(ctrl, cima.Iid));
            }
            return stato with { Giocatori = Sostituisci(stato, ctrl, g with { Mazzo = mazzo, Mano = mano }) };
        }

        // v1: il verbo genera_mana produce ENERGIA (colorless). Il colore nel dato è ignorato.
        private static StatoPartita EseguiGeneraMana(StatoPartita stato, int ctrl, int n, List<Evento> ev)
        {
            Giocatore g = stato.Giocatori[ctrl];
            ev.Add(new EnergiaGenerata(ctrl, n));
            return stato with { Giocatori = Sostituisci(stato, ctrl, g with { Energia = g.Energia + n }) };
        }

        private static StatoPartita EseguiInfliggiDanno(
            StatoPartita stato, int ctrl, Bersaglio bersaglio, int valore, List<Evento> ev)
        {
            // Bersaglio creatura: accumula Danno (la morte la decide lo state-based globale).
            if (bersaglio.Tipo == "creatura")
            {
                if (bersaglio.Quantificatore == Quantificatore.Una) return stato; // scelta -> 🟡
                foreach (int idx in RisolviGiocatori(stato, ctrl, bersaglio.Proprietario).ToList())
                {
                    Giocatore g = stato.Giocatori[idx];
                    var campo = g.Campo.Select(c =>
                    {
                        if (!ECreatura(stato, c)) return c;
                        ev.Add(new DannoCreatura(c.Iid, valore));
                        return c with { Danno = c.Danno + valore };
                    }).ToList();
                    stato = stato with { Giocatori = Sostituisci(stato, idx, g with { Campo = campo }) };
                }
                return stato;
            }

            // Bersaglio giocatore.
            foreach (int idx in RisolviGiocatori(stato, ctrl, bersaglio.Proprietario).ToList())
            {
                Giocatore g = stato.Giocatori[idx];
                int hp = g.Hp - valore;
                stato = stato with { Giocatori = Sostituisci(stato, idx, g with { Hp = hp }) };
                ev.Add(new DannoGiocatore(idx, valore));

                if (hp <= 0 && !stato.Finita)
                {
                    int? vinc = stato.Giocatori.Count == 2 ? ctrl : (int?)null;
                    stato = stato with { Finita = true, Vincitore = vinc };
                    ev.Add(new PartitaFinita(vinc, "danno-effetto"));
                }
            }
            return stato;
        }

        private static StatoPartita EseguiDistruggi(
            StatoPartita stato, int ctrl, Bersaglio bersaglio, List<Evento> ev)
        {
            // E3a: solo quantificatori deterministici (Tutte/Ogni); Una -> E3b.
            if (bersaglio.Quantificatore == Quantificatore.Una) return stato;

            var morti = new List<(string iid, string defId, int prop)>();
            foreach (int idx in RisolviGiocatori(stato, ctrl, bersaglio.Proprietario).ToList())
            {
                Giocatore g = stato.Giocatori[idx];
                var rimaste = new List<CartaIstanza>();
                var cimitero = g.Cimitero.ToList();
                foreach (CartaIstanza c in g.Campo)
                {
                    if (ECreatura(stato, c))
                    {
                        cimitero.Add(c);
                        ev.Add(new CreaturaDistrutta(c.Iid, c.Proprietario));
                        morti.Add((c.Iid, c.DefId, c.Proprietario));
                    }
                    else rimaste.Add(c);
                }
                stato = stato with
                {
                    Giocatori = Sostituisci(stato, idx, g with { Campo = rimaste, Cimitero = cimitero }),
                };
            }
            return EseguiMorti(stato, morti, ev);
        }

        // E3c.2 — fa scattare gli effetti morte delle creature appena morte.
        // Controllore = proprietario della creatura. Cascata naturale (board finito → termina);
        // i verbi non-Distruggi non causano nuove morti, quindi non c'è loop infinito.
        public static StatoPartita EseguiMorti(
            StatoPartita stato, IEnumerable<(string iid, string defId, int prop)> morti, List<Evento> ev)
        {
            foreach (var m in morti)
            {
                if (!stato.Carte.TryGetValue(m.defId, out DefCarta? def)) continue;
                Risultato r = EseguiTrigger(stato, def, m.prop, m.iid, Trigger.Morte);
                stato = r.Stato;
                ev.AddRange(r.Eventi);
            }
            return stato;
        }

        private static StatoPartita EseguiMill(
            StatoPartita stato, int ctrl, Bersaglio bersaglio, int n, List<Evento> ev)
        {
            foreach (int idx in RisolviGiocatori(stato, ctrl, bersaglio.Proprietario).ToList())
            {
                Giocatore g = stato.Giocatori[idx];
                var mazzo = g.Mazzo.ToList();
                var cimitero = g.Cimitero.ToList();
                for (int k = 0; k < n && mazzo.Count > 0; k++)
                {
                    CartaIstanza cima = mazzo[0];
                    mazzo.RemoveAt(0);
                    cimitero.Add(cima);
                    ev.Add(new CartaMacinata(idx, cima.Iid));
                }
                stato = stato with
                {
                    Giocatori = Sostituisci(stato, idx, g with { Mazzo = mazzo, Cimitero = cimitero }),
                };
            }
            return stato;
        }

        // One-shot: aggiunge segnalini +X/+X persistenti alle creature bersaglio (deterministico).
        private static StatoPartita EseguiApplicaStat(
            StatoPartita stato, int ctrl, ApplicaStat ap, List<Evento> ev)
        {
            if (ap.Bersaglio.Quantificatore == Quantificatore.Una) return stato; // scelta -> 🟡
            foreach (int idx in RisolviGiocatori(stato, ctrl, ap.Bersaglio.Proprietario).ToList())
            {
                Giocatore g = stato.Giocatori[idx];
                var campo = g.Campo.Select(c =>
                    ECreatura(stato, c)
                        ? c with { BonusAtk = c.BonusAtk + ap.Atk, BonusDef = c.BonusDef + ap.Def }
                        : c).ToList();
                stato = stato with { Giocatori = Sostituisci(stato, idx, g with { Campo = campo }) };
            }
            return stato;
        }

        private static StatoPartita EseguiGeneraToken(
            StatoPartita stato, int ctrl, string sorgenteIid, GeneraToken t, List<Evento> ev)
        {
            int dest = t.Controllore == "avversario" ? (ctrl + 1) % stato.Giocatori.Count : ctrl;

            // Cap board: se il campo del destinatario è pieno, il token fizzla (non entra).
            int cap = stato.Config.CapCampo > 0 ? stato.Config.CapCampo : 6;
            if (stato.Giocatori[dest].Campo.Count(c => ECreatura(stato, c)) >= cap)
                return stato;

            string tokDefId = "TOKEN_" + t.Nome;

            // Registra la definizione del token (con stat) se non già presente.
            if (!stato.Carte.ContainsKey(tokDefId))
            {
                var carte = new Dictionary<string, DefCarta>(stato.Carte)
                {
                    [tokDefId] = new DefCarta(tokDefId, "Creatura", Atk: t.Atk, Def: t.Def),
                };
                stato = stato with { Carte = carte };
            }

            Giocatore g = stato.Giocatori[dest];
            // iid deterministico (replay-stabile): sorgente + indice nel campo.
            string iid = $"{sorgenteIid}#tok{g.Campo.Count}";
            var token = new CartaIstanza
            {
                Iid = iid,
                DefId = tokDefId,
                Proprietario = dest,
                EntrataQuestoTurno = true, // summoning sickness come una creatura giocata
            };
            var campo = g.Campo.Concat(new[] { token }).ToList();
            ev.Add(new TokenGenerato(dest, iid, t.Nome));
            return stato with { Giocatori = Sostituisci(stato, dest, g with { Campo = campo }) };
        }
    }
}
