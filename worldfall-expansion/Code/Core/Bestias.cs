using System.Collections.Generic;

namespace WorldfallExpansion.Core
{
    // Bestias legendarias: criaturas QUE YA EXISTEN en el juego (Worldfall tiene modelo 3D para todas
    // estas) pero mas grandes, fuertes y con nombre propio. Se sueltan con el poder «Bestia legendaria»
    // y abren una busqueda de caza. Datos y nombres deterministas: sin Unity.
    public sealed class BestiaDef
    {
        public string Especie = "";            // id de ActorAsset del juego
        public string NombreEs = "";           // «Lobo», «Oso»...
        public bool Femenino;
    }

    public static class Bestias
    {
        // Especies con modelo 3D propio en Worldfall 0.9.2 (FirstPerson.species.*.bin) y ActorAsset en la build 719.
        public static readonly BestiaDef[] Especies =
        {
            new BestiaDef { Especie = "wolf", NombreEs = "Lobo" },
            new BestiaDef { Especie = "bear", NombreEs = "Oso" },
            new BestiaDef { Especie = "crocodile", NombreEs = "Cocodrilo" },
            new BestiaDef { Especie = "rhino", NombreEs = "Rinoceronte" },
            new BestiaDef { Especie = "snake", NombreEs = "Serpiente", Femenino = true },
            new BestiaDef { Especie = "hyena", NombreEs = "Hiena", Femenino = true },
            new BestiaDef { Especie = "scorpion", NombreEs = "Escorpión" },
            new BestiaDef { Especie = "buffalo", NombreEs = "Búfalo" },
        };

        // Rasgos del juego (ActorTraitLibrary build 719) + del Arsenal que hacen «legendaria» a la bestia.
        public static readonly string[] Rasgos = { "giant", "strong", "tough", "regeneration", "bloodlust", "wfx_vengador" };

        static readonly string[] AdjM = { "Blanco", "Negro", "Rojo", "Viejo", "Tuerto", "Gris", "Sombrío", "Hambriento" };
        static readonly string[] AdjF = { "Blanca", "Negra", "Roja", "Vieja", "Tuerta", "Gris", "Sombría", "Hambrienta" };
        static readonly string[] Lugares = { "de las Colinas", "del Pantano", "de la Niebla", "del Norte", "de los Huesos",
                                             "del Río Seco", "de la Ceniza", "del Bosque Hondo" };

        public static BestiaDef Elige(int semilla)
        {
            return Especies[Mod(semilla, Especies.Length)];
        }

        // «El Lobo Blanco de las Colinas», «La Hiena Tuerta del Pantano».
        public static string Nombre(BestiaDef b, int semilla)
        {
            string adj = b.Femenino ? AdjF[Mod(semilla / 7, AdjF.Length)] : AdjM[Mod(semilla / 7, AdjM.Length)];
            return (b.Femenino ? "La " : "El ") + b.NombreEs + " " + adj + " " + Lugares[Mod(semilla / 61, Lugares.Length)];
        }

        static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

        // Busqueda de caza para una bestia recien soltada.
        public static Busqueda Caza(string id, string nombre, string reinoCercano)
        {
            return new Busqueda
            {
                Tipo = TipoBusqueda.Bestia, Clave = "bestia|" + id,
                Objetivo = id, ObjetivoNombre = nombre,
                Titulo = "Caza: " + nombre,
                Descripcion = nombre + " ronda cerca de " + (string.IsNullOrEmpty(reinoCercano) ? "tierras salvajes" : reinoCercano)
                              + ". Sigue a la bestia y abátela.",
                Prioridad = 8,
            };
        }
    }

    // Poderes de dios nuevos (pestaña propia). Datos y textos; el adaptador (Poderes.cs) pone los efectos.
    public sealed class PoderDef
    {
        public string Id = "", NombreEs = "", NombreEn = "", DescEs = "", DescEn = "", Icono = "";
    }

    public static class Poderes
    {
        public const string Pestana = "wfx_tab";

        // Icono: ruta de un sprite del juego (los de rasgos, leidos del binario).
        public static readonly PoderDef[] Lista =
        {
            new PoderDef { Id = "wfx_bestia", NombreEs = "Bestia legendaria", NombreEn = "Legendary beast",
                DescEs = "Suelta una bestia enorme con nombre propio. Abre una búsqueda de caza.",
                DescEn = "Release a huge named beast. Opens a hunt quest.", Icono = "ui/Icons/actor_traits/iconDragonslayer" },
            new PoderDef { Id = "wfx_bendicion", NombreEs = "Bendición del Obelisco", NombreEn = "Obelisk's blessing",
                DescEs = "Bendice a la persona que toques: maná, vida y renombre. La pecera lo recordará.",
                DescEn = "Bless the person you touch: mana, health and renown. The fishbowl will remember.", Icono = "ui/Icons/actor_traits/iconBlessing" },
            new PoderDef { Id = "wfx_maldicion", NombreEs = "Maldición", NombreEn = "Curse",
                DescEs = "Maldice a la persona que toques. Todo le saldrá un poco peor.",
                DescEn = "Curse the person you touch. Everything goes a bit worse for them.", Icono = "ui/Icons/actor_traits/iconUnlucky" },
            new PoderDef { Id = "wfx_juramento", NombreEs = "Juramento", NombreEn = "Oath",
                DescEs = "La persona que toques jura lealtad a su reino.",
                DescEn = "The person you touch swears loyalty to their realm.", Icono = "ui/Icons/actor_traits/iconVeteran" },
            new PoderDef { Id = "wfx_destierro", NombreEs = "Destierro divino", NombreEn = "Divine exile",
                DescEs = "Expulsa de su ciudad a la persona que toques (nunca a un rey).",
                DescEn = "Drive the person you touch out of their town (never a king).", Icono = "ui/Icons/actor_traits/iconParanoid" },
            new PoderDef { Id = "wfx_discordia", NombreEs = "Sembrar discordia", NombreEn = "Sow discord",
                DescEs = "La persona que toques se pelea con su mejor amigo (en la pecera: agravio y rumores).",
                DescEn = "The person you touch falls out with their best friend (fishbowl: grievance and rumours).", Icono = "ui/Icons/actor_traits/iconDeceitful" },
            new PoderDef { Id = "wfx_reconciliacion", NombreEs = "Reconciliar", NombreEn = "Reconcile",
                DescEs = "La persona que toques hace las paces con su rival (en la pecera).",
                DescEn = "The person you touch makes peace with their rival (in the fishbowl).", Icono = "ui/Icons/actor_traits/iconHonest" },
        };
    }
}
