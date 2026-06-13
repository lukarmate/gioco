using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1 obiettivi slice 3: streak (condizione soddisfatta per N turni consecutivi propri).
    public class ObiettiviStreakTests
    {
        private static StatoPartita ConObiettivo(StatoPartita s, int g, string id)
            => H.ConGiocatore(s, g, gg => gg with { ObiettivoId = id });

        // Chiude il turno del g0 e fa passare anche il g1 (così si torna al g0).
        private static StatoPartita GiroCompleto(StatoPartita s)
        {
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 -> g1
            return GameEngine.Applica(s, new PassaTurno()).Stato!; // g1 -> g0
        }

        [Fact]
        public void OB01_dannoPer3TurniConsecutivi_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["GOBLIN"] = new DefCarta("GOBLIN", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-01");
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "GOBLIN");

            // round 1 e 2: attacca, poi giro completo
            s = GameEngine.Applica(s, new Attacca(a)).Stato!;
            s = GiroCompleto(s);
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);

            s = GameEngine.Applica(s, new Attacca(a)).Stato!;
            s = GiroCompleto(s);
            Assert.Equal(2, s.Giocatori[0].StreakObiettivo);

            // round 3: attacca e chiudi il turno -> streak 3 -> vittoria
            s = GameEngine.Applica(s, new Attacca(a)).Stato!;
            var r = GameEngine.Applica(s, new PassaTurno());

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
            Assert.Contains(r.Eventi, e => e is ObiettivoCompletato o && o.ObiettivoId == "OB-01");
        }

        [Fact]
        public void OB01_streakSiAzzeraSeSaltiUnTurno()
        {
            var carte = new Dictionary<string, DefCarta> { ["GOBLIN"] = new DefCarta("GOBLIN", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-01");
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "GOBLIN");

            // round 1: attacca -> streak 1
            s = GameEngine.Applica(s, new Attacca(a)).Stato!;
            s = GiroCompleto(s);
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);

            // round 2: NON attacca -> streak azzerata
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 chiude senza attaccare
            Assert.Equal(0, s.Giocatori[0].StreakObiettivo);
        }

        [Fact]
        public void OB07_piuCreaturePer3Turni_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 1, Def: 2) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-07");
            (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: "a"); // g0 ha 1 creatura, g1 ne ha 0

            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 chiude -> streak 1
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g1 -> g0
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 chiude -> streak 2
            Assert.Equal(2, s.Giocatori[0].StreakObiettivo);
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g1 -> g0
            var r = GameEngine.Applica(s, new PassaTurno()); // g0 chiude -> streak 3 -> vittoria

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
        }
    }
}
