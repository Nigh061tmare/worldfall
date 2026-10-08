using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldfallExpansion.Core
{
    // config.txt (clave=valor, '#' comenta). Mismo formato que PeceraWB.
    // Sin IO: el adaptador lee las lineas y vuelve a escribir Serializa() para que aparezcan las claves nuevas.
    public sealed class Config
    {
        public static readonly string[][] Defecto =
        {
            new[] { "activo", "1" },
            new[] { "busquedas", "1" },                 // generar busquedas a partir de lo que pasa en el mundo (solo lectura)
            new[] { "busquedas_intervalo_seg", "10" },  // segundos reales entre fotos del mundo
            new[] { "busquedas_max_activas", "5" },     // busquedas abiertas a la vez
            new[] { "busquedas_max_nuevas", "2" },      // busquedas nuevas por foto (evita avalanchas en guerras)
            new[] { "busquedas_caducidad_seg", "900" }, // segundos de MUNDO hasta que una busqueda caduca
            new[] { "busquedas_avisos", "1" },          // aviso arriba (WorldTip) al abrir/cerrar una busqueda
            new[] { "busquedas_trono", "1" },           // muere un rey con hijos adultos: el heredero debe reinar
            new[] { "busquedas_huerfanos", "1" },       // muere un rey o lider con hijos pequenos: que lleguen a adultos
            new[] { "busquedas_ciudades", "1" },        // muere el lider de una ciudad y queda sin gobierno
            new[] { "busquedas_amantes", "1" },         // una pareja queda en reinos distintos
            new[] { "busquedas_tecla", "F8" },          // lista las busquedas activas (F9/F10 son de PeceraWB)
            new[] { "contenido", "1" },                 // Arsenal (16 objetos) y 12 rasgos nuevos en el juego (Worldfall los usa)
            new[] { "busquedas_recompensas", "0" },     // ESCRIBE en el mundo: renombre y rasgos al cumplir busquedas
            new[] { "busquedas_recompensas_max_hora", "6" },
            new[] { "social", "1" },                    // en primera persona: lo que la pecera sabe de quien tienes cerca
            new[] { "social_radio", "6" },              // casillas
            new[] { "frases", "1" },                    // frases nuevas de recuerdos para los PNJ de Worldfall
            new[] { "pecera_busquedas", "1" },          // regicidios, destierros y disputas de PeceraWB abren busquedas
            new[] { "brujula", "1" },                   // linea arriba con distancia y rumbo a la busqueda que sigues
            new[] { "brujula_tecla", "F7" },            // cambia de busqueda seguida
            new[] { "brujula_altura", "0.07" },         // altura de la linea (fraccion de la pantalla, 0 = arriba)
            new[] { "brujula_invertir", "0" },          // 1 si «izquierda/derecha» salen al reves en primera persona
            new[] { "traduccion", "1" },                // traduce al espanol los textos de Worldfall (Traducciones/*_es.txt)
            new[] { "traduccion_registro", "0" },       // apunta en traduccion_faltan.txt lo que salio sin traducir
        };

        readonly Dictionary<string, string> d = new Dictionary<string, string>();

        public Config()
        {
            foreach (var kv in Defecto) d[kv[0]] = kv[1];
        }

        public static Config Desde(IEnumerable<string> lineas)
        {
            var c = new Config();
            if (lineas == null) return c;
            foreach (string l in lineas)
            {
                if (l == null) continue;
                string s = l.Trim();
                if (s.Length == 0 || s[0] == '#') continue;
                int i = s.IndexOf('=');
                if (i <= 0) continue;
                string v = s.Substring(i + 1);
                int com = v.IndexOf('#');          // permite "clave=1   # comentario"
                if (com >= 0) v = v.Substring(0, com);
                c.d[s.Substring(0, i).Trim()] = v.Trim();
            }
            return c;
        }

        // Fichero completo: claves conocidas en orden, con el valor actual (respeta lo que puso el usuario).
        public string Serializa()
        {
            var sb = new StringBuilder("# Worldfall Expansion: edita con el juego cerrado\n");
            foreach (var kv in Defecto) sb.Append(kv[0]).Append('=').Append(d[kv[0]]).Append('\n');
            return sb.ToString();
        }

        public string Str(string k) { string v; return d.TryGetValue(k, out v) ? v : ""; }
        public bool Bool(string k) { string s = Str(k).ToLowerInvariant(); return s == "1" || s == "true"; }

        public double Num(string k, double defecto)
        {
            double v;
            return double.TryParse(Str(k), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : defecto;
        }

        public int Ent(string k, int defecto, int min, int max)
        {
            double v = Num(k, defecto);
            if (v < min) return min;
            if (v > max) return max;
            return (int)v;
        }

        public void Set(string k, string v) { d[k] = v; }
    }
}
