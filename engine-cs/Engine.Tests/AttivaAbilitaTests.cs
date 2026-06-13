using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E3c.2b — AttivaAbilita: attiva gli effetti "attivata" di un permanente proprio,
    // tappandolo (1 uso/turno).
    public class AttivaAbilitaTests
    {
        private static DefCarta ConAttivata(string id, params AzioneEffetto[] az)
            => new DefCarta(id, "Artefatto",
                Effetti: new List<Effetto> { new Effetto(Trigger.Attivata, az.ToList()) });

        [Fact]
        public void Attiva_eseguiEffetti_eTappa()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FONTE"] = ConAttivata("FONTE", new GeneraMana(1, "Centro")),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInCampo(s, 0, "FONTE");

            int energiaPrima = s.Giocatori[0].Energia;
            var r = GameEngine.Applica(s, new AttivaAbilita(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(energiaPrima + 1, r.Stato!.Giocatori[0].Energia);
            CartaIstanza carta = r.Stato.Giocatori[0].Campo.First(c => c.Iid == iid);
            Assert.True(carta.Tappata);
            Assert.Single(r.Eventi.OfType<EnergiaGenerata>());
        }

        [Fact]
        public void Attiva_seTappata_fallisce()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FONTE"] = ConAttivata("FONTE", new GeneraMana(1, "Centro")),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInCampo(s, 0, "FONTE", tappata: true);

            var r = GameEngine.Applica(s, new AttivaAbilita(iid));

            Assert.False(r.Ok);
        }

        [Fact]
        public void Attiva_senzaAbilitaAttivata_fallisce()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["MURO"] = new DefCarta("MURO", "Creatura", Atk: 0, Def: 5),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInCampo(s, 0, "MURO");

            var r = GameEngine.Applica(s, new AttivaAbilita(iid));

            Assert.False(r.Ok);
        }

        [Fact]
        public void Attiva_cartaNonInCampo_fallisce()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["FONTE"] = ConAttivata("FONTE", new GeneraMana(1, "Centro")),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);

            var r = GameEngine.Applica(s, new AttivaAbilita("inesistente"));

            Assert.False(r.Ok);
        }
    }
}
