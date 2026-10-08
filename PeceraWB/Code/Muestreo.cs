using System;
using System.Collections.Generic;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Observacion del mundo: de lo que ve (parejas, vecinos de ciudad, reinos) deriva eventos
    // afectivos, rumores, juicios, facciones y cultura. SOLO LEE el juego.
    internal static partial class Mundo
    {
        static int cursor;
        static readonly Dictionary<City, List<Actor>> grupos = new Dictionary<City, List<Actor>>();
        static readonly HashSet<string> parejas = new HashSet<string>();
        static readonly Dictionary<string, double> parejasT = new Dictionary<string, double>();
        static readonly Dictionary<string, double> convT = new Dictionary<string, double>();
        static readonly HashSet<string> facConocidas = new HashSet<string>();
        static readonly HashSet<string> tensionesConocidas = new HashSet<string>();
        static readonly Dictionary<string, Faccion> facDe = new Dictionary<string, Faccion>();
        static readonly HashSet<string> muertos = new HashSet<string>();
        static readonly Dictionary<string, string> amigos = new Dictionary<string, string>();
        static string ultimoCredo = "";
        public static List<Faccion> UltimasFacciones = new List<Faccion>();

        static bool Valido(Actor a)
        {
            return a != null && a.isAlive() && a.isSapient() && a.isAdult();
        }

        static void Registra(Actor a)
        {
            string id = Id(a);
            Nombres[id] = a.getName();
            Vivos[id] = a;
            if (Vivos.Count > 4000) { Vivos.Clear(); Vivos[id] = a; }
        }

        static void Muestrea()
        {
            var units = World.world.units != null ? World.world.units.getSimpleList() : null;
            if (units == null || units.Count == 0) return;
            double wt = ultimoWt;
            int n = Math.Min(units.Count, Math.Max(5, (int)Cfg.Num("muestra_n")));
            grupos.Clear();
            for (int i = 0; i < n; i++)
            {
                Actor a = units[cursor++ % units.Count];
                if (!Valido(a)) continue;
                Registra(a);
                string id = Id(a);
                FichaDe(id);
                int h = FichaGen.Hash(id);
                if (!Secretos.TieneSecreto(id) && h % 100 < 22)
                    Secretos.Asigna(id, RedSecretos.SecretoDeReserva(id), 0.3 + (FichaGen.Hash(id + "g") % 60) / 100.0);

                if (a.hasLover())
                {
                    Actor l = a.lover;
                    if (Valido(l)) { Registra(l); Pareja(a, l, wt); }
                }
                FeedLinaje(a);
                City c = a.city;
                if (c != null)
                {
                    List<Actor> g;
                    if (!grupos.TryGetValue(c, out g)) { g = new List<Actor>(); grupos[c] = g; }
                    g.Add(a);
                }
            }
            foreach (var kv in grupos)
            {
                var g = kv.Value;
                if (g.Count < 2) continue;
                // Ciudades grandes charlan mas: hasta 6 parejas por pasada.
                int parejasCharla = Math.Min(6, 1 + g.Count / 10);
                for (int p = 0; p < parejasCharla; p++)
                {
                    Actor x = g[Azar.Next(g.Count)], y = null;
                    // Las amistades se repiten: con 60 % de probabilidad x busca a su amigo habitual si esta a mano.
                    string ix = Id(x), iy;
                    if (amigos.TryGetValue(ix, out iy) && Azar.Chance(0.6))
                        foreach (Actor c in g) if (Id(c) == iy) { y = c; break; }
                    if (y == null) y = g[Azar.Next(g.Count)];
                    if (x != y) Conversa(x, y, wt);
                }
            }
            if (convT.Count > 3000) convT.Clear();
            if (parejasT.Count > 3000) parejasT.Clear();
        }

        // Figura notable: rey o personaje de carisma muy alto (~3 % de la poblacion). Solo ellos dan noticias y hablan.
        public static bool Notable(Actor a)
        {
            try { return a != null && (a.isKing() || FichaDe(Id(a)).Carisma >= 0.93); }
            catch (Exception) { return false; }
        }

        public static bool Notable(string id)
        {
            Actor a;
            if (Vivos.TryGetValue(id, out a)) return Notable(a);
            try { return FichaDe(id).Carisma >= 0.93; }
            catch (Exception) { return false; }
        }

        static string Par(string a, string b) { return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a; }

        // Alimenta el arbol de linaje con las aristas que se ven por el muestreo (getChildren).
        static void FeedLinaje(Actor a)
        {
            try
            {
                if (!a.hasFamily() || !a.isAdult()) return;
                var hijos = a.getChildren(false);
                if (hijos == null) return;
                int diaNac = Dia();
                foreach (Actor h in hijos)
                {
                    if (h == null || !h.isAlive()) continue;
                    Registra(h);
                    string ih = Id(h);
                    Arbol.Nace(ih, diaNac, Id(a), null);
                    Arbol.Nace(Id(a), diaNac, null, null);
                }
            }
            catch (Exception e) { }   // el API de familia puede cambiar: se ignora sin tumbar nada
        }

        // Mentoría: empareja dentro de cada nación y avanza el aprendizaje de los lazos vivos.
        static double tMentoria;
        static readonly Dictionary<string, double> mentoresClaros = new Dictionary<string, double>();
        static void Mentorias(double wt)
        {
            if (wt - tMentoria < 30) return;   // una pasada por media hora de mundo
            tMentoria = wt;
            try
            {
                var porReino = new Dictionary<Kingdom, List<string>>();
                foreach (var kv in Vivos)
                {
                    Actor a = kv.Value;
                    if (a == null || !a.isAlive() || a.kingdom == null) continue;
                    List<string> l; if (!porReino.TryGetValue(a.kingdom, out l)) { l = new List<string>(); porReino[a.kingdom] = l; }
                    if (l.Count < 200) l.Add(kv.Key);
                }
                if (porReino.Count == 0) return;
                double aprende = 2 * (wt - tMentoria) / Math.Max(0.5, Cfg.Num("dia_segundos"));
                foreach (var kv in porReino)
                {
                    var ids = kv.Value;
                    if (ids.Count < 2) continue;
                    string[] habs = new[] { "guerra", "arte", "saber" };
                    Ensena.Empareja(ids, NivelDe, habs, Afectos, 0.1);
                }
                // Avanza los lazos y, al cumplirse, deshace el vinculo (el aprendiz ya sabe).
                var term = new List<Mentoria.Lazo>();
                foreach (var l in Ensena.Lazos)
                {
                    double nM = NivelDe(l.Mentor, l.Habilidad), nA = NivelDe(l.Aprendiz, l.Habilidad);
                    double nuevo = Ensena.Avanza(l, nM, nA, aprende, Afectos);
                    if (l.Progreso >= 1 || nuevo >= nM - 0.02) term.Add(l);
                    else mentoresClaros[l.Aprendiz] = nM;
                    if (nuevo - nA > 0.01) Memoria.Registra(l.Aprendiz, "mentoria", l.Mentor, "aprendio " + l.Habilidad + " con " + NombreDe(l.Mentor), 0.7);
                }
                foreach (var l in term) Ensena.Lazos.Remove(l);
            }
            catch (Exception e) { Fallo("mentoria", e); }
        }

        // Nivel de una habilidad: determinista por ficha, sin tocar el juego.
        public static double NivelDe(string id, string habilidad)
        {
            Ficha f = FichaDe(id);
            double h = FichaGen.Hash(id + "@" + habilidad) % 10000 / 10000.0;   // [0,1)
            double baseNivel = f.Carisma * 0.3 + h * 0.5;
            return Math.Min(1, baseNivel * (1 + 0.4 * f.Ambicion));
        }

        static void Pareja(Actor a, Actor b, double wt)
        {
            string ia = Id(a), ib = Id(b), k = Par(ia, ib);
            double t;
            if (parejasT.TryGetValue(k, out t) && wt - t < 20) return;
            parejasT[k] = wt;
            Afectos.Evento(ia, ib, TipoEvento.Cortejo, 0.2);
            Afectos.Evento(ib, ia, TipoEvento.Cortejo, 0.2);
            Cultura.Suceso("comunidad", 0.1);
            // Parejas que ya existian al cargar el mundo: se registran sin anunciarlas como noticia.
            if (parejas.Add(k) && wt >= silencioHasta)
            {
                // Con miles de habitantes solo las parejas de figuras notables son noticia; el resto queda en su memoria.
                if (Notable(a) || Notable(b)) Hito("amor", Nombres[ia] + " y " + Nombres[ib] + " son pareja", 3);
                Memoria.Registra(ia, "amor", ib, "se hizo pareja de " + Nombres[ib], 0.7);
                Memoria.Registra(ib, "amor", ia, "se hizo pareja de " + Nombres[ia], 0.7);
                Inspira(ia, "cohesion", 0.4);
                Inspira(ib, "cohesion", 0.4);
            }
        }

        // Dos vecinos de la misma ciudad se cruzan: compatibilidad de caracteres, favores y chismes.
        static void Conversa(Actor a, Actor b, double wt)
        {
            string ia = Id(a), ib = Id(b), k = Par(ia, ib);
            double t;
            if (convT.TryGetValue(k, out t) && wt - t < 25) return;
            convT[k] = wt;
            Ficha fa = FichaDe(ia), fb = FichaDe(ib);

            bool comun = fa.Rasgos.Exists(r => fb.Rasgos.Contains(r));
            bool choque = Choca(fa, fb) || Choca(fb, fa);
            string previo;
            bool habitual = amigos.TryGetValue(ia, out previo) && previo == ib;
            if (habitual) { Afectos.Evento(ia, ib, TipoEvento.Aprecio, 0.4); Afectos.Evento(ib, ia, TipoEvento.Aprecio, 0.4); }   // la amistad se afianza
            else if (comun) { Afectos.Evento(ia, ib, TipoEvento.Aprecio, 0.2); Afectos.Evento(ib, ia, TipoEvento.Aprecio, 0.2); }
            else if (choque)
            {
                Afectos.Evento(ia, ib, TipoEvento.Agravio, 0.12); Afectos.Evento(ib, ia, TipoEvento.Competencia, 0.2);
                Memoria.Registra(ia, "roce", ib, "discutio con " + Nombres[ib], 0.4);
            }
            else { Afectos.Evento(ia, ib, TipoEvento.Aprecio, 0.05); Afectos.Evento(ib, ia, TipoEvento.Aprecio, 0.05); }

            if (fa.Rasgos.Contains("generoso") && Azar.Chance(0.15))
            {
                Afectos.Evento(ib, ia, TipoEvento.Ayuda, 0.3);
                Memoria.Registra(ib, "favor", ia, Nombres[ia] + " le ayudo", 0.6);
            }
            if (Afectos.Sentimiento(ia, ib) > 0.5 && Azar.Chance(0.08))
            {
                Afectos.Evento(ia, ib, TipoEvento.Fiesta, 0.5); Afectos.Evento(ib, ia, TipoEvento.Fiesta, 0.5);
                Cultura.Suceso("comunidad", 0.5);
            }
            // Quien cae bien pasa a ser el companero habitual de charla (alimenta las facciones).
            if (Afectos.Sentimiento(ia, ib) > 0.03) amigos[ia] = ib;
            if (Afectos.Sentimiento(ib, ia) > 0.03) amigos[ib] = ia;
            if (amigos.Count > 5000) amigos.Clear();
            foreach (Fuga f in Secretos.Conversan(ia, ib)) ProcesaFuga(f);
        }

        static bool Choca(Ficha a, Ficha b)
        {
            bool susceptible = a.Rasgos.Contains("orgulloso") || a.Rasgos.Contains("rencoroso") || a.Rasgos.Contains("desconfiado");
            bool irritante = b.Rasgos.Contains("bromista") || b.Rasgos.Contains("ambicioso");
            return susceptible && irritante;
        }

        static void Inspira(string id, string categoria, double cantidad)
        {
            foreach (string t in Suenos.Avanza(FichaDe(id), categoria, cantidad, Afectos))
                Hito("sueno", t, t.Contains("cumple") ? 4 : 1);
        }

        static void ProcesaFuga(Fuga f)
        {
            string suj = NombreDe(f.Sujeto), rec = NombreDe(f.Receptor), emi = NombreDe(f.Emisor);
            Secreto s = Secretos.Get(f.SecretoId);
            double grav = s != null ? s.Gravedad : 0.3;
            if (f.Motivo == "confesion")
            {
                Hito("confesion", emi + " le confio un secreto a " + rec, 1.5);
                Memoria.Registra(f.Receptor, "confidencia", f.Emisor, emi + " le confio un secreto", 0.7);
                return;
            }
            Hito("rumor", rec + " oyo de boca de " + emi + " que " + suj + " " + f.Texto + (f.Fidelidad < 0.8 ? " (la historia ya va cambiada)" : ""), 1 + 3 * grav);
            Memoria.Registra(f.Receptor, "rumor", f.Sujeto, suj + " " + f.Texto, 0.6);
            Cultura.Suceso("saber", 0.3);
            if (f.SujetoSeEntera)
            {
                Afectos.Evento(f.Sujeto, f.Emisor, TipoEvento.Traicion, 0.5 * grav + 0.2);
                Hito("traicion", suj + " supo que " + emi + " hablaba de el", 2 + 2 * grav);
                Memoria.Registra(f.Sujeto, "traicion", f.Emisor, emi + " contaba su secreto", 0.9);
                Actor sa; if (Vivos.TryGetValue(f.Sujeto, out sa) && Notable(sa)) VozLlm.Pide(f.Sujeto, f.Emisor, -1, "se ha enterado de que " + emi + " va contando que " + suj + " " + f.Texto);
            }
            if (Cfg.Bool("juicios")) Juicio(f, s);
        }

        static string JuezDe(string acusado)
        {
            Actor a;
            if (!Vivos.TryGetValue(acusado, out a) || a.kingdom == null || a.kingdom.wild) return null;
            Actor rey = a.kingdom.king;
            if (rey == null || !rey.isAlive() || Id(rey) == acusado) return null;
            Registra(rey);
            return Id(rey);
        }

        static void Juicio(Fuga f, Secreto s)
        {
            Acusacion ac = Tribunal.DesdeFuga(f, s, Dia());
            if (ac == null || !Azar.Chance(0.5)) return;
            string juez = JuezDe(ac.Acusado);
            if (juez == null) return;
            Sentencia se = Tribunal.Juzga(ac, juez, false);    // sin penas irreversibles
            Tribunal.Aplica(se);
            string pena;
            switch (se.Pena)
            {
                case Pena.Absolver: pena = "lo absuelve"; break;
                case Pena.Amonestar: pena = "lo amonesta ante todos"; break;
                case Pena.Desterrar: pena = "lo desierra"; break;
                case Pena.Encarcelar: pena = "lo encarcela"; break;
                default: pena = "le impone una multa"; break;
            }
            Hito("juicio", NombreDe(juez) + " juzga a " + NombreDe(ac.Acusado) + " por " + ac.Delito + " y " + pena, 3.5 + 2 * ac.Gravedad);
            Memoria.Registra(ac.Acusado, "juicio", juez, "fue juzgado por " + NombreDe(juez) + ": " + pena, 0.9);
            Cultura.Suceso(se.Pena == Pena.Absolver ? "clemencia" : "honor", 1);
            VozLlm.Pide(juez, ac.Acusado, se.Pena == Pena.Absolver ? 1 : -1, "acaba de juzgar a " + NombreDe(ac.Acusado) + " y " + pena, true);
            // Nivel 3: un destierro (o encarcelamiento de un notable) echa al acusado de su ciudad.
            if (se.Pena == Pena.Desterrar || (se.Pena == Pena.Encarcelar && Notable(ac.Acusado)))
            {
                Actor acusado;
                if (Vivos.TryGetValue(ac.Acusado, out acusado)) Destierra(acusado, ac.Delito);
            }
        }

        // Facciones, lideres, tensiones entre casas y credo del mundo.
        static void Facciones()
        {
            var ids = new HashSet<string>();
            foreach (var kv in Afectos.Todos())
            {
                string a, b; ModeloAfectivo.Separa(kv.Key, out a, out b);
                ids.Add(a); ids.Add(b);
            }
            if (ids.Count < 3) return;
            var lista = new List<string>(ids);
            if (lista.Count > 250) lista.RemoveRange(250, lista.Count - 250);
            var fs = Sociedad.Facciones(Afectos, lista, Cfg.Num("faccion_umbral"), 3);
            Sociedad.AsignaLideres(Afectos, fs, FichaDe, NombreDe);
            UltimasFacciones = fs;
            foreach (var f in fs)
            {
                if (!facConocidas.Add(f.Lider)) continue;
                Hito("faccion", "Surge la " + f.Nombre + " con " + f.Miembros.Count + " miembros", 3);
                Cultura.Suceso("comunidad", 1);
                Inspira(f.Lider, "crecimiento", 0.5);
                VozLlm.Pide(f.Lider, f.Lider, 1, "ha reunido a su gente: " + f.Nombre + " (" + f.Miembros.Count + " miembros)", f.Miembros.Count >= 8);
            }
            facDe.Clear();
            foreach (var f in fs) foreach (string m in f.Miembros) facDe[m] = f;
            for (int i = 0; i < fs.Count; i++)
                for (int j = i + 1; j < fs.Count; j++)
                {
                    double s = 0; int c = 0;
                    foreach (string a in fs[i].Miembros) foreach (string b in fs[j].Miembros) { s += Afectos.Sentimiento(a, b) + Afectos.Sentimiento(b, a); c += 2; }
                    if (c > 0 && s / c < -0.3 && tensionesConocidas.Add(fs[i].Lider + "|" + fs[j].Lider))
                    {
                        Hito("tension", "La " + fs[i].Nombre + " y la " + fs[j].Nombre + " se detestan", 4);
                        Cultura.Suceso("venganza", 1);
                    }
                }
            string credo = Cultura.Credo();
            if (credo != ultimoCredo && Cronica.Count > 5)
            {
                if (ultimoCredo.Length > 0) Hito("credo", "Cambia el credo del mundo: " + credo, 4);
                ultimoCredo = credo;
            }
        }
    }
}
