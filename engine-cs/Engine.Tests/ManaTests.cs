using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    // v1: il pool colorato (Mana.Paga) è stato rimosso (energia automatica). Restano i test
    // del parsing del costo, perché il costo = ManaCosto.Totale (energia richiesta).
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
    }
}
