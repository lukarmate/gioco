using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    // v1: una sola fase di gioco (Azioni). Il "begin step" del turno (untap + upkeep + pesca)
    // è automatico e gira a inizio turno del giocatore attivo (InizioTurno).
    public static class Fasi
    {
        public sealed record RisultatoFase(StatoPartita Stato, IReadOnlyList<Evento> Eventi);

        // Begin step automatico: energia, untap, upkeep, pesca. Lascia il giocatore in fase Azioni.
        public static RisultatoFase InizioTurno(StatoPartita stato)
        {
            var eventi = new List<Evento>();
            stato = Energia(stato, eventi).Stato;
            stato = Untap(stato, eventi).Stato;
            stato = Upkeep(stato, eventi).Stato;
            stato = Pesca(stato, eventi).Stato;
            return new RisultatoFase(stato, eventi);
        }

        // v1: a inizio turno EnergiaMax += 1 (fino al cap), Energia ricaricata a EnergiaMax.
        private static RisultatoFase Energia(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            int cap = stato.Config.CapEnergia > 0 ? stato.Config.CapEnergia : 8;
            int max = System.Math.Min(cap, g.EnergiaMax + 1);
            var nuovo = g with { EnergiaMax = max, Energia = max };
            eventi.Add(new EnergiaRicaricata(att, max));
            return new RisultatoFase(stato with { Giocatori = SostituisciGiocatore(stato, att, nuovo) }, eventi);
        }

        // Wrapper pubblici single-arg (comodi per i test).
        public static RisultatoFase Untap(StatoPartita stato)
        {
            var e = new List<Evento>();
            return Untap(stato, e);
        }

        public static RisultatoFase Pesca(StatoPartita stato)
        {
            var e = new List<Evento>();
            return Pesca(stato, e);
        }

        private static Giocatore[] SostituisciGiocatore(StatoPartita stato, int idx, Giocatore nuovo)
        {
            return stato.Giocatori.Select((g, i) => i == idx ? nuovo : g).ToArray();
        }

        private static RisultatoFase Untap(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];

            // Stappa e azzera la summoning sickness delle carte in campo del giocatore attivo.
            var campo = g.Campo.Select(c =>
            {
                if (c.Tappata) eventi.Add(new CartaStappata(c.Iid));
                bool cambia = c.Tappata || c.EntrataQuestoTurno;
                return cambia ? c with { Tappata = false, EntrataQuestoTurno = false } : c;
            }).ToList();

            var mazzo = g.Mazzo.Select(c =>
            {
                if (c.Tappata) eventi.Add(new CartaStappata(c.Iid));
                return c.Tappata ? c with { Tappata = false } : c;
            }).ToList();

            var nuovo = g with { Campo = campo, Mazzo = mazzo };
            var giocatori = SostituisciGiocatore(stato, att, nuovo);
            return new RisultatoFase(stato with { Giocatori = giocatori }, eventi);
        }

        // E3 — all'inizio turno scattano gli effetti upkeep dei permanenti del giocatore attivo.
        private static RisultatoFase Upkeep(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            var permanenti = stato.Giocatori[att].Campo.Select(c => (c.Iid, c.DefId)).ToList();
            foreach (var (iid, defId) in permanenti)
            {
                if (!stato.Carte.TryGetValue(defId, out DefCarta? def)) continue;
                Effetti.Risultato r = Effetti.EseguiTrigger(stato, def, att, iid, Trigger.Upkeep);
                stato = r.Stato;
                eventi.AddRange(r.Eventi);
            }
            return new RisultatoFase(stato, eventi);
        }

        private static RisultatoFase Pesca(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            if (stato.NumeroTurno == 1 && att == stato.PrimoGiocatore && stato.Config.PrimoNonPescaT1)
            {
                return new RisultatoFase(stato, eventi);
            }

            Giocatore g = stato.Giocatori[att];
            if (g.Mazzo.Count == 0)
            {
                return Deckout(stato, eventi);
            }

            CartaIstanza cima = g.Mazzo[0];
            var resto = g.Mazzo.Skip(1).ToList();
            var nuovaMano = g.Mano.Concat(new[] { cima }).ToList();
            var nuovo = g with { Mazzo = resto, Mano = nuovaMano };
            var giocatori = SostituisciGiocatore(stato, att, nuovo);
            eventi.Add(new CartaPescata(att, cima.Iid));
            return new RisultatoFase(stato with { Giocatori = giocatori }, eventi);
        }

        private static RisultatoFase Deckout(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            eventi.Add(new MazzoVuoto(att));

            if (stato.Config.PenalitaMazzoVuoto == PenalitaMazzoVuoto.Fatigue)
            {
                Giocatore g = stato.Giocatori[att];
                int fat = g.Fatigue + 1;          // danno crescente: 1, 2, 3, ...
                int hp = g.Hp - fat;
                var giocatori = SostituisciGiocatore(stato, att, g with { Fatigue = fat, Hp = hp });
                eventi.Add(new FatigueSubita(att, fat));

                if (hp <= 0)
                {
                    int? vincF = stato.Giocatori.Count == 2 ? (att + 1) % 2 : (int?)null;
                    eventi.Add(new PartitaFinita(vincF, "fatigue"));
                    return new RisultatoFase(
                        stato with { Giocatori = giocatori, Finita = true, Vincitore = vincF }, eventi);
                }
                return new RisultatoFase(stato with { Giocatori = giocatori }, eventi);
            }

            if (stato.Config.PenalitaMazzoVuoto == PenalitaMazzoVuoto.DannoPerTurno)
            {
                int danno = stato.Config.DannoMazzoVuoto ?? 1;
                Giocatore g = stato.Giocatori[att];
                int hp = g.Hp - danno;
                var giocatori = SostituisciGiocatore(stato, att, g with { Hp = hp });

                if (hp <= 0)
                {
                    int? vincitore = stato.Giocatori.Count == 2 ? (att + 1) % 2 : (int?)null;
                    eventi.Add(new PartitaFinita(vincitore, "deckout-danno"));
                    return new RisultatoFase(
                        stato with { Giocatori = giocatori, Finita = true, Vincitore = vincitore },
                        eventi);
                }
                return new RisultatoFase(stato with { Giocatori = giocatori }, eventi);
            }

            // PerditaImmediata
            int? vinc = stato.Giocatori.Count == 2 ? (att + 1) % 2 : (int?)null;
            eventi.Add(new PartitaFinita(vinc, "deckout"));
            return new RisultatoFase(stato with { Finita = true, Vincitore = vinc }, eventi);
        }
    }
}
