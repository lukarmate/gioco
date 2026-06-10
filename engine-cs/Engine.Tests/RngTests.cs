using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class RngTests
    {
        [Fact]
        public void StessoSeedStessaSequenza()
        {
            var a = Rng.Prossimo(123);
            var b = Rng.Prossimo(123);
            Assert.Equal(a.Val, b.Val);
            Assert.Equal(a.Stato, b.Stato);
        }

        [Fact]
        public void AvanzaLoStato()
        {
            var a = Rng.Prossimo(123);
            var b = Rng.Prossimo(a.Stato);
            Assert.NotEqual(a.Val, b.Val);
            Assert.True(a.Val >= 0);
            Assert.True(a.Val < 1);
        }

        [Fact]
        public void MescolaDeterministicoEPermutazioneValida()
        {
            var arr = new List<int> { 1, 2, 3, 4, 5 };
            var r1 = Rng.Mescola(arr, 42);
            var r2 = Rng.Mescola(arr, 42);
            Assert.Equal(r1.arr, r2.arr);
            Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, r1.arr.OrderBy(x => x).ToList());
            Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, arr); // input non mutato
        }
    }
}
