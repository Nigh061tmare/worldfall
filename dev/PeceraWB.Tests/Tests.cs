using System;
using System.Collections.Generic;
using Pecera.Core;

// Tests del Nivel 3 avanzado de PeceraWB (Core/Diplomacia.cs) y del puente con worldfall-expansion.
static class Tests
{
    static int ok, ko;
    static void Check(bool c, string que) { if (c) ok++; else { ko++; Console.WriteLine("FALLA: " + que); } }

    static int Main()
    {
        Guerras();
        Civil();
        Intervenciones();
        Console.WriteLine(ok + " comprobaciones OK (PeceraWB), " + ko + " fallos");
        return ko == 0 ? 0 : 1;
    }

    static void Guerras()
    {
        var reyes = new List<ReyInfo> { new ReyInfo { Id = "a", Reino = "N" }, new ReyInfo { Id = "b", Reino = "S" }, new ReyInfo { Id = "c", Reino = "E" } };
        var odio = new Dictionary<string, double> { { "a|b", -0.8 }, { "b|a", -0.9 }, { "a|c", -0.6 }, { "c|b", -0.2 } };
        Func<string, string, double> s = (x, y) => { double v; return odio.TryGetValue(x + "|" + y, out v) ? v : 0; };
        var r = Diplomacia.GuerrasPorOdio(reyes, s, (ra, rb) => false, 0.5, 5);
        Check(r.Count == 2, "dos guerras posibles (a-b una sola vez, a-c): " + r.Count);
        Check(r[0].Key == "b" && r[0].Value == "a", "ataca quien mas odia primero");
        Check(Diplomacia.GuerrasPorOdio(reyes, s, (ra, rb) => false, 0.5, 1).Count == 1, "tope max");
        Check(Diplomacia.GuerrasPorOdio(reyes, s, (ra, rb) => true, 0.5, 5).Count == 0, "ya en guerra o aliados: nada");
        Check(Diplomacia.GuerrasPorOdio(reyes, s, (ra, rb) => false, 0.95, 5).Count == 0, "odio insuficiente: nada");
        Check(Diplomacia.GuerrasPorOdio(null, s, (ra, rb) => false, 0.5, 5).Count == 0, "sin reyes");
    }

    static void Civil()
    {
        var info = new Dictionary<string, LiderInfo>
        {
            { "rey", new LiderInfo { Id = "rey", Reino = "N", EsRey = true, EsLiderCiudad = true, CiudadEsCapital = true, CiudadesDelReino = 3 } },
            { "duque", new LiderInfo { Id = "duque", Reino = "N", EsLiderCiudad = true, CiudadesDelReino = 3 } },
            { "capi", new LiderInfo { Id = "capi", Reino = "N", EsLiderCiudad = true, CiudadEsCapital = true, CiudadesDelReino = 3 } },
            { "otro", new LiderInfo { Id = "otro", Reino = "S", EsLiderCiudad = true, CiudadesDelReino = 3 } },
            { "solo", new LiderInfo { Id = "solo", Reino = "U", EsRey = true, CiudadesDelReino = 1 } },
            { "unico", new LiderInfo { Id = "unico", Reino = "U", EsLiderCiudad = true, CiudadesDelReino = 1 } },
        };
        Func<string, LiderInfo> f = id => { LiderInfo l; return info.TryGetValue(id, out l) ? l : null; };
        Func<string, string, KeyValuePair<string, string>> T = (a, b) => new KeyValuePair<string, string>(a, b);
        Check(Diplomacia.Rebelde(new[] { T("rey", "duque") }, f) == "duque", "el duque enfrentado al rey se alza");
        Check(Diplomacia.Rebelde(new[] { T("duque", "rey") }, f) == "duque", "en cualquier orden");
        Check(Diplomacia.Rebelde(new[] { T("rey", "capi") }, f) == null, "la capital no se alza");
        Check(Diplomacia.Rebelde(new[] { T("rey", "otro") }, f) == null, "reinos distintos: no es guerra civil");
        Check(Diplomacia.Rebelde(new[] { T("duque", "otro") }, f) == null, "sin rey en la tension: nada");
        Check(Diplomacia.Rebelde(new[] { T("solo", "unico") }, f) == null, "reino de una sola ciudad: nada");
        Check(Diplomacia.Rebelde(new[] { T("rey", "fantasma") }, f) == null && Diplomacia.Rebelde(null, f) == null, "datos que faltan");
    }

    static void Intervenciones()
    {
        var iv = Intervencion.Lee("{\"t\":\"wfx_bendicion\",\"id\":\"a12\",\"n\":\"Ana\"}");
        Check(iv != null && iv.Id == "a12" && iv.Nombre == "Ana", "lee la linea de worldfall-expansion");
        Check(iv.Hito()[1] == "Los dioses bendijeron a Ana", "hito de cronica");
        foreach (string t in new[] { "wfx_bendicion", "wfx_maldicion", "wfx_juramento", "wfx_destierro", "wfx_discordia", "wfx_reconciliacion", "wfx_bestia" })
            Check(new Intervencion { Tipo = t, Id = "x", Nombre = "X" }.Hito() != null, "todos los poderes tienen hito: " + t);
        Check(new Intervencion { Tipo = "otro" }.Hito() == null, "tipo desconocido: nada");
        Check(Intervencion.Lee("basura") == null && Intervencion.Lee("{\"t\":\"x\"}") == null, "lineas rotas");
        Check(Intervencion.Lee("{\"t\":\"wfx_maldicion\",\"id\":\"a1\"}").Nombre == "a1", "sin nombre: el id");
    }
}
