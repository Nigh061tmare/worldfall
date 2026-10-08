using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Configuracion en config.txt (clave=valor). Se crea con valores por defecto.
    internal static class Cfg
    {
        static readonly Dictionary<string, string> d = new Dictionary<string, string>();
        static readonly string[][] Defecto =
        {
            new[] { "activo", "1" },
            new[] { "dia_segundos", "2" },          // segundos de mundo que cuentan como un "dia" de la pecera
            new[] { "muestra_seg", "2" },           // cada cuanto se observan unidades (tiempo de mundo)
            new[] { "muestra_n", "60" },            // unidades observadas por pasada
            new[] { "avisos", "1" },                // mostrar hitos importantes arriba
            new[] { "aviso_peso", "5" },            // peso minimo del hito para avisar
            new[] { "voz", "0" },                   // frases de TODOS los eventos notables con LLM local (F10 lo alterna)
            new[] { "voz_grandes", "1" },           // aun con voz=0: guerras, juicios de reyes y facciones grandes
            new[] { "ollama", "http://127.0.0.1:11434" },
            new[] { "modelo", "qwen4b-silly:latest" },
            new[] { "voz_gap_seg", "25" },
            new[] { "olvido_factor", "3" },         // >1: el afecto se enfria mas despacio (el mundo tiene mucha gente y poco roce)
            new[] { "faccion_umbral", "0.08" },     // afecto mutuo minimo para que dos personas cuenten como aliadas
            new[] { "silencio_seg", "90" },         // tras cargar un mundo no se anuncian parejas ya existentes
            new[] { "nivel3", "0" },                // consecuencias reales (destierros, disputas): ESCRIBE en el mundo, apagado por defecto
            new[] { "nivel3_max_hora", "6" },       // tope de acciones reales por hora de mundo
            new[] { "nivel3_herencia", "0" },       // +renombre al heredero (desactivado: Worldsmith ya lo hace)
            new[] { "juicios", "1" },               // juicios simulados (solo relato y afectos del modelo)
            new[] { "informe_tecla", "F9" },
            new[] { "voz_tecla", "F10" },
            new[] { "exporta_social", "1" },        // estado_social.jsonl para worldfall-expansion (solo carpeta de la pecera)
            new[] { "intervenciones", "1" },        // lee los poderes de dios de worldfall-expansion (cronica y afectos)
            new[] { "nivel3_guerras", "0" },        // ESCRIBE (con nivel3=1): reyes que se odian entran en guerra
            new[] { "nivel3_guerra_odio", "0.5" },  // sentimiento <= -esto para declarar la guerra
            new[] { "nivel3_guerras_civiles", "0" },// ESCRIBE (con nivel3=1): facciones enfrentadas -> rebelion de una ciudad
            new[] { "nivel3_rasgos", "1" },         // con nivel3=1: rasgos del Arsenal (desterrado, pretendiente, corazon roto)
        };

        public static void Init(string dir)
        {
            d.Clear();
            foreach (var kv in Defecto) d[kv[0]] = kv[1];
            string f = Path.Combine(dir, "config.txt");
            if (File.Exists(f))
            {
                foreach (string l in File.ReadAllLines(f, Encoding.UTF8))
                {
                    string s = l.Trim();
                    if (s.Length == 0 || s[0] == '#') continue;
                    int i = s.IndexOf('=');
                    if (i > 0) d[s.Substring(0, i).Trim()] = s.Substring(i + 1).Trim();
                }
            }
            var sb = new StringBuilder("# PeceraWB: edita con el juego cerrado\n");
            foreach (var kv in Defecto) sb.Append(kv[0]).Append('=').Append(d[kv[0]]).Append('\n');
            File.WriteAllText(f, sb.ToString(), new UTF8Encoding(false));
        }

        public static string Str(string k) { string v; return d.TryGetValue(k, out v) ? v : ""; }
        public static bool Bool(string k) { return Str(k) == "1" || Str(k).ToLowerInvariant() == "true"; }
        public static double Num(string k)
        {
            double v;
            return double.TryParse(Str(k), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0;
        }
        public static void Set(string k, string v) { d[k] = v; }
    }

    // Estado de la simulacion social del mundo cargado. Un directorio por mundo.
    internal static partial class Mundo
    {
        public const string Version = "0.1.0";
        public static bool Activo;
        public static string Dir = "", Clave = "";
        public static IStorage Disco;
        public static IClock Reloj = new SystemClock();
        public static Rng Azar = new Rng(1234);
        public static ModeloAfectivo Afectos;
        public static RedSecretos Secretos;
        public static Cronica Cronica;
        public static Cultura Cultura;
        public static Suenos Suenos;
        public static Tribunal Tribunal;
        public static Memoria Memoria;
        public static Linaje Arbol = new Linaje();
        public static Mentoria Ensena = new Mentoria();

        public static readonly Dictionary<string, Ficha> Fichas = new Dictionary<string, Ficha>();
        public static readonly Dictionary<string, string> Nombres = new Dictionary<string, string>();
        public static readonly Dictionary<string, Actor> Vivos = new Dictionary<string, Actor>();
        static readonly ConcurrentQueue<Action> principal = new ConcurrentQueue<Action>();

        static double ultimoWt;
        internal static double silencioHasta;
        static float tClave, tGuarda;
        static double tMuestra, tDia, tFacc;
        static bool inicializado;

        public static void Inicia()
        {
            Dir = Path.Combine(Application.persistentDataPath, "PeceraWB");
            Directory.CreateDirectory(Dir);
            Cfg.Init(Dir);
            Activo = Cfg.Bool("activo");
            if (!Activo) { Debug.Log("[PeceraWB] activo=0: inerte"); return; }
            Construye("sin_mundo");
            VozLlm.Arranca();
        }

        // Nunca devuelve null: sin ficha guardada se genera una determinista (misma id -> misma personalidad).
        public static Ficha FichaDe(string id)
        {
            Ficha f;
            if (Fichas.TryGetValue(id, out f)) return f;
            string n; Nombres.TryGetValue(id, out n);
            f = FichaGen.Determinista(id, n ?? id);
            Fichas[id] = f;
            return f;
        }

        public static string NombreDe(string id) { string n; return Nombres.TryGetValue(id, out n) ? n : id; }

        public static string Id(Actor a) { return "a" + a.data.id; }

        static void Construye(string clave)
        {
            Clave = clave;
            string seguro = clave;
            foreach (char c in Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
            Disco = new DiskStorage(Path.Combine(Dir, "mundos", seguro), 4 * 1024 * 1024);
            Fichas.Clear(); Nombres.Clear(); Vivos.Clear();
            Afectos = new ModeloAfectivo(id => FichaDe(id).Rencor);
            Azar = new Rng(FichaGen.Hash(clave));
            Secretos = new RedSecretos(Afectos, FichaDe, Reloj, Azar);
            Cronica = new Cronica { DiasPorTemporada = 7 };
            Cultura = new Cultura();
            Suenos = new Suenos();
            Tribunal = new Tribunal(Afectos);
            Memoria = new Memoria(Disco, Reloj);
            facConocidas.Clear(); tensionesConocidas.Clear(); parejas.Clear(); parejasT.Clear(); convT.Clear(); amigos.Clear();
            muertos.Clear(); facDe.Clear();
            Arbol = new Linaje();
            Ensena = new Mentoria { MaxAprendicesPorMentor = 2, Ritmo = 0.02 };
            ultimoCredo = "";
            Carga();
            Arbol.CargarAristas(new List<string>(Disco.ReadLines("linaje.txt")));
            Debug.Log("[PeceraWB] linaje: " + Arbol.Aristas().Count + " padres registrados");
            tMuestra = tDia = tFacc = ultimoWt;
            silencioHasta = ultimoWt + Cfg.Num("silencio_seg");
        }

        static void Carga()
        {
            foreach (string l in Disco.ReadLines("fichas.jsonl"))
            {
                Ficha f = Ficha.FromJson(l);
                if (f != null) { Fichas[f.Id] = f; Nombres[f.Id] = f.Nombre; }
            }
            Afectos.Carga(Disco.ReadLines("afectos.jsonl"));
            foreach (string l in Disco.ReadLines("cronica.jsonl"))
            {
                object o;
                if (!Json.TryParse(l, out o)) continue;
                var h = new Hito { Dia = (int)Json.Num(o, "d", 0), Tipo = Json.Str(o, "t"), Texto = Json.Str(o, "x"), Peso = Json.Num(o, "p", 1) };
                Cronica.Hitos.Add(h);
                Cultura.Observa(h);
            }
            Debug.Log("[PeceraWB] mundo '" + Clave + "': " + Fichas.Count + " fichas, " + Afectos.Pares + " relaciones, " + Cronica.Count + " hitos");
        }

        public static void Guarda()
        {
            if (Disco == null) return;
            var fl = new List<string>();
            foreach (var f in Fichas.Values) fl.Add(f.ToJson());
            Disco.Rewrite("fichas.jsonl", fl);
            Disco.Rewrite("afectos.jsonl", Afectos.Serializa());
            try { Memoria.Compacta(); } catch (Exception) { }
            try { Disco.Rewrite("linaje.txt", Arbol.AJSONL()); } catch (Exception) { }
        }

        public static int Dia() { return (int)(ultimoWt / Math.Max(0.5, Cfg.Num("dia_segundos"))); }

        // Anota un hito en la cronica (y en disco). Si pesa mucho, se avisa en pantalla.
        public static void Hito(string tipo, string texto, double peso)
        {
            var h = new Hito { Dia = Dia(), Tipo = tipo, Texto = Json.UnaLinea(texto), Peso = peso };
            Cronica.Anota(h.Dia, h.Tipo, h.Texto, h.Peso);
            Cultura.Observa(h);
            Disco.Append("cronica.jsonl", "{\"d\":" + h.Dia + ",\"t\":\"" + Json.Escape(tipo) + "\",\"x\":\"" + Json.Escape(h.Texto) + "\",\"p\":" + Json.Num(peso) + "}");
            if (Cfg.Bool("avisos") && peso >= Cfg.Num("aviso_peso")) Aviso(h.Texto, 3f);
        }

        // Texto breve arriba de la pantalla (mismo mecanismo que usan los mods del juego).
        public static void Aviso(string texto, float segundos)
        {
            try { WorldTip.showNow(texto, false, "top", segundos); } catch (Exception) { }
        }

        public static void EnPrincipal(Action a) { principal.Enqueue(a); }

        static readonly Dictionary<string, int> fallos = new Dictionary<string, int>();

        // Un fallo del adaptador nunca debe tumbar el juego ni llenar el log: se avisa 3 veces por origen.
        public static void Fallo(string donde, Exception e)
        {
            int n; fallos.TryGetValue(donde, out n);
            fallos[donde] = ++n;
            if (n <= 3) Debug.LogWarning("[PeceraWB] " + donde + ": " + e.GetType().Name + " " + e.Message + (n == 3 ? " (no se repetira)" : ""));
        }

        static string ClaveMundo()
        {
            try
            {
                var ms = Traverse.Create(World.world).Field("map_stats");
                string nombre = ms.Field("name").GetValue<string>() ?? "";
                object id = ms.Field("id").GetValue<object>();
                string k = (nombre + "_" + (id ?? "")).Trim('_');
                return k.Length > 0 ? k : "mundo";
            }
            catch (Exception) { return "mundo"; }
        }

        public static void Tick()
        {
            if (!Activo || World.world == null) return;
            Action a;
            while (principal.TryDequeue(out a)) { try { a(); } catch (Exception e) { Fallo("cola", e); } }
            Teclas();

            double wt = World.world.getCurWorldTime();
            float real = Time.unscaledTime;

            if (real - tClave > 5f)
            {
                tClave = real;
                string k = ClaveMundo();
                // Primera vez: se adopta el mundo. Si cambia (otra partida) o el reloj de mundo retrocede: se recarga.
                if (!inicializado) { inicializado = true; ultimoWt = wt; if (k != Clave) Construye(k); }
                else if (k != Clave || wt < ultimoWt - 5) { Guarda(); ultimoWt = wt; Construye(k); }
            }
            if (!inicializado) return;
            ultimoWt = wt;

            if (wt - tMuestra >= Cfg.Num("muestra_seg")) { tMuestra = wt; Muestrea(); }
            if (wt - tDia >= 5)
            {
                Afectos.Avanza((wt - tDia) / (Math.Max(0.5, Cfg.Num("dia_segundos")) * Math.Max(1, Cfg.Num("olvido_factor"))));
                tDia = wt;
            }
            if (wt - tFacc >= 45) { tFacc = wt; Facciones(); }
            Mentorias(wt);
            TickExporta();
            TickIntervenciones();
            TickNivel3Guerras(wt);
            if (real - tGuarda >= 60f) { tGuarda = real; Guarda(); }
            if (real - tDiag >= 45f) { tDiag = real; Diagnostico(wt); }
        }

        static float tDiag;

        // Una linea de estado en el log del juego: permite comprobar de un vistazo que la simulacion vive.
        static void Diagnostico(double wt)
        {
            int total = 0, sapientes = 0;
            try
            {
                var u = World.world.units != null ? World.world.units.getSimpleList() : null;
                if (u != null) { total = u.Count; foreach (Actor a in u) if (Valido(a)) sapientes++; }
            }
            catch (Exception) { }
            Debug.Log("[PeceraWB] estado: t=" + (int)wt + "s unidades=" + total + " sapientes=" + sapientes + " fichas=" + Fichas.Count
                + " relaciones=" + Afectos.Pares + " hitos=" + Cronica.Count + " facciones=" + UltimasFacciones.Count);
        }

        static void Teclas()
        {
            if (Input.GetKeyDown(Tecla(Cfg.Str("informe_tecla"), KeyCode.F9)))
            {
                string f = EscribeInforme();
                Aviso("Pecera: informe en " + f, 3f);
            }
            if (Input.GetKeyDown(Tecla(Cfg.Str("voz_tecla"), KeyCode.F10)))
            {
                bool on = !Cfg.Bool("voz");
                Cfg.Set("voz", on ? "1" : "0");
                Aviso("Pecera: voz " + (on ? "activada" : "desactivada"), 2f);
            }
        }

        static KeyCode Tecla(string s, KeyCode defecto)
        {
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), s, true); } catch (Exception) { return defecto; }
        }
    }
}
