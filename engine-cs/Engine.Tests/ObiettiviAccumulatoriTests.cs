using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1 obiettivi slice 2: accumulatori per-turno (contatori su Giocatore, reset a inizio turno).
    public class ObiettiviAccumulatoriTests
    {
        private static StatoPartita ConObiettivo(StatoPartita s, int g, string id)
            => H.ConGiocatore(s, g, gg => gg with { ObiettivoId = id });

        [Fact]
        public void OB13_quattroCarteInUnTurno_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-13");

            string? ultimo = null;
            for (int i = 0; i < 4; i++)
            {
                string iid;
                (s, iid) = E2.MettiInMano(s, 0, "SOLD");
                var r = GameEngine.Applica(s, new GiocaCreatura(iid));
                Assert.True(r.Ok, r.Errore);
                s = r.Stato!;
                ultimo = r.Eventi.OfType<ObiettivoCompletato>().Any() ? "win" : ultimo;
            }

            Assert.True(s.Finita);
            Assert.Equal(0, s.Vincitore);
            Assert.Equal("win", ultimo);
        }

        [Fact]
        public void OB03_setteDanniInUnTurno_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["BRUTO"] = new DefCarta("BRUTO", "Creatura", Atk: 7, Def: 7) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-03");
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "BRUTO"); // niente sickness

            var r = GameEngine.Applica(s, new Attacca(a)); // 7 danni agli HP avversario

            Assert.True(r.Ok, r.Errore);
            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
            Assert.Contains(r.Eventi, e => e is ObiettivoCompletato o && o.ObiettivoId == "OB-03");
        }

        [Fact]
        public void Accumulatore_resettaAInizioTurno()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 1, Def: 1) };
            var s = E2.Avvia(carte);

            // gioca 2 carte nel turno del g0
            for (int i = 0; i < 2; i++)
            {
                string iid;
                (s, iid) = E2.MettiInMano(s, 0, "SOLD");
                s = GameEngine.Applica(s, new GiocaCreatura(iid)).Stato!;
            }
            Assert.Equal(2, s.Giocatori[0].CarteGiocateQuestoTurno);

            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // -> g1
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // -> g0 (begin step resetta)
            Assert.Equal(0, s.Giocatori[0].CarteGiocateQuestoTurno);
        }
    }
}
