using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class GiocaAvampostoTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["AVAMP"] = new DefCarta("AVAMP", "Avamposto",
                Produzione: new ManaProdotto(1, new[] { "est" }, false)),
            ["BESTIA"] = new DefCarta("BESTIA", "Creatura — Bestia",
                Costo: new ManaCosto { Est = 1 }, Atk: 2, Def: 2),
        };

        private static (StatoPartita s, string iid) Setup()
        {
            var stato = E2.Avvia(Carte());
            stato = E2.FinoAMain1(stato);
            return E2.MettiInMano(stato, 0, "AVAMP");
        }

        [Fact]
        public void GiocaAvampostoSpostaInCampo()
        {
            var (s, iid) = Setup();
            int campoPrima = s.Giocatori[0].Campo.Count;
            var r = Engine.Core.Engine.Applica(s, new GiocaAvamposto(iid));
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(campoPrima + 1, r.Stato!.Giocatori[0].Campo.Count);
            Assert.DoesNotContain(r.Stato.Giocatori[0].Mano, c => c.Iid == iid);
            Assert.Contains(r.Stato.Giocatori[0].Campo, c => c.Iid == iid);
            Assert.Contains(r.Eventi, e => e is AvampostoGiocato);
            Assert.True(r.Stato.Giocatori[0].AvampostoGiocatoQuestoTurno);
        }

        [Fact]
        public void UnSoloAvampostoPerTurno()
        {
            var (s, iid) = Setup();
            var r1 = Engine.Core.Engine.Applica(s, new GiocaAvamposto(iid));
            Assert.True(r1.Ok);
            // metti un secondo avamposto in mano e prova a giocarlo
            var (s2, iid2) = E2.MettiInMano(r1.Stato!, 0, "AVAMP");
            var r2 = Engine.Core.Engine.Applica(s2, new GiocaAvamposto(iid2));
            Assert.False(r2.Ok);
        }

        [Fact]
        public void NonGiocaCartaNonAvamposto()
        {
            var (s0, _) = Setup();
            var (s, iid) = E2.MettiInMano(s0, 0, "BESTIA");
            var r = Engine.Core.Engine.Applica(s, new GiocaAvamposto(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void NonGiocaCartaNonInMano()
        {
            var (s, _) = Setup();
            var r = Engine.Core.Engine.Applica(s, new GiocaAvamposto("iid-inesistente"));
            Assert.False(r.Ok);
        }
    }
}
