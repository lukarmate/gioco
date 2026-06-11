using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public sealed record Risultato
    {
        public bool Ok { get; init; }
        public StatoPartita? Stato { get; init; }
        public IReadOnlyList<Evento> Eventi { get; init; } = new List<Evento>();
        public string? Errore { get; init; }

        public static Risultato Successo(StatoPartita stato, IReadOnlyList<Evento> eventi)
            => new Risultato { Ok = true, Stato = stato, Eventi = eventi };

        public static Risultato Fallito(string errore)
            => new Risultato { Ok = false, Errore = errore };
    }

    public static class Engine
    {
        // Funzione pura: applica(stato, azione) → {ok, stato, eventi[]} | {ok:false, errore}.
        public static Risultato Applica(StatoPartita? stato, Azione azione)
        {
            if (azione is IniziaPartita inizia)
            {
                if (inizia.Mazzi.Count < 2 || inizia.Mazzi.Count > 4)
                    return Risultato.Fallito("servono da 2 a 4 mazzi");
                var r = Setup.IniziaPartita(inizia);
                return Risultato.Successo(r.Stato, r.Eventi);
            }

            if (stato is null) return Risultato.Fallito("partita non iniziata");
            if (stato.Finita) return Risultato.Fallito("partita finita");

            switch (azione)
            {
                case AvanzaFase _:
                    return AvanzaFase(stato);
                case Scarta scarta:
                    return ScartaImpl(stato, scarta.Iids);
                case GiocaAvamposto ga:
                    return GiocaAvampostoImpl(stato, ga.Iid);
                case AttivaAvamposto aa:
                    return AttivaAvampostoImpl(stato, aa.Iid, aa.Scelte);
                case GiocaCreatura gc:
                    return GiocaCreaturaImpl(stato, gc.Iid);
                case DichiaraAttacco da:
                    return DichiaraAttaccoImpl(stato, da.Attaccanti);
                case DichiaraBlocchi db:
                    return DichiaraBlocchiImpl(stato, db.Assegnazioni);
                default:
                    return Risultato.Fallito("azione sconosciuta");
            }
        }

        private static int DaScartare(StatoPartita stato)
        {
            Giocatore g = stato.Giocatori[stato.TurnoDi];
            return Math.Max(0, g.Mano.Count - stato.Config.LimiteMano);
        }

        private static Risultato AvanzaFase(StatoPartita stato)
        {
            if (stato.Fase == Fase.End)
            {
                if (DaScartare(stato) > 0)
                    return Risultato.Fallito("devi scartare prima di passare il turno");
                return FineTurno(stato);
            }

            int idx = Fasi.Ordine.ToList().IndexOf(stato.Fase);
            Fase prossima = Fasi.Ordine[idx + 1];
            var r = Fasi.EseguiEntrataFase(stato with { Fase = prossima }, prossima);
            var eventi = new List<Evento>(r.Eventi);

            if (prossima == Fase.End && DaScartare(r.Stato) > 0)
            {
                eventi.Add(new RichiestaScarto(r.Stato.TurnoDi, DaScartare(r.Stato)));
            }
            return Risultato.Successo(r.Stato, eventi);
        }

        private static Risultato FineTurno(StatoPartita stato)
        {
            int att = stato.TurnoDi;
            int n = stato.Giocatori.Count;
            int prossimo = (att + 1) % n;

            var giocatori = stato.Giocatori
                .Select((g, i) => i == att ? g with { ManaDisponibile = ManaPool.Vuoto() } : g)
                .ToArray();

            var statoPassato = stato with
            {
                Giocatori = giocatori,
                TurnoDi = prossimo,
                NumeroTurno = stato.NumeroTurno + 1,
                Fase = Fase.Untap,
            };

            var eventi = new List<Evento>
            {
                new ManaAzzerato(att),
                new TurnoPassato(att, prossimo),
                new TurnoIniziato(prossimo, statoPassato.NumeroTurno),
            };

            var r = Fasi.EseguiEntrataFase(statoPassato, Fase.Untap);
            eventi.AddRange(r.Eventi);
            return Risultato.Successo(r.Stato, eventi);
        }

        private static bool InMainPhase(Fase f) => f == Fase.Main1 || f == Fase.Main2;

        private static Risultato GiocaAvampostoImpl(StatoPartita stato, string iid)
        {
            if (!InMainPhase(stato.Fase))
                return Risultato.Fallito("gli avamposti si giocano solo nelle Main Phase");

            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            if (g.AvampostoGiocatoQuestoTurno)
                return Risultato.Fallito("hai già giocato un avamposto questo turno");

            CartaIstanza? carta = g.Mano.FirstOrDefault(c => c.Iid == iid);
            if (carta is null) return Risultato.Fallito("carta non in mano");

            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def) || def.Produzione is null)
                return Risultato.Fallito("la carta non è un avamposto");

            var mano = g.Mano.Where(c => c.Iid != iid).ToList();
            var campo = g.Campo.Concat(new[] { carta }).ToList();
            var nuovo = g with { Mano = mano, Campo = campo, AvampostoGiocatoQuestoTurno = true };
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? nuovo : gg).ToArray();
            var eventi = new List<Evento> { new AvampostoGiocato(att, iid) };
            return Risultato.Successo(stato with { Giocatori = giocatori }, eventi);
        }

        private static Risultato GiocaCreaturaImpl(StatoPartita stato, string iid)
        {
            if (!InMainPhase(stato.Fase))
                return Risultato.Fallito("le creature si giocano solo nelle Main Phase");

            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            CartaIstanza? carta = g.Mano.FirstOrDefault(c => c.Iid == iid);
            if (carta is null) return Risultato.Fallito("carta non in mano");

            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def))
                return Risultato.Fallito("definizione carta mancante");
            if (def.Tipo.IndexOf("Creatura", System.StringComparison.OrdinalIgnoreCase) < 0)
                return Risultato.Fallito("la carta non è una creatura");

            ManaCosto costo = def.Costo ?? new ManaCosto();
            ManaPool? pool = Mana.Paga(g.ManaDisponibile, costo);
            if (pool is null) return Risultato.Fallito("mana insufficiente");

            var mano = g.Mano.Where(c => c.Iid != iid).ToList();
            var inCampo = carta with { EntrataQuestoTurno = true };
            var campo = g.Campo.Concat(new[] { inCampo }).ToList();
            var nuovo = g with { Mano = mano, Campo = campo, ManaDisponibile = pool };
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? nuovo : gg).ToArray();
            var nuovoStato = stato with { Giocatori = giocatori };
            var eventi = new List<Evento> { new CreaturaGiocata(att, iid) };

            // E3 — gli effetti ETB (entra-in-campo) scattano subito dopo l'arrivo in campo.
            var etb = Effetti.EseguiTrigger(nuovoStato, def, att, iid, Trigger.Etb);
            nuovoStato = etb.Stato;
            eventi.AddRange(etb.Eventi);

            return Risultato.Successo(nuovoStato, eventi);
        }

        private static bool ECreatura(StatoPartita stato, CartaIstanza c)
            => stato.Carte.TryGetValue(c.DefId, out DefCarta? d) && d.Atk != null;

        private static Risultato DichiaraAttaccoImpl(StatoPartita stato, IReadOnlyList<string> attaccanti)
        {
            if (stato.Fase != Fase.Combat)
                return Risultato.Fallito("si attacca solo nella fase di Combattimento");
            if (attaccanti.Count == 0)
                return Risultato.Fallito("nessun attaccante dichiarato");

            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];

            foreach (string iid in attaccanti)
            {
                CartaIstanza? c = g.Campo.FirstOrDefault(x => x.Iid == iid);
                if (c is null) return Risultato.Fallito($"attaccante non in campo: {iid}");
                if (!ECreatura(stato, c)) return Risultato.Fallito($"non è una creatura: {iid}");
                if (c.Tappata) return Risultato.Fallito($"creatura tappata non può attaccare: {iid}");
                if (c.EntrataQuestoTurno) return Risultato.Fallito($"summoning sickness: {iid}");
            }

            var setAtt = new HashSet<string>(attaccanti);
            var campo = g.Campo.Select(c => setAtt.Contains(c.Iid) ? c with { Tappata = true } : c).ToList();
            var nuovo = g with { Campo = campo };
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? nuovo : gg).ToArray();
            var nuovoStato = stato with { Giocatori = giocatori, Combattimento = new Combattimento(attaccanti) };
            var eventi = attaccanti.Select(iid => (Evento)new CreaturaAttacca(att, iid)).ToList();

            // E3 — gli effetti "attacco" scattano alla dichiarazione, per ogni attaccante.
            foreach (string iid in attaccanti)
            {
                CartaIstanza c = g.Campo.First(x => x.Iid == iid);
                if (!stato.Carte.TryGetValue(c.DefId, out DefCarta? def)) continue;
                var r = Effetti.EseguiTrigger(nuovoStato, def, att, iid, Trigger.Attacco);
                nuovoStato = r.Stato;
                eventi.AddRange(r.Eventi);
            }

            return Risultato.Successo(nuovoStato, eventi);
        }

        private static Risultato DichiaraBlocchiImpl(StatoPartita stato, IReadOnlyDictionary<string, string> assegnazioni)
        {
            if (stato.Combattimento is null)
                return Risultato.Fallito("nessun combattimento in corso");

            int att = stato.TurnoDi;
            int dif = (att + 1) % stato.Giocatori.Count; // 2p: il difensore è l'altro
            Giocatore gAtt = stato.Giocatori[att];
            Giocatore gDif = stato.Giocatori[dif];
            var attaccanti = stato.Combattimento.Attaccanti;

            // Validazione blocchi.
            var bloccantiUsati = new HashSet<string>();
            foreach (var kv in assegnazioni)
            {
                if (!attaccanti.Contains(kv.Key))
                    return Risultato.Fallito($"{kv.Key} non è tra gli attaccanti");
                CartaIstanza? b = gDif.Campo.FirstOrDefault(c => c.Iid == kv.Value);
                if (b is null) return Risultato.Fallito($"bloccante non in campo: {kv.Value}");
                if (!ECreatura(stato, b)) return Risultato.Fallito($"il bloccante non è una creatura: {kv.Value}");
                if (b.Tappata) return Risultato.Fallito($"bloccante tappato: {kv.Value}");
                if (!bloccantiUsati.Add(kv.Value))
                    return Risultato.Fallito($"un bloccante non può bloccare due attaccanti: {kv.Value}");
            }

            var morti = new HashSet<string>();
            var eventi = new List<Evento>();
            int dannoGiocatore = 0;

            int Atk(CartaIstanza c) => stato.Carte[c.DefId].Atk ?? 0;
            int Def(CartaIstanza c) => stato.Carte[c.DefId].Def ?? 0;

            foreach (string aid in attaccanti)
            {
                CartaIstanza? a = gAtt.Campo.FirstOrDefault(c => c.Iid == aid);
                if (a is null) continue; // già rimosso (non dovrebbe)

                if (assegnazioni.TryGetValue(aid, out string? bid))
                {
                    CartaIstanza b = gDif.Campo.First(c => c.Iid == bid);
                    eventi.Add(new CreaturaBlocca(dif, bid, aid));
                    int atk = Atk(a), bdef = Def(b);
                    // Regola 7.3: confronto ATK attaccante vs DEF bloccante.
                    if (atk > bdef) morti.Add(bid);
                    else if (atk == bdef) { morti.Add(bid); morti.Add(aid); }
                    else morti.Add(aid);
                }
                else
                {
                    dannoGiocatore += Atk(a); // attaccante non bloccato
                }
            }

            // Applica morti (sposta in cimitero) + danno al difensore.
            Giocatore Aggiorna(Giocatore g, bool eDifensore)
            {
                var rimaste = new List<CartaIstanza>();
                var nuoveCimitero = new List<CartaIstanza>(g.Cimitero);
                foreach (var c in g.Campo)
                {
                    if (morti.Contains(c.Iid))
                    {
                        nuoveCimitero.Add(c);
                        eventi.Add(new CreaturaDistrutta(c.Iid, c.Proprietario));
                    }
                    else rimaste.Add(c);
                }
                int hp = eDifensore ? g.Hp - dannoGiocatore : g.Hp;
                return g with { Campo = rimaste, Cimitero = nuoveCimitero, Hp = hp };
            }

            var giocatori = stato.Giocatori
                .Select((g, i) => Aggiorna(g, i == dif))
                .ToArray();

            if (dannoGiocatore > 0)
                eventi.Add(new DannoGiocatore(dif, dannoGiocatore));

            return Risultato.Successo(
                stato with { Giocatori = giocatori, Combattimento = null },
                eventi);
        }

        private static ManaPool AggiungiMana(ManaPool p, string colore, int q)
        {
            switch (colore.ToLowerInvariant())
            {
                case "nord": return p with { Nord = p.Nord + q };
                case "sud": return p with { Sud = p.Sud + q };
                case "est": return p with { Est = p.Est + q };
                case "ovest": return p with { Ovest = p.Ovest + q };
                case "centro": return p with { Centro = p.Centro + q };
                default: return p with { Generico = p.Generico + q };
            }
        }

        private static Risultato AttivaAvampostoImpl(StatoPartita stato, string iid, IReadOnlyList<string>? scelte)
        {
            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            CartaIstanza? carta = g.Campo.FirstOrDefault(c => c.Iid == iid);
            if (carta is null) return Risultato.Fallito("avamposto non in campo");
            if (carta.Tappata) return Risultato.Fallito("avamposto già tappato");

            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def) || def.Produzione is null)
                return Risultato.Fallito("la carta non è un avamposto");
            ManaProdotto prod = def.Produzione;

            // Calcola il mana prodotto come dizionario colore -> quantità.
            var prodotto = new Dictionary<string, int>();
            void Somma(string col, int q) => prodotto[col] = prodotto.TryGetValue(col, out int v) ? v + q : q;

            if (prod.Scelta)
            {
                if (scelte is null || scelte.Count != prod.Quantita)
                    return Risultato.Fallito($"devi scegliere esattamente {prod.Quantita} colori");
                if (!scelte.All(c => prod.Colori.Contains(c)))
                    return Risultato.Fallito("colore scelto non prodotto da questo avamposto");
                foreach (string c in scelte) Somma(c, 1);
            }
            else
            {
                foreach (string c in prod.Colori) Somma(c, prod.Quantita);
            }

            ManaPool pool = g.ManaDisponibile;
            foreach (var kv in prodotto) pool = AggiungiMana(pool, kv.Key, kv.Value);

            var campo = g.Campo.Select(c => c.Iid == iid ? c with { Tappata = true } : c).ToList();
            var nuovo = g with { Campo = campo, ManaDisponibile = pool };
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? nuovo : gg).ToArray();
            var eventi = new List<Evento> { new ManaGenerato(att, iid, prodotto) };
            return Risultato.Successo(stato with { Giocatori = giocatori }, eventi);
        }

        private static Risultato ScartaImpl(StatoPartita stato, IReadOnlyList<string> iids)
        {
            if (stato.Fase != Fase.End) return Risultato.Fallito("scarto solo a fine turno");
            int richiesti = DaScartare(stato);
            if (richiesti == 0) return Risultato.Fallito("nessuno scarto richiesto");
            if (iids.Count != richiesti)
                return Risultato.Fallito($"devi scartare esattamente {richiesti} carte");

            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            var inMano = new HashSet<string>(g.Mano.Select(c => c.Iid));
            if (!iids.All(id => inMano.Contains(id)))
                return Risultato.Fallito("iid non in mano");

            var daRimuovere = new HashSet<string>(iids);
            var scartate = g.Mano.Where(c => daRimuovere.Contains(c.Iid)).ToList();
            var mano = g.Mano.Where(c => !daRimuovere.Contains(c.Iid)).ToList();
            var cimitero = g.Cimitero.Concat(scartate).ToList();
            var giocatori = stato.Giocatori
                .Select((gg, i) => i == att ? gg with { Mano = mano, Cimitero = cimitero } : gg)
                .ToArray();
            var eventi = scartate.Select(c => (Evento)new CartaScartata(att, c.Iid)).ToList();
            return Risultato.Successo(stato with { Giocatori = giocatori }, eventi);
        }
    }
}
