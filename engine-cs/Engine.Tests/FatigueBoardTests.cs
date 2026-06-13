using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // v1: fatigue (danno crescente a mazzo vuoto) + cap board (6 slot creatura).
    public class FatigueBoardTests
    {
        private static DefCarta Cr(string id) => new DefCarta(id, "Creatura", Atk: 2, Def: 2);

        private static readonly string[] Ids = { "A", "B", "C", "D", "E", "F", "G", "H" };

        private static StatoPartita AvviaFatigue()
        {
            var config = H.Config() with { PenalitaMazzoVuoto = PenalitaMazzoVuoto.Fatigue };
            var mazzi = new List<IReadOnlyList<string>> { Ids.ToList(), Ids.ToList() };
            // gioca il giocatore 1, turno 2 (così pesca, niente skip del primo)
            var s = H.Avvia(5, config, mazzi, H.CarteFinte(Ids)) with { TurnoDi = 1, NumeroTurno = 2 };
            return H.ConGiocatore(s, 1, g => g with { Mazzo = new List<CartaIstanza>() }); // mazzo vuoto
        }

        [Fact]
        public void Fatigue_dannoCrescente()
        {
            var s = AvviaFatigue();
            int hp0 = s.Giocatori[1].Hp;

            var r1 = Fasi.Pesca(s);
            Assert.Equal(hp0 - 1, r1.Stato.Giocatori[1].Hp);   // prima volta: -1
            Assert.Equal(1, r1.Stato.Giocatori[1].Fatigue);
            Assert.Contains(r1.Eventi, e => e is FatigueSubita);

            var r2 = Fasi.Pesca(r1.Stato);
            Assert.Equal(hp0 - 1 - 2, r2.Stato.Giocatori[1].Hp); // seconda: -2
            Assert.Equal(2, r2.Stato.Giocatori[1].Fatigue);
        }

        [Fact]
        public void Fatigue_letaleFiniscePartita()
        {
            var s = AvviaFatigue();
            s = H.ConGiocatore(s, 1, g => g with { Hp = 1 });

            var r = Fasi.Pesca(s); // -1 -> HP 0

            Assert.True(r.Stato.Finita);
            Assert.Contains(r.Eventi, e => e is PartitaFinita);
        }

        [Fact]
        public void CapBoard_nonGiochiLa7aCreatura()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = Cr("SOLD") };
            var s = E2.Avvia(carte);
            for (int i = 0; i < 6; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "SOLD");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.False(r.Ok); // campo pieno (6)
        }

        [Fact]
        public void CapBoard_la6aSiGioca()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLD"] = Cr("SOLD") };
            var s = E2.Avvia(carte);
            for (int i = 0; i < 5; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "SOLD");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(6, r.Stato!.Giocatori[0].Campo.Count(c => c.DefId == "SOLD"));
        }

        [Fact]
        public void CapBoard_tokenInEccessoFizzla()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["SOLD"] = Cr("SOLD"),
                ["NECRO"] = new DefCarta("NECRO", "Creatura", Atk: 1, Def: 1,
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[] { new GeneraToken("Spettro", 1, 1) }),
                    }),
            };
            var s = E2.Avvia(carte);
            for (int i = 0; i < 5; i++) (s, _) = E2.MettiInCampo(s, 0, "SOLD", iidSuffix: i.ToString());
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "NECRO");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            // NECRO entra (6° slot), poi l'etb genererebbe un token -> board pieno -> fizzla.
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(6, r.Stato!.Giocatori[0].Campo.Count);
            Assert.DoesNotContain(r.Stato.Giocatori[0].Campo, c => c.DefId == "TOKEN_Spettro");
            Assert.Empty(r.Eventi.OfType<TokenGenerato>());
        }
    }
}
