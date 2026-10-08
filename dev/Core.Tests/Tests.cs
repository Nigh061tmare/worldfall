using System;
using System.Collections.Generic;
using WorldfallExpansion.Core;

// Tests del Core de busquedas, sin framework (C# 5, se puede compilar tambien con csc /langversion:5):
//   csc /langversion:5 /out:tests.exe worldfall-expansion\Code\Core\*.cs dev\Core.Tests\Tests.cs
static class Tests
{
    static int ok, ko;

    static void Check(bool c, string que)
    {
        if (c) ok++;
        else { ko++; Console.WriteLine("FALLA: " + que); }
    }

    // Constructor de fotos para los tests.
    sealed class M
    {
        public readonly FotoMundo F;
        public M(double t) { F = new FotoMundo { Tiempo = t }; }

        public M Reino(string k, string nombre, string rey)
        {
            F.Reinos[k] = new FotoReino { Clave = k, Nombre = nombre, Rey = rey };
            return this;
        }

        public M Ciudad(string c, string reino, string lider)
        {
            F.Ciudades[c] = new FotoCiudad { Clave = c, Reino = reino, Lider = lider };
            return this;
        }

        public M U(string id, string reino, bool adulto = true, bool rey = false, string amante = null, string ciudad = null, params string[] hijos)
        {
            var u = new FotoUnidad { Id = id, Nombre = id.ToUpperInvariant(), Reino = reino, Adulto = adulto, EsRey = rey, Amante = amante, Ciudad = ciudad };
            u.Hijos.AddRange(hijos);
            F.Unidades[id] = u;
            return this;
        }
    }

    static List<EventoBusqueda> De(List<EventoBusqueda> ev, EstadoBusqueda e)
    {
        return ev.FindAll(x => x.Estado == e);
    }

    static int Main(string[] args)
    {
        // dotnet run --project dev/Core.Tests -- --locales worldfall-expansion/Locales  (regenera los JSON)
        if (args.Length == 2 && args[0] == "--locales")
        {
            System.IO.Directory.CreateDirectory(args[1]);
            System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "es.json"), LocaleJson(true), new System.Text.UTF8Encoding(false));
            System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "en.json"), LocaleJson(false), new System.Text.UTF8Encoding(false));
            Console.WriteLine("Locales escritos en " + args[1]);
            return 0;
        }
        LineaBase();
        TronoCumplido();
        TronoUsurpado();
        TronoNadaSiYaHayRey();
        Huerfano();
        CiudadSinLider();
        Amantes();
        AmantesMuerte();
        Topes();
        Caducidad();
        NoRepite();
        ReglaApagada();
        Reinicio();
        Configuracion();
        Escape();
        Traduccion();
        BrujulaTest();
        PeceraTest();
        FicheroWorldfall();
        CatalogoTest();
        Fase2Test();
        Console.WriteLine(ok + " comprobaciones OK, " + ko + " fallos");
        return ko == 0 ? 0 : 1;
    }

    static void LineaBase()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        // Al cargar, una pareja ya separada no es noticia.
        var ev = t.Compara(new M(0).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        Check(ev.Count == 0, "la primera foto es linea base");
        ev = t.Compara(new M(10).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        Check(ev.Count == 0, "lo que ya estaba asi no abre busqueda");
    }

    static void TronoCumplido()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "hijo" }).U("hijo", "k2").F);
        var ev = t.Compara(new M(10).Reino("k1", "Norte", null).U("hijo", "k2").F);
        var nuevas = De(ev, EstadoBusqueda.Activa);
        Check(nuevas.Count == 1 && nuevas[0].B.Tipo == TipoBusqueda.Trono, "muere el rey -> busqueda de trono");
        Check(nuevas.Count == 1 && nuevas[0].B.Objetivo == "hijo", "el objetivo es el heredero");
        Check(nuevas.Count == 1 && nuevas[0].Texto.Contains("HIJO") && nuevas[0].Texto.Contains("Norte"), "descripcion con heredero y reino");
        ev = t.Compara(new M(20).Reino("k1", "Norte", "hijo").U("hijo", "k1", rey: true).F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 1, "el heredero reina -> cumplida");
        Check(t.Activas.Count == 0, "cerrada");
    }

    static void TronoUsurpado()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "hijo" }).U("hijo", "k1").U("x", "k1").F);
        t.Compara(new M(10).Reino("k1", "Norte", null).U("hijo", "k1").U("x", "k1").F);
        var ev = t.Compara(new M(20).Reino("k1", "Norte", "x").U("hijo", "k1").U("x", "k1", rey: true).F);
        var f = De(ev, EstadoBusqueda.Fallida);
        Check(f.Count == 1 && f[0].Texto.Contains("X"), "otro se corona -> fallida nombrando al usurpador");
    }

    static void TronoNadaSiYaHayRey()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "hijo" }).U("hijo", "k1").U("x", "k1").F);
        var ev = t.Compara(new M(10).Reino("k1", "Norte", "x").U("hijo", "k1").U("x", "k1", rey: true).F);
        Check(ev.Count == 0, "si el trono ya esta ocupado en la misma foto, no hay busqueda");
    }

    static void Huerfano()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "nino" }).U("nino", "k1", adulto: false).F);
        var ev = t.Compara(new M(10).Reino("k1", "Norte", null).U("nino", "k1", adulto: false).F);
        var n = De(ev, EstadoBusqueda.Activa);
        Check(n.Count == 1 && n[0].B.Tipo == TipoBusqueda.Huerfano, "hijo pequeno del rey -> huerfano (no trono)");
        ev = t.Compara(new M(20).Reino("k1", "Norte", null).U("nino", "k1", adulto: true).F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 1, "llega a adulto -> cumplida");
    }

    static void CiudadSinLider()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").Ciudad("c1", "k1", "lid").U("rey", "k1", rey: true).U("lid", "k1", ciudad: "c1").U("v", "k1", ciudad: "c1").F);
        var ev = t.Compara(new M(10).Reino("k1", "Norte", "rey").Ciudad("c1", "k1", null).U("rey", "k1", rey: true).U("v", "k1", ciudad: "c1").F);
        var n = De(ev, EstadoBusqueda.Activa);
        Check(n.Count == 1 && n[0].B.Tipo == TipoBusqueda.Ciudad && n[0].B.Objetivo == "", "muere el lider -> ciudad sin gobierno (sin objetivo vivo)");
        Check(t.Lista().Contains("vigila su ciudad"), "la lista lo dice");
        ev = t.Compara(new M(20).Reino("k1", "Norte", "rey").Ciudad("c1", "k1", "v").U("rey", "k1", rey: true).U("v", "k1", ciudad: "c1").F);
        var c = De(ev, EstadoBusqueda.Cumplida);
        Check(c.Count == 1 && c[0].Texto.Contains("V gobierna"), "nuevo lider -> cumplida");
    }

    static void Amantes()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k1", amante: "a").F);
        var ev = t.Compara(new M(10).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        var n = De(ev, EstadoBusqueda.Activa);
        Check(n.Count == 1 && n[0].B.Tipo == TipoBusqueda.Amantes, "pareja separada por la frontera -> busqueda");
        Check(n.Count == 1 && n[0].Texto.Contains("Norte") && n[0].Texto.Contains("Sur"), "dice donde esta cada uno");
        ev = t.Compara(new M(20).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("a", "k2", amante: "b").U("b", "k2", amante: "a").F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 1, "mismo reino otra vez -> cumplida");
    }

    static void AmantesMuerte()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k1", amante: "a").F);
        t.Compara(new M(10).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        var ev = t.Compara(new M(20).Reino("k1", "N", null).U("a", "k1").F);
        var f = De(ev, EstadoBusqueda.Fallida);
        Check(f.Count == 1 && f[0].Texto.StartsWith("B murio"), "muere uno -> fallida");
    }

    static void Topes()
    {
        var r = new ReglasBusquedas { MaxNuevasPorFoto = 2, MaxActivas = 3 };
        var t = new TableroBusquedas(r);
        var m0 = new M(0).Reino("k1", "N", "rey").Reino("k2", "S", null).U("rey", "k1", rey: true, hijos: new[] { "h" }).U("h", "k1");
        var m1 = new M(10).Reino("k1", "N", null).Reino("k2", "S", null).U("h", "k1");
        for (int i = 0; i < 5; i++)
        {
            string a = "p" + i + "a", b = "p" + i + "b";
            m0.U(a, "k1", amante: b).U(b, "k1", amante: a);
            m1.U(a, "k1", amante: b).U(b, "k2", amante: a);
        }
        t.Compara(m0.F);
        var ev = t.Compara(m1.F);
        var n = De(ev, EstadoBusqueda.Activa);
        Check(n.Count == 2, "como mucho MaxNuevasPorFoto por foto (" + n.Count + ")");
        Check(n.Count > 0 && n[0].B.Tipo == TipoBusqueda.Trono, "el trono va antes que las parejas");
        ev = t.Compara(m1.F);
        Check(De(ev, EstadoBusqueda.Activa).Count == 0, "los candidatos no se repiten si la situacion no cambia");
        Check(t.Activas.Count <= r.MaxActivas, "nunca mas de MaxActivas");
    }

    static void Caducidad()
    {
        var t = new TableroBusquedas(new ReglasBusquedas { CaducidadSeg = 100 });
        t.Compara(new M(0).Reino("k1", "N", "rey").U("rey", "k1", rey: true, hijos: new[] { "h" }).U("h", "k1").F);
        t.Compara(new M(10).Reino("k1", "N", null).U("h", "k1").F);
        var ev = t.Compara(new M(50).Reino("k1", "N", null).U("h", "k1").F);
        Check(ev.Count == 0, "aun no caduca");
        ev = t.Compara(new M(200).Reino("k1", "N", null).U("h", "k1").F);
        Check(De(ev, EstadoBusqueda.Caducada).Count == 1 && t.Activas.Count == 0, "caduca por tiempo de mundo");
    }

    static void NoRepite()
    {
        var t = new TableroBusquedas(new ReglasBusquedas { RepeticionSeg = 1000 });
        var junta = new M(0).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k1", amante: "a").F;
        t.Compara(junta);
        t.Compara(new M(10).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        t.Compara(new M(20).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k1", amante: "a").F);
        var ev = t.Compara(new M(30).Reino("k1", "N", null).Reino("k2", "S", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        Check(De(ev, EstadoBusqueda.Activa).Count == 0, "la misma pareja no reabre busqueda enseguida");
    }

    static void ReglaApagada()
    {
        var t = new TableroBusquedas(new ReglasBusquedas { Trono = false });
        t.Compara(new M(0).Reino("k1", "N", "rey").U("rey", "k1", rey: true, hijos: new[] { "h" }).U("h", "k1").F);
        var ev = t.Compara(new M(10).Reino("k1", "N", null).U("h", "k1").F);
        Check(ev.Count == 0, "busquedas_trono=0 no abre busquedas de trono");
    }

    static void Reinicio()
    {
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "N", "rey").U("rey", "k1", rey: true, hijos: new[] { "h" }).U("h", "k1").F);
        t.Reinicia();
        var ev = t.Compara(new M(10).Reino("k1", "N", null).U("h", "k1").F);
        Check(ev.Count == 0 && !t.Activas.GetEnumerator().MoveNext(), "tras Reinicia la siguiente foto vuelve a ser linea base");
    }

    static void Configuracion()
    {
        var c = Config.Desde(new[] { "# comentario", "busquedas_max_activas = 50", "busquedas_trono=0  # sin tronos", "basura", "=x", "clave_rara=1" });
        var r = ReglasBusquedas.Desde(c);
        Check(r.MaxActivas == 20, "max_activas acotado a 20");
        Check(!r.Trono && r.Amantes, "lee interruptores, comentario al final incluido");
        Check(c.Bool("activo"), "valor por defecto");
        string s = c.Serializa();
        Check(s.Contains("busquedas_max_activas=50") && s.Contains("busquedas_tecla=F8") && !s.Contains("clave_rara"), "serializa claves conocidas con valores del usuario");
        Check(Config.Desde(null).Bool("busquedas"), "sin fichero: defectos");
        var r2 = ReglasBusquedas.Desde(Config.Desde(new[] { "busquedas_caducidad_seg=abc" }));
        Check(Math.Abs(r2.CaducidadSeg - 900) < 1e-9, "numero invalido -> defecto");
    }

    static void Escape()
    {
        Check(Texto_.Escape("a\"b\\c\nd\u0001") == "a\\\"b\\\\c\\nd\\u0001", "escape JSON");
        var b = new Busqueda { Num = 3, Tipo = TipoBusqueda.Trono, Objetivo = "a1", ObjetivoNombre = "Ña \"x\"" };
        string j = new EventoBusqueda { B = b, Estado = EstadoBusqueda.Cumplida, Texto = "ok" }.ToJson(12.5);
        Check(j == "{\"t\":12.5,\"n\":3,\"ev\":\"cumplida\",\"tipo\":\"trono\",\"obj\":\"a1\",\"objn\":\"Ña \\\"x\\\"\",\"x\":\"ok\"}", "linea JSON: " + j);
    }
    static void Traduccion()
    {
        var t = new Traductor();
        t.Carga(new[]
        {
            "# comentario",
            "Any work for me? => ¿Tienes trabajo para mi?",
            "The army of {0} gathers: {1} of {2}. => El ejercito de {0} se reune: {1} de {2}.",
            "{0} killed {1} => {1} murio a manos de {0}",
            "Take {0} from {1}. {2} => Toma {0} de {1}. {2}",
            "Wars => Guerras",
            "{0}: {1} => sin palabra fija, se ignora",
            "linea sin flecha",
            "Line\\nbreak => Salto\\nde linea",
            "{0}{1} hello => pegados, se ignora",
        });
        Check(t.Exactas == 3 && t.Plantillas == 3, "carga: " + t.Exactas + " exactas, " + t.Plantillas + " plantillas");
        Check(t.Errores == 3, "errores contados: " + t.Errores);
        Check(t.Traduce("Any work for me?") == "¿Tienes trabajo para mi?", "exacta");
        Check(t.Traduce("  Wars ") == "  Guerras ", "conserva espacios de los bordes");
        Check(t.Traduce("The army of Oslo gathers: 4 of 10.") == "El ejercito de Oslo se reune: 4 de 10.", "plantilla");
        Check(t.Traduce("Bob killed Ann") == "Ann murio a manos de Bob", "huecos reordenados");
        Check(t.Traduce("Take Oslo from Norte. Wars") == "Toma Oslo de Norte. Guerras", "el hueco se traduce tambien");
        Check(t.Traduce("Unknown text") == "Unknown text", "lo desconocido sale igual");
        Check(t.Traduce("Line\nbreak") == "Salto\nde linea", "salto de linea en el fichero");
        Check(t.Traduce("") == "" && t.Traduce(null) == null, "vacio y null");
        Check(t.Traduce("{1} killed X") == "X murio a manos de {1}", "un valor con llaves no se sustituye dos veces");

        t.RegistrarFaltan = true;
        t.Traduce("Unknown thing here"); t.Traduce("Unknown thing here"); t.Traduce("42"); t.Traduce("Wars");
        var f = t.SacaFaltan();
        Check(f.Count == 1 && f[0] == "Unknown thing here", "registro de faltan: solo textos nuevos sin traducir");
        Check(t.SacaFaltan().Count == 0, "faltan se vacia");

        t.Carga(new[] { "Wars => Las guerras" });
        Check(t.Traduce("Wars") == "Las guerras", "un fichero posterior sustituye (y vacia la cache)");
        Check(Traductor.Desescapa(Traductor.Escapa("a\\b\nc")) == "a\\b\nc", "escapa/desescapa ida y vuelta");

        var g = new Traductor { MaxCache = 2 };
        g.Carga(new[] { "A b c => X" });
        for (int i = 0; i < 10; i++) g.Traduce("n" + i);
        Check(g.Traduce("A b c") == "X", "la cache acotada no rompe nada");
    }
    static void BrujulaTest()
    {
        Check(Brujula.Rumbo(10, 0) == "este" && Brujula.Rumbo(0, 10) == "norte" && Brujula.Rumbo(-5, -5) == "suroeste", "rumbos (convenio de Worldfall)");
        double yawEste = 0, yawNorte = Math.PI / 2;
        Check(Brujula.Giro(10, 0, yawEste, false) == "justo delante", "delante");
        Check(Brujula.Giro(0, 10, yawEste, false) == "a tu izquierda", "mirando al este, el norte queda a la izquierda");
        Check(Brujula.Giro(0, -10, yawEste, false) == "a tu derecha", "y el sur a la derecha");
        Check(Brujula.Giro(-10, 0, yawEste, false) == "a tu espalda", "detras");
        Check(Brujula.Giro(10, 0, yawNorte, false) == "a tu derecha", "mirando al norte, el este a la derecha");
        Check(Brujula.Giro(0, 10, yawEste, true) == "a tu derecha", "brujula_invertir cambia el lado");
        Check(Brujula.Giro(10, 10, yawEste, false) == "delante a la izquierda", "diagonal");
        Check(Math.Abs(Brujula.Relativo(1, 0, 3 * Math.PI)) - 180 < 1e-9, "angulo normalizado");
        Check(Brujula.Linea(30, 40, false, 0, false) == "50 casillas al noreste", "linea modo dios: " + Brujula.Linea(30, 40, false, 0, false));
        Check(Brujula.Linea(1, 1, true, 0, false) == "aqui mismo", "muy cerca");
        Check(Brujula.Distancia(2500) == "2.5 mil casillas", "miles");
    }

    static void PeceraTest()
    {
        string tipo, texto;
        // Lineas tal como las escribe PeceraWB (Mundo.Hito -> Json.Escape).
        Check(LectorPecera.Lee("{\"d\":12,\"t\":\"regicidio\",\"x\":\"Ana mato a Bo, rey de Norte\",\"p\":8}", out tipo, out texto)
              && tipo == "regicidio" && texto == "Ana mato a Bo, rey de Norte", "lee una linea de la cronica");
        Check(LectorPecera.Campo("{\"x\":\"di \\\"hola\\\" \\u00f1\"}", "x") == "di \"hola\" ñ", "escapes JSON");
        Check(LectorPecera.Campo("{\"x\":\"sin cerrar", "x") == null && LectorPecera.Campo("{}", "x") == null, "linea rota -> null");

        var f = new M(100).Reino("k1", "Norte", "s").U("ana", "k1").U("bo2", "k2", ciudad: "c9").U("s", "k1", rey: true).U("rv", "k1").F;
        f.Unidades["ana"].Nombre = "Ana"; f.Unidades["rv"].Nombre = "Rival Uno"; f.Unidades["s"].Nombre = "Sol";
        Busqueda b = LectorPecera.Desde("regicidio", "Ana mato a Bo, rey de Norte", f);
        Check(b != null && b.Tipo == TipoBusqueda.Venganza && b.Objetivo == "ana", "regicidio -> venganza contra el asesino");
        b = LectorPecera.Desde("sucesion", "Sucede a Bo en Norte: Sol (disputado con Rival Uno)", f);
        Check(b != null && b.Tipo == TipoBusqueda.Pretendiente && b.Objetivo == "rv" && b.Titulo.Contains("Norte"), "sucesion disputada -> pretendiente");
        Check(LectorPecera.Desde("sucesion", "Sucede a Bo en Norte: Sol", f) == null, "sucesion sin disputa -> nada");
        Check(LectorPecera.Desde("regicidio", "Nadie mato a Bo, rey de Norte", f) == null, "nombre que no esta -> nada");
        Check(LectorPecera.Desde("amor", "Ana y Sol son pareja", f) == null, "otros hitos -> nada");
        f.Unidades["s"].Nombre = "Ana";
        Check(LectorPecera.Desde("regicidio", "Ana mato a Bo, rey de Norte", f) == null, "homonimos -> nada (mejor que equivocarse)");

        // Destierro: abre, se ve sin ciudad, luego encuentra hogar -> cumplida.
        var t = new TableroBusquedas(new ReglasBusquedas());
        var foto = new M(0).Reino("k1", "Norte", null).U("d", "k1", ciudad: "c1").F;
        foto.Unidades["d"].Nombre = "Dan";
        t.Compara(foto);
        var ev = t.Ofrece(new List<Busqueda> { LectorPecera.Desde("destierro", "Dan es desterrado: perdio la disputa", foto) }, 5);
        Check(ev.Count == 1 && ev[0].B.Tipo == TipoBusqueda.Destierro, "destierro abre busqueda");
        ev = t.Compara(new M(10).Reino("k1", "Norte", null).U("d", "k1", ciudad: "c1").F);
        Check(ev.Count == 0, "la foto vieja (aun con ciudad) no la da por cumplida");
        t.Compara(new M(20).Reino("k1", "Norte", null).U("d", "k1").F);
        ev = t.Compara(new M(30).Reino("k1", "Norte", null).U("d", "k1", ciudad: "c2").F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 1, "sin ciudad y luego con ciudad -> cumplida");

        // Venganza: el asesino muere -> cumplida. Pretendiente: se corona -> cumplida.
        t = new TableroBusquedas(new ReglasBusquedas());
        foto = new M(0).Reino("k1", "Norte", null).Reino("k2", "Sur", null).U("ana", "k1").U("rv", "k1").F;
        foto.Unidades["ana"].Nombre = "Ana"; foto.Unidades["rv"].Nombre = "Rival Uno";
        t.Compara(foto);
        t.Ofrece(new List<Busqueda> { LectorPecera.Desde("regicidio", "Ana mato a Bo, rey de Norte", foto),
                                      LectorPecera.Desde("sucesion", "Sucede a Bo en Norte: Sol (disputado con Rival Uno)", foto) }, 5);
        Check(t.Activas.Count == 2, "dos busquedas de la pecera");
        ev = t.Compara(new M(10).Reino("k1", "Norte", null).Reino("k2", "Sur", "rv").U("rv", "k2", rey: true).F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 2, "venganza (muere el asesino) y pretendiente (se corona) cumplidas");
        Check(new TableroBusquedas(null).Ofrece(new List<Busqueda> { b }, 1).Count == 0, "sin linea base no se abre nada de fuera");
    }
    // El fichero REAL de traducciones: carga sin errores y traduce casos del juego.
    static void FicheroWorldfall()
    {
        string dir = System.IO.Directory.GetCurrentDirectory(), f = null;
        for (int i = 0; i < 6 && dir != null; i++)
        {
            string c = System.IO.Path.Combine(dir, "worldfall-expansion", "Traducciones", "worldfall_es.txt");
            if (System.IO.File.Exists(c)) { f = c; break; }
            dir = System.IO.Path.GetDirectoryName(dir);
        }
        Check(f != null, "encuentro Traducciones/worldfall_es.txt");
        if (f == null) return;
        var t = new Traductor();
        t.Carga(System.IO.File.ReadAllLines(f, System.Text.Encoding.UTF8));
        Check(t.Errores == 0, "fichero sin lineas rotas (" + t.Errores + ")");
        Check(t.Exactas > 1400 && t.Plantillas > 550, "volumen: " + t.Exactas + " frases, " + t.Plantillas + " plantillas");
        Check(t.Traduce("Any work for me?") == "¿Tienes trabajo para mí?", "frase de dialogo");
        Check(t.Traduce("Settings") == "Ajustes", "menu");
        Check(t.Traduce("The sky over Oslo splits with falling fire!") == "¡El cielo sobre Oslo se abre en una lluvia de fuego!",
              "la plantilla mas especifica gana: " + t.Traduce("The sky over Oslo splits with falling fire!"));
        Check(t.Traduce("Your army is already marching on Oslo (12 warriors).") == "Tu ejército ya marcha sobre Oslo (12 guerreros).", "plantilla con numeros");
        Check(t.Traduce("Expecting, about 3 months") == "Esperando un bebé, faltan unos 3 meses", "plantilla de embarazo");
        string r = t.Traduce("There's good land North-east of Oslo. Go there, raise a village for Norte, and lead it. Take care.\nYour map (M) shows the way; any free land will do if you find better.");
        Check(r.StartsWith("Hay buena tierra al noreste de Oslo."), "hueco traducido dentro de plantilla: " + r);

        // Cada plantilla, rellenada con valores inventados, debe traducirse con ELLA misma (ninguna otra la tapa).
        int mal = 0, total = 0;
        var hueco = new System.Text.RegularExpressions.Regex(@"\{(\d)\}");
        foreach (string linea in System.IO.File.ReadAllLines(f, System.Text.Encoding.UTF8))
        {
            if (linea.StartsWith("#") || linea.IndexOf(" => ", StringComparison.Ordinal) < 0) continue;
            int k = linea.IndexOf(" => ", StringComparison.Ordinal);
            string en = Traductor.Desescapa(linea.Substring(0, k)).Trim(), es = Traductor.Desescapa(linea.Substring(k + 4)).Trim();
            if (!hueco.IsMatch(en)) continue;
            total++;
            System.Text.RegularExpressions.MatchEvaluator val = mm => "Qz" + mm.Groups[1].Value + "x";
            string entrada = hueco.Replace(en, val), esperado = hueco.Replace(es, val);
            string sale = t.Traduce(entrada);
            if (sale != esperado) { mal++; if (mal <= 8) Console.WriteLine("  choque: «" + entrada + "» -> «" + sale + "» (esperado «" + esperado + "»)"); }
        }
        Check(mal == 0, "plantillas sin choques: " + (total - mal) + "/" + total);

        Check(t.Traduce("\"¡No!\" When I lost my love, 2 years ago.") == "\"¡No!\" Cuando perdí a mi amor, hace 2 años.",
              "recuerdo compuesto: " + t.Traduce("\"¡No!\" When I lost my love, 2 years ago."));
        Check(t.Traduce("When we won the war, last year.") == "Cuando ganamos la guerra, el año pasado.", "recuerdo sin frase");

        // Rendimiento: 5.000 textos distintos (como numeros cambiantes en el HUD) en poco tiempo.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 5000; i++) t.Traduce("Your army is already marching on City" + i + " (" + i + " warriors).");
        sw.Stop();
        Check(sw.ElapsedMilliseconds < 2000, "5000 traducciones nuevas en " + sw.ElapsedMilliseconds + " ms");
    }
    static string LocaleJson(bool es)
    {
        var sb = new System.Text.StringBuilder("{\n");
        bool primero = true;
        foreach (var kv in Catalogo.Textos(es))
        {
            if (!primero) sb.Append(",\n");
            primero = false;
            sb.Append("  \"").Append(Texto_.Escape(kv.Key)).Append("\": \"").Append(Texto_.Escape(kv.Value)).Append('"');
        }
        return sb.Append("\n}\n").ToString();
    }

    static string Raiz()
    {
        string dir = System.IO.Directory.GetCurrentDirectory();
        for (int i = 0; i < 6 && dir != null; i++)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(dir, "worldfall-expansion"))) return dir;
            dir = System.IO.Path.GetDirectoryName(dir);
        }
        return null;
    }

    static void CatalogoTest()
    {
        var errores = Catalogo.Valida();
        Check(errores.Count == 0, "catalogo valido: " + string.Join("; ", errores.ToArray()));
        Check(Catalogo.Objetos.Length >= 15 && Catalogo.Rasgos.Length >= 12, "volumen del catalogo");
        int fabricables = 0;
        foreach (var o in Catalogo.Objetos)
            if (!o.Base.StartsWith("ring_") && !o.Base.StartsWith("amulet_")) fabricables++;
        Check(fabricables >= 12, "armas y armaduras fabricables en Worldfall: " + fabricables);
        foreach (var o in Catalogo.Objetos)
        {
            float d; o.Stats.TryGetValue("damage", out d);
            float a; o.Stats.TryGetValue("armor", out a);
            if (d > 14 || a > 14) Check(false, o.Id + " desequilibrado (dano " + d + ", armadura " + a + ")");
        }
        Check(Catalogo.ClaveObjeto(Catalogo.Objetos[0]) == "item_" + Catalogo.Objetos[0].Id, "clave = translation_key del juego");
        Check(Catalogo.ClaveRasgo(Catalogo.Rasgos[0]) == "trait_" + Catalogo.Rasgos[0].Id, "clave de rasgo = typed_id + _ + id");
        string raiz = Raiz();
        Check(raiz != null, "raiz del repo");
        if (raiz == null) return;
        foreach (bool es in new[] { true, false })
        {
            string f = System.IO.Path.Combine(raiz, "worldfall-expansion", "Locales", es ? "es.json" : "en.json");
            Check(System.IO.File.Exists(f) && System.IO.File.ReadAllText(f, System.Text.Encoding.UTF8) == LocaleJson(es),
                  System.IO.Path.GetFileName(f) + " al dia (regenera con: dotnet run --project dev/Core.Tests -- --locales worldfall-expansion/Locales)");
        }
    }
    static void Fase2Test()
    {
        // --- Persistencia: guardar, restaurar en otra sesion y enlazar con la primera foto ---
        var t = new TableroBusquedas(new ReglasBusquedas());
        t.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "hijo" }).U("hijo", "k1")
                  .Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k1", amante: "a").F);
        t.Compara(new M(10).Reino("k1", "Norte", null).U("hijo", "k1").Reino("k2", "Sur", null).U("a", "k1", amante: "b").U("b", "k2", amante: "a").F);
        Check(t.Activas.Count == 2, "dos busquedas abiertas antes de guardar");
        var lineas = t.Serializa();
        var restauradas = new List<Busqueda>();
        foreach (string l in lineas) restauradas.Add(Busqueda.Desde(l));
        Check(restauradas.TrueForAll(x => x != null), "las lineas guardadas se releen");
        Check(restauradas.Exists(x => x.Tipo == TipoBusqueda.Trono && x.Objetivo == "hijo" && x.OtroNombre == "Norte"), "ida y vuelta del trono");

        // Otra sesion: los reinos tienen OTRAS claves; el trono se enlaza por nombre. «b» ha muerto.
        var t2 = new TableroBusquedas(new ReglasBusquedas());
        t2.Restaura(restauradas);
        var ev = t2.Compara(new M(5).Reino("x9", "Norte", null).U("hijo", "x9").U("a", "x9").F);
        Check(ev.Count == 0, "restaurar no genera avisos");
        Check(t2.Activas.Count == 1 && t2.Activas[0].Tipo == TipoBusqueda.Trono && t2.Activas[0].Reino == "x9",
              "trono reenlazado por nombre; la de amantes cae (b murio fuera de juego)");
        ev = t2.Compara(new M(15).Reino("x9", "Norte", "hijo").U("hijo", "x9", rey: true).F);
        Check(De(ev, EstadoBusqueda.Cumplida).Count == 1, "la restaurada se cumple como una normal");
        // Los numeros nuevos siguen tras los restaurados (aunque las restauradas caigan).
        var t3 = new TableroBusquedas(new ReglasBusquedas());
        t3.Restaura(new[] { new Busqueda { Num = 41, Tipo = TipoBusqueda.Huerfano, Clave = "z", Objetivo = "fantasma" } });
        t3.Compara(new M(0).Reino("k1", "N", "r").U("r", "k1", rey: true, hijos: new[] { "h2" }).U("h2", "k1").F);
        var nueva = t3.Compara(new M(10).Reino("k1", "N", null).U("h2", "k1").F);
        Check(nueva.Count == 1 && nueva[0].B.Num == 42, "numeracion continua: " + (nueva.Count > 0 ? nueva[0].B.Num : -1));
        Check(Busqueda.Desde("basura") == null && Busqueda.Desde("{\"tipo\":\"Nada\",\"clave\":\"x\"}") == null, "lineas rotas -> null");

        // --- Cadena: trono usurpado -> heredero desposeido ---
        var c = new TableroBusquedas(new ReglasBusquedas());
        c.Compara(new M(0).Reino("k1", "Norte", "rey").U("rey", "k1", rey: true, hijos: new[] { "hijo" }).U("hijo", "k1").U("x", "k1").F);
        c.Compara(new M(10).Reino("k1", "Norte", null).U("hijo", "k1").U("x", "k1").F);
        ev = c.Compara(new M(20).Reino("k1", "Norte", "x").U("hijo", "k1").U("x", "k1", rey: true).F);
        Check(De(ev, EstadoBusqueda.Fallida).Count == 1, "usurpado: fallida");
        var nuevas = De(ev, EstadoBusqueda.Activa);
        Check(nuevas.Count == 1 && nuevas[0].B.Tipo == TipoBusqueda.Pretendiente && nuevas[0].B.Objetivo == "hijo"
              && nuevas[0].B.Titulo.Contains("desposeído"), "y abre «El heredero desposeído»");

        // --- Recompensas ---
        var f = new M(0).Reino("k1", "Norte", "hijo").U("hijo", "k1", rey: true).U("a", "k1").F;
        var rb = new Busqueda { Tipo = TipoBusqueda.Trono, Estado = EstadoBusqueda.Cumplida, Objetivo = "hijo" };
        var r = TableroBusquedas.RecompensaDe(rb, f);
        Check(r != null && r.Id == "hijo" && r.Renombre == 10 && r.Rasgo == "wfx_voz_del_pueblo", "trono cumplido: renombre y rasgo");
        r = TableroBusquedas.RecompensaDe(new Busqueda { Tipo = TipoBusqueda.Amantes, Estado = EstadoBusqueda.Fallida, Objetivo = "muerto", Otro = "a" }, f);
        Check(r != null && r.Id == "a" && r.Rasgo == "wfx_corazon_roto", "amante muerto: el vivo, corazon roto");
        r = TableroBusquedas.RecompensaDe(new Busqueda { Tipo = TipoBusqueda.Amantes, Estado = EstadoBusqueda.Fallida, Objetivo = "hijo", Otro = "a" }, f);
        Check(r == null, "dejaron de ser pareja (ambos vivos): nada");
        Check(TableroBusquedas.RecompensaDe(new Busqueda { Tipo = TipoBusqueda.Venganza, Estado = EstadoBusqueda.Cumplida, Objetivo = "x" }, f) == null, "venganza: nadie a quien premiar");
        foreach (var cat in new[] { "wfx_voz_del_pueblo", "wfx_vengador", "wfx_peregrino", "wfx_corazon_roto" })
            Check(Array.Exists(Catalogo.Rasgos, x => x.Id == cat), "el rasgo de recompensa existe en el catalogo: " + cat);

        // --- Estado social (formato de PeceraWB/Code/Exporta.cs) ---
        var e = EstadoSocial.Lee("{\"id\":\"a7\",\"n\":\"Ana\",\"casa\":\"Casa del Roble\",\"rasgos\":\"valiente\",\"amigo\":\"Bo\",\"rival\":\"\",\"sueno\":\"ser reina (40 %)\",\"rumor\":\"se acuesta con Cid\"}");
        Check(e != null && e.Id == "a7" && e.Casa == "Casa del Roble", "lee una linea de la pecera");
        string tx = e.Texto();
        Check(tx.StartsWith("Ana — Casa del Roble · valiente") && tx.Contains("Amigo: Bo") && !tx.Contains("Rival")
              && tx.Contains("Se dice que Ana se acuesta con Cid"), "texto del HUD: " + tx);
        Check(EstadoSocial.Lee("{\"n\":\"sin id\"}") == null && new EstadoSocial { Nombre = "X" }.Texto() == "", "sin id / sin datos");
        Check(EstadoSocial.LeeTodo(new[] { "{\"id\":\"a1\",\"n\":\"A\"}", "rota", "{\"id\":\"a2\",\"n\":\"B\"}" }).Count == 2, "LeeTodo ignora lineas rotas");

        // --- Frases de recuerdos ---
        var existentes = new HashSet<string> { Frases.Clave("death_lover", 0), Frases.Clave("death_lover", 1) };
        var plan = Frases.Plan("death_lover", true, k => existentes.Contains(k));
        Check(plan.Count == 3 && plan[0].Key == "happiness_dialog_death_lover_2", "se anaden tras las del juego, sin pisarlas");
        var lleno = Frases.Plan("death_lover", true, k => true);
        Check(lleno.Count == 0, "nunca pasa de 24 por tipo");
        int tipos = 0; foreach (string ti in Frases.Tipos) { tipos++; Check(Frases.De(ti, true).Count == Frases.De(ti, false).Count, "es/en iguales: " + ti); }
        Check(tipos >= 30, "tipos de recuerdo: " + tipos);
    }
}
