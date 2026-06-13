using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // 🟡 ultimi obiettivi: kill-attribution (OB-09/10), leader-streak (OB-17),
    // faccia distinti (OB-04), no-danno (OB-20), difensivi (OB-08/21).
    public class ObiettiviPool2Tests
    {
        private static StatoPartita ConObiettivo(StatoPartita s, int g, string id)
            => H.ConGiocatore(s, g, gg => gg with { ObiettivoId = id });

        private static StatoPartita GiroCompleto(StatoPartita s)
        {
            s = GameEngine.Applica(s, new PassaTurno()).Stato!;
            return GameEngine.Applica(s, new PassaTurno()).Stato!;
        }

        [Fact]
        public void OB10_distruggeDueCreatureNelTurno_vince()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["BOMBA"] = new DefCarta("BOMBA", "Creatura", Atk: 1, Def: 1,
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[]
                        {
                            new Distruggi(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte)),
                        }),
                    }),
                ["GOLEM"] = new DefCarta("GOLEM", "Creatura", Atk: 2, Def: 2),
            };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-10");
            (s, _) = E2.MettiInCampo(s, 1, "GOLEM", iidSuffix: "1");
            (s, _) = E2.MettiInCampo(s, 1, "GOLEM", iidSuffix: "2");
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "BOMBA");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid)); // etb distrugge 2 creature avversarie

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
        }

        [Fact]
        public void OB17_leaderSopravvive2Turni_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["RE"] = new DefCarta("RE", "Leader", Atk: 3, Def: 3) };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with
            {
                ObiettivoId = "OB-17",
                Leader = new StatoLeader { DefId = "RE", InCampo = true, Iid = "lead0" },
                Campo = g.Campo.Concat(new[] { new CartaIstanza { Iid = "lead0", DefId = "RE", Proprietario = 0 } }).ToList(),
            });

            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 chiude, leader in campo -> streak 1
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g1 -> g0
            var r = GameEngine.Applica(s, new PassaTurno()); // g0 chiude -> streak 2 -> vittoria

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
        }

        [Fact]
        public void OB04_treCreatureDiverseColpisconoFaccia_vince()
        {
            var carte = new Dictionary<string, DefCarta> { ["GOB"] = new DefCarta("GOB", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-04");
            var iids = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                string id;
                (s, id) = E2.MettiInCampo(s, 0, "GOB", iidSuffix: i.ToString());
                iids.Add(id);
            }

            foreach (var id in iids)
            {
                var rr = GameEngine.Applica(s, new Attacca(id)); // colpisce la faccia
                Assert.True(rr.Ok, rr.Errore);
                s = rr.Stato!;
            }

            Assert.True(s.Finita);
            Assert.Equal(0, s.Vincitore);
        }

        [Fact]
        public void Pool_contiene22Obiettivi()
        {
            Assert.Equal(22, Obiettivi.Pool.Count);
        }

        [Fact]
        public void OB18_treHeroPower_completa()
        {
            var carte = new Dictionary<string, DefCarta> { ["X"] = new DefCarta("X", "Creatura", Atk: 1, Def: 1) };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with { ObiettivoId = "OB-18", HeroPowerTurniUsati = 3 });
            var (s2, _) = Obiettivi.AggiornaEControlla(s);
            Assert.True(s2.Finita);
            Assert.Equal(0, s2.Vincitore);
        }

        [Fact]
        public void OB20_nessunDannoPer2Turni_vince()
        {
            // g0 difensivo, nessuno lo attacca: streak cresce. Dal turno 3.
            var carte = new Dictionary<string, DefCarta> { ["X"] = new DefCarta("X", "Creatura", Atk: 1, Def: 1) };
            var s = ConObiettivo(E2.Avvia(carte), 0, "OB-20");

            // porta NumeroTurno avanti senza danni: vari giri completi
            s = GiroCompleto(s); // turno 3 (g0)
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g0 chiude turno 3 -> streak 1 (NumeroTurno>=3, no danno)
            Assert.Equal(1, s.Giocatori[0].StreakObiettivo);
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // g1 -> g0
            var r = GameEngine.Applica(s, new PassaTurno()); // g0 chiude -> streak 2 -> vittoria

            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
        }
    }
}
