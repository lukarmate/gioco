using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class CombattimentoTests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["ATK3"] = new DefCarta("ATK3", "Creatura", Atk: 3, Def: 2),
            ["ATK2"] = new DefCarta("ATK2", "Creatura", Atk: 2, Def: 2),
            ["DEF2"] = new DefCarta("DEF2", "Creatura", Atk: 1, Def: 2),
            ["DEF3"] = new DefCarta("DEF3", "Creatura", Atk: 1, Def: 3),
        };

        // p0 attacca con attaccante 'atk' (defId atkDef); p1 ha 'blk' (defId blkDef) come potenziale bloccante.
        private static StatoPartita Setup(string atkDef, string blkDef)
        {
            var s = E2.Avvia(Carte());
            var attaccante = new CartaIstanza { Iid = "atk", DefId = atkDef, Proprietario = 0 };
            var bloccante = new CartaIstanza { Iid = "blk", DefId = blkDef, Proprietario = 1 };
            s = H.ConGiocatore(s, 0, g => g with { Campo = g.Campo.Concat(new[] { attaccante }).ToList() });
            s = H.ConGiocatore(s, 1, g => g with { Campo = g.Campo.Concat(new[] { bloccante }).ToList() });
            s = s with { Fase = Fase.Combat };
            // dichiara l'attacco
            return Engine.Core.Engine.Applica(s, new DichiaraAttacco(new[] { "atk" })).Stato!;
        }

        private static readonly Dictionary<string, string> NessunBlocco = new();

        [Fact]
        public void AttaccanteNonBloccatoDannoAlGiocatore()
        {
            var s = Setup("ATK3", "DEF2");
            int hpPrima = s.Giocatori[1].Hp;
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(NessunBlocco));
            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpPrima - 3, r.Stato!.Giocatori[1].Hp);
            Assert.Contains(r.Eventi, e => e is DannoGiocatore);
            Assert.Null(r.Stato.Combattimento); // combattimento risolto e chiuso
        }

        [Fact]
        public void AttaccanteBatteBloccante()
        {
            var s = Setup("ATK3", "DEF2"); // 3 > 2 => bloccante muore
            int hpPrima = s.Giocatori[1].Hp;
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(new Dictionary<string, string> { ["atk"] = "blk" }));
            Assert.True(r.Ok, r.Errore);
            Assert.DoesNotContain(r.Stato!.Giocatori[1].Campo, c => c.Iid == "blk");
            Assert.Contains(r.Stato.Giocatori[1].Cimitero, c => c.Iid == "blk");
            Assert.Contains(r.Stato.Giocatori[0].Campo, c => c.Iid == "atk"); // attaccante sopravvive
            Assert.Equal(hpPrima, r.Stato.Giocatori[1].Hp); // nessun danno al giocatore
        }

        [Fact]
        public void BloccoPariEntrambiMuoiono()
        {
            var s = Setup("ATK3", "DEF3"); // 3 == 3 => entrambi muoiono
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(new Dictionary<string, string> { ["atk"] = "blk" }));
            Assert.True(r.Ok, r.Errore);
            Assert.Contains(r.Stato!.Giocatori[1].Cimitero, c => c.Iid == "blk");
            Assert.Contains(r.Stato.Giocatori[0].Cimitero, c => c.Iid == "atk");
        }

        [Fact]
        public void BloccanteBatteAttaccante()
        {
            var s = Setup("ATK2", "DEF3"); // 2 < 3 => attaccante muore, bloccante vive
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(new Dictionary<string, string> { ["atk"] = "blk" }));
            Assert.True(r.Ok, r.Errore);
            Assert.Contains(r.Stato!.Giocatori[0].Cimitero, c => c.Iid == "atk");
            Assert.Contains(r.Stato.Giocatori[1].Campo, c => c.Iid == "blk");
        }

        [Fact]
        public void BlocchiSenzaCombattimentoFallisce()
        {
            var s = E2.Avvia(Carte()) with { Fase = Fase.Combat };
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(NessunBlocco));
            Assert.False(r.Ok);
        }

        [Fact]
        public void BloccanteNonInCampoDifensoreFallisce()
        {
            var s = Setup("ATK3", "DEF2");
            var r = Engine.Core.Engine.Applica(s, new DichiaraBlocchi(new Dictionary<string, string> { ["atk"] = "ignoto" }));
            Assert.False(r.Ok);
        }
    }
}
