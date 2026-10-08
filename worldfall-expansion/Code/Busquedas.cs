using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // «Cronica -> Busquedas»: cada pocos segundos hace una foto del mundo y el Core la compara con la
    // anterior para abrir/cerrar busquedas (heredero al trono, hijo de un difunto, ciudad sin lider,
    // amantes separados). SOLO LEE el juego y sin Harmony: si WorldBox cambia algo interno, como mucho
    // falla la foto (se avisa en el log) y el juego sigue.
    //
    // Firmas usadas (todas de la lista verificada de la build 719):
    //   World.world.units.getSimpleList(), World.world.getCurWorldTime(), map_stats (por Traverse)
    //   actor.isAlive/isSapient/isAdult/isKing/getName/data.id/hasLover/lover/hasFamily/getChildren(false)/city/kingdom
    //   kingdom.wild/king/data.name, city.leader, WorldTip.showNow
    internal static class Busquedas
    {
        static TableroBusquedas tablero;
        static string clave = "";
        static double ultimoWt = -1;
        static float tFoto;
        static string rutaLog = "";

        // Claves estables por objeto (no hay id de reino/ciudad en la API verificada).
        static readonly Dictionary<Kingdom, string> clavesReino = new Dictionary<Kingdom, string>();
        static readonly Dictionary<City, string> clavesCiudad = new Dictionary<City, string>();
        static int nClave;
        // Unidades de la foto anterior: si una falta en la lista, se comprueba si sigue viva antes de darla por muerta.
        static Dictionary<string, Actor> actoresPrevios = new Dictionary<string, Actor>();

        public static bool Activo { get { return Estado.Activo && Estado.Cfg.Bool("busquedas"); } }

        public static IList<Busqueda> Activas { get { return tablero != null ? tablero.Activas : null; } }

        // Unidad viva de la ultima foto (para la brujula). null si no esta.
        public static Actor ActorDe(string id)
        {
            Actor a;
            return id != null && actoresPrevios.TryGetValue(id, out a) ? a : null;
        }

        public static void Inicia()
        {
            tablero = new TableroBusquedas(ReglasBusquedas.Desde(Estado.Cfg));
            if (!Activo) Debug.Log("[WorldfallExp] busquedas=0: busquedas apagadas");
        }

        public static void Tick()
        {
            if (!Activo || tablero == null || World.world == null) return;
            Teclas();

            float real = Time.unscaledTime;
            if (real - tFoto < Math.Max(2.0, Estado.Cfg.Num("busquedas_intervalo_seg", 10))) return;
            tFoto = real;

            double wt = World.world.getCurWorldTime();
            string k = Estado.ClaveMundo();
            // Otra partida, o el reloj del mundo retrocede (se cargo una partida): linea base nueva.
            if (k != clave || wt < ultimoWt - 5) Reinicia(k);
            if (tablero.TieneBase && wt == ultimoWt) return;   // juego en pausa: nada que comparar
            ultimoWt = wt;

            FotoMundo f = Foto(wt);
            if (f == null || f.Unidades.Count == 0) return;   // menu o mundo vacio: no se toma como base
            var ev = tablero.Compara(f);
            foreach (EventoBusqueda e in ev) Publica(e, wt);
            if (ev.Count > 0 || real - tGuarda > 30f) Guarda(real);
        }

        static float tGuarda;
        static string rutaActivas = "";

        // busquedas_activas.jsonl: las abiertas sobreviven a cerrar el juego (se enlazan al volver).
        static void Guarda(float real)
        {
            tGuarda = real;
            if (rutaActivas.Length == 0 || !tablero.TieneBase) return;
            try
            {
                string tmp = rutaActivas + ".tmp";
                File.WriteAllLines(tmp, tablero.Serializa().ToArray(), new UTF8Encoding(false));
                if (File.Exists(rutaActivas)) File.Delete(rutaActivas);
                File.Move(tmp, rutaActivas);
            }
            catch (Exception e) { Estado.Fallo("guardar_busquedas", e); }
        }

        static void Restaura()
        {
            try
            {
                if (!File.Exists(rutaActivas)) return;
                var l = new List<Busqueda>();
                foreach (string linea in File.ReadAllLines(rutaActivas, Encoding.UTF8))
                {
                    Busqueda b = Busqueda.Desde(linea);
                    if (b != null) l.Add(b);
                }
                tablero.Restaura(l);
                if (l.Count > 0) Debug.Log("[WorldfallExp] busquedas: " + l.Count + " guardadas de la sesion anterior");
            }
            catch (Exception e) { Estado.Fallo("restaurar_busquedas", e); }
        }

        // Busquedas que propone otro modulo (PuentePecera): mismos topes, avisos y registro.
        public static void Externas(List<Busqueda> cand)
        {
            if (!Activo || tablero == null || cand == null || cand.Count == 0) return;
            foreach (EventoBusqueda e in tablero.Ofrece(cand, ultimoWt)) Publica(e, ultimoWt);
        }

        // La unidad viva mas cercana a una posicion (sin contar a «excepto»), dentro de un radio en casillas.
        public static Actor Cercano(Vector2 p, float radio, Actor excepto)
        {
            Actor mejor = null;
            float mejorD = radio * radio;
            foreach (Actor a in actoresPrevios.Values)
            {
                if (a == null || a == excepto) continue;
                Vector2 q = a.current_position;
                float dx = q.x - p.x, dy = q.y - p.y, d = dx * dx + dy * dy;
                if (d < mejorD) { mejorD = d; mejor = a; }
            }
            return mejor;
        }

        public static string IdDe(Actor a) { return Id(a); }

        public static FotoMundo UltimaFoto { get { return tablero != null ? tablero.Ultima : null; } }

        static void Reinicia(string k)
        {
            clave = k;
            tablero.Reinicia();
            clavesReino.Clear(); clavesCiudad.Clear(); actoresPrevios.Clear();
            string seguro = k;
            foreach (char c in Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
            string dir = Path.Combine(Path.Combine(Estado.Dir, "mundos"), seguro);
            try { Directory.CreateDirectory(dir); rutaLog = Path.Combine(dir, "busquedas.jsonl"); rutaActivas = Path.Combine(dir, "busquedas_activas.jsonl"); }
            catch (Exception e) { rutaLog = ""; rutaActivas = ""; Estado.Fallo("carpeta_mundo", e); }
            if (rutaActivas.Length > 0) Restaura();
            Debug.Log("[WorldfallExp] busquedas: mundo '" + k + "' (linea base en la proxima foto)");
        }

        static void Publica(EventoBusqueda e, double wt)
        {
            Anota(e.ToJson(wt));
            if (e.Estado == EstadoBusqueda.Cumplida || e.Estado == EstadoBusqueda.Fallida)
                Recompensas.Aplica(TableroBusquedas.RecompensaDe(e.B, tablero.Ultima), e.B);
            if (!Estado.Cfg.Bool("busquedas_avisos")) return;
            switch (e.Estado)
            {
                case EstadoBusqueda.Activa: Estado.Aviso("Nueva busqueda: " + e.B.Titulo + "\n" + e.Texto, 5f); break;
                case EstadoBusqueda.Cumplida: Estado.Aviso("Busqueda cumplida: " + e.Texto, 4f); break;
                case EstadoBusqueda.Fallida: Estado.Aviso("Busqueda fallida: " + e.Texto, 4f); break;
                default: break;   // las caducadas solo van al registro
            }
        }

        static void Anota(string linea)
        {
            if (rutaLog.Length == 0) return;
            try
            {
                // Tope de 2 MB: se rota a busquedas.1.jsonl (se conserva la ultima rotacion).
                var fi = new FileInfo(rutaLog);
                if (fi.Exists && fi.Length > 2 * 1024 * 1024)
                {
                    string viejo = Path.ChangeExtension(rutaLog, ".1.jsonl");
                    if (File.Exists(viejo)) File.Delete(viejo);
                    File.Move(rutaLog, viejo);
                }
                File.AppendAllText(rutaLog, linea + "\n", new UTF8Encoding(false));
            }
            catch (Exception ex) { Estado.Fallo("registro", ex); }
        }

        static void Teclas()
        {
            if (Input.GetKeyDown(Estado.Tecla(Estado.Cfg.Str("busquedas_tecla"), KeyCode.F8)))
                Estado.Aviso(tablero.Lista(), 6f);
        }

        // ---------- foto del mundo ----------

        static string Id(Actor a) { return "a" + a.data.id; }

        static string ClaveDe(Kingdom k)
        {
            string c;
            if (!clavesReino.TryGetValue(k, out c)) { c = "k" + (++nClave); clavesReino[k] = c; }
            return c;
        }

        static string ClaveDe(City ci)
        {
            string c;
            if (!clavesCiudad.TryGetValue(ci, out c)) { c = "c" + (++nClave); clavesCiudad[ci] = c; }
            return c;
        }

        static FotoMundo Foto(double wt)
        {
            var lista = World.world.units != null ? World.world.units.getSimpleList() : null;
            if (lista == null) return null;
            // Muchisimos reinos/ciudades creados y destruidos: se empieza de cero para no crecer sin limite.
            if (clavesReino.Count + clavesCiudad.Count > 20000) { Reinicia(clave); return null; }

            var f = new FotoMundo { Tiempo = wt };
            var actores = new Dictionary<string, Actor>();
            foreach (Actor a in lista) Agrega(f, actores, a);
            // Una unidad puede faltar de la lista sin haber muerto: solo cuenta como muerta si isAlive() lo dice.
            foreach (var kv in actoresPrevios)
                if (!f.Unidades.ContainsKey(kv.Key)) Agrega(f, actores, kv.Value);

            // Hijos solo de reyes y lideres de ciudad (getChildren recorre la familia: no se llama para todos).
            var notables = new HashSet<string>();
            foreach (var r in f.Reinos.Values) if (r.Rey != null) notables.Add(r.Rey);
            foreach (var c in f.Ciudades.Values) if (c.Lider != null) notables.Add(c.Lider);
            foreach (string id in notables)
            {
                Actor a; FotoUnidad u;
                if (actores.TryGetValue(id, out a) && f.Unidades.TryGetValue(id, out u)) Hijos(a, u);
            }
            actoresPrevios = actores;
            return f;
        }

        static void Agrega(FotoMundo f, Dictionary<string, Actor> actores, Actor a)
        {
            try
            {
                if (a == null || !a.isAlive() || !a.isSapient()) return;
                var u = new FotoUnidad { Id = Id(a), Nombre = a.getName() ?? "", Adulto = a.isAdult(), EsRey = a.isKing() };
                if (f.Unidades.ContainsKey(u.Id)) return;

                Kingdom k = a.kingdom;
                if (k != null && !k.wild)
                {
                    u.Reino = ClaveDe(k);
                    u.ReinoNombre = k.data != null ? (k.data.name ?? "") : "";
                    if (!f.Reinos.ContainsKey(u.Reino))
                    {
                        Actor rey = k.king;
                        f.Reinos[u.Reino] = new FotoReino
                        {
                            Clave = u.Reino, Nombre = u.ReinoNombre,
                            Rey = rey != null && rey.isAlive() ? Id(rey) : null,
                        };
                    }
                }
                City c = a.city;
                if (c != null)
                {
                    u.Ciudad = ClaveDe(c);
                    if (!f.Ciudades.ContainsKey(u.Ciudad))
                    {
                        Actor l = c.leader;
                        f.Ciudades[u.Ciudad] = new FotoCiudad { Clave = u.Ciudad, Reino = u.Reino, Lider = l != null && l.isAlive() ? Id(l) : null };
                    }
                }
                if (a.hasLover())
                {
                    Actor p = a.lover;
                    if (p != null && p.isAlive()) u.Amante = Id(p);
                }
                f.Unidades[u.Id] = u;
                actores[u.Id] = a;
            }
            catch (Exception e) { Estado.Fallo("foto_unidad", e); }
        }

        static void Hijos(Actor a, FotoUnidad u)
        {
            try
            {
                if (!a.hasFamily()) return;
                var hijos = a.getChildren(false);
                if (hijos == null) return;
                foreach (Actor h in hijos)
                    if (h != null && h.isAlive()) u.Hijos.Add(Id(h));
            }
            catch (Exception e) { Estado.Fallo("foto_hijos", e); }
        }
    }
}
