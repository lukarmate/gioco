using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class ReplayTests
    {
        private static readonly ConfigPartita Config = H.Config();
        private static readonly string[] Ids = { "A", "B", "C", "D", "E", "F", "G", "H" };

        private static StatoPartita Gioca(int seed)
        {
            var carte = H.CarteFinte(Ids);
            var mazzi = new List<IReadOnlyList<string>> { Ids.ToList(), Ids.ToList() };
            var azioni = new List<Azione>
            {
                new IniziaPartita(seed, Config, mazzi, carte),
                new AvanzaFase(), new AvanzaFase(), new AvanzaFase(),
                new AvanzaFase(), new AvanzaFase(), new AvanzaFase(), new AvanzaFase(),
            };
            StatoPartita? stato = null;
            foreach (var a in azioni)
            {
                var r = Engine.Core.Engine.Applica(stato, a);
                Assert.True(r.Ok, r.Errore);
                stato = r.Stato;
            }
            return stato!;
        }

        [Fact]
        public void StessoSeedStesseAzioniStatoFinaleIdentico()
        {
            var a = Gioca(2024);
            var b = Gioca(2024);
            Assert.Equal(H.Json(a), H.Json(b));
        }

        [Fact]
        public void SeedDiversiManiInizialiDiverse()
        {
            var a = Gioca(1);
            var b = Gioca(2);
            Assert.NotEqual(H.Json(a.Giocatori[0].Mazzo), H.Json(b.Giocatori[0].Mazzo));
        }
    }
}
