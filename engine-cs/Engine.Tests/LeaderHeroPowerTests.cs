using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1 Leader slice 2: Hero Power (cooldown + costo energia) e passiva attiva da Zona Comando.
    public class LeaderHeroPowerTests
    {
        // Leader con hero power: costo 2, cooldown 2, infligge 3 danni all'avversario.
        private static DefCarta ReConHP() => new DefCarta("RE", "Leader", Atk: 3, Def: 3,
            HeroPower: new HeroPower(2, 2, new AzioneEffetto[]
            {
                new InfliggiDanno(new Bersaglio("giocatore", Proprietario.Avversario), 3),
            }));

        private static StatoPartita ConLeaderHP(int energia = 5, int cooldown = 0)
        {
            var carte = new Dictionary<string, DefCarta> { ["RE"] = ReConHP() };
            var s = E2.Avvia(carte);
            return H.ConGiocatore(s, 0, g => g with
            {
                Energia = energia,
                Leader = new StatoLeader { DefId = "RE", CooldownHeroPower = cooldown },
            });
        }

        [Fact]
        public void HeroPower_esegueEffettoPagaEMetteInCooldown()
        {
            var s = ConLeaderHP(energia: 5);
            int hpOpp = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new AttivaHeroPower());

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpOpp - 3, r.Stato!.Giocatori[1].Hp);
            Assert.Equal(3, r.Stato.Giocatori[0].Energia);             // 5 - 2
            Assert.Equal(2, r.Stato.Giocatori[0].Leader!.CooldownHeroPower);
        }

        [Fact]
        public void HeroPower_inCooldown_fallisce()
        {
            var s = ConLeaderHP(energia: 5, cooldown: 1);
            var r = GameEngine.Applica(s, new AttivaHeroPower());
            Assert.False(r.Ok);
        }

        [Fact]
        public void HeroPower_cooldownScalaAInizioTurno()
        {
            var s = ConLeaderHP(energia: 5);
            s = GameEngine.Applica(s, new AttivaHeroPower()).Stato!; // cooldown 2
            Assert.Equal(2, s.Giocatori[0].Leader!.CooldownHeroPower);

            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // -> g1
            s = GameEngine.Applica(s, new PassaTurno()).Stato!; // -> g0, begin step scala -1
            Assert.Equal(1, s.Giocatori[0].Leader!.CooldownHeroPower);
        }

        [Fact]
        public void Passiva_leaderAttivaDaZonaComando()
        {
            // Leader con passiva +1/+1 alle proprie creature, MAI giocato (resta in Zona Comando).
            var leaderDef = new DefCarta("VESSILLIFERO", "Leader", Atk: 2, Def: 2,
                Effetti: new List<Effetto>
                {
                    new Effetto(Trigger.Passiva, new AzioneEffetto[]
                    {
                        new ModificaStat(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), 1, 1),
                    }),
                });
            var carte = new Dictionary<string, DefCarta>
            {
                ["VESSILLIFERO"] = leaderDef,
                ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 2, Def: 2),
            };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with
            {
                Leader = new StatoLeader { DefId = "VESSILLIFERO" }, // in Zona Comando (non InCampo)
            });
            string sid;
            (s, sid) = E2.MettiInCampo(s, 0, "SOLD");

            var (atk, def) = Effetti.StatEffettive(s, s.Giocatori[0].Campo.First(c => c.Iid == sid));

            Assert.Equal((3, 3), (atk, def)); // 2/2 + buff leader dalla Zona Comando
        }
    }
}
