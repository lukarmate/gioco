using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class GiocaCreaturaTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["BESTIA"] = new DefCarta("BESTIA", "Creatura — Bestia",
                Costo: new ManaCosto { Est = 1 }, Atk: 2, Def: 2),
            ["AVAMP"] = new DefCarta("AVAMP", "Avamposto",
                Produzione: new ManaProdotto(1, new[] { "est" }, false)),
        };

        private static StatoPartita Main1ConMana(int est = 1)
        {
            var s = E2.FinoAMain1(E2.Avvia(Carte()));
            return H.ConGiocatore(s, 0, g => g with { ManaDisponibile = new ManaPool { Est = est } });
        }

        [Fact]
        public void GiocaCreaturaPagaManaEMetteInCampo()
        {
            var (s, iid) = E2.MettiInMano(Main1ConMana(), 0, "BESTIA");
            var r = Engine.Core.Engine.Applica(s, new GiocaCreatura(iid));
            Assert.True(r.Ok, r.Errore);
            Assert.Contains(r.Stato!.Giocatori[0].Campo, c => c.Iid == iid);
            Assert.DoesNotContain(r.Stato.Giocatori[0].Mano, c => c.Iid == iid);
            Assert.Equal(0, r.Stato.Giocatori[0].ManaDisponibile.Est);
            Assert.True(r.Stato.Giocatori[0].Campo.First(c => c.Iid == iid).EntrataQuestoTurno);
            Assert.Contains(r.Eventi, e => e is CreaturaGiocata);
        }

        [Fact]
        public void NonGiocaSenzaManaSufficiente()
        {
            var (s, iid) = E2.MettiInMano(Main1ConMana(est: 0), 0, "BESTIA");
            var r = Engine.Core.Engine.Applica(s, new GiocaCreatura(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonGiocaCartaNonCreatura()
        {
            var (s, iid) = E2.MettiInMano(Main1ConMana(), 0, "AVAMP");
            var r = Engine.Core.Engine.Applica(s, new GiocaCreatura(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonGiocaCartaNonInMano()
        {
            var r = Engine.Core.Engine.Applica(Main1ConMana(), new GiocaCreatura("ignoto"));
            Assert.False(r.Ok);
        }
    }
}
