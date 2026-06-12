using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class FasiTests
    {
        private static readonly ConfigPartita Config = H.Config();
        private static IReadOnlyDictionary<string, DefCarta> Carte
            => H.CarteFinte(new[] { "A", "B", "C", "D", "E" });
        private static IReadOnlyList<IReadOnlyList<string>> Mazzi => new List<IReadOnlyList<string>>
        {
            new[] { "A", "B", "C", "D", "E" },
            new[] { "A", "B", "C", "D", "E" },
        };

        private static StatoPartita Stato(int seed = 1)
            => Setup.IniziaPartita(new IniziaPartita(seed, Config, Mazzi, Carte)).Stato;

        [Fact]
        public void UntapStappaLeCarteTappateDelGiocatoreAttivo()
        {
            var stato = Stato();
            stato = H.ConGiocatore(stato, 0, g => g with
            {
                Mazzo = g.Mazzo.Select((c, i) => i == 0 ? c with { Tappata = true } : c).ToList()
            });
            var r = Fasi.Untap(stato);
            Assert.False(r.Stato.Giocatori[0].Mazzo[0].Tappata);
            Assert.Contains(r.Eventi, e => e is CartaStappata);
        }

        [Fact]
        public void InizioTurnoPrimoGiocatoreNonPescaAlTurno1()
        {
            var stato = Stato();
            int manoPrima = stato.Giocatori[0].Mano.Count;
            var r = Fasi.InizioTurno(stato);
            Assert.Equal(manoPrima, r.Stato.Giocatori[0].Mano.Count);
            Assert.DoesNotContain(r.Eventi, e => e is CartaPescata);
        }

        [Fact]
        public void InizioTurnoGiocatoreNonPrimoPesca1()
        {
            var stato = Stato() with { TurnoDi = 1, NumeroTurno = 2 };
            int manoPrima = stato.Giocatori[1].Mano.Count;
            int mazzoPrima = stato.Giocatori[1].Mazzo.Count;
            var r = Fasi.InizioTurno(stato);
            Assert.Equal(manoPrima + 1, r.Stato.Giocatori[1].Mano.Count);
            Assert.Equal(mazzoPrima - 1, r.Stato.Giocatori[1].Mazzo.Count);
            Assert.Contains(r.Eventi, e => e is CartaPescata);
        }

        [Fact]
        public void PescaDaMazzoVuotoPerditaImmediataPartitaFinita()
        {
            var stato = Stato() with { TurnoDi = 1, NumeroTurno = 2 };
            stato = H.ConGiocatore(stato, 1, g => g with { Mazzo = new List<CartaIstanza>() });
            var r = Fasi.Pesca(stato);
            Assert.True(r.Stato.Finita);
            Assert.Contains(r.Eventi, e => e is MazzoVuoto);
            Assert.Contains(r.Eventi, e => e is PartitaFinita);
        }
    }
}
