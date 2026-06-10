using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public static class Setup
    {
        public sealed record RisultatoSetup(StatoPartita Stato, IReadOnlyList<Evento> Eventi);

        public static RisultatoSetup IniziaPartita(IniziaPartita args)
        {
            int rng = args.Seed; // | 0 dei TS: già int 32 bit
            int contatore = 0;
            var giocatori = new List<Giocatore>();

            for (int id = 0; id < args.Mazzi.Count; id++)
            {
                var istanze = args.Mazzi[id]
                    .Select(defId => new CartaIstanza
                    {
                        Iid = "c" + contatore++,
                        DefId = defId,
                        Proprietario = id,
                        Tappata = false,
                    })
                    .ToList();

                var (mescolate, nuovoStato) = Rng.Mescola(istanze, rng);
                rng = nuovoStato;

                var mano = mescolate.Take(args.Config.ManoIniziale).ToList();
                var mazzo = mescolate.Skip(args.Config.ManoIniziale).ToList();

                giocatori.Add(new Giocatore
                {
                    Id = id,
                    Hp = args.Config.HpIniziali,
                    Mazzo = mazzo,
                    Mano = mano,
                    Campo = new List<CartaIstanza>(),
                    Cimitero = new List<CartaIstanza>(),
                    Esilio = new List<CartaIstanza>(),
                    ManaDisponibile = ManaPool.Vuoto(),
                    Morti = 0,
                });
            }

            var stato = new StatoPartita
            {
                Config = args.Config,
                Rng = rng,
                Giocatori = giocatori,
                TurnoDi = 0,
                NumeroTurno = 1,
                Fase = Fase.Untap,
                PrimoGiocatore = 0,
                Finita = false,
                Carte = args.Carte,
            };

            var eventi = new List<Evento>
            {
                new PartitaIniziata(giocatori.Select(g => g.Id).ToList(), 0),
                new TurnoIniziato(0, 1),
                new FaseEntrata(Fase.Untap),
            };

            return new RisultatoSetup(stato, eventi);
        }
    }
}
