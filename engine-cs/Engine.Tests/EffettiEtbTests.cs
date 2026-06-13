using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E3a — interprete effetti, trigger ETB (entra-in-campo) su GiocaCreatura.
    // Verbi deterministici: pesca, genera_mana, infliggi_danno (giocatore),
    // distruggi (creature), mill. Targeting per proprietario relativo al controllore.
    public class EffettiEtbTests
    {
        // Creatura giocabile a costo 0, con gli effetti dati.
        private static DefCarta Creatura(string id, params Effetto[] eff)
            => new DefCarta(id, "Creatura", Atk: 1, Def: 1, Effetti: eff.ToList());

        private static DefCarta Vanilla(string id, int atk = 2, int def = 2)
            => new DefCarta(id, "Creatura", Atk: atk, Def: def);

        // Avvia 2p, porta a Main1, mette in mano al giocatore 0 il defId dato.
        private static (StatoPartita stato, string iid) Pronto(
            IReadOnlyDictionary<string, DefCarta> carte, string defId)
        {
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            return E2.MettiInMano(s, 0, defId);
        }

        [Fact]
        public void Etb_Pesca_controllorePescaN()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["PESCATORE"] = Creatura("PESCATORE",
                    new Effetto(Trigger.Etb, new AzioneEffetto[] { new Pesca(2) })),
            };
            var (s, iid) = Pronto(carte, "PESCATORE");
            int manoPrima = s.Giocatori[0].Mano.Count;
            int mazzoPrima = s.Giocatori[0].Mazzo.Count;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            var g = r.Stato!.Giocatori[0];
            // -1 (la creatura lascia la mano) +2 (pescate)
            Assert.Equal(manoPrima - 1 + 2, g.Mano.Count);
            Assert.Equal(mazzoPrima - 2, g.Mazzo.Count);
            Assert.Equal(2, r.Eventi.OfType<CartaPescata>().Count());
        }

        [Fact]
        public void Etb_GeneraMana_aggiungeAlPoolControllore()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FONTE"] = Creatura("FONTE",
                    new Effetto(Trigger.Etb, new AzioneEffetto[] { new GeneraMana(2, "Centro") })),
            };
            var (s, iid) = Pronto(carte, "FONTE");
            int energiaPrima = s.Giocatori[0].Energia;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            // FONTE costa 0; l'etb genera 2 energia.
            Assert.Equal(energiaPrima + 2, r.Stato!.Giocatori[0].Energia);
            Assert.Single(r.Eventi.OfType<EnergiaGenerata>());
        }

        [Fact]
        public void Etb_InfliggiDanno_avversario_riduceHp()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FOLGORE"] = Creatura("FOLGORE",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new InfliggiDanno(new Bersaglio("giocatore", Proprietario.Avversario), 3),
                    })),
            };
            var (s, iid) = Pronto(carte, "FOLGORE");
            int hpPrima = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpPrima - 3, r.Stato!.Giocatori[1].Hp);
            var d = Assert.Single(r.Eventi.OfType<DannoGiocatore>());
            Assert.Equal(1, d.Giocatore);
            Assert.Equal(3, d.Danno);
        }

        [Fact]
        public void Etb_Distruggi_creatureAvversario_tutte()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["STERMINIO"] = Creatura("STERMINIO",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Distruggi(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte)),
                    })),
                ["GOLEM"] = Vanilla("GOLEM"),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            (s, _) = E2.MettiInCampo(s, 1, "GOLEM", iidSuffix: "1");
            (s, _) = E2.MettiInCampo(s, 1, "GOLEM", iidSuffix: "2");
            var (s3, iid) = E2.MettiInMano(s, 0, "STERMINIO");
            s = s3;
            int campoPrima = s.Giocatori[1].Campo.Count;
            Assert.Equal(2, campoPrima);

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Empty(r.Stato!.Giocatori[1].Campo);
            Assert.Equal(2, r.Stato.Giocatori[1].Cimitero.Count);
            Assert.Equal(2, r.Eventi.OfType<CreaturaDistrutta>().Count());
        }

        [Fact]
        public void Etb_Distruggi_nonColpisceLeProprie()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["STERMINIO"] = Creatura("STERMINIO",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Distruggi(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte)),
                    })),
                ["GOLEM"] = Vanilla("GOLEM"),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            (s, _) = E2.MettiInCampo(s, 0, "GOLEM"); // creatura PROPRIA, non va distrutta
            var (s3, iid) = E2.MettiInMano(s, 0, "STERMINIO");
            s = s3;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            // il GOLEM proprio resta (più la creatura appena giocata) -> campo non vuoto
            Assert.Contains(r.Stato!.Giocatori[0].Campo, c => c.DefId == "GOLEM");
            Assert.Empty(r.Eventi.OfType<CreaturaDistrutta>());
        }

        [Fact]
        public void Etb_Mill_avversario_dalMazzoAlCimitero()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["MACINA"] = Creatura("MACINA",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Mill(new Bersaglio("giocatore", Proprietario.Avversario), 2),
                    })),
            };
            var (s, iid) = Pronto(carte, "MACINA");
            int mazzoPrima = s.Giocatori[1].Mazzo.Count;
            int cimiteroPrima = s.Giocatori[1].Cimitero.Count;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(mazzoPrima - 2, r.Stato!.Giocatori[1].Mazzo.Count);
            Assert.Equal(cimiteroPrima + 2, r.Stato.Giocatori[1].Cimitero.Count);
            Assert.Equal(2, r.Eventi.OfType<CartaMacinata>().Count());
        }

        [Fact]
        public void Etb_PiuAzioni_eseguiteInOrdine()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["COMBO"] = Creatura("COMBO",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Pesca(1),
                        new GeneraMana(1, "Est"),
                        new InfliggiDanno(new Bersaglio("giocatore", Proprietario.Avversario), 1),
                    })),
            };
            var (s, iid) = Pronto(carte, "COMBO");
            int hpPrima = s.Giocatori[1].Hp;
            int energiaPrima = s.Giocatori[0].Energia;

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Single(r.Eventi.OfType<CartaPescata>());
            Assert.Equal(energiaPrima + 1, r.Stato!.Giocatori[0].Energia);
            Assert.Equal(hpPrima - 1, r.Stato.Giocatori[1].Hp);
        }

        [Fact]
        public void SenzaEffetti_nessunEventoExtra()
        {
            var carte = new Dictionary<string, DefCarta> { ["MUTO"] = Vanilla("MUTO") };
            var (s, iid) = Pronto(carte, "MUTO");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            // solo l'evento CreaturaGiocata, nessun effetto
            Assert.Single(r.Eventi);
            Assert.IsType<CreaturaGiocata>(r.Eventi[0]);
        }
    }
}
