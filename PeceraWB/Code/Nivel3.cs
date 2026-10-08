using System;
using System.Collections.Generic;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Nivel 3: consecuencias reales en el mundo. Es lo unico de PeceraWB que ESCRIBE en el juego,
    // por eso va aparte, apagado por defecto (nivel3=0) y con tope de acciones por hora de mundo.
    // Solo usa llamadas ya probadas por otros mods en la build 719 (setCity, removeLeader).
    // Cada accion queda en PeceraWB\mundos\<mundo>\nivel3.log para poder revisarla.
    internal static partial class Mundo
    {
        static readonly Queue<double> accionesN3 = new Queue<double>();

        public static bool Nivel3Activo { get { return Activo && Cfg.Bool("nivel3"); } }

        static bool CupoN3()
        {
            double wt = ultimoWt;
            while (accionesN3.Count > 0 && wt - accionesN3.Peek() > 3600) accionesN3.Dequeue();
            if (accionesN3.Count >= (int)Math.Max(1, Cfg.Num("nivel3_max_hora"))) return false;
            accionesN3.Enqueue(wt);
            return true;
        }

        static void LogN3(string linea)
        {
            try { Disco.Append("nivel3.log", DateTime.Now.ToString("s") + " " + linea); }
            catch (Exception) { }
        }

        // Destierro: el personaje pierde su ciudad (y el cargo de lider si lo tenia). Reversible:
        // puede volver a asentarse solo. Nunca se destierra a un rey.
        public static bool Destierra(Actor a, string motivo)
        {
            if (!Nivel3Activo || a == null || !a.isAlive() || a.isKing()) return false;
            if (!CupoN3()) { LogN3("tope por hora: no se destierra a " + NombreDe(Id(a))); return false; }
            try
            {
                City c = a.city;
                if (c == null) return false;
                if (c.leader == a) c.removeLeader();
                a.setCity(null);
                string n = NombreDe(Id(a));
                Hito("destierro", n + " es desterrado: " + motivo, 5);
                Memoria.Registra(Id(a), "destierro", Id(a), "fue desterrado: " + motivo, 1.0);
                Afectos.Evento(Id(a), Id(a), TipoEvento.Agravio, 0.2);
                LogN3("DESTIERRO " + n + " (" + Id(a) + ") motivo=" + motivo);
                return true;
            }
            catch (Exception e) { Fallo("destierro", e); return false; }
        }

        // Disputa de sucesion: quien pierde el trono abandona la corte (se le destierra de la ciudad).
        public static void TrasDisputa(ResultadoSucesion res)
        {
            if (!Nivel3Activo || res == null || !res.Disputada || res.Rival.Length == 0) return;
            Actor rival;
            if (!Vivos.TryGetValue(res.Rival, out rival)) return;
            if (Destierra(rival, "perdio la disputa por el trono ante " + NombreDe(res.Sucesor)))
                Cultura.Suceso("venganza", 1);
        }

        // El heredero recibe prestigio del difunto. Apagado si Worldsmith ya lo hace
        // (dynastic_inheritance) para no duplicar: se controla con nivel3_herencia.
        public static void Hereda(string idHeredero)
        {
            if (!Nivel3Activo || !Cfg.Bool("nivel3_herencia")) return;
            Actor h;
            if (!Vivos.TryGetValue(idHeredero, out h) || !h.isAlive()) return;
            try { h.addRenown(5); LogN3("HERENCIA +5 renombre a " + NombreDe(idHeredero)); }
            catch (Exception e) { Fallo("herencia", e); }
        }
    }
}
