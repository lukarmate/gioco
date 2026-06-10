using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class AttivaAvampostoTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["FISSO"] = new DefCarta("FISSO", "Avamposto",
                Produzione: new ManaProdotto(1, new[] { "est" }, false)),
            ["DUALE"] = new DefCarta("DUALE", "Avamposto",
                Produzione: new ManaProdotto(1, new[] { "nord", "centro" }, true)),
            ["BESTIA"] = new DefCarta("BESTIA", "Creatura — Bestia",
                Costo: new ManaCosto { Est = 1 }, Atk: 2, Def: 2),
        };

        private static StatoPartita Main1() => E2.FinoAMain1(E2.Avvia(Carte()));

        [Fact]
        public void AttivaProduceManaMonocoloreETappa()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "FISSO");
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid));
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(1, r.Stato!.Giocatori[0].ManaDisponibile.Est);
            Assert.True(r.Stato.Giocatori[0].Campo.First(c => c.Iid == iid).Tappata);
            Assert.Contains(r.Eventi, e => e is ManaGenerato);
        }

        [Fact]
        public void AttivaDualeUsaIlColoreScelto()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "DUALE");
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid, new[] { "centro" }));
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(1, r.Stato!.Giocatori[0].ManaDisponibile.Centro);
            Assert.Equal(0, r.Stato.Giocatori[0].ManaDisponibile.Nord);
        }

        [Fact]
        public void NonAttivaSeGiaTappato()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "FISSO", tappata: true);
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonAttivaNonAvamposto()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "BESTIA");
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonAttivaCartaNonInCampo()
        {
            var r = Engine.Core.Engine.Applica(Main1(), new AttivaAvamposto("ignoto"));
            Assert.False(r.Ok);
        }

        [Fact]
        public void SceltaColoreFuoriDaiColoriProdotti()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "DUALE");
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid, new[] { "sud" }));
            Assert.False(r.Ok);
        }

        [Fact]
        public void SceltaNumeroErrato()
        {
            var (s, iid) = E2.MettiInCampo(Main1(), 0, "DUALE");
            var r = Engine.Core.Engine.Applica(s, new AttivaAvamposto(iid, new string[0]));
            Assert.False(r.Ok);
        }
    }
}
