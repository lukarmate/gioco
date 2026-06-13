using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1 Leader slice 1: gioca dalla Zona Comando (costo base + incremento per morte),
    // morte -> ritorno in Zona Comando.
    public class LeaderTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["RE"] = new DefCarta("RE", "Leader", Costo: new ManaCosto { Generico = 3 }, Atk: 3, Def: 3),
            ["ORCO"] = new DefCarta("ORCO", "Creatura", Atk: 3, Def: 3),
        };

        private static StatoPartita ConLeader(int energia = 6, int morti = 0)
        {
            var s = E2.Avvia(Carte());
            return H.ConGiocatore(s, 0, g => g with
            {
                Energia = energia,
                Leader = new StatoLeader { DefId = "RE", Morti = morti },
            });
        }

        [Fact]
        public void GiocaLeader_inCampoEPagaEnergia()
        {
            var s = ConLeader(energia: 5);

            var r = GameEngine.Applica(s, new GiocaLeader());

            Assert.True(r.Ok, r.Errore);
            var g = r.Stato!.Giocatori[0];
            Assert.True(g.Leader!.InCampo);
            Assert.NotNull(g.Leader.Iid);
            Assert.Contains(g.Campo, c => c.Iid == g.Leader.Iid);
            Assert.Equal(2, g.Energia); // 5 - 3
            Assert.Contains(r.Eventi, e => e is LeaderGiocato);
        }

        [Fact]
        public void GiocaLeader_costoIncrementaConLeMorti()
        {
            // 1 morte -> costo 3 + 2 = 5
            var pochiSoldi = ConLeader(energia: 4, morti: 1);
            Assert.False(GameEngine.Applica(pochiSoldi, new GiocaLeader()).Ok);

            var abbastanza = ConLeader(energia: 5, morti: 1);
            var r = GameEngine.Applica(abbastanza, new GiocaLeader());
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(0, r.Stato!.Giocatori[0].Energia); // 5 - 5
        }

        [Fact]
        public void GiocaLeader_giaInCampo_fallisce()
        {
            var s = ConLeader();
            s = GameEngine.Applica(s, new GiocaLeader()).Stato!;
            var r = GameEngine.Applica(s, new GiocaLeader());
            Assert.False(r.Ok);
        }

        [Fact]
        public void LeaderMuore_tornaInZonaComando_nonAlCimitero()
        {
            // Leader del g0 in campo; il g1 lo attacca e lo uccide.
            string leadIid = "lead0";
            var s = E2.Avvia(Carte());
            s = H.ConGiocatore(s, 0, g => g with
            {
                Leader = new StatoLeader { DefId = "RE", InCampo = true, Iid = leadIid },
                Campo = g.Campo.Concat(new[]
                {
                    new CartaIstanza { Iid = leadIid, DefId = "RE", Proprietario = 0 },
                }).ToList(),
            });
            string orco;
            (s, orco) = E2.MettiInCampo(s, 1, "ORCO");
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // -> turno del g1

            var r = GameEngine.Applica(s, new Attacca(orco, leadIid)); // ORCO 3 vs RE 3/3 -> RE muore

            Assert.True(r.Ok, r.Errore);
            var g0 = r.Stato!.Giocatori[0];
            Assert.False(g0.Leader!.InCampo);          // tornato in Zona Comando
            Assert.Equal(1, g0.Leader.Morti);
            Assert.Null(g0.Leader.Iid);
            Assert.DoesNotContain(g0.Campo, c => c.Iid == leadIid);    // non in campo
            Assert.DoesNotContain(g0.Cimitero, c => c.Iid == leadIid); // non in cimitero
            Assert.Contains(r.Eventi, e => e is LeaderTornatoInComando);
        }
    }
}
