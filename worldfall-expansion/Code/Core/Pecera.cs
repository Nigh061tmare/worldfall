using System.Text;
using System.Text.RegularExpressions;

namespace WorldfallExpansion.Core
{
    // Lee la cronica de PeceraWB (mundos\<mundo>\cronica.jsonl, una linea por hito:
    // {"d":dia,"t":"tipo","x":"texto","p":peso}) y convierte algunos hitos en busquedas.
    // No depende del DLL de la pecera, solo del formato de su fichero. Los textos son los que escribe
    // PeceraWB/Code/Hooks.cs y Nivel3.cs (ver tests): si cambian, la linea se ignora sin mas.
    public static class LectorPecera
    {
        static readonly Regex Regicidio = new Regex(@"^(?<a>.+?) mato a (?<v>.+), rey de (?<r>.+)$", RegexOptions.CultureInvariant);
        static readonly Regex Destierro = new Regex(@"^(?<n>.+?) es desterrado: (?<m>.+)$", RegexOptions.CultureInvariant);
        static readonly Regex Disputa = new Regex(@"^Sucede a (?<d>.+?) en (?<r>.+?): (?<s>.+) \(disputado con (?<rv>.+)\)$", RegexOptions.CultureInvariant);

        public static bool Lee(string linea, out string tipo, out string texto)
        {
            tipo = Campo(linea, "t");
            texto = Campo(linea, "x");
            return tipo != null && texto != null;
        }

        // Busqueda a partir de un hito, o null si no aplica o los nombres no se resuelven en la foto.
        public static Busqueda Desde(string tipo, string texto, FotoMundo f)
        {
            if (f == null || texto == null) return null;
            Match m;
            switch (tipo)
            {
                case "regicidio":
                {
                    m = Regicidio.Match(texto);
                    if (!m.Success) return null;
                    FotoUnidad a = f.PorNombre(m.Groups["a"].Value);
                    if (a == null) return null;
                    string v = m.Groups["v"].Value, r = m.Groups["r"].Value;
                    return new Busqueda
                    {
                        Tipo = TipoBusqueda.Venganza, Clave = "venganza|" + a.Id,
                        Objetivo = a.Id, ObjetivoNombre = a.Nombre, OtroNombre = v,
                        Titulo = "Venganza por " + v,
                        Descripcion = a.Nombre + " mato a " + v + ", rey de " + r + ". La pecera pide justicia: vive en "
                            + f.NombreReino(a.Reino) + ".",
                        Prioridad = 8,
                    };
                }
                case "destierro":
                {
                    m = Destierro.Match(texto);
                    if (!m.Success) return null;
                    FotoUnidad n = f.PorNombre(m.Groups["n"].Value);
                    if (n == null) return null;
                    return new Busqueda
                    {
                        Tipo = TipoBusqueda.Destierro, Clave = "destierro|" + n.Id,
                        Objetivo = n.Id, ObjetivoNombre = n.Nombre,
                        Titulo = "El desterrado " + n.Nombre,
                        Descripcion = n.Nombre + " ha sido desterrado (" + m.Groups["m"].Value + "). ¿Encontrara un nuevo hogar?",
                        Prioridad = 5,
                    };
                }
                case "sucesion":
                {
                    m = Disputa.Match(texto);
                    if (!m.Success) return null;
                    FotoUnidad rv = f.PorNombre(m.Groups["rv"].Value);
                    if (rv == null) return null;
                    string r = m.Groups["r"].Value, s = m.Groups["s"].Value;
                    return new Busqueda
                    {
                        Tipo = TipoBusqueda.Pretendiente, Clave = "pretendiente|" + rv.Id,
                        Objetivo = rv.Id, ObjetivoNombre = rv.Nombre, OtroNombre = s,
                        Titulo = "El pretendiente de " + r,
                        Descripcion = rv.Nombre + " perdio el trono de " + r + " frente a " + s + ". No se rinde: ¿conseguira una corona?",
                        Prioridad = 7,
                    };
                }
            }
            return null;
        }

        // Valor de texto de un campo de una linea JSON plana (con escapes \" \\ \n \uXXXX). null si no esta.
        public static string Campo(string linea, string nombre)
        {
            if (linea == null) return null;
            string k = "\"" + nombre + "\":";
            int i = linea.IndexOf(k, System.StringComparison.Ordinal);
            if (i < 0) return null;
            i += k.Length;
            while (i < linea.Length && linea[i] == ' ') i++;
            if (i >= linea.Length || linea[i] != '"') return null;
            var sb = new StringBuilder();
            for (i++; i < linea.Length; i++)
            {
                char c = linea[i];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (++i >= linea.Length) return null;
                char e = linea[i];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'u':
                        if (i + 4 >= linea.Length) return null;
                        int cp;
                        if (!int.TryParse(linea.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out cp)) return null;
                        sb.Append((char)cp); i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            return null;   // cadena sin cerrar
        }
    }
}
