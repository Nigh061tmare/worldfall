using System;
using HarmonyLib;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Muertes: duelo de la pareja, hito de reyes y asesinatos, venganza en la cultura.
    [HarmonyPatch]
    internal static class HookMuerte
    {
        static readonly AccessTools.FieldRef<Actor, BaseSimObject> AtacadoPor =
            AccessTools.FieldRefAccess<Actor, BaseSimObject>("attackedBy");

        [HarmonyPatch(typeof(Actor), "die")]
        [HarmonyPrefix]
        static void Antes(Actor __instance)
        {
            try
            {
                if (!Mundo.Activo || __instance == null || !__instance.isSapient()) return;
                Mundo.AlMorir(__instance, AtacadoPor(__instance) as Actor);
            }
            catch (Exception e) { Mundo.Fallo("muerte", e); }
        }
    }

    // Guerras: crónica, rivalidad entre reyes, cultura del honor.
    [HarmonyPatch]
    internal static class HookGuerra
    {
        [HarmonyPatch(typeof(DiplomacyManager), "startWar")]
        [HarmonyPostfix]
        static void Despues(Kingdom pAttacker, Kingdom pDefender)
        {
            try { if (Mundo.Activo) Mundo.AlDeclararGuerra(pAttacker, pDefender); }
            catch (Exception e) { Mundo.Fallo("guerra", e); }
        }
    }

    internal static partial class Mundo
    {
        public static void AlMorir(Actor v, Actor asesino)
        {
            string iv = Id(v);
            // El juego puede llamar a die() mas de una vez por la misma unidad: solo se cuenta la primera.
            if (muertos.Count > 5000) muertos.Clear();
            if (!muertos.Add(iv)) return;
            Registra(v);
            string nv = Nombres[iv];
            bool rey = v.isKing();
            string reino = v.kingdom != null && !v.kingdom.wild && v.kingdom.data != null ? v.kingdom.data.name : "";
            Actor amante = v.hasLover() ? v.lover : null;

            if (asesino != null && asesino != v && asesino.isSapient())
            {
                Registra(asesino);
                string ia = Id(asesino), na = Nombres[ia];
                // En un mundo grande las muertes corrientes no son noticia: solo reyes y figuras notables.
                if (rey || Notable(v) || Notable(asesino))
                    Hito(rey ? "regicidio" : "muerte", na + " mato a " + nv + (rey ? ", rey de " + reino : ""), rey ? 8 : 4);
                Cultura.Suceso("venganza", rey ? 3 : 0.2);
                Inspira(ia, "defensa", 0.15);
                if (amante != null && amante.isAlive())
                {
                    string il = Id(amante); Registra(amante);
                    Afectos.Evento(il, ia, TipoEvento.Duelo, 0.9);
                    Memoria.Registra(il, "duelo", ia, na + " mato a " + nv + ", a quien amaba", 1.0);
                    if (Notable(amante)) VozLlm.Pide(il, ia, -1, na + " acaba de matar a " + nv + ", su amado");
                }
                Memoria.Registra(ia, "muerte", iv, "mato a " + nv, 0.6);
            }
            else if (rey)
            {
                Hito("trono", "Muere " + nv + ", rey de " + reino, 7);
                Cultura.Suceso("honor", 1);
            }
            else if (amante != null && amante.isAlive())
            {
                string il = Id(amante); Registra(amante);
                if (Notable(amante) || Notable(v)) Hito("viudez", Nombres[il] + " llora la muerte de " + nv, 3);
                Memoria.Registra(il, "duelo", iv, nv + " murio", 0.9);
                Afectos.Evento(il, iv, TipoEvento.Duelo, 0.5);
                if (Notable(amante)) VozLlm.Pide(il, iv, -1, nv + " ha muerto");
            }
            Vivos.Remove(iv);
            // Sucesion: solo importa al morir un rey/soberano con familia. Se resuelve con el arbol
            // alimentado por el muestreo (aristas padre->hijo) y solo anota en la pecera: no toca al juego.
            if (rey || Notable(v))
            {
                Arbol.Muere(iv);
                var herederos = Arbol.Herederos(iv);
                if (herederos.Count > 0)
                {
                    var reinoIds = new System.Collections.Generic.List<string>(); Actor rey2 = v.kingdom != null ? v.kingdom.king : null;
                    foreach (var a in Vivos.Values)
                        if (a != null && a.kingdom != null && a.kingdom == v.kingdom) reinoIds.Add(Id(a));
                    var res = Sucesion.Elige(iv, Arbol, reinoIds, Afectos, FichaDe, 0.25);
                    if (res.Sucesor.Length > 0)
                    {
                        string s = NombreDe(res.Sucesor);
                        Hito("sucesion", "Sucede a " + nv + " en " + (v.kingdom != null && v.kingdom.data != null ? v.kingdom.data.name : "el trono") + ": " + s + (res.Disputada ? " (disputado con " + NombreDe(res.Rival) + ")" : ""), 6);
                        Memoria.Registra(res.Sucesor, "trono", iv, "heredo el trono de " + nv, 1);
                        if (res.Disputada) Memoria.Registra(res.Rival, "trono", res.Sucesor, res.Sucesor + " le gano el trono de " + nv, 1);
                        if (Notable(rey2)) VozLlm.Pide(res.Sucesor, iv, 1, "hereda el trono de " + nv, true);
                        // Nivel 3: el heredero recibe prestigio y el perdedor de una disputa deja la corte.
                        Hereda(res.Sucesor);
                        TrasDisputa(res);
                    }
                }
            }
        }

        public static void AlDeclararGuerra(Kingdom atq, Kingdom def)
        {
            if (atq == null || def == null || atq.data == null || def.data == null) return;
            Hito("guerra", atq.data.name + " declara la guerra a " + def.data.name, 6);
            Cultura.Suceso("honor", 1);
            Actor ra = atq.king, rd = def.king;
            if (ra != null && rd != null && ra.isAlive() && rd.isAlive())
            {
                Registra(ra); Registra(rd);
                Afectos.Evento(Id(rd), Id(ra), TipoEvento.Agravio, 0.6);
                Afectos.Evento(Id(ra), Id(rd), TipoEvento.Competencia, 0.6);
                Memoria.Registra(Id(rd), "guerra", Id(ra), Nombres[Id(ra)] + " le declaro la guerra", 1.0);
                VozLlm.Pide(Id(ra), Id(rd), 1, "ha declarado la guerra a " + def.data.name + " y a su rey " + Nombres[Id(rd)], true);
            }
        }
    }
}
