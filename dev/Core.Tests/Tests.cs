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

    static int Main()
    {
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
}
