using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // 🟡 estensione pool obiettivi: OB-06 (snapshot), OB-22 (accumulatore attacchi),
    // OB-15 (accumulatore energia spesa), OB-19 (snapshot+turno), OB-12 (streak mano).
    public class ObiettiviPoolTests
    {
        private static StatoPartita ConObiettivo(StatoPartita s, int g, string id)
            => H.ConGiocatore(s, g, gg => gg with { ObiettivoId = id });

        [Fact]
        public void OB06_cinqueCreature_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-06");
            for (int i = 0; i < 5; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());

            var (s2, _) = Obiettivi.AggiornaEControlla(s);

            Assert.True(s2.Finita);
            Assert.Equal(0, s2.Vincitore);
        }

        [Fact]
        public void OB22_attaccaCon4Creature_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["GOB"] = new DefCarta("GOB", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-22");
            var iids = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                string id;
                (s, id) = E2.MettiInCampo(s, 0, "GOB", iidSuffix: i.ToString());
                iids.Add(id);
            }

            foreach (var id in iids) // 4 attacchi agli HP avversario
            {
                var rr = GameEngine.Applica(s, new Attacca(id));
                Assert.True(rr.Ok, rr.Errore);
                s = rr.Stato!;
            }

            Assert.True(s.Finita);
            Assert.Equal(0, s.Vincitore);
        }

        [Fact]
        public void OB12_seiCarteInManoPer2Turni_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["X"] = new DefCarta("X", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-12");
            // porta la mano del g0 a 6 carte (lascia il mazzo: al turno 3 pescherà -> 7, sempre >=6)
            s = H.ConGiocatore(s, 0, g => g with
            {
                Mano = Enumerable.Range(0, 6).Select(i => new CartaIstanza { Iid = "h" + i, DefId = "X", Proprietario = 0 }).ToList(),
            });

            // turno 1 g0 chiude con 6 carte -> streak 1
            s = GameEngine.Applica(s, new PassaTurno()).Stato!;
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);
            // g1 chiude -> torna al g0
            s = GameEngine.Applica(s, new PassaTurno()).Stato!;
            // g0 chiude di nuovo con 6 carte -> streak 2 -> vittoria
            var r = GameEngine.Applica(s, new PassaTurno());

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
        }
    }
}
