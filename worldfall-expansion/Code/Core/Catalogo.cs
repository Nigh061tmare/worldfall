using System.Collections.Generic;

namespace WorldfallExpansion.Core
{
    // Contenido nuevo para el JUEGO (no para Worldfall): Worldfall lo usa solo porque lee los catalogos
    // del juego. Las armas y armaduras con coste salen como recetas en su fabricacion (I), y los anillos,
    // amuletos y rasgos «que se pueden dar» en su panel de regalos. Datos puros: el adaptador
    // (Contenido.cs) los registra en AssetManager clonando un objeto del juego, como hace el propio
    // juego en ItemLibrary.
    public sealed class ObjetoDef
    {
        public string Id = "", Base = "";              // Base: objeto del juego que se clona (icono, sprite, tipo)
        public string NombreEs = "", NombreEn = "", DescEs = "", DescEn = "";
        public string Res1 = "", Res2 = "none";
        public int Cant1, Cant2;
        public int Valor;                              // equipment_value (orden y precio)
        public Dictionary<string, float> Stats = new Dictionary<string, float>();

        public ObjetoDef S(string k, float v) { Stats[k] = v; return this; }
    }

    public enum TipoRasgo { Positivo, Negativo, Otro }

    public sealed class RasgoDef
    {
        public string Id = "", NombreEs = "", NombreEn = "", DescEs = "", DescEn = "";
        public string Icono = "", Grupo = "";
        public TipoRasgo Tipo;
        public Dictionary<string, float> Stats = new Dictionary<string, float>();

        public RasgoDef S(string k, float v) { Stats[k] = v; return this; }
    }

    public static class Catalogo
    {
        // Leidos del binario (build 719): ids de ItemLibrary, ResourceLibrary, base_stats y iconos de rasgos.
        public static readonly HashSet<string> BasesValidas = new HashSet<string>
        {
            "sword_steel", "sword_mythril", "sword_adamantine", "bow_silver", "bow_adamantine", "axe_mythril",
            "spear_iron", "hammer_adamantine", "armor_steel", "armor_adamantine", "helmet_leather", "helmet_mythril",
            "boots_leather", "boots_steel", "ring_adamantine", "amulet_silver", "amulet_bone",
        };
        public static readonly HashSet<string> Recursos = new HashSet<string>
        {
            "wood", "stone", "common_metals", "silver", "gold", "gems", "mythril", "adamantine", "bones",
            "leather", "dragon_scales", "herbs",
        };
        public static readonly HashSet<string> StatsValidas = new HashSet<string>
        {
            "damage", "armor", "speed", "critical_chance", "critical_damage_multiplier", "stamina", "mana", "health",
            "range", "accuracy", "diplomacy", "warfare", "stewardship", "intelligence", "lifespan", "loyalty_traits",
        };
        public static readonly HashSet<string> GruposRasgo = new HashSet<string>
        {
            "acquired", "body", "cognitive", "fate", "merits", "mind", "skills", "spirit",
        };
        public static readonly HashSet<string> IconosRasgo = new HashSet<string>
        {
            "iconVeteran", "iconUnlucky", "iconBlessing", "iconDeceitful", "iconBloodlust", "iconParanoid",
            "iconAmbitious", "iconMadness", "iconFast", "iconDragonslayer", "iconWise", "iconHonest",
        };

        static ObjetoDef O(string id, string bas, string es, string en, string des, string den, int valor,
                           string r1, int c1, string r2, int c2)
        {
            return new ObjetoDef { Id = id, Base = bas, NombreEs = es, NombreEn = en, DescEs = des, DescEn = den,
                                   Valor = valor, Res1 = r1, Cant1 = c1, Res2 = r2, Cant2 = c2 };
        }

        public static readonly ObjetoDef[] Objetos =
        {
            // --- Armas (fabricables en Worldfall: I) ---
            O("wfx_espada_obsidiana", "sword_steel", "Espada de obsidiana", "Obsidian sword",
              "Piedra volcánica afilada hasta cortar el aire.", "Volcanic stone honed to cut the air.", 45, "stone", 8, "gems", 1)
              .S("damage", 8).S("critical_chance", 0.1f).S("speed", 1).S("stamina", 5),
            O("wfx_espada_rey_caido", "sword_mythril", "Espada del rey caído", "Fallen king's sword",
              "Perteneció a un rey. Los suyos aún la reconocen.", "It belonged to a king. His people still know it.", 60, "mythril", 1, "gold", 3)
              .S("damage", 9).S("critical_chance", 0.1f).S("diplomacy", 2).S("warfare", 3),
            O("wfx_hacha_enana", "axe_mythril", "Hacha de guerra enana", "Dwarven war axe",
              "Pesada, honesta y brutal.", "Heavy, honest and brutal.", 55, "common_metals", 4, "mythril", 1)
              .S("damage", 10).S("critical_damage_multiplier", 0.3f).S("stamina", 10),
            O("wfx_lanza_cazador", "spear_iron", "Lanza del cazador", "Hunter's spear",
              "Madera curada y punta de hueso: para presas grandes.", "Cured wood and a bone tip: for big game.", 30, "wood", 4, "bones", 3)
              .S("damage", 7).S("range", 1).S("accuracy", 2),
            O("wfx_martillo_trueno", "hammer_adamantine", "Martillo del trueno", "Thunder hammer",
              "Cada golpe suena como una tormenta lejana.", "Every blow sounds like a distant storm.", 75, "adamantine", 1, "gold", 2)
              .S("damage", 12).S("critical_chance", 0.15f).S("critical_damage_multiplier", 0.5f).S("mana", 20),
            O("wfx_arco_elfico", "bow_silver", "Arco élfico", "Elven bow",
              "Ligero como una rama y certero como un halcón.", "Light as a branch, sure as a hawk.", 35, "wood", 5, "silver", 1)
              .S("damage", 6).S("range", 10).S("accuracy", 3),
            O("wfx_arco_hueso_dragon", "bow_adamantine", "Arco de hueso de dragón", "Dragonbone bow",
              "Tensado con tendón de dragón. Pocos pueden siquiera doblarlo.", "Strung with dragon sinew. Few can even bend it.", 70, "bones", 6, "dragon_scales", 1)
              .S("damage", 10).S("range", 12).S("critical_chance", 0.1f),
            // --- Armaduras, cascos y botas (fabricables) ---
            O("wfx_armadura_escamas_dragon", "armor_adamantine", "Armadura de escamas de dragón", "Dragon scale armor",
              "Ni el fuego ni el acero la atraviesan con facilidad.", "Neither fire nor steel pierce it easily.", 80, "dragon_scales", 3, "leather", 2)
              .S("armor", 12).S("health", 40).S("stamina", 5),
            O("wfx_coraza_juramentada", "armor_steel", "Coraza juramentada", "Sworn breastplate",
              "Lleva grabado el juramento de quien la viste.", "Engraved with the oath of whoever wears it.", 45, "common_metals", 5, "gold", 1)
              .S("armor", 8).S("warfare", 2).S("stamina", 5),
            O("wfx_capucha_rastreador", "helmet_leather", "Capucha del rastreador", "Tracker's hood",
              "Huele a bosque. Nadie te oye llegar.", "It smells of forest. No one hears you coming.", 15, "leather", 3, "herbs", 2)
              .S("armor", 2).S("accuracy", 3).S("speed", 2),
            O("wfx_yelmo_heredero", "helmet_mythril", "Yelmo del heredero", "Heir's helm",
              "Hecho para una cabeza que espera una corona.", "Made for a head waiting for a crown.", 55, "mythril", 1, "gold", 2)
              .S("armor", 7).S("diplomacy", 3).S("mana", 15),
            O("wfx_botas_peregrino", "boots_leather", "Botas del peregrino", "Pilgrim's boots",
              "Han pisado más caminos que su dueño.", "They have walked more roads than their owner.", 15, "leather", 3, "wood", 2)
              .S("armor", 2).S("speed", 4).S("stamina", 30),
            O("wfx_grebas_obsidiana", "boots_steel", "Grebas de obsidiana", "Obsidian greaves",
              "Pesan, pero nada las raya.", "Heavy, but nothing scratches them.", 40, "stone", 6, "gems", 1)
              .S("armor", 6).S("stamina", 10),
            // --- Anillos y amuletos (panel de regalos de Worldfall; el juego los reparte en sus ciudades) ---
            O("wfx_anillo_obelisco", "ring_adamantine", "Anillo del obelisco", "Obelisk ring",
              "Tibio al tacto. Zumba cuando hay tormenta.", "Warm to the touch. It hums before a storm.", 75, "gems", 2, "gold", 2)
              .S("mana", 40).S("critical_chance", 0.1f),
            O("wfx_amuleto_pecera", "amulet_silver", "Amuleto de la pecera", "Fishbowl amulet",
              "Quien lo lleva oye los rumores antes que nadie.", "Its wearer hears the rumours before anyone.", 30, "silver", 1, "gems", 1)
              .S("diplomacy", 4).S("stewardship", 2).S("mana", 10),
            O("wfx_amuleto_luto", "amulet_bone", "Amuleto del luto", "Mourning amulet",
              "Tallado con el hueso de alguien a quien se quiso.", "Carved from the bone of someone loved.", 20, "bones", 4, "none", 0)
              .S("mana", 15).S("stamina", 20),
        };

        static RasgoDef R(string id, string es, string en, string des, string den, string icono, string grupo, TipoRasgo tipo)
        {
            return new RasgoDef { Id = id, NombreEs = es, NombreEn = en, DescEs = des, DescEn = den, Icono = icono, Grupo = grupo, Tipo = tipo };
        }

        // Rasgos «que se pueden dar» (panel de Worldfall y del juego). Algunos los pone PeceraWB en Nivel 3.
        public static readonly RasgoDef[] Rasgos =
        {
            R("wfx_juramentado", "Juramentado", "Sworn", "Juró lealtad a su reino y no la rompe.", "Swore loyalty to the realm and keeps it.",
              "iconVeteran", "merits", TipoRasgo.Positivo).S("warfare", 3).S("loyalty_traits", 10),
            R("wfx_maldito", "Maldito", "Cursed", "Algo le persigue. Todo le sale un poco peor.", "Something follows them. Everything goes a bit worse.",
              "iconUnlucky", "fate", TipoRasgo.Negativo).S("critical_chance", -0.05f).S("lifespan", -5),
            R("wfx_bendecido_obelisco", "Bendecido por el Obelisco", "Obelisk-blessed", "Tocó la vieja piedra y la piedra le respondió.", "Touched the old stone and the stone answered.",
              "iconBlessing", "spirit", TipoRasgo.Positivo).S("mana", 30).S("health", 30),
            R("wfx_chismoso", "Chismoso", "Gossip", "Sabe de todos y lo cuenta todo.", "Knows everyone's business and tells it.",
              "iconDeceitful", "mind", TipoRasgo.Otro).S("diplomacy", -2).S("intelligence", 2),
            R("wfx_vengador", "Vengador", "Avenger", "Vive para saldar una deuda de sangre.", "Lives to settle a blood debt.",
              "iconBloodlust", "mind", TipoRasgo.Positivo).S("damage", 3).S("critical_chance", 0.05f),
            R("wfx_desterrado", "Desterrado", "Exile", "Le echaron de su ciudad. No lo olvida.", "Driven from their town. They don't forget.",
              "iconParanoid", "acquired", TipoRasgo.Negativo).S("diplomacy", -3).S("speed", 2),
            R("wfx_pretendiente", "Pretendiente", "Claimant", "Cree que una corona le pertenece.", "Believes a crown is theirs by right.",
              "iconAmbitious", "merits", TipoRasgo.Otro).S("diplomacy", 2).S("warfare", 2),
            R("wfx_corazon_roto", "Corazón roto", "Heartbroken", "Perdió a quien amaba.", "Lost the one they loved.",
              "iconMadness", "spirit", TipoRasgo.Negativo).S("stamina", -10).S("mana", 10),
            R("wfx_peregrino", "Peregrino", "Pilgrim", "Ha caminado hasta los lugares santos.", "Has walked to the holy places.",
              "iconFast", "acquired", TipoRasgo.Positivo).S("speed", 3).S("stamina", 20),
            R("wfx_cazador_bestias", "Cazador de bestias", "Beast hunter", "Ha abatido a lo que asustaba a los niños.", "Has felled what frightened the children.",
              "iconDragonslayer", "skills", TipoRasgo.Positivo).S("damage", 2).S("accuracy", 3),
            R("wfx_mentor", "Mentor", "Mentor", "Enseña a otros lo que sabe.", "Teaches others what they know.",
              "iconWise", "cognitive", TipoRasgo.Positivo).S("intelligence", 3).S("stewardship", 2),
            R("wfx_voz_del_pueblo", "Voz del pueblo", "Voice of the people", "La gente le escucha y le sigue.", "People listen to them and follow.",
              "iconHonest", "merits", TipoRasgo.Positivo).S("diplomacy", 4).S("stewardship", 2),
        };

        // Claves de idioma (las mismas que calcula el juego: ItemAsset.getLocaleID / BaseTrait.getLocaleID).
        public static string ClaveObjeto(ObjetoDef o) { return "item_" + o.Id; }
        public static string ClaveObjetoDesc(ObjetoDef o) { return o.Id + "_description"; }
        public static string ClaveRasgo(RasgoDef r) { return "trait_" + r.Id; }
        public static string ClaveRasgoDesc(RasgoDef r) { return "trait_" + r.Id + "_info"; }

        // Todas las cadenas para Locales/<idioma>.json (es / en).
        public static SortedDictionary<string, string> Textos(bool espanol)
        {
            var d = new SortedDictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var o in Objetos)
            {
                d[ClaveObjeto(o)] = espanol ? o.NombreEs : o.NombreEn;
                d[ClaveObjetoDesc(o)] = espanol ? o.DescEs : o.DescEn;
            }
            foreach (var r in Rasgos)
            {
                d[ClaveRasgo(r)] = espanol ? r.NombreEs : r.NombreEn;
                d[ClaveRasgoDesc(r)] = espanol ? r.DescEs : r.DescEn;
            }
            return d;
        }

        // Errores de datos (vacio = todo bien). Lo comprueban los tests y el adaptador al arrancar.
        public static List<string> Valida()
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            foreach (var o in Objetos)
            {
                if (!o.Id.StartsWith("wfx_")) e.Add(o.Id + ": sin prefijo wfx_");
                if (!ids.Add(o.Id)) e.Add(o.Id + ": repetido");
                if (!BasesValidas.Contains(o.Base)) e.Add(o.Id + ": base desconocida " + o.Base);
                if (!Recursos.Contains(o.Res1) || o.Cant1 <= 0) e.Add(o.Id + ": recurso 1 invalido");
                if (o.Res2 != "none" && (!Recursos.Contains(o.Res2) || o.Cant2 <= 0)) e.Add(o.Id + ": recurso 2 invalido");
                if (o.NombreEs.Length == 0 || o.NombreEn.Length == 0 || o.DescEs.Length == 0) e.Add(o.Id + ": faltan textos");
                foreach (var kv in o.Stats) if (!StatsValidas.Contains(kv.Key)) e.Add(o.Id + ": stat desconocida " + kv.Key);
            }
            foreach (var r in Rasgos)
            {
                if (!r.Id.StartsWith("wfx_")) e.Add(r.Id + ": sin prefijo wfx_");
                if (!ids.Add(r.Id)) e.Add(r.Id + ": repetido");
                if (!IconosRasgo.Contains(r.Icono)) e.Add(r.Id + ": icono desconocido " + r.Icono);
                if (!GruposRasgo.Contains(r.Grupo)) e.Add(r.Id + ": grupo desconocido " + r.Grupo);
                if (r.NombreEs.Length == 0 || r.NombreEn.Length == 0 || r.DescEs.Length == 0) e.Add(r.Id + ": faltan textos");
                foreach (var kv in r.Stats) if (!StatsValidas.Contains(kv.Key)) e.Add(r.Id + ": stat desconocida " + kv.Key);
            }
            return e;
        }
    }
}
