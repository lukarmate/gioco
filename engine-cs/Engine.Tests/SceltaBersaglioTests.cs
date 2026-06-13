using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // 🟡 verbi con scelta (quantificatore "una"): la Magia porta il bersaglio scelto dal giocatore.
    public class SceltaBersaglioTests
    {
        private static DefCarta Saetta() => new DefCarta("SAETTA", "Magia — Sorcery",
            Costo: new ManaCosto { Generico = 1 },
            Effetti: new List<Effetto>
            {
                new Effetto(Trigger.Etb, new AzioneEffetto[]
                {
                    new InfliggiDanno(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Una), 2),
                }),
            });

        private static (StatoPartita s, string saetta, string a, string b) Setup()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["SAETTA"] = Saetta(),
                ["TARGET"] = new DefCarta("TARGET", "Creatura", Atk: 1, Def: 3),
            };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with { Energia = 3 });
            string a, b;
            (s, a) = E2.MettiInCampo(s, 1, "TARGET", iidSuffix: "a"); // due creature avversarie
            (s, b) = E2.MettiInCampo(s, 1, "TARGET", iidSuffix: "b");
            string saetta;
            (s, saetta) = E2.MettiInMano(s, 0, "SAETTA");
            return (s, saetta, a, b);
        }

        [Fact]
        public void Una_colpisceSoloIlBersaglioScelto()
        {
            var (s, saetta, a, b) = Setup();

            var r = GameEngine.Applica(s, new GiocaMagia(saetta, a)); // sceglie la creatura "a"

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(2, r.Stato!.Giocatori[1].Campo.First(c => c.Iid == a).Danno);
            Assert.Equal(0, r.Stato.Giocatori[1].Campo.First(c => c.Iid == b).Danno); // l'altra intatta
        }

        [Fact]
        public void Una_senzaScelta_nonColpisceNiente()
        {
            var (s, saetta, a, b) = Setup();

            var r = GameEngine.Applica(s, new GiocaMagia(saetta)); // nessun bersaglio

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(0, r.Stato!.Giocatori[1].Campo.First(c => c.Iid == a).Danno);
            Assert.Equal(0, r.Stato.Giocatori[1].Campo.First(c => c.Iid == b).Danno);
        }

        [Fact]
        public void Una_bersaglioNonValido_ignorato()
        {
            var (s, saetta, _, _) = Setup();
            // sceglie una creatura PROPRIA: il bersaglio è "avversario" -> non valido -> ignorato
            string mia;
            (s, mia) = E2.MettiInCampo(s, 0, "TARGET", iidSuffix: "mia");

            var r = GameEngine.Applica(s, new GiocaMagia(saetta, mia));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(0, r.Stato!.Giocatori[0].Campo.First(c => c.Iid == mia).Danno);
        }
    }
}
