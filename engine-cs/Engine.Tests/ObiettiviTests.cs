using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1: obiettivi segreti — framework + obiettivi snapshot. Win-check parallelo agli HP.
    public class ObiettiviTests
    {
        private static DefCarta Cr(string id) => new DefCarta(id, "Creatura", Atk: 2, Def: 2);

        private static StatoPartita Avvia(string defId = "SOLD")
            => E2.Avvia(new Dictionary<string, DefCarta> { [defId] = Cr(defId) });

        private static StatoPartita ConObiettivo(StatoPartita s, int g, string id)
            => H.ConGiocatore(s, g, gg => gg with { ObiettivoId = id });

        [Fact]
        public void OB05_quattroCreature_completaEVince()
        {
            var s = ConObiettivo(Avvia(), 0, "OB-05");
            for (int i = 0; i < 4; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());

            var (s2, ev) = Obiettivi.AggiornaEControlla(s);

            Assert.True(s2.Finita);
            Assert.Equal(0, s2.Vincitore);
            Assert.Contains(ev, e => e is ObiettivoCompletato o && o.Giocatore == 0);
            Assert.Contains(ev, e => e is PartitaFinita);
        }

        [Fact]
        public void OB05_treCreature_nonCompleta()
        {
            var s = ConObiettivo(Avvia(), 0, "OB-05");
            for (int i = 0; i < 3; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());

            var (s2, _) = Obiettivi.AggiornaEControlla(s);

            Assert.False(s2.Finita);
            Assert.False(s2.Giocatori[0].ObiettivoCompletato);
        }

        [Fact]
        public void OB14_ottoCimitero_vince()
        {
            var s = ConObiettivo(Avvia(), 1, "OB-14");
            var morte = Enumerable.Range(0, 8)
                .Select(i => new CartaIstanza { Iid = "m" + i, DefId = "SOLD", Proprietario = 1 })
                .ToList();
            s = H.ConGiocatore(s, 1, g => g with { Cimitero = morte });

            var (s2, ev) = Obiettivi.AggiornaEControlla(s);

            Assert.True(s2.Finita);
            Assert.Equal(1, s2.Vincitore);
            Assert.Contains(ev, e => e is ObiettivoCompletato o && o.Giocatore == 1);
        }

        [Fact]
        public void OB02_avversarioSottoSoglia_vince()
        {
            var s = ConObiettivo(Avvia(), 0, "OB-02");
            s = H.ConGiocatore(s, 1, g => g with { Hp = 12 }); // avversario a 12

            var (s2, _) = Obiettivi.AggiornaEControlla(s);

            Assert.True(s2.Finita);
            Assert.Equal(0, s2.Vincitore);
        }

        [Fact]
        public void Telegrafo_aggiornaProgresso()
        {
            var s = ConObiettivo(Avvia(), 0, "OB-05"); // target 4 creature
            (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: "a");
            (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: "b"); // 2/4 = 0.5 -> Vicino

            var (s2, _) = Obiettivi.AggiornaEControlla(s);

            Assert.False(s2.Finita);
            Assert.Equal(ProgressoObiettivo.Vicino, s2.Giocatori[0].ObiettivoProgresso);
        }

        [Fact]
        public void Hook_completaGiocandoLaQuartaCreatura()
        {
            var s = ConObiettivo(Avvia(), 0, "OB-05");
            for (int i = 0; i < 3; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "SOLD");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid)); // 4ª creatura -> obiettivo

            Assert.True(r.Ok, r.Errore);
            Assert.True(r.Stato!.Finita);
            Assert.Equal(0, r.Stato.Vincitore);
            Assert.Contains(r.Eventi, e => e is ObiettivoCompletato);
        }

        [Fact]
        public void SenzaObiettivo_nessunEffetto()
        {
            var s = Avvia();
            for (int i = 0; i < 6; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());

            var (s2, ev) = Obiettivi.AggiornaEControlla(s);

            Assert.False(s2.Finita);
            Assert.Empty(ev);
        }
    }
}
