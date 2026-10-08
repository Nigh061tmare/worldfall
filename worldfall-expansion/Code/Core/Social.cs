using System.Collections.Generic;

namespace WorldfallExpansion.Core
{
    // Estado social de un personaje segun PeceraWB (mundos\<mundo>\estado_social.jsonl, lo escribe
    // PeceraWB/Code/Exporta.cs). Lo usa el HUD social en primera persona.
    public sealed class EstadoSocial
    {
        public string Id = "", Nombre = "", Casa = "", Rasgos = "", Amigo = "", Rival = "", Sueno = "", Rumor = "";

        public static EstadoSocial Lee(string linea)
        {
            string id = LectorPecera.Campo(linea, "id");
            if (string.IsNullOrEmpty(id)) return null;
            return new EstadoSocial
            {
                Id = id,
                Nombre = LectorPecera.Campo(linea, "n") ?? "",
                Casa = LectorPecera.Campo(linea, "casa") ?? "",
                Rasgos = LectorPecera.Campo(linea, "rasgos") ?? "",
                Amigo = LectorPecera.Campo(linea, "amigo") ?? "",
                Rival = LectorPecera.Campo(linea, "rival") ?? "",
                Sueno = LectorPecera.Campo(linea, "sueno") ?? "",
                Rumor = LectorPecera.Campo(linea, "rumor") ?? "",
            };
        }

        public static Dictionary<string, EstadoSocial> LeeTodo(IEnumerable<string> lineas)
        {
            var d = new Dictionary<string, EstadoSocial>();
            if (lineas == null) return d;
            foreach (string l in lineas)
            {
                EstadoSocial e = Lee(l);
                if (e != null) d[e.Id] = e;
            }
            return d;
        }

        // Dos o tres lineas para el HUD. Vacio si la pecera no sabe nada interesante.
        public string Texto()
        {
            var l1 = new List<string>();
            if (Casa.Length > 0) l1.Add(Casa);
            if (Rasgos.Length > 0) l1.Add(Rasgos);
            var l2 = new List<string>();
            if (Amigo.Length > 0) l2.Add("Amigo: " + Amigo);
            if (Rival.Length > 0) l2.Add("Rival: " + Rival);
            if (Sueno.Length > 0) l2.Add("Sueño: " + Sueno);
            if (l1.Count == 0 && l2.Count == 0 && Rumor.Length == 0) return "";
            string s = Nombre + (l1.Count > 0 ? " — " + string.Join(" · ", l1.ToArray()) : "");
            if (l2.Count > 0) s += "\n" + string.Join(" · ", l2.ToArray());
            // La pecera guarda el rumor sin sujeto («se acuesta con X»): se antepone el nombre.
            if (Rumor.Length > 0) s += "\nSe dice que " + Nombre + " " + Corta(Rumor, 90);
            return s;
        }

        static string Corta(string s, int max) { return s.Length <= max ? s : s.Substring(0, max - 1) + "…"; }
    }
}
