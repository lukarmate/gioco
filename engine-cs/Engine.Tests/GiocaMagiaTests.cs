using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1: terzo tipo carta — Magia. Gioca dalla mano, esegue effetti one-shot, va al cimitero.
    public class GiocaMagiaTests
    {
        private static DefCarta Folgore() => new DefCarta("FOLGORE", "Magia — Istante",
            Costo: new ManaCosto { Generico = 2 },
            Effetti: new List<Effetto>
            {
                new Effetto(Trigger.Etb, new AzioneEffetto[]
                {
                    new InfliggiDanno(new Bersaglio("giocatore", Proprietario.Avversario), 3),
                }),
            });

        private static (StatoPartita s, string iid) Pronto(int energia = 5)
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FOLGORE"] = Folgore(),
                ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 1, Def: 1),
            };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with { Energia = energia });
            return E2.MettiInMano(s, 0, "FOLGORE");
        }

        [Fact]
        public void GiocaMagia_esegueEffettoEVaAlCimitero()
        {
            var (s, iid) = Pronto(energia: 5);
            int hpOpp = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new GiocaMagia(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpOpp - 3, r.Stato!.Giocatori[1].Hp);
            Assert.DoesNotContain(r.Stato.Giocatori[0].Mano, c => c.Iid == iid);
            Assert.Contains(r.Stato.Giocatori[0].Cimitero, c => c.Iid == iid);
            Assert.Equal(3, r.Stato.Giocatori[0].Energia); // 5 - 2
            Assert.Contains(r.Eventi, e => e is MagiaGiocata);
        }

        [Fact]
        public void GiocaMagia_contaComeCartaGiocata()
        {
            var (s, iid) = Pronto();
            var r = GameEngine.Applica(s, new GiocaMagia(iid));
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(1, r.Stato!.Giocatori[0].CarteGiocateQuestoTurno);
        }

        [Fact]
        public void GiocaMagia_energiaInsufficiente_fallisce()
        {
            var (s, iid) = Pronto(energia: 1);
            var r = GameEngine.Applica(s, new GiocaMagia(iid));
            Assert.False(r.Ok);
        }

        [Fact]
        public void GiocaMagia_buffPersistente_restaDopoIlCimitero()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FORZA"] = new DefCarta("FORZA", "Magia — Sorcery",
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[]
                        {
                            new ApplicaStat(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), 2, 2),
                        }),
                    }),
                ["SOLD"] = new DefCarta("SOLD", "Creatura", Atk: 2, Def: 2),
            };
            var s = E2.Avvia(carte);
            s = H.ConGiocatore(s, 0, g => g with { Energia = 5 });
            string sid;
            (s, sid) = E2.MettiInCampo(s, 0, "SOLD"); // 2/2
            string mid;
            (s, mid) = E2.MettiInMano(s, 0, "FORZA");

            var r = GameEngine.Applica(s, new GiocaMagia(mid));

            Assert.True(r.Ok, r.Errore);
            var c = r.Stato!.Giocatori[0].Campo.First(x => x.Iid == sid);
            Assert.Equal((4, 4), Effetti.StatEffettive(r.Stato, c)); // 2/2 + segnalino 2/2 persistente
            Assert.Contains(r.Stato.Giocatori[0].Cimitero, x => x.Iid == mid); // la magia è al cimitero
        }

        [Fact]
        public void GiocaMagia_suCreatura_fallisce()
        {
            var (s, _) = Pronto();
            string sid;
            (s, sid) = E2.MettiInMano(s, 0, "SOLD");
            var r = GameEngine.Applica(s, new GiocaMagia(sid)); // SOLD è una creatura, non una magia
            Assert.False(r.Ok);
        }
    }
}
