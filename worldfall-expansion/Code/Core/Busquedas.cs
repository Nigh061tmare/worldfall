using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldfallExpansion.Core
{
    public enum TipoBusqueda { Trono, Huerfano, Ciudad, Amantes, Venganza, Destierro, Pretendiente }

    public enum EstadoBusqueda { Activa, Cumplida, Fallida, Caducada }

    // Una busqueda a seguir: un personaje concreto del mundo (Objetivo) y lo que tiene que pasar.
    public sealed class Busqueda
    {
        public int Num;
        public TipoBusqueda Tipo;
        public EstadoBusqueda Estado = EstadoBusqueda.Activa;
        public string Clave = "";            // evita repetir la misma busqueda
        public string Objetivo = "";         // id de la unidad a seguir
        public string ObjetivoNombre = "";
        public string Otro = "";             // segundo personaje (amante) o difunto
        public string OtroNombre = "";
        public string Reino;                 // clave del reino implicado (trono)
        public string Ciudad;                // clave de la ciudad (ciudad)
        public string Titulo = "", Descripcion = "", Final = "";
        public double Inicio, Fin;
        public int Prioridad;
        public bool Visto;                   // destierro: ya se le vio sin ciudad

        // Persistencia entre sesiones (busquedas_activas.jsonl). Reino/Ciudad son claves de sesion: no se
        // guardan; el trono se vuelve a enlazar por el nombre del reino (OtroNombre).
        public string ToJson()
        {
            var sb = new StringBuilder("{");
            sb.Append("\"n\":").Append(Num);
            sb.Append(",\"tipo\":\"").Append(Tipo.ToString()).Append('"');
            sb.Append(",\"ini\":").Append(Inicio.ToString("0.#", CultureInfo.InvariantCulture));
            sb.Append(",\"prio\":").Append(Prioridad);
            sb.Append(",\"visto\":").Append(Visto ? 1 : 0);
            foreach (var kv in new[] { new[] { "clave", Clave }, new[] { "obj", Objetivo }, new[] { "objn", ObjetivoNombre },
                                       new[] { "otro", Otro }, new[] { "otron", OtroNombre }, new[] { "tit", Titulo }, new[] { "desc", Descripcion } })
                sb.Append(",\"").Append(kv[0]).Append("\":\"").Append(Texto_.Escape(kv[1])).Append('"');
            return sb.Append('}').ToString();
        }

        public static Busqueda Desde(string linea)
        {
            string tipo = LectorPecera.Campo(linea, "tipo"), clave = LectorPecera.Campo(linea, "clave");
            if (tipo == null || clave == null) return null;
            TipoBusqueda t;
            try { t = (TipoBusqueda)Enum.Parse(typeof(TipoBusqueda), tipo); }
            catch (Exception) { return null; }
            return new Busqueda
            {
                Num = (int)Numero(linea, "n"), Tipo = t, Clave = clave, Inicio = Numero(linea, "ini"),
                Prioridad = (int)Numero(linea, "prio"), Visto = Numero(linea, "visto") > 0,
                Objetivo = LectorPecera.Campo(linea, "obj") ?? "", ObjetivoNombre = LectorPecera.Campo(linea, "objn") ?? "",
                Otro = LectorPecera.Campo(linea, "otro") ?? "", OtroNombre = LectorPecera.Campo(linea, "otron") ?? "",
                Titulo = LectorPecera.Campo(linea, "tit") ?? "", Descripcion = LectorPecera.Campo(linea, "desc") ?? "",
            };
        }

        static double Numero(string linea, string k)
        {
            string key = "\"" + k + "\":";
            int i = linea.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return 0;
            i += key.Length;
            int j = i;
            while (j < linea.Length && (char.IsDigit(linea[j]) || linea[j] == '.' || linea[j] == '-')) j++;
            double v;
            return double.TryParse(linea.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0;
        }
    }

    // Recompensa REAL en el mundo al cerrarse una busqueda (solo con busquedas_recompensas=1, con tope).
    public sealed class Recompensa
    {
        public string Id = "", Nombre = "", Rasgo = "", Motivo = "";
        public int Renombre;
    }

    public sealed class EventoBusqueda
    {
        public Busqueda B;
        public EstadoBusqueda Estado;        // Activa = nueva
        public string Texto = "";

        // Una linea JSON para busquedas.jsonl (registro, no se relee).
        public string ToJson(double tiempo)
        {
            var sb = new StringBuilder("{");
            sb.Append("\"t\":").Append(tiempo.ToString("0.#", CultureInfo.InvariantCulture));
            sb.Append(",\"n\":").Append(B.Num);
            sb.Append(",\"ev\":\"").Append(Texto_.Escape(Estado.ToString().ToLowerInvariant())).Append('"');
            sb.Append(",\"tipo\":\"").Append(Texto_.Escape(B.Tipo.ToString().ToLowerInvariant())).Append('"');
            sb.Append(",\"obj\":\"").Append(Texto_.Escape(B.Objetivo)).Append('"');
            sb.Append(",\"objn\":\"").Append(Texto_.Escape(B.ObjetivoNombre)).Append('"');
            sb.Append(",\"x\":\"").Append(Texto_.Escape(Texto)).Append('"');
            sb.Append('}');
            return sb.ToString();
        }
    }

    public sealed class ReglasBusquedas
    {
        public int MaxActivas = 5, MaxNuevasPorFoto = 2;
        public double CaducidadSeg = 900;
        public bool Trono = true, Huerfanos = true, Ciudades = true, Amantes = true;
        // Una misma clave no se vuelve a abrir hasta pasado este tiempo de mundo.
        public double RepeticionSeg = 1800;

        public static ReglasBusquedas Desde(Config c)
        {
            var r = new ReglasBusquedas();
            r.MaxActivas = c.Ent("busquedas_max_activas", 5, 1, 20);
            r.MaxNuevasPorFoto = c.Ent("busquedas_max_nuevas", 2, 1, 10);
            r.CaducidadSeg = Math.Max(60, c.Num("busquedas_caducidad_seg", 900));
            r.RepeticionSeg = r.CaducidadSeg * 2;
            r.Trono = c.Bool("busquedas_trono");
            r.Huerfanos = c.Bool("busquedas_huerfanos");
            r.Ciudades = c.Bool("busquedas_ciudades");
            r.Amantes = c.Bool("busquedas_amantes");
            return r;
        }
    }

    // Tablero: compara la foto anterior con la nueva y abre/cierra busquedas.
    // SOLO LEE: nunca pide al juego que cambie nada. La primera foto es la linea base y no abre nada
    // (al cargar una partida no se convierten en busqueda cosas que ya estaban asi).
    public sealed class TableroBusquedas
    {
        public ReglasBusquedas Reglas;
        readonly List<Busqueda> activas = new List<Busqueda>();
        readonly Dictionary<string, double> claves = new Dictionary<string, double>();
        FotoMundo anterior;
        int contador;
        readonly List<Busqueda> pendientes = new List<Busqueda>();   // restauradas, a enlazar con la primera foto

        public TableroBusquedas(ReglasBusquedas reglas) { Reglas = reglas ?? new ReglasBusquedas(); }

        public IList<Busqueda> Activas { get { return activas; } }
        public FotoMundo Ultima { get { return anterior; } }
        public bool TieneBase { get { return anterior != null; } }

        public void Reinicia()
        {
            activas.Clear();
            claves.Clear();
            pendientes.Clear();
            anterior = null;
        }

        // Busquedas guardadas de una sesion anterior: se enlazan con la primera foto (linea base).
        public void Restaura(IEnumerable<Busqueda> guardadas)
        {
            if (guardadas == null) return;
            foreach (Busqueda b in guardadas)
            {
                if (b == null) continue;
                pendientes.Add(b);
                if (b.Num > contador) contador = b.Num;
            }
        }

        public List<string> Serializa()
        {
            var l = new List<string>();
            foreach (Busqueda b in activas) if (b.Tipo != TipoBusqueda.Ciudad) l.Add(b.ToJson());
            return l;
        }

        void Enlaza(FotoMundo f)
        {
            foreach (Busqueda b in pendientes)
            {
                if (activas.Count >= Reglas.MaxActivas) break;
                if (b.Tipo == TipoBusqueda.Ciudad) continue;                       // la ciudad no tiene id estable
                if (b.Objetivo.Length > 0 && f.Unidad(b.Objetivo) == null) continue; // murio mientras no jugabas
                if (b.Tipo == TipoBusqueda.Amantes && f.Unidad(b.Otro) == null) continue;
                if (b.Tipo == TipoBusqueda.Trono)
                {
                    string k = null;
                    foreach (var kv in f.Reinos)
                        if (kv.Value.Nombre == b.OtroNombre) { if (k != null) { k = null; break; } k = kv.Key; }
                    if (k == null) continue;
                    b.Reino = k;
                }
                if (b.Inicio > f.Tiempo) b.Inicio = f.Tiempo;   // otra partida con el reloj mas atras
                b.Estado = EstadoBusqueda.Activa;
                activas.Add(b);
                claves[b.Clave] = f.Tiempo;
            }
            pendientes.Clear();
        }

        // Tabla de recompensas: quien recibe que al cerrarse una busqueda (null = nada).
        public static Recompensa RecompensaDe(Busqueda b, FotoMundo f)
        {
            if (b == null || f == null) return null;
            FotoUnidad o = f.Unidad(b.Objetivo);
            if (b.Estado == EstadoBusqueda.Cumplida)
            {
                switch (b.Tipo)
                {
                    case TipoBusqueda.Trono:
                        if (o != null) return new Recompensa { Id = o.Id, Nombre = o.Nombre, Renombre = 10, Rasgo = "wfx_voz_del_pueblo", Motivo = "heredo el trono" };
                        break;
                    case TipoBusqueda.Pretendiente:
                        if (o != null) return new Recompensa { Id = o.Id, Nombre = o.Nombre, Renombre = 15, Motivo = "conquisto una corona" };
                        break;
                    case TipoBusqueda.Huerfano:
                        if (o != null) return new Recompensa { Id = o.Id, Nombre = o.Nombre, Renombre = 3, Rasgo = "wfx_vengador", Motivo = "crecio sin su padre" };
                        break;
                    case TipoBusqueda.Destierro:
                        if (o != null) return new Recompensa { Id = o.Id, Nombre = o.Nombre, Renombre = 2, Rasgo = "wfx_peregrino", Motivo = "encontro un nuevo hogar" };
                        break;
                    case TipoBusqueda.Amantes:
                        if (o != null) return new Recompensa { Id = o.Id, Nombre = o.Nombre, Renombre = 2, Motivo = "se reencontro con su amor" };
                        break;
                }
            }
            else if (b.Estado == EstadoBusqueda.Fallida && b.Tipo == TipoBusqueda.Amantes)
            {
                // Uno murio: el que queda vivo se lleva el corazon roto.
                FotoUnidad vivo = o ?? f.Unidad(b.Otro);
                if (vivo != null && (o == null || f.Unidad(b.Otro) == null))
                    return new Recompensa { Id = vivo.Id, Nombre = vivo.Nombre, Rasgo = "wfx_corazon_roto", Motivo = "perdio a su amor" };
            }
            return null;
        }

        public List<EventoBusqueda> Compara(FotoMundo foto)
        {
            var ev = new List<EventoBusqueda>();
            if (foto == null) return ev;
            if (anterior == null) { anterior = foto; Enlaza(foto); return ev; }

            var cand = new List<Busqueda>();
            Cierra(foto, ev, cand);
            if (Reglas.Trono || Reglas.Huerfanos) MuertesDeReyes(foto, cand);
            if (Reglas.Ciudades || Reglas.Huerfanos) MuertesDeLideres(foto, cand);
            if (Reglas.Amantes) Separaciones(foto, cand);
            Abre(foto, cand, ev);

            anterior = foto;
            return ev;
        }

        // Busquedas que llegan de fuera (la cronica de PeceraWB): mismos topes que las propias.
        public List<EventoBusqueda> Ofrece(List<Busqueda> cand, double tiempo)
        {
            var ev = new List<EventoBusqueda>();
            if (anterior == null || cand == null || cand.Count == 0) return ev;
            var f = new FotoMundo { Tiempo = tiempo };
            Abre(f, cand, ev);
            return ev;
        }

        // ---------- cierre de busquedas abiertas ----------

        void Cierra(FotoMundo foto, List<EventoBusqueda> ev, List<Busqueda> cand)
        {
            for (int i = activas.Count - 1; i >= 0; i--)
            {
                Busqueda b = activas[i];
                string final;
                EstadoBusqueda e = Evalua(b, foto, out final);
                if (e == EstadoBusqueda.Activa && foto.Tiempo - b.Inicio > Reglas.CaducidadSeg)
                {
                    e = EstadoBusqueda.Caducada;
                    final = "Caduca: " + b.Titulo;
                }
                if (e == EstadoBusqueda.Activa) continue;
                b.Estado = e; b.Final = final; b.Fin = foto.Tiempo;
                activas.RemoveAt(i);
                ev.Add(new EventoBusqueda { B = b, Estado = e, Texto = final });
                Busqueda sigue = Encadena(b, foto);
                if (sigue != null) cand.Add(sigue);
            }
        }

        // Cadenas: una busqueda que acaba abre la siguiente historia.
        //   Trono usurpado (el heredero sigue vivo y otro reina)  ->  «El heredero desposeído» (pretendiente)
        static Busqueda Encadena(Busqueda b, FotoMundo f)
        {
            if (b.Tipo != TipoBusqueda.Trono || b.Estado != EstadoBusqueda.Fallida) return null;
            FotoUnidad h = f.Unidad(b.Objetivo);
            FotoReino r = f.Reino(b.Reino);
            if (h == null || r == null || r.Rey == null || r.Rey == h.Id) return null;
            FotoUnidad usurpador = f.Unidad(r.Rey);
            string u = usurpador != null ? usurpador.Nombre : "otro";
            return new Busqueda
            {
                Tipo = TipoBusqueda.Pretendiente, Clave = "pretendiente|" + h.Id,
                Objetivo = h.Id, ObjetivoNombre = h.Nombre, OtroNombre = u,
                Titulo = "El heredero desposeído de " + r.Nombre,
                Descripcion = u + " le ha robado el trono de " + r.Nombre + " a " + h.Nombre + ". ¿Recuperará una corona?",
                Prioridad = 9,
            };
        }

        static EstadoBusqueda Evalua(Busqueda b, FotoMundo f, out string final)
        {
            final = "";
            FotoUnidad o = f.Unidad(b.Objetivo);
            switch (b.Tipo)
            {
                case TipoBusqueda.Trono:
                {
                    FotoReino r = f.Reino(b.Reino);
                    if (r == null) { final = "El reino de " + b.OtroNombre + " ha caido antes de que " + b.ObjetivoNombre + " reinara"; return EstadoBusqueda.Fallida; }
                    if (r.Rey == b.Objetivo) { final = b.ObjetivoNombre + " ya reina en " + r.Nombre; return EstadoBusqueda.Cumplida; }
                    if (o == null) { final = b.ObjetivoNombre + " murio sin llegar a reinar en " + r.Nombre; return EstadoBusqueda.Fallida; }
                    if (r.Rey != null)
                    {
                        FotoUnidad u = f.Unidad(r.Rey);
                        final = (u != null ? u.Nombre : "Otro") + " se adelanta y reina en " + r.Nombre + " en lugar de " + b.ObjetivoNombre;
                        return EstadoBusqueda.Fallida;
                    }
                    return EstadoBusqueda.Activa;
                }
                case TipoBusqueda.Huerfano:
                    if (o == null) { final = b.ObjetivoNombre + ", hijo de " + b.OtroNombre + ", no llego a adulto"; return EstadoBusqueda.Fallida; }
                    if (o.Adulto) { final = b.ObjetivoNombre + ", hijo de " + b.OtroNombre + ", ya es adulto"; return EstadoBusqueda.Cumplida; }
                    return EstadoBusqueda.Activa;
                case TipoBusqueda.Ciudad:
                {
                    FotoCiudad c = f.Ciudad(b.Ciudad);
                    if (c == null) { final = "La ciudad que gobernaba " + b.OtroNombre + " ha desaparecido"; return EstadoBusqueda.Fallida; }
                    if (c.Lider != null)
                    {
                        FotoUnidad l = f.Unidad(c.Lider);
                        final = (l != null ? l.Nombre : "Alguien") + " gobierna ahora la ciudad de " + b.OtroNombre;
                        return EstadoBusqueda.Cumplida;
                    }
                    return EstadoBusqueda.Activa;
                }
                case TipoBusqueda.Amantes:
                {
                    FotoUnidad p = f.Unidad(b.Otro);
                    if (o == null || p == null)
                    {
                        string muerto = o == null ? b.ObjetivoNombre : b.OtroNombre, vivo = o == null ? b.OtroNombre : b.ObjetivoNombre;
                        final = muerto + " murio sin reencontrarse con " + vivo;
                        return EstadoBusqueda.Fallida;
                    }
                    if (o.Amante != b.Otro) { final = b.ObjetivoNombre + " y " + b.OtroNombre + " ya no son pareja"; return EstadoBusqueda.Fallida; }
                    if (o.Reino != null && o.Reino == p.Reino) { final = b.ObjetivoNombre + " y " + b.OtroNombre + " vuelven a vivir en el mismo reino"; return EstadoBusqueda.Cumplida; }
                    return EstadoBusqueda.Activa;
                }
                case TipoBusqueda.Venganza:
                    if (o == null) { final = b.ObjetivoNombre + ", que mato a " + b.OtroNombre + ", ya no vive: se ha cumplido la venganza"; return EstadoBusqueda.Cumplida; }
                    return EstadoBusqueda.Activa;
                case TipoBusqueda.Destierro:
                    if (o == null) { final = b.ObjetivoNombre + " murio en el destierro"; return EstadoBusqueda.Fallida; }
                    // La cronica puede llegar antes que la foto donde ya no tiene ciudad: primero hay que verlo sin ella.
                    if (o.Ciudad == null) { b.Visto = true; return EstadoBusqueda.Activa; }
                    if (b.Visto) { final = b.ObjetivoNombre + " encuentra un nuevo hogar en " + f.NombreReino(o.Reino); return EstadoBusqueda.Cumplida; }
                    return EstadoBusqueda.Activa;
                case TipoBusqueda.Pretendiente:
                    if (o == null) { final = b.ObjetivoNombre + " murio sin conseguir la corona"; return EstadoBusqueda.Fallida; }
                    if (o.EsRey) { final = b.ObjetivoNombre + " se ha hecho con una corona: rey de " + f.NombreReino(o.Reino); return EstadoBusqueda.Cumplida; }
                    return EstadoBusqueda.Activa;
            }
            return EstadoBusqueda.Activa;
        }

        // ---------- deteccion ----------

        // Un rey vivo en la foto anterior ya no esta: sus hijos adultos heredan una busqueda de trono,
        // los pequenos una de huerfano.
        void MuertesDeReyes(FotoMundo f, List<Busqueda> cand)
        {
            foreach (var kv in anterior.Reinos)
            {
                FotoReino antes = kv.Value;
                if (antes.Rey == null || f.Unidad(antes.Rey) != null) continue;   // sigue vivo (o no habia rey)
                FotoUnidad rey = anterior.Unidad(antes.Rey);
                if (rey == null) continue;
                FotoReino ahora = f.Reino(kv.Key);

                FotoUnidad heredero = null;
                foreach (string h in rey.Hijos)
                {
                    FotoUnidad u = f.Unidad(h);
                    if (u == null) continue;
                    if (u.Adulto) { if (heredero == null) heredero = u; }
                    else if (Reglas.Huerfanos) cand.Add(Huerfano(u, rey, f, 3));
                }
                // Si otro ya ocupa el trono (o el reino cayo) no hay nada que seguir.
                if (!Reglas.Trono || heredero == null || ahora == null || ahora.Rey != null) continue;
                cand.Add(new Busqueda
                {
                    Tipo = TipoBusqueda.Trono,
                    Clave = "trono|" + kv.Key + "|" + rey.Id,
                    Objetivo = heredero.Id, ObjetivoNombre = heredero.Nombre,
                    Otro = antes.Nombre, OtroNombre = antes.Nombre,
                    Reino = kv.Key,
                    Titulo = "El trono de " + antes.Nombre,
                    Descripcion = "Muere " + rey.Nombre + ", rey de " + antes.Nombre + ". " + heredero.Nombre
                        + " debe reclamar el trono. Vive en " + f.NombreReino(heredero.Reino) + ".",
                    Prioridad = 10,
                });
            }
        }

        // Un lider de ciudad ya no esta: si la ciudad queda sin gobierno, busqueda de ciudad; sus hijos pequenos, huerfanos.
        void MuertesDeLideres(FotoMundo f, List<Busqueda> cand)
        {
            foreach (var kv in anterior.Ciudades)
            {
                FotoCiudad antes = kv.Value;
                if (antes.Lider == null || f.Unidad(antes.Lider) != null) continue;
                FotoUnidad lider = anterior.Unidad(antes.Lider);
                if (lider == null || lider.EsRey) continue;   // los reyes ya los trata MuertesDeReyes
                FotoCiudad ahora = f.Ciudad(kv.Key);

                if (Reglas.Huerfanos)
                    foreach (string h in lider.Hijos)
                    {
                        FotoUnidad u = f.Unidad(h);
                        if (u != null && !u.Adulto) cand.Add(Huerfano(u, lider, f, 2));
                    }
                if (!Reglas.Ciudades || ahora == null || ahora.Lider != null) continue;
                cand.Add(new Busqueda
                {
                    Tipo = TipoBusqueda.Ciudad,
                    Clave = "ciudad|" + kv.Key + "|" + lider.Id,
                    // No hay a quien seguir: se vigila la ciudad (Objetivo vacio).
                    Otro = lider.Id, OtroNombre = lider.Nombre,
                    Ciudad = kv.Key,
                    Titulo = "Sin gobierno tras " + lider.Nombre,
                    Descripcion = "Muere " + lider.Nombre + " y su ciudad de " + f.NombreReino(antes.Reino ?? lider.Reino)
                        + " queda sin lider. ¿Quien tomara el mando?",
                    Prioridad = 4,
                });
            }
        }

        static Busqueda Huerfano(FotoUnidad hijo, FotoUnidad padre, FotoMundo f, int prioridad)
        {
            return new Busqueda
            {
                Tipo = TipoBusqueda.Huerfano,
                Clave = "huerfano|" + hijo.Id,
                Objetivo = hijo.Id, ObjetivoNombre = hijo.Nombre,
                Otro = padre.Id, OtroNombre = padre.Nombre,
                Titulo = "El hijo de " + padre.Nombre,
                Descripcion = padre.Nombre + " ha muerto dejando a " + hijo.Nombre + ", aun nino, en "
                    + f.NombreReino(hijo.Reino) + ". Que llegue a adulto.",
                Prioridad = prioridad,
            };
        }

        // Una pareja que en la foto anterior vivia en el mismo reino y ahora en reinos distintos.
        void Separaciones(FotoMundo f, List<Busqueda> cand)
        {
            foreach (var kv in f.Unidades)
            {
                FotoUnidad a = kv.Value;
                if (a.Amante == null || string.CompareOrdinal(a.Id, a.Amante) > 0) continue;   // cada pareja una vez
                FotoUnidad b = f.Unidad(a.Amante);
                if (b == null || a.Reino == null || b.Reino == null || a.Reino == b.Reino) continue;
                FotoUnidad a0 = anterior.Unidad(a.Id), b0 = anterior.Unidad(b.Id);
                if (a0 == null || b0 == null || a0.Amante != b.Id || a0.Reino == null || a0.Reino != b0.Reino) continue;
                cand.Add(new Busqueda
                {
                    Tipo = TipoBusqueda.Amantes,
                    Clave = "amantes|" + a.Id + "|" + b.Id,
                    Objetivo = a.Id, ObjetivoNombre = a.Nombre,
                    Otro = b.Id, OtroNombre = b.Nombre,
                    Titulo = "Amantes separados: " + a.Nombre + " y " + b.Nombre,
                    Descripcion = a.Nombre + " (" + f.NombreReino(a.Reino) + ") y " + b.Nombre + " ("
                        + f.NombreReino(b.Reino) + ") han quedado a ambos lados de la frontera.",
                    Prioridad = (a.EsRey || b.EsRey) ? 6 : 1,
                });
            }
        }

        // ---------- apertura con topes ----------

        void Abre(FotoMundo f, List<Busqueda> cand, List<EventoBusqueda> ev)
        {
            // Mas importante primero; a igualdad, orden estable por clave (determinista).
            cand.Sort((x, y) => x.Prioridad != y.Prioridad ? y.Prioridad.CompareTo(x.Prioridad) : string.CompareOrdinal(x.Clave, y.Clave));
            int nuevas = 0;
            foreach (Busqueda b in cand)
            {
                if (activas.Count >= Reglas.MaxActivas || nuevas >= Reglas.MaxNuevasPorFoto) break;
                double t;
                if (claves.TryGetValue(b.Clave, out t) && f.Tiempo - t < Reglas.RepeticionSeg) continue;
                if (b.Objetivo.Length > 0 && YaSigue(b.Objetivo)) continue;
                claves[b.Clave] = f.Tiempo;
                b.Num = ++contador;
                b.Inicio = f.Tiempo;
                activas.Add(b);
                nuevas++;
                ev.Add(new EventoBusqueda { B = b, Estado = EstadoBusqueda.Activa, Texto = b.Descripcion });
            }
            if (claves.Count > 2000) Poda(f.Tiempo);
        }

        bool YaSigue(string objetivo)
        {
            foreach (Busqueda b in activas) if (b.Objetivo == objetivo) return true;
            return false;
        }

        void Poda(double ahora)
        {
            var viejas = new List<string>();
            foreach (var kv in claves) if (ahora - kv.Value >= Reglas.RepeticionSeg) viejas.Add(kv.Key);
            foreach (string k in viejas) claves.Remove(k);
        }

        // Texto para la tecla de lista: una linea por busqueda.
        public string Lista()
        {
            if (activas.Count == 0) return "Sin busquedas activas";
            var sb = new StringBuilder("Busquedas (" + activas.Count + "):");
            foreach (Busqueda b in activas) sb.Append('\n').Append(b.Num).Append(". ").Append(b.Titulo)
                  .Append(b.Objetivo.Length > 0 ? " - sigue a " + b.ObjetivoNombre : " - vigila su ciudad");
            return sb.ToString();
        }
    }

    // Escape JSON minimo (el Core no depende del Json de PeceraWB: son mods separados).
    public static class Texto_
    {
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
