using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pecera.Core;

namespace PeceraWB
{
    internal static partial class Mundo
    {
        // Informe legible del mundo: leyendas, facciones con lider, chismosos, credo y salud del modelo.
        public static string EscribeInforme()
        {
            var sb = new StringBuilder();
            sb.Append(Cronica.ToMarkdown("este mundo")).Append('\n');

            sb.Append("## Facciones\n\n");
            if (UltimasFacciones.Count == 0) sb.Append("Aun no hay facciones: hacen falta relaciones sostenidas.\n");
            foreach (var f in UltimasFacciones)
                sb.Append("- **").Append(f.Nombre).Append("** — ").Append(f.Miembros.Count).Append(" miembros, cohesion ")
                  .Append(Json.Num(f.Cohesion)).Append(", lider ").Append(NombreDe(f.Lider)).Append('\n');

            sb.Append("\n## Cultura\n\n");
            sb.Append("Credo: ").Append(Cultura.Credo()).Append('\n');
            foreach (var kv in Cultura.Reparto()) sb.Append("- ").Append(kv.Key).Append(": ").Append((int)(kv.Value * 100)).Append(" %\n");

            sb.Append("\n## Red de chismes\n\n");
            foreach (var kv in Secretos.TopChismosos(5)) sb.Append("- ").Append(NombreDe(kv.Key)).Append(": ").Append(kv.Value).Append(" fugas\n");
            sb.Append("Secretos en juego: ").Append(Secretos.Count).Append(", fugas totales: ").Append(Secretos.Fugas.Count).Append('\n');

            sb.Append("\n## Sueños\n\nCumplidos: ").Append(Suenos.Cumplidos).Append(" de ").Append(Suenos.Total).Append('\n');

            sb.Append("\n## Modelo\n\nPersonajes con ficha: ").Append(Fichas.Count).Append(", relaciones activas: ").Append(Afectos.Pares)
              .Append(", hitos: ").Append(Cronica.Count).Append('\n');

            string ruta = Path.Combine(Dir, "informe.md");
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
            return ruta;
        }
    }
}
