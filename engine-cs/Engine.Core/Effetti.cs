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

    public sealed record Effetto(Trigger Trigger, IReadOnlyList<AzioneEffetto> Azioni);

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

        private static StatoPartita ApplicaAzione(
            StatoPartita stato, int ctrl, string iid, AzioneEffetto az, List<Evento> ev)
        {
            switch (az)
            {
                case Pesca p: return EseguiPesca(stato, ctrl, p.Valore, ev);
                case GeneraMana g: return EseguiGeneraMana(stato, ctrl, iid, g.Valore, g.Colore, ev);
                case InfliggiDanno d: return EseguiInfliggiDanno(stato, ctrl, d.Bersaglio, d.Valore, ev);
                case Distruggi ds: return EseguiDistruggi(stato, ctrl, ds.Bersaglio, ev);
                case Mill m: return EseguiMill(stato, ctrl, m.Bersaglio, m.Valore, ev);
                default: return stato;
            }
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

        private static StatoPartita EseguiGeneraMana(
            StatoPartita stato, int ctrl, string iid, int n, string colore, List<Evento> ev)
        {
            Giocatore g = stato.Giocatori[ctrl];
            ManaPool pool = AggiungiMana(g.ManaDisponibile, colore, n);
            ev.Add(new ManaGenerato(ctrl, iid, new Dictionary<string, int> { [colore] = n }));
            return stato with { Giocatori = Sostituisci(stato, ctrl, g with { ManaDisponibile = pool }) };
        }

        private static StatoPartita EseguiInfliggiDanno(
            StatoPartita stato, int ctrl, Bersaglio bersaglio, int valore, List<Evento> ev)
        {
            // E3a: solo bersagli di tipo "giocatore".
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
                    }
                    else rimaste.Add(c);
                }
                stato = stato with
                {
                    Giocatori = Sostituisci(stato, idx, g with { Campo = rimaste, Cimitero = cimitero }),
                };
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

        private static ManaPool AggiungiMana(ManaPool p, string colore, int q)
        {
            switch (colore.ToLowerInvariant())
            {
                case "nord": return p with { Nord = p.Nord + q };
                case "sud": return p with { Sud = p.Sud + q };
                case "est": return p with { Est = p.Est + q };
                case "ovest": return p with { Ovest = p.Ovest + q };
                case "centro": return p with { Centro = p.Centro + q };
                default: return p with { Generico = p.Generico + q };
            }
        }
    }
}
