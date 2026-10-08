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
}
