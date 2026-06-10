using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class ManaTests
    {
        [Fact]
        public void ParsaCostoMonocolore()
        {
            var c = ManaCosto.Parse("1 Centro");
            Assert.Equal(new ManaCosto { Centro = 1 }, c);
        }

        [Fact]
        public void ParsaCostoColoratoConGenerico()
        {
            var c = ManaCosto.Parse("2 Est + 1");
            Assert.Equal(new ManaCosto { Est = 2, Generico = 1 }, c);
        }

        [Fact]
        public void ParsaGenericoTestuale()
        {
            // "1 Centro più 1 mana qualsiasi" => 1 centro + 1 generico
            var c = ManaCosto.Parse("1 Centro più 1 mana qualsiasi");
            Assert.Equal(new ManaCosto { Centro = 1, Generico = 1 }, c);
        }

        [Fact]
        public void CostoTotaleSommaTutto()
        {
            Assert.Equal(3, new ManaCosto { Est = 2, Generico = 1 }.Totale);
        }

        [Fact]
        public void PagaColoratoEsatto()
        {
            var pool = new ManaPool { Est = 2, Centro = 1 };
            var ok = Mana.Paga(pool, new ManaCosto { Est = 2 });
            Assert.NotNull(ok);
            Assert.Equal(0, ok!.Est);
            Assert.Equal(1, ok.Centro);
        }

        [Fact]
        public void PagaGenericoDaQualsiasiColore()
        {
            var pool = new ManaPool { Est = 2, Centro = 1 };
            // costo: 1 Est + 1 generico => generico pagato dal Centro (o dall'Est avanzato)
            var ok = Mana.Paga(pool, new ManaCosto { Est = 1, Generico = 1 });
            Assert.NotNull(ok);
            Assert.Equal(1, ok!.Est + ok.Centro); // restano 1 mana totale (3 spesi 2)
        }

        [Fact]
        public void NonPagaSeColoreInsufficiente()
        {
            var pool = new ManaPool { Est = 1 };
            Assert.Null(Mana.Paga(pool, new ManaCosto { Est = 2 }));
        }

        [Fact]
        public void NonPagaSeGenericoInsufficiente()
        {
            var pool = new ManaPool { Est = 1 };
            Assert.Null(Mana.Paga(pool, new ManaCosto { Est = 1, Generico = 1 }));
        }
    }
}
