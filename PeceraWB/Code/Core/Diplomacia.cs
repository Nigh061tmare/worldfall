using System;
using System.Collections.Generic;

namespace Pecera.Core
{
    // Decisiones del Nivel 3 avanzado (puras, sin Unity): el adaptador (Nivel3Guerras.cs) las ejecuta en
    // el juego solo con nivel3=1 y su interruptor propio, con tope por hora y registro.
    public sealed class ReyInfo
    {
        public string Id = "", Reino = "";
    }

    public sealed class LiderInfo
    {
        public string Id = "", Reino = "";
        public bool EsRey, EsLiderCiudad, CiudadEsCapital;
        public int CiudadesDelReino;
    }

    public static class Diplomacia
    {
        // Guerras por odio entre reyes: parejas (atacante, defensor) donde el atacante odia al defensor
        // por debajo de -umbral, de reinos distintos y sin guerra ni alianza entre ellos. Mas odio primero.
        public static List<KeyValuePair<string, string>> GuerrasPorOdio(IList<ReyInfo> reyes, Func<string, string, double> sentimiento,
                                                                         Func<string, string, bool> bloqueada, double umbral, int max)
        {
            var cand = new List<KeyValuePair<double, KeyValuePair<string, string>>>();
            if (reyes == null) return new List<KeyValuePair<string, string>>();
            foreach (ReyInfo a in reyes)
                foreach (ReyInfo b in reyes)
                {
                    if (a == b || a.Reino == b.Reino || a.Id == b.Id) continue;
                    double s = sentimiento(a.Id, b.Id);
                    if (s > -umbral) continue;
                    if (bloqueada(a.Reino, b.Reino)) continue;
                    cand.Add(new KeyValuePair<double, KeyValuePair<string, string>>(s, new KeyValuePair<string, string>(a.Id, b.Id)));
                }
            cand.Sort((x, y) => x.Key != y.Key ? x.Key.CompareTo(y.Key) : string.CompareOrdinal(x.Value.Key, y.Value.Key));
            var r = new List<KeyValuePair<string, string>>();
            var usados = new HashSet<string>();
            foreach (var c in cand)
            {
                if (r.Count >= max) break;
                // Un mismo par no se cuenta dos veces (A->B y B->A).
                string k = string.CompareOrdinal(c.Value.Key, c.Value.Value) < 0 ? c.Value.Key + "|" + c.Value.Value : c.Value.Value + "|" + c.Value.Key;
                if (usados.Add(k)) r.Add(c.Value);
            }
            return r;
        }

        // Guerra civil: dos facciones que se detestan (tension) con lideres del MISMO reino; uno es el rey
        // y el otro lidera una ciudad que no es la capital, en un reino con 2+ ciudades. Devuelve el id del
        // lider que se rebela, o null.
        public static string Rebelde(IEnumerable<KeyValuePair<string, string>> tensiones, Func<string, LiderInfo> info)
        {
            if (tensiones == null) return null;
            foreach (var t in tensiones)
            {
                LiderInfo a = info(t.Key), b = info(t.Value);
                if (a == null || b == null || a.Reino.Length == 0 || a.Reino != b.Reino) continue;
                LiderInfo rey = a.EsRey ? a : (b.EsRey ? b : null);
                LiderInfo otro = rey == a ? b : a;
                if (rey == null || otro.EsRey) continue;
                if (otro.EsLiderCiudad && !otro.CiudadEsCapital && otro.CiudadesDelReino >= 2) return otro.Id;
            }
            return null;
        }
    }

    // Intervenciones divinas que apunta worldfall-expansion (sus poderes) en
    // WorldfallExpansion\mundos\<mundo>\intervenciones.jsonl: {"t":"wfx_bendicion","id":"a12","n":"Ana"}.
    public sealed class Intervencion
    {
        public string Tipo = "", Id = "", Nombre = "";

        public static Intervencion Lee(string linea)
        {
            object o;
            if (!Json.TryParse(linea, out o)) return null;
            string t = Json.Str(o, "t"), id = Json.Str(o, "id");
            if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(id)) return null;
            string n = Json.Str(o, "n");
            return new Intervencion { Tipo = t, Id = id, Nombre = string.IsNullOrEmpty(n) ? id : n };
        }

        // Hito de cronica (tipo, texto, peso) para cada intervencion; null si no se reconoce.
        public string[] Hito()
        {
            switch (Tipo)
            {
                case "wfx_bendicion": return new[] { "divino", "Los dioses bendijeron a " + Nombre, "5" };
                case "wfx_maldicion": return new[] { "divino", "Los dioses maldijeron a " + Nombre, "5" };
                case "wfx_juramento": return new[] { "juramento", Nombre + " juró lealtad a su reino ante los dioses", "3" };
                case "wfx_destierro": return new[] { "destierro", Nombre + " es desterrado por los dioses", "5" };
                case "wfx_discordia": return new[] { "discordia", "Los dioses sembraron la discordia en el corazón de " + Nombre, "4" };
                case "wfx_reconciliacion": return new[] { "reconciliacion", "Los dioses ablandaron el corazón de " + Nombre, "3" };
                case "wfx_bestia": return new[] { "bestia", "Despierta " + Nombre + ". Nadie duerme tranquilo", "6" };
            }
            return null;
        }
    }
}
