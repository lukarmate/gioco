using System.Collections.Generic;

namespace Engine.Core
{
    // PRNG mulberry32: deterministico, stato = intero 32 bit. Nessun random/clock.
    // Replica fedele dell'implementazione TS: aritmetica wrappata a 32 bit (unchecked)
    // e shift logico `>>>` per combaciare bit-per-bit col motore TypeScript.
    public static class Rng
    {
        public readonly struct PassoRng
        {
            public double Val { get; }   // 0 <= Val < 1
            public int Stato { get; }
            public PassoRng(double val, int stato) { Val = val; Stato = stato; }
        }

        // Equivalente di Math.imul: moltiplicazione intera troncata a 32 bit.
        private static int Imul(int a, int b) => unchecked(a * b);

        public static PassoRng Prossimo(int stato)
        {
            int a = unchecked(stato + 0x6d2b79f5);
            int t = Imul(a ^ (a >>> 15), 1 | a);
            t = unchecked((t + Imul(t ^ (t >>> 7), 61 | t)) ^ t);
            double val = (uint)(t ^ (t >>> 14)) / 4294967296.0;
            return new PassoRng(val, a);
        }

        // Fisher-Yates seedato. Ritorna nuovo array + stato RNG aggiornato.
        public static (List<T> arr, int stato) Mescola<T>(IReadOnlyList<T> arr, int stato)
        {
            var outList = new List<T>(arr);
            int s = stato;
            for (int i = outList.Count - 1; i > 0; i--)
            {
                PassoRng p = Prossimo(s);
                s = p.Stato;
                int j = (int)(p.Val * (i + 1));
                T tmp = outList[i];
                outList[i] = outList[j];
                outList[j] = tmp;
            }
            return (outList, s);
        }
    }
}
