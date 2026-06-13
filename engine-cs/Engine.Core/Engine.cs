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
                var rs = Setup.IniziaPartita(inizia);
                return Risultato.Successo(rs.Stato, rs.Eventi);
            }

            if (stato is null) return Risultato.Fallito("partita non iniziata");
            if (stato.Finita) return Risultato.Fallito("partita finita");

            Risultato r = Dispatch(stato, azione);
            if (!r.Ok || r.Stato is null) return r;

            // Dopo ogni azione: valuta gli obiettivi segreti (vittoria parallela agli HP).
            var (s2, evObj) = Obiettivi.AggiornaEControlla(r.Stato);
            if (evObj.Count == 0) return r;
            return Risultato.Successo(s2, r.Eventi.Concat(evObj).ToList());
        }

        private static Risultato Dispatch(StatoPartita stato, Azione azione)
        {
            switch (azione)
            {
                case PassaTurno _:
                    return PassaTurnoImpl(stato);
                case Scarta scarta:
                    return ScartaImpl(stato, scarta.Iids);
                case GiocaCreatura gc:
                    return GiocaCreaturaImpl(stato, gc.Iid);
                case AttivaAbilita ab:
                    return AttivaAbilitaImpl(stato, ab.Iid);
                case Attacca atk:
                    return AttaccaImpl(stato, atk.Attaccante, atk.Bersaglio);
                default:
                    return Risultato.Fallito("azione sconosciuta");
            }
        }

        private static int DaScartare(StatoPartita stato)
        {
            Giocatore g = stato.Giocatori[stato.TurnoDi];
            return Math.Max(0, g.Mano.Count - stato.Config.LimiteMano);
        }

        // v1: passa il turno. End step (scarto a 7) + begin step automatico del prossimo giocatore.
        private static Risultato PassaTurnoImpl(StatoPartita stato)
        {
            if (DaScartare(stato) > 0)
                return Risultato.Fallito("devi scartare fino al limite di mano prima di passare il turno");

            int att = stato.TurnoDi;
            int n = stato.Giocatori.Count;
            int prossimo = (att + 1) % n;

            var statoPassato = stato with
            {
                TurnoDi = prossimo,
                NumeroTurno = stato.NumeroTurno + 1,
                Fase = Fase.Azioni,
            };

            var eventi = new List<Evento>
            {
                new TurnoPassato(att, prossimo),
                new TurnoIniziato(prossimo, statoPassato.NumeroTurno),
            };

            var r = Fasi.InizioTurno(statoPassato);
            eventi.AddRange(r.Eventi);
            return Risultato.Successo(r.Stato, eventi);
        }

        private static Risultato GiocaCreaturaImpl(StatoPartita stato, string iid)
        {
            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            CartaIstanza? carta = g.Mano.FirstOrDefault(c => c.Iid == iid);
            if (carta is null) return Risultato.Fallito("carta non in mano");

            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def))
                return Risultato.Fallito("definizione carta mancante");
            if (def.Tipo.IndexOf("Creatura", System.StringComparison.OrdinalIgnoreCase) < 0)
                return Risultato.Fallito("la carta non è una creatura");

            int cap = stato.Config.CapCampo > 0 ? stato.Config.CapCampo : 6;
            if (g.Campo.Count(c => ECreatura(stato, c)) >= cap)
                return Risultato.Fallito("campo pieno");

            int costo = def.Costo?.Totale ?? 0;
            if (g.Energia < costo) return Risultato.Fallito("energia insufficiente");

            var mano = g.Mano.Where(c => c.Iid != iid).ToList();
            var inCampo = carta with { EntrataQuestoTurno = true };
            var campo = g.Campo.Concat(new[] { inCampo }).ToList();
            var nuovo = g with
            {
                Mano = mano, Campo = campo, Energia = g.Energia - costo,
                CarteGiocateQuestoTurno = g.CarteGiocateQuestoTurno + 1,
            };
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? nuovo : gg).ToArray();
            var nuovoStato = stato with { Giocatori = giocatori };
            var eventi = new List<Evento> { new CreaturaGiocata(att, iid) };

            // E3 — gli effetti ETB (entra-in-campo) scattano subito dopo l'arrivo in campo.
            var etb = Effetti.EseguiTrigger(nuovoStato, def, att, iid, Trigger.Etb);
            nuovoStato = etb.Stato;
            eventi.AddRange(etb.Eventi);

            return Risultato.Successo(nuovoStato, eventi);
        }

        private static Risultato AttivaAbilitaImpl(StatoPartita stato, string iid)
        {
            int att = stato.TurnoDi;
            Giocatore g = stato.Giocatori[att];
            CartaIstanza? carta = g.Campo.FirstOrDefault(c => c.Iid == iid);
            if (carta is null) return Risultato.Fallito("permanente non in campo");
            if (carta.Tappata) return Risultato.Fallito("permanente già tappato");

            if (!stato.Carte.TryGetValue(carta.DefId, out DefCarta? def))
                return Risultato.Fallito("definizione carta mancante");
            bool haAttivata = def.Effetti?.Any(e => e.Trigger == Trigger.Attivata) ?? false;
            if (!haAttivata) return Risultato.Fallito("la carta non ha un'abilità attivabile");

            // Tappa il permanente, poi esegue gli effetti attivata.
            var campo = g.Campo.Select(c => c.Iid == iid ? c with { Tappata = true } : c).ToList();
            var giocatori = stato.Giocatori.Select((gg, i) => i == att ? gg with { Campo = campo } : gg).ToArray();
            var nuovoStato = stato with { Giocatori = giocatori };

            var r = Effetti.EseguiTrigger(nuovoStato, def, att, iid, Trigger.Attivata);
            return Risultato.Successo(r.Stato, r.Eventi);
        }

        private static bool ECreatura(StatoPartita stato, CartaIstanza c)
            => stato.Carte.TryGetValue(c.DefId, out DefCarta? d) && d.Atk != null;

        private const string KwProvocazione = "provocazione";
        private const string KwVelocita = "velocita";
        private const string KwTravolta = "travolta";

        // Attacco diretto stile Hearthstone. Bersaglio = creatura avversaria (iid) o null = HP avversario.
        private static Risultato AttaccaImpl(StatoPartita stato, string attaccanteIid, string? bersaglioIid)
        {
            int att = stato.TurnoDi;
            int dif = (att + 1) % stato.Giocatori.Count; // 2p
            Giocatore gAtt = stato.Giocatori[att];
            Giocatore gDif = stato.Giocatori[dif];

            CartaIstanza? a = gAtt.Campo.FirstOrDefault(c => c.Iid == attaccanteIid);
            if (a is null) return Risultato.Fallito("attaccante non in campo");
            if (!ECreatura(stato, a)) return Risultato.Fallito("l'attaccante non è una creatura");
            if (a.Tappata) return Risultato.Fallito("creatura tappata non può attaccare");
            ISet<string> kwAtt = Effetti.KeywordEffettive(stato, a);
            if (a.EntrataQuestoTurno && !kwAtt.Contains(KwVelocita))
                return Risultato.Fallito("summoning sickness");

            // Provocazione: se il difensore controlla creature con Provocazione, il bersaglio è obbligato.
            var taunt = gDif.Campo
                .Where(c => ECreatura(stato, c) && Effetti.KeywordEffettive(stato, c).Contains(KwProvocazione))
                .Select(c => c.Iid).ToHashSet();
            if (taunt.Count > 0 && (bersaglioIid is null || !taunt.Contains(bersaglioIid)))
                return Risultato.Fallito("devi attaccare una creatura con Provocazione");

            CartaIstanza? bersaglio = bersaglioIid is null
                ? null
                : gDif.Campo.FirstOrDefault(c => c.Iid == bersaglioIid);
            if (bersaglioIid is not null && bersaglio is null)
                return Risultato.Fallito("bersaglio non in campo");
            if (bersaglio is not null && !ECreatura(stato, bersaglio))
                return Risultato.Fallito("il bersaglio non è una creatura");

            var eventi = new List<Evento> { new CreaturaAttacca(att, attaccanteIid) };

            // Tappa l'attaccante, poi fa scattare il trigger "attacco".
            stato = ConCampo(stato, att, campo =>
                campo.Select(c => c.Iid == attaccanteIid ? c with { Tappata = true } : c).ToList());
            if (stato.Carte.TryGetValue(a.DefId, out DefCarta? defA))
            {
                var rt = Effetti.EseguiTrigger(stato, defA, att, attaccanteIid, Trigger.Attacco);
                stato = rt.Stato;
                eventi.AddRange(rt.Eventi);
            }

            int atkAtt = Effetti.StatEffettive(stato, a).Atk;

            if (bersaglio is null)
            {
                // Attacco agli HP del giocatore avversario.
                stato = InfliggiAGiocatore(stato, dif, atkAtt, att, eventi);
            }
            else
            {
                // Danno reciproco: l'attaccante e il bersaglio si infliggono il proprio ATK.
                int atkBer = Effetti.StatEffettive(stato, bersaglio).Atk;
                int saluteBer = Effetti.StatEffettive(stato, bersaglio).Def - bersaglio.Danno;

                stato = AggiungiDanno(stato, att, attaccanteIid, atkBer, eventi);
                stato = AggiungiDanno(stato, dif, bersaglioIid!, atkAtt, eventi);

                // Travolta: l'eccesso oltre la salute del bersaglio passa agli HP del giocatore.
                if (kwAtt.Contains(KwTravolta) && atkAtt > saluteBer)
                {
                    int eccesso = atkAtt - System.Math.Max(0, saluteBer);
                    stato = InfliggiAGiocatore(stato, dif, eccesso, att, eventi);
                }
            }

            // Morti state-based (Danno >= DEF effettiva) + trigger morte.
            stato = MortiStateBased(stato, eventi);
            return Risultato.Successo(stato, eventi);
        }

        private static StatoPartita ConCampo(
            StatoPartita stato, int idx, System.Func<IReadOnlyList<CartaIstanza>, List<CartaIstanza>> f)
        {
            var giocatori = stato.Giocatori
                .Select((g, i) => i == idx ? g with { Campo = f(g.Campo) } : g).ToArray();
            return stato with { Giocatori = giocatori };
        }

        private static StatoPartita AggiungiDanno(
            StatoPartita stato, int giocatore, string iid, int danno, List<Evento> ev)
        {
            if (danno <= 0) return stato;
            stato = ConCampo(stato, giocatore, campo =>
                campo.Select(c => c.Iid == iid ? c with { Danno = c.Danno + danno } : c).ToList());
            ev.Add(new DannoCreatura(iid, danno));
            return stato;
        }

        private static StatoPartita InfliggiAGiocatore(
            StatoPartita stato, int giocatore, int danno, int attaccante, List<Evento> ev)
        {
            if (danno <= 0) return stato;
            Giocatore g = stato.Giocatori[giocatore];
            int hp = g.Hp - danno;
            // accumulatore obiettivi: danno inflitto all'avversario dall'attaccante, questo turno
            var giocatori = stato.Giocatori.Select((gg, i) =>
                i == giocatore ? gg with { Hp = hp }
                : i == attaccante ? gg with { DanniAvversarioQuestoTurno = gg.DanniAvversarioQuestoTurno + danno }
                : gg).ToArray();
            stato = stato with { Giocatori = giocatori };
            ev.Add(new DannoGiocatore(giocatore, danno));
            if (hp <= 0 && !stato.Finita)
            {
                int? vinc = stato.Giocatori.Count == 2 ? attaccante : (int?)null;
                stato = stato with { Finita = true, Vincitore = vinc };
                ev.Add(new PartitaFinita(vinc, "hp-azzerati"));
            }
            return stato;
        }

        // State-based actions: ogni creatura con Danno >= DEF effettiva muore. Poi scattano i trigger morte.
        private static StatoPartita MortiStateBased(StatoPartita stato, List<Evento> ev)
        {
            var mortiInfo = new List<(string iid, string defId, int prop)>();
            var giocatori = stato.Giocatori.Select(g =>
            {
                var rimaste = new List<CartaIstanza>();
                var cimitero = new List<CartaIstanza>(g.Cimitero);
                foreach (var c in g.Campo)
                {
                    if (ECreatura(stato, c) && c.Danno >= Effetti.StatEffettive(stato, c).Def)
                    {
                        cimitero.Add(c with { Danno = 0 });
                        ev.Add(new CreaturaDistrutta(c.Iid, c.Proprietario));
                        mortiInfo.Add((c.Iid, c.DefId, c.Proprietario));
                    }
                    else rimaste.Add(c);
                }
                return g with { Campo = rimaste, Cimitero = cimitero };
            }).ToArray();
            stato = stato with { Giocatori = giocatori };
            return Effetti.EseguiMorti(stato, mortiInfo, ev);
        }


        private static Risultato ScartaImpl(StatoPartita stato, IReadOnlyList<string> iids)
        {
            // v1: lo scarto serve solo quando la mano supera il limite (in qualunque momento del proprio turno).
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
