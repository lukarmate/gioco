using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Engine.Core
{
    // Mana prodotto da un Avamposto. Scelta=true => il giocatore sceglie il colore tra Colori
    // (terra duale); Scelta=false => produce Quantita mana di ogni colore in Colori.
    public sealed record ManaProdotto(int Quantita, IReadOnlyList<string> Colori, bool Scelta);

    // Costo in mana di una carta. Colori espliciti + generico (pagabile con qualsiasi colore).
    public sealed record ManaCosto
    {
        public int Nord { get; init; }
        public int Sud { get; init; }
        public int Est { get; init; }
        public int Ovest { get; init; }
        public int Centro { get; init; }
        public int Generico { get; init; }

        public int Totale => Nord + Sud + Est + Ovest + Centro + Generico;

        private static readonly Regex RegexColore =
            new Regex(@"(\d+)\s*(Nord|Sud|Est|Ovest|Centro)", RegexOptions.IgnoreCase);
        private static readonly Regex RegexNumero = new Regex(@"\d+");

        // Parsa i costi prodotti dal parser carte: "1 Centro", "2 Est + 1",
        // "1 Centro più 1 mana qualsiasi". I numeri non legati a un colore = generico.
        public static ManaCosto Parse(string testo)
        {
            int nord = 0, sud = 0, est = 0, ovest = 0, centro = 0;
            string resto = testo;

            foreach (Match m in RegexColore.Matches(testo))
            {
                int q = int.Parse(m.Groups[1].Value);
                switch (m.Groups[2].Value.ToLowerInvariant())
                {
                    case "nord": nord += q; break;
                    case "sud": sud += q; break;
                    case "est": est += q; break;
                    case "ovest": ovest += q; break;
                    case "centro": centro += q; break;
                }
                resto = resto.Replace(m.Value, " ");
            }

            int generico = 0;
            foreach (Match m in RegexNumero.Matches(resto))
                generico += int.Parse(m.Value);

            return new ManaCosto
            {
                Nord = nord, Sud = sud, Est = est, Ovest = ovest, Centro = centro, Generico = generico,
            };
        }
    }

    public static class Mana
    {
        // Tenta di pagare un costo da un pool. Ritorna il pool aggiornato, o null se insufficiente.
        // Colorato: speso dal colore esatto. Generico: speso da qualsiasi colore residuo.
        public static ManaPool? Paga(ManaPool pool, ManaCosto costo)
        {
            int nord = pool.Nord, sud = pool.Sud, est = pool.Est,
                ovest = pool.Ovest, centro = pool.Centro, generico = pool.Generico;

            if (nord < costo.Nord || sud < costo.Sud || est < costo.Est ||
                ovest < costo.Ovest || centro < costo.Centro)
                return null;

            nord -= costo.Nord; sud -= costo.Sud; est -= costo.Est;
            ovest -= costo.Ovest; centro -= costo.Centro;

            int restanti = nord + sud + est + ovest + centro + generico;
            if (restanti < costo.Generico) return null;

            int daPagare = costo.Generico;
            // Ordine deterministico: prima il pool generico, poi i colori.
            void Scala(ref int canale)
            {
                if (daPagare <= 0) return;
                int spesa = canale < daPagare ? canale : daPagare;
                canale -= spesa;
                daPagare -= spesa;
            }
            Scala(ref generico);
            Scala(ref nord); Scala(ref sud); Scala(ref est); Scala(ref ovest); Scala(ref centro);

            return new ManaPool
            {
                Nord = nord, Sud = sud, Est = est, Ovest = ovest, Centro = centro, Generico = generico,
            };
        }
    }
}
