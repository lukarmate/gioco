using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class SetupTests
    {
        private static readonly ConfigPartita Config = H.Config();

        [Fact]
        public void DistribuisceManoInizialeEImpostaStatoTurno1()
        {
            var carte = H.CarteFinte(new[] { "A", "B", "C", "D", "E" });
            var mazzi = new List<IReadOnlyList<string>>
            {
                new[] { "A", "B", "C", "D", "E" },
                new[] { "A", "B", "C", "D", "E" },
            };
            var r = Setup.IniziaPartita(new IniziaPartita(1, Config, mazzi, carte));

            Assert.Equal(2, r.Stato.Giocatori.Count);
            Assert.Equal(2, r.Stato.Giocatori[0].Mano.Count);
            Assert.Equal(3, r.Stato.Giocatori[0].Mazzo.Count);
            Assert.Equal(0, r.Stato.TurnoDi);
            Assert.Equal(1, r.Stato.NumeroTurno);
            Assert.Equal(Fase.Untap, r.Stato.Fase);
            Assert.Equal(30, r.Stato.Giocatori[0].Hp);
            var ev = Assert.IsType<PartitaIniziata>(r.Eventi[0]);
            Assert.Equal(new[] { 0, 1 }, ev.Giocatori);
            Assert.Equal(0, ev.Primo);
        }

        [Fact]
        public void IidTuttiUnivociTraIGiocatori()
        {
            var carte = H.CarteFinte(new[] { "A", "B", "C" });
            var mazzi = new List<IReadOnlyList<string>>
            {
                new[] { "A", "B", "C" },
                new[] { "A", "B", "C" },
            };
            var r = Setup.IniziaPartita(new IniziaPartita(7, Config, mazzi, carte));
            var tutte = r.Stato.Giocatori.SelectMany(g => g.Mazzo.Concat(g.Mano)).ToList();
            var iids = new HashSet<string>(tutte.Select(c => c.Iid));
            Assert.Equal(tutte.Count, iids.Count);
        }

        [Fact]
        public void DeterministicoPerSeed()
        {
            var carte = H.CarteFinte(new[] { "A", "B", "C", "D", "E" });
            var mazzi = new List<IReadOnlyList<string>>
            {
                new[] { "A", "B", "C", "D", "E" },
                new[] { "A", "B", "C", "D", "E" },
            };
            var r1 = Setup.IniziaPartita(new IniziaPartita(99, Config, mazzi, carte));
            var r2 = Setup.IniziaPartita(new IniziaPartita(99, Config, mazzi, carte));
            Assert.Equal(
                r1.Stato.Giocatori[0].Mano.Select(c => c.DefId).ToList(),
                r2.Stato.Giocatori[0].Mano.Select(c => c.DefId).ToList());
        }
    }
}
