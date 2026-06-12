using System.Collections.Generic;
using System.Linq;
using Engine.Core;

namespace Engine.Tests
{
    // Helper per i test E2: avvia una partita 2p e mette carte specifiche in mano al giocatore 0,
    // con le relative definizioni registrate in stato.Carte.
    public static class E2
    {
        public static ConfigPartita Config() => H.Config();

        // Avvia con definizioni custom; mazzi pieni di filler così Carte contiene tutto.
        public static StatoPartita Avvia(IReadOnlyDictionary<string, DefCarta> carte, int seed = 5)
        {
            var ids = carte.Keys.ToList();
            // garantisce >= manoIniziale carte per mazzo
            while (ids.Count < 8) ids.Add(ids[0]);
            var mazzi = new List<IReadOnlyList<string>> { ids.ToList(), ids.ToList() };
            return H.Avvia(seed, Config(), mazzi, carte);
        }

        // Mette in mano al giocatore una singola CartaIstanza con il defId dato.
        public static (StatoPartita stato, string iid) MettiInMano(StatoPartita s, int giocatore, string defId)
        {
            string iid = "test-" + defId;
            var carta = new CartaIstanza { Iid = iid, DefId = defId, Proprietario = giocatore };
            var ns = H.ConGiocatore(s, giocatore, g => g with { Mano = g.Mano.Concat(new[] { carta }).ToList() });
            return (ns, iid);
        }

        // Mette una CartaIstanza in campo (es. avamposto già giocato).
        // sick = summoning sickness (EntrataQuestoTurno). iidSuffix per metterne più di una stesso defId.
        public static (StatoPartita stato, string iid) MettiInCampo(
            StatoPartita s, int giocatore, string defId,
            bool tappata = false, bool sick = false, string iidSuffix = "")
        {
            string iid = "campo-" + defId + iidSuffix;
            var carta = new CartaIstanza
            {
                Iid = iid, DefId = defId, Proprietario = giocatore,
                Tappata = tappata, EntrataQuestoTurno = sick,
            };
            var ns = H.ConGiocatore(s, giocatore, g => g with { Campo = g.Campo.Concat(new[] { carta }).ToList() });
            return (ns, iid);
        }

        // v1: dopo Avvia il giocatore 0 è già in fase Azioni e può agire. Shim per compat coi test.
        public static StatoPartita FinoAMain1(StatoPartita s) => s;
    }
}
