using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // 🟡 assegnazione leader/obiettivi a inizio partita + verbo cura.
    public class SetupAssegnazioneTests
    {
        private static readonly string[] Ids = { "A", "B", "C", "D", "E" };

        [Fact]
        public void IniziaPartita_assegnaLeaderEObiettivi()
        {
            var carte = new Dictionary<string, DefCarta>(H.CarteFinte(Ids))
            {
                ["RE"] = new DefCarta("RE", "Leader", Atk: 3, Def: 3),
            };
            var mazzi = new List<IReadOnlyList<string>> { Ids.ToList(), Ids.ToList() };
            var azione = new IniziaPartita(1, H.Config(), mazzi, carte,
                Leader: new string?[] { "RE", null },
                Obiettivi: new string?[] { "OB-05", "OB-14" });

            var r = GameEngine.Applica(null, azione);

            Assert.True(r.Ok, r.Errore);
            Assert.Equal("RE", r.Stato!.Giocatori[0].Leader!.DefId);
            Assert.Null(r.Stato.Giocatori[1].Leader);
            Assert.Equal("OB-05", r.Stato.Giocatori[0].ObiettivoId);
            Assert.Equal("OB-14", r.Stato.Giocatori[1].ObiettivoId);
        }

        [Fact]
        public void Cura_rimuoveDanno()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["GUARITORE"] = new DefCarta("GUARITORE", "Creatura", Atk: 1, Def: 1,
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[]
                        {
                            new Cura(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), 2),
                        }),
                    }),
                ["FERITO"] = new DefCarta("FERITO", "Creatura", Atk: 2, Def: 5),
            };
            var s = E2.Avvia(carte);
            string ferito;
            (s, ferito) = E2.MettiInCampo(s, 0, "FERITO");
            // infligge 3 danni al FERITO a mano (stato diretto)
            s = H.ConGiocatore(s, 0, g => g with
            {
                Campo = g.Campo.Select(c => c.Iid == ferito ? c with { Danno = 3 } : c).ToList(),
            });
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "GUARITORE");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid)); // etb: cura 2 alle proprie creature

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(1, r.Stato!.Giocatori[0].Campo.First(c => c.Iid == ferito).Danno); // 3 - 2
        }
    }
}
