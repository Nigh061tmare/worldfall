using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WorldfallExpansion.Core
{
    // Traductor de textos de pantalla (sin Unity). Lo usa el adaptador Traduccion.cs justo antes de que
    // Unity pinte un texto. Dos tipos de entrada en el fichero de traduccion:
    //
    //   Exacta:     Any work for me? => ¿Tienes trabajo para mi?
    //   Plantilla:  The army of {0} gathers: {1} of {2}. => El ejercito de {0} se reune: {1} de {2}.
    //
    // Los huecos {n} capturan cualquier texto; lo capturado se intenta traducir tambien (un nivel), asi
    // «{0} The army of...» traduce el trozo de delante si es una frase conocida. En la traduccion los
    // huecos pueden cambiar de orden. Una plantilla sin ninguna palabra fija de 3+ letras se ignora
    // (seria tan generica que lo atraparia todo).
    //
    // Lo que no esta en el fichero se deja tal cual: si Worldfall se actualiza, lo nuevo sale en ingles.
    public sealed class Traductor
    {
        sealed class Plantilla
        {
            public Regex Re;
            public string Destino = "";
            public int Huecos;
            public int Literal;        // letras fijas: a mas, mas especifica
        }

        readonly Dictionary<string, string> exactas = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, List<Plantilla>> indice = new Dictionary<string, List<Plantilla>>(StringComparer.Ordinal);
        readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> faltan = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> faltanNuevas = new List<string>();
        int plantillas;

        public int MaxCache = 20000;
        public bool RegistrarFaltan;
        public int Exactas { get { return exactas.Count; } }
        public int Plantillas { get { return plantillas; } }
        public int Errores;            // lineas del fichero que no se pudieron leer

        static readonly Regex Hueco = new Regex(@"\{(\d)\}", RegexOptions.CultureInvariant);
        const string Flecha = " => ";

        // Admite varios ficheros: lo cargado despues sustituye a lo anterior (traducciones del usuario).
        public void Carga(IEnumerable<string> lineas)
        {
            if (lineas == null) return;
            foreach (string l in lineas)
            {
                if (string.IsNullOrEmpty(l)) continue;
                string s = l.TrimEnd('\r');
                if (s.TrimStart().StartsWith("#")) continue;
                int i = s.IndexOf(Flecha, StringComparison.Ordinal);
                if (i <= 0) { if (s.Trim().Length > 0) Errores++; continue; }
                string en = Desescapa(s.Substring(0, i)).Trim();
                string es = Desescapa(s.Substring(i + Flecha.Length)).Trim();
                if (en.Length == 0 || es.Length == 0) { Errores++; continue; }
                if (!Agrega(en, es)) Errores++;
            }
            cache.Clear();
        }

        public bool Agrega(string en, string es)
        {
            if (!Hueco.IsMatch(en)) { exactas[en] = es; return true; }
            // Plantilla: literal -> regex, {n} -> grupo con nombre.
            var sb = new StringBuilder("^");
            int pos = 0, huecos = 0, maxN = -1;
            string clave = null;
            bool pegados = false;
            foreach (Match m in Hueco.Matches(en))
            {
                string lit = en.Substring(pos, m.Index - pos);
                if (pos > 0 && lit.Length == 0) pegados = true;   // "{0}{1}": ambiguo
                sb.Append(Regex.Escape(lit));
                clave = Mejor(clave, lit, pos > 0, true);
                int n = m.Groups[1].Value[0] - '0';
                if (sb.ToString().Contains("(?<h" + n + ">")) sb.Append("\\k<h" + n + ">");
                else sb.Append("(?<h" + n + ">.*?)");   // puede ir vacio: Worldfall compone trozos opcionales
                if (n > maxN) maxN = n;
                huecos++;
                pos = m.Index + m.Length;
            }
            string fin = en.Substring(pos);
            sb.Append(Regex.Escape(fin)).Append('$');
            clave = Mejor(clave, fin, pos > 0, false);
            if (clave == null || pegados) return false;
            // Todos los huecos del destino deben existir en el origen.
            foreach (Match m in Hueco.Matches(es))
                if (m.Groups[1].Value[0] - '0' > maxN) return false;

            var p = new Plantilla
            {
                Re = new Regex(sb.ToString(), RegexOptions.CultureInvariant | RegexOptions.Singleline),
                Destino = es,
                Huecos = maxN + 1,
                Literal = Hueco.Replace(en, "").Length,
            };
            List<Plantilla> l;
            if (!indice.TryGetValue(clave, out l)) { l = new List<Plantilla>(); indice[clave] = l; }
            // La misma plantilla otra vez: se sustituye (el fichero del usuario manda).
            for (int i = 0; i < l.Count; i++)
                if (l[i].Re.ToString() == p.Re.ToString()) { l[i] = p; return true; }
            l.Add(p);
            plantillas++;
            return true;
        }

        // Palabra de indice: la mas larga (3+ letras) de los trozos fijos que sea una palabra COMPLETA:
        // una pegada a un hueco ("settler{1}") en el juego sale distinta ("settlers") y no se encontraria.
        static string Mejor(string actual, string literal, bool huecoAntes, bool huecoDespues)
        {
            int i = 0;
            while (i < literal.Length)
            {
                while (i < literal.Length && !char.IsLetter(literal[i])) i++;
                int ini = i;
                while (i < literal.Length && (char.IsLetter(literal[i]) || literal[i] == '\'')) i++;
                if (i - ini < 3) continue;
                if (huecoAntes && ini == 0) continue;
                if (huecoDespues && i == literal.Length) continue;
                string w = literal.Substring(ini, i - ini);
                if (actual == null || w.Length > actual.Length) actual = w;
            }
            return actual;
        }

        static IEnumerable<string> Palabras(string s)
        {
            int i = 0;
            while (i < s.Length)
            {
                while (i < s.Length && !char.IsLetter(s[i])) i++;
                int ini = i;
                while (i < s.Length && (char.IsLetter(s[i]) || s[i] == '\'')) i++;
                if (i - ini >= 3) yield return s.Substring(ini, i - ini);
            }
        }

        public string Traduce(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 4000) return s;
            string r;
            if (cache.TryGetValue(s, out r)) return r;
            r = TraduceSinCache(s, 0);
            if (cache.Count >= MaxCache) cache.Clear();
            cache[s] = r;
            if (RegistrarFaltan && (object)r == (object)s && TieneTexto(s) && faltan.Count < 20000 && faltan.Add(s.Trim()))
                faltanNuevas.Add(s.Trim());
            return r;
        }

        // Textos no traducidos desde la ultima llamada (para el registro traduccion_faltan.txt).
        public List<string> SacaFaltan()
        {
            var l = new List<string>(faltanNuevas);
            faltanNuevas.Clear();
            return l;
        }

        string TraduceSinCache(string s, int nivel)
        {
            // Se conservan los espacios de los bordes: Worldfall compone frases a trozos.
            int a = 0, b = s.Length;
            while (a < b && char.IsWhiteSpace(s[a])) a++;
            while (b > a && char.IsWhiteSpace(s[b - 1])) b--;
            if (a == b) return s;
            string nucleo = (a == 0 && b == s.Length) ? s : s.Substring(a, b - a);

            string t;
            if (!exactas.TryGetValue(nucleo, out t)) t = Capitalizada(nucleo);
            if (t == null) t = PorPlantilla(nucleo, nivel);
            if (t == null) return s;
            if (a == 0 && b == s.Length) return t;
            return s.Substring(0, a) + t + s.Substring(b);
        }

        // Worldfall capitaliza trozos al componer («When I lost my love, ...»): se prueba la frase con la
        // primera letra en minuscula y se devuelve la traduccion con mayuscula.
        string Capitalizada(string s)
        {
            if (s.Length < 2 || !char.IsUpper(s[0])) return null;
            string t;
            if (!exactas.TryGetValue(char.ToLowerInvariant(s[0]) + s.Substring(1), out t) || t.Length == 0) return null;
            return char.ToUpperInvariant(t[0]) + t.Substring(1);
        }

        string PorPlantilla(string s, int nivel)
        {
            if (indice.Count == 0) return null;
            // Gana la plantilla que encaja con MAS texto fijo: «The {0}» no puede tapar a
            // «The sky over {0} splits with falling fire!».
            HashSet<string> vistas = null;
            Plantilla p = null;
            Match m = null;
            foreach (string w in Palabras(s))
            {
                if (vistas == null) vistas = new HashSet<string>(StringComparer.Ordinal);
                if (!vistas.Add(w)) continue;
                List<Plantilla> l;
                if (!indice.TryGetValue(w, out l)) continue;
                foreach (Plantilla c in l)
                {
                    if (p != null && c.Literal <= p.Literal) continue;
                    if (c.Literal > s.Length) continue;
                    Match mc = c.Re.Match(s);
                    if (mc.Success) { p = c; m = mc; }
                }
            }
            if (p == null) return null;
            var vals = new string[p.Huecos];
            for (int n = 0; n < p.Huecos; n++)
            {
                Group g = m.Groups["h" + n.ToString(CultureInfo.InvariantCulture)];
                string v = g.Success ? g.Value : "";
                if (nivel < 1 && v.Length > 0) v = TraduceSinCache(v, nivel + 1);
                vals[n] = v;
            }
            // Sustitucion en una pasada: un valor que contenga "{1}" no se vuelve a sustituir.
            return Hueco.Replace(p.Destino, h => vals[h.Groups[1].Value[0] - '0']);
        }

        static bool TieneTexto(string s)
        {
            int letras = 0;
            foreach (char c in s) if (char.IsLetter(c) && ++letras >= 3) return true;
            return false;
        }

        // En el fichero, \n es salto de linea y \\ una barra.
        public static string Desescapa(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char d = s[++i];
                    if (d == 'n') sb.Append('\n');
                    else if (d == 't') sb.Append('\t');
                    else if (d == '\\') sb.Append('\\');
                    else sb.Append('\\').Append(d);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static string Escapa(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "");
        }
    }
}
