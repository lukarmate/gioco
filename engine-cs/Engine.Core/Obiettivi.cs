using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    // Telegrafo a 3 stati (+ Completo): l'avversario vede il progresso vago, non il contenuto.
    public enum ProgressoObiettivo { Lontano, Vicino, Quasi, Completo }

    public sealed record DefObiettivo(string Id, string Nome);

    // v1: obiettivi segreti. Vittoria parallela alla riduzione HP: chi completa il proprio
    // obiettivo vince. Gli obiettivi snapshot si valutano sullo stato corrente; quelli con
    // accumulatori/streak (turni consecutivi, danno per turno) arrivano nello slice successivo.
    public static class Obiettivi
    {
        // Registro: id -> (definizione, valutatore). Il valutatore ritorna (corrente, target);
        // completato quando corrente >= target. Il pool ranked completo è in
        // FEEDBACK_DESIGN_V1_E_OBIETTIVI.md §3 (qui solo gli snapshot dello slice 1).
        private static readonly Dictionary<string, (DefObiettivo Def, Func<StatoPartita, int, (int, int)> Valuta)> Reg
            = new()
            {
                ["OB-02"] = (new DefObiettivo("OB-02", "Sotto Assedio"), (s, g) =>
                {
                    int opp = (g + 1) % s.Giocatori.Count;
                    int hp0 = s.Config.HpIniziali;
                    int soglia = 12;
                    return (Math.Max(0, hp0 - s.Giocatori[opp].Hp), Math.Max(1, hp0 - soglia));
                }),
                ["OB-03"] = (new DefObiettivo("OB-03", "Colpo Grosso"), (s, g) =>
                    (s.Giocatori[g].DanniAvversarioQuestoTurno, 7)),
                ["OB-05"] = (new DefObiettivo("OB-05", "Esercito"), (s, g) =>
                    (Creature(s, g), 4)),
                ["OB-13"] = (new DefObiettivo("OB-13", "Profusione"), (s, g) =>
                    (s.Giocatori[g].CarteGiocateQuestoTurno, 4)),
                ["OB-14"] = (new DefObiettivo("OB-14", "Eco dei Caduti"), (s, g) =>
                    (s.Giocatori[g].Cimitero.Count, 8)),
            };

        public static IReadOnlyCollection<DefObiettivo> Pool => Reg.Values.Select(v => v.Def).ToList();

        private static int Creature(StatoPartita s, int g)
            => s.Giocatori[g].Campo.Count(c =>
                s.Carte.TryGetValue(c.DefId, out DefCarta? d) && d.Atk != null);

        // Valuta l'obiettivo di un giocatore: (progresso telegrafato, completato).
        public static (ProgressoObiettivo Progresso, bool Completo) Valuta(StatoPartita s, int g)
        {
            string? id = s.Giocatori[g].ObiettivoId;
            if (id == null || !Reg.TryGetValue(id, out var entry)) return (ProgressoObiettivo.Lontano, false);

            var (corrente, target) = entry.Valuta(s, g);
            if (target <= 0 || corrente >= target) return (ProgressoObiettivo.Completo, true);

            double r = (double)corrente / target;
            ProgressoObiettivo p = r >= 0.66 ? ProgressoObiettivo.Quasi
                                 : r >= 0.33 ? ProgressoObiettivo.Vicino
                                 : ProgressoObiettivo.Lontano;
            return (p, false);
        }

        // Da chiamare dopo ogni azione: aggiorna il progresso di tutti i giocatori e, se uno
        // completa il proprio obiettivo (e la partita non è già finita), lo fa vincere.
        public static (StatoPartita Stato, IReadOnlyList<Evento> Eventi) AggiornaEControlla(StatoPartita s)
        {
            var eventi = new List<Evento>();
            if (s.Finita) return (s, eventi);

            for (int g = 0; g < s.Giocatori.Count; g++)
            {
                if (s.Giocatori[g].ObiettivoId == null) continue;
                var (prog, completo) = Valuta(s, g);
                Giocatore gioc = s.Giocatori[g];

                if (prog != gioc.ObiettivoProgresso)
                {
                    s = Sostituisci(s, g, gioc with { ObiettivoProgresso = prog });
                    eventi.Add(new ObiettivoProgredito(g, prog));
                }

                if (completo && !s.Finita)
                {
                    s = Sostituisci(s, g, s.Giocatori[g] with { ObiettivoCompletato = true });
                    s = s with { Finita = true, Vincitore = g };
                    eventi.Add(new ObiettivoCompletato(g, s.Giocatori[g].ObiettivoId!));
                    eventi.Add(new PartitaFinita(g, "obiettivo"));
                }
            }
            return (s, eventi);
        }

        private static StatoPartita Sostituisci(StatoPartita s, int idx, Giocatore nuovo)
            => s with { Giocatori = s.Giocatori.Select((g, i) => i == idx ? nuovo : g).ToList() };
    }
}
