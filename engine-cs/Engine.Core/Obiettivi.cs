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
                ["OB-06"] = (new DefObiettivo("OB-06", "Orda"), (s, g) =>
                    (Creature(s, g), 5)),
                ["OB-15"] = (new DefObiettivo("OB-15", "Ultima Riserva"), (s, g) =>
                    (s.Giocatori[g].EnergiaSpesaQuestoTurno, 8)),
                ["OB-19"] = (new DefObiettivo("OB-19", "Fortezza"), (s, g) =>
                    (s.NumeroTurno >= 7 ? s.Giocatori[g].Hp : 0, 24)),
                ["OB-22"] = (new DefObiettivo("OB-22", "Sfida Aperta"), (s, g) =>
                    (s.Giocatori[g].AttacchiQuestoTurno, 4)),
                ["OB-04"] = (new DefObiettivo("OB-04", "Sangue per Sangue"), (s, g) =>
                    (s.Giocatori[g].CreatureColpisconoFaccia.Count, 3)),
                ["OB-09"] = (new DefObiettivo("OB-09", "Mietitore"), (s, g) =>
                    (s.Giocatori[g].CreatureNemicheDistrutte, 4)),
                ["OB-10"] = (new DefObiettivo("OB-10", "Strage"), (s, g) =>
                    (s.Giocatori[g].CreatureNemicheDistrutteQuestoTurno, 2)),
                ["OB-16"] = (new DefObiettivo("OB-16", "Avanguardia"), (s, g) =>
                    (s.Giocatori[g].LeaderHaColpitoFaccia ? 1 : 0, 1)),
                ["OB-18"] = (new DefObiettivo("OB-18", "Volontà di Ferro"), (s, g) =>
                    (s.Giocatori[g].HeroPowerTurniUsati, 3)),
                ["OB-13"] = (new DefObiettivo("OB-13", "Profusione"), (s, g) =>
                    (s.Giocatori[g].CarteGiocateQuestoTurno, 4)),
                ["OB-14"] = (new DefObiettivo("OB-14", "Eco dei Caduti"), (s, g) =>
                    (s.Giocatori[g].Cimitero.Count, 8)),
                // Streak: il progresso è il contatore StreakObiettivo (aggiornato a fine turno).
                ["OB-01"] = (new DefObiettivo("OB-01", "Ferite Aperte"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 3)),
                ["OB-07"] = (new DefObiettivo("OB-07", "Dominio"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 3)),
                ["OB-12"] = (new DefObiettivo("OB-12", "Mente Lucida"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 2)),
                ["OB-08"] = (new DefObiettivo("OB-08", "Linea di Difesa"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 2)),
                ["OB-11"] = (new DefObiettivo("OB-11", "Rappresaglia"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 3)),
                ["OB-17"] = (new DefObiettivo("OB-17", "Stendardo di Guerra"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 2)),
                ["OB-20"] = (new DefObiettivo("OB-20", "Muro Inviolato"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 2)),
                ["OB-21"] = (new DefObiettivo("OB-21", "Resistenza"), (s, g) =>
                    (s.Giocatori[g].StreakObiettivo, 4)),
            };

        // Condizione "per turno" degli obiettivi streak: valutata a fine turno del giocatore.
        // Se vera lo streak cresce di 1, altrimenti si azzera.
        private static readonly Dictionary<string, Func<StatoPartita, int, bool>> StreakCondizioni
            = new()
            {
                ["OB-01"] = (s, g) => s.Giocatori[g].DanniAvversarioQuestoTurno > 0,
                ["OB-07"] = (s, g) => Creature(s, g) > Creature(s, (g + 1) % s.Giocatori.Count),
                ["OB-12"] = (s, g) => s.Giocatori[g].Mano.Count >= 6,
                // OB-08: almeno 3 creature con DEF non danneggiata (Danno == 0).
                ["OB-08"] = (s, g) => s.Giocatori[g].Campo.Count(c =>
                    s.Carte.TryGetValue(c.DefId, out var d) && d.Atk != null && c.Danno == 0) >= 3,
                // OB-11: ha distrutto almeno una creatura avversaria questo turno.
                ["OB-11"] = (s, g) => s.Giocatori[g].CreatureNemicheDistrutteQuestoTurno > 0,
                // OB-17: il Leader è sopravvissuto in campo per tutto il turno.
                ["OB-17"] = (s, g) => s.Giocatori[g].Leader is { InCampo: true },
                // OB-20: non ha subito danno nella finestra (dal turno 3 in poi).
                ["OB-20"] = (s, g) => s.NumeroTurno >= 3 && s.Giocatori[g].DannoSubitoFinestra == 0,
                // OB-21: controlla almeno 1 creatura.
                ["OB-21"] = (s, g) => Creature(s, g) >= 1,
            };

        // Da chiamare a FINE turno del giocatore g (prima di passare): aggiorna il suo streak.
        public static StatoPartita AggiornaStreak(StatoPartita s, int g)
        {
            string? id = s.Giocatori[g].ObiettivoId;
            if (id == null || !StreakCondizioni.TryGetValue(id, out var cond)) return s;
            int streak = cond(s, g) ? s.Giocatori[g].StreakObiettivo + 1 : 0;
            return Sostituisci(s, g, s.Giocatori[g] with { StreakObiettivo = streak });
        }

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
