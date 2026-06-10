using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class EngineTests
    {
        private static readonly ConfigPartita Config = H.Config();
        private static readonly string[] Ids = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

        private static StatoPartita Avvia(int nGiocatori = 2)
        {
            var mazzi = Enumerable.Range(0, nGiocatori)
                .Select(_ => (IReadOnlyList<string>)Ids.ToList())
                .ToList();
            return H.Avvia(5, Config, mazzi, H.CarteFinte(Ids));
        }

        [Fact]
        public void IniziaPartitaValida2A4Mazzi()
        {
            var uno = Engine.Core.Engine.Applica(null,
                new IniziaPartita(1, Config,
                    new List<IReadOnlyList<string>> { Ids.ToList() },
                    H.CarteFinte(Ids)));
            Assert.False(uno.Ok);
        }

        [Fact]
        public void AvanzaFaseCiclaLeFasiNellOrdine()
        {
            var stato = Avvia();
            var fasi = new List<Fase>();
            for (int k = 0; k < 6; k++)
            {
                var r = Engine.Core.Engine.Applica(stato, new AvanzaFase());
                Assert.True(r.Ok);
                stato = r.Stato!;
                fasi.Add(stato.Fase);
            }
            Assert.Equal(
                new[] { Fase.Upkeep, Fase.Pesca, Fase.Main1, Fase.Combat, Fase.Main2, Fase.End },
                fasi);
        }

        [Fact]
        public void DaEndPassaAlProssimoGiocatoreEAzzeraMana()
        {
            var stato = Avvia();
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
            Assert.Equal(Fase.End, stato.Fase);

            var r = Engine.Core.Engine.Applica(stato, new AvanzaFase());
            Assert.True(r.Ok);
            Assert.Equal(1, r.Stato!.TurnoDi);
            Assert.Equal(2, r.Stato.NumeroTurno);
            Assert.Equal(Fase.Untap, r.Stato.Fase);
            Assert.Contains(r.Eventi, e => e is TurnoPassato);
            Assert.Contains(r.Eventi, e => e is ManaAzzerato);
        }

        [Fact]
        public void RotazioneTurniCon3E4Giocatori()
        {
            foreach (int n in new[] { 3, 4 })
            {
                var stato = Avvia(n);
                for (int k = 0; k < 7; k++)
                    stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
                Assert.Equal(1 % n, stato.TurnoDi);
            }
        }

        // Mette mano sopra il limite per il giocatore 0 (mazzo svuotato per evitare deckout).
        private static StatoPartita ManoSovraccarica(StatoPartita stato)
        {
            var extra = stato.Giocatori[0].Mazzo.ToList();
            return H.ConGiocatore(stato, 0, g => g with
            {
                Mano = g.Mano.Concat(extra).ToList(),
                Mazzo = new List<CartaIstanza>(),
            });
        }

        [Fact]
        public void ScartoA7EndConManoTroppoGrandeBloccaFincheNonScarti()
        {
            var stato = ManoSovraccarica(Avvia());
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
            Assert.Equal(Fase.End, stato.Fase);

            var bloccato = Engine.Core.Engine.Applica(stato, new AvanzaFase());
            Assert.False(bloccato.Ok);

            int daScartare = stato.Giocatori[0].Mano.Count - Config.LimiteMano;
            var iids = stato.Giocatori[0].Mano.Take(daScartare).Select(c => c.Iid).ToList();
            var sc = Engine.Core.Engine.Applica(stato, new Scarta(iids));
            Assert.True(sc.Ok);
            Assert.Equal(Config.LimiteMano, sc.Stato!.Giocatori[0].Mano.Count);
        }

        [Fact]
        public void AzioneSuPartitaFinitaOkFalse()
        {
            var stato = Avvia() with { Finita = true };
            var r = Engine.Core.Engine.Applica(stato, new AvanzaFase());
            Assert.False(r.Ok);
        }

        [Fact]
        public void DopoScartoValidoAvanzaFasePassaIlTurno()
        {
            var stato = ManoSovraccarica(Avvia());
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;

            int daScartare = stato.Giocatori[0].Mano.Count - Config.LimiteMano;
            var iids = stato.Giocatori[0].Mano.Take(daScartare).Select(c => c.Iid).ToList();
            var sc = Engine.Core.Engine.Applica(stato, new Scarta(iids));
            Assert.True(sc.Ok);

            var avanti = Engine.Core.Engine.Applica(sc.Stato, new AvanzaFase());
            Assert.True(avanti.Ok);
            Assert.Equal(1, avanti.Stato!.TurnoDi);
            Assert.Equal(Fase.Untap, avanti.Stato.Fase);
        }

        [Fact]
        public void ScartaFuoriDallaFaseEndOkFalse()
        {
            var stato = Avvia(); // fase untap
            var r = Engine.Core.Engine.Applica(stato, new Scarta(new List<string>()));
            Assert.False(r.Ok);
        }

        [Fact]
        public void ScartaQuandoNonRichiestoOkFalse()
        {
            var stato = Avvia();
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
            var r = Engine.Core.Engine.Applica(stato, new Scarta(new List<string>()));
            Assert.False(r.Ok);
        }

        [Fact]
        public void ScartaConNumeroErratoDiIidsOkFalse()
        {
            var stato = ManoSovraccarica(Avvia());
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
            var r = Engine.Core.Engine.Applica(stato, new Scarta(new List<string>())); // 0 quando ne servono >0
            Assert.False(r.Ok);
        }

        [Fact]
        public void ScartaConIidNonInManoOkFalse()
        {
            var stato = ManoSovraccarica(Avvia());
            for (int k = 0; k < 6; k++)
                stato = Engine.Core.Engine.Applica(stato, new AvanzaFase()).Stato!;
            int daScartare = stato.Giocatori[0].Mano.Count - Config.LimiteMano;
            var iids = Enumerable.Repeat("iid-inesistente", daScartare).ToList();
            var r = Engine.Core.Engine.Applica(stato, new Scarta(iids));
            Assert.False(r.Ok);
        }
    }
}
