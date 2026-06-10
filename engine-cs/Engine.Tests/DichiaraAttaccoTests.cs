using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class DichiaraAttaccoTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["GUERRIERO"] = new DefCarta("GUERRIERO", "Creatura — Guerriero",
                Costo: new ManaCosto { Est = 1 }, Atk: 3, Def: 2),
        };

        // Stato in fase Combat, giocatore 0 attivo, con una creatura pronta in campo
        // (EntrataQuestoTurno=false => niente summoning sickness).
        private static (StatoPartita s, string iid) ConCreaturaPronta(bool sick = false, bool tappata = false)
        {
            var s = E2.Avvia(Carte());
            var carta = new CartaIstanza
            {
                Iid = "atk", DefId = "GUERRIERO", Proprietario = 0,
                EntrataQuestoTurno = sick, Tappata = tappata,
            };
            s = H.ConGiocatore(s, 0, g => g with { Campo = g.Campo.Concat(new[] { carta }).ToList() });
            s = s with { Fase = Fase.Combat };
            return (s, "atk");
        }

        [Fact]
        public void DichiaraAttaccoTappaLAttaccanteELoRegistra()
        {
            var (s, iid) = ConCreaturaPronta();
            var r = Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { iid }));
            Assert.True(r.Ok, r.Errore);
            Assert.True(r.Stato!.Giocatori[0].Campo.First(c => c.Iid == iid).Tappata);
            Assert.Contains(iid, r.Stato.Combattimento!.Attaccanti);
            Assert.Contains(r.Eventi, e => e is CreaturaAttacca);
        }

        [Fact]
        public void CreaturaConSummoningSicknessNonPuoAttaccare()
        {
            var (s, iid) = ConCreaturaPronta(sick: true);
            var r = Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { iid }));
            Assert.False(r.Ok);
        }

        [Fact]
        public void CreaturaTappataNonPuoAttaccare()
        {
            var (s, iid) = ConCreaturaPronta(tappata: true);
            var r = Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { iid }));
            Assert.False(r.Ok);
        }

        [Fact]
        public void AttaccoSoloInFaseCombat()
        {
            var (s0, iid) = ConCreaturaPronta();
            var s = s0 with { Fase = Fase.Main1 };
            var r = Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { iid }));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonPuoAttaccareCreaturaNonInCampo()
        {
            var (s, _) = ConCreaturaPronta();
            var r = Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { "ignoto" }));
            Assert.False(r.Ok);
        }
    }
}
