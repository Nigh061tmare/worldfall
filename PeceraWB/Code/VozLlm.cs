using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Voz con Ollama local. Nunca bloquea el juego: una sola hebra en vuelo, cupo por tiempo,
    // y si Ollama no esta, el mod sigue sin frases (modo degradado) y reintenta cada 2 minutos.
    internal static class VozLlm
    {
        static CupoVoz cupo;
        static volatile bool ok;
        static string modelo = "";
        static float proximoSondeo;
        static readonly PoliticaInfluencia politica = new PoliticaInfluencia(new SystemClock());

        public static void Arranca()
        {
            cupo = new CupoVoz(new SystemClock(), (int)Math.Max(5, Cfg.Num("voz_gap_seg")));
            Sondea();
        }

        static string Http(string url, string cuerpo, int timeoutMs)
        {
            var rq = (HttpWebRequest)WebRequest.Create(url);
            rq.Timeout = timeoutMs; rq.ReadWriteTimeout = timeoutMs; rq.Proxy = null;
            if (cuerpo != null)
            {
                rq.Method = "POST"; rq.ContentType = "application/json";
                byte[] b = new UTF8Encoding(false).GetBytes(cuerpo);
                rq.ContentLength = b.Length;
                using (Stream s = rq.GetRequestStream()) s.Write(b, 0, b.Length);
            }
            using (var rs = (HttpWebResponse)rq.GetResponse())
            using (var sr = new StreamReader(rs.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        // Comprueba Ollama y elige modelo: el configurado si existe, si no el primero que no sea de embeddings.
        public static void Sondea()
        {
            var t = new Thread(delegate ()
            {
                try
                {
                    string raw = Http(Cfg.Str("ollama").TrimEnd('/') + "/api/tags", null, 2500);
                    object o;
                    if (!Json.TryParse(raw, out o)) { ok = false; return; }
                    var l = Json.Lista(o, "models");
                    string primero = "", pedido = Cfg.Str("modelo"); bool hay = false;
                    if (l != null)
                        foreach (object m in l)
                        {
                            string n = Json.Str(m, "name");
                            if (n.Length == 0 || n.IndexOf("embed", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            if (primero.Length == 0) primero = n;
                            if (n == pedido) hay = true;
                        }
                    modelo = hay ? pedido : primero;
                    bool antes = ok;
                    ok = modelo.Length > 0;
                    if (ok && !antes) Debug.Log("[PeceraWB] Ollama disponible, modelo " + modelo);
                }
                catch (Exception) { ok = false; }
            });
            t.IsBackground = true;
            t.Start();
        }

        // grande = momento historico (guerra, juicio de rey, facción grande): habla aunque voz=0.
        public static void Pide(string idHabla, string idOtro, double delta, string porQue, bool grande = false)
        {
            try
            {
                if (!Cfg.Bool("voz") && !(grande && Cfg.Bool("voz_grandes"))) return;
                if (!ok)
                {
                    if (Time.unscaledTime >= proximoSondeo) { proximoSondeo = Time.unscaledTime + 120f; Sondea(); }
                    return;
                }
                if (!cupo.TryAcquire()) return;

                string nombre = Mundo.NombreDe(idHabla), otro = Mundo.NombreDe(idOtro);
                string ficha = Mundo.FichaDe(idHabla).Resumen();
                string recuerdos = Mundo.Memoria.ParaPrompt(idHabla, 400);
                string usuario = Prompts.UsuarioVoz(nombre, otro, delta, porQue, Mundo.Afectos.Sentimiento(idHabla, idOtro), ficha, recuerdos, "");
                string url = Cfg.Str("ollama").TrimEnd('/') + "/api/chat";
                string cuerpo = OllamaWire.BodyNativo(modelo, "10m", 2048, 160, Prompts.SistemaVoz, usuario);

                var t = new Thread(delegate ()
                {
                    Replica r = null;
                    try
                    {
                        string raw = Http(url, cuerpo, 25000);
                        r = ReplicaParser.Parse(OllamaWire.Contenido(raw));
                    }
                    catch (Exception) { ok = false; }
                    finally { cupo.Release(); }
                    if (r == null || !r.Ok) return;
                    Replica rf = r;
                    Mundo.EnPrincipal(() => Dice(idHabla, idOtro, nombre, delta, rf));
                });
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception e) { Mundo.Fallo("voz", e); }
        }

        // Ya en el hilo principal: se muestra, se recuerda y (solo en el modelo interno) se matiza el afecto.
        static void Dice(string idHabla, string idOtro, string nombre, double delta, Replica r)
        {
            // Modelos pequenos a veces devuelven JSON roto y el "texto" arrastra claves: se descarta en vez de mostrarlo.
            if (r.Texto.IndexOf("piensa", StringComparison.OrdinalIgnoreCase) >= 0 || r.Texto.IndexOf("actitud", StringComparison.OrdinalIgnoreCase) >= 0
                || r.Texto.IndexOf("\", \"", StringComparison.Ordinal) >= 0) return;
            Mundo.Aviso(nombre + ": «" + r.Texto + "»", 4f);
            Mundo.Memoria.Registra(idHabla, "dijo", idOtro, "dijo: " + r.Texto, 0.5);
            Mundo.Disco.Append("voces.jsonl", "{\"dia\":" + Mundo.Dia() + ",\"quien\":\"" + Json.Escape(nombre) + "\",\"texto\":\"" + Json.Escape(r.Texto)
                + "\",\"piensa\":\"" + Json.Escape(r.Piensa) + "\",\"actitud\":\"" + r.Actitud + "\"}");
            double d = politica.Decide(idHabla + "|" + idOtro, delta, r.Actitud);
            if (d != 0)
            {
                Mundo.Afectos.Evento(idHabla, idOtro, d < 0 ? TipoEvento.Agravio : TipoEvento.Perdon, Math.Abs(d));
                if (d > 0) Mundo.Cultura.Suceso("clemencia", 0.5);
            }
        }
    }
}
