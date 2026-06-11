using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public static class Fasi
    {
        public static readonly IReadOnlyList<Fase> Ordine = new List<Fase>
        {
            Fase.Untap, Fase.Upkeep, Fase.Pesca, Fase.Main1, Fase.Combat, Fase.Main2, Fase.End
        };

        public sealed record RisultatoFase(StatoPartita Stato, IReadOnlyList<Evento> Eventi);

        // Esegue la logica di INGRESSO di una fase per il giocatore attivo.
        // Non modifica TurnoDi/NumeroTurno (lo fa l'orchestratore in Engine).
        public static RisultatoFase EseguiEntrataFase(StatoPartita stato, Fase fase)
        {
            var eventi = new List<Evento> { new FaseEntrata(fase) };
            switch (fase)
            {
                case Fase.Untap: return Untap(stato, eventi);
                case Fase.Upkeep: return Upkeep(stato, eventi);
                case Fase.Pesca: return Pesca(stato, eventi);
                default: return new RisultatoFase(stato, eventi);
            }
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

            // Nuovo turno del giocatore: può rigiocare un avamposto.
            var nuovo = g with { Campo = campo, Mazzo = mazzo, AvampostoGiocatoQuestoTurno = false };
            var giocatori = SostituisciGiocatore(stato, att, nuovo);
            return new RisultatoFase(stato with { Giocatori = giocatori }, eventi);
        }

        // E3 — all'ingresso dell'upkeep, scattano gli effetti upkeep dei permanenti
        // in campo del giocatore attivo (in ordine di campo).
        private static RisultatoFase Upkeep(StatoPartita stato, List<Evento> eventi)
        {
            int att = stato.TurnoDi;
            // Snapshot iid+defId prima del loop: gli effetti possono modificare il campo.
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
