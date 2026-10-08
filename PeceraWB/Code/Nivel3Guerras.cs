using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Nivel 3 avanzado: ESCRIBE en el mundo. Cada parte tiene interruptor propio (apagado por defecto)
    // y ademas exige nivel3=1; comparte el tope de acciones por hora (CupoN3) y queda en nivel3.log.
    //
    //   nivel3_guerras=1           dos reyes que se odian (sentimiento <= -nivel3_guerra_odio) entran en guerra
    //   nivel3_guerras_civiles=1   faccion enfrentada a la del rey cuyo lider manda una ciudad (no capital):
    //                              esa ciudad se alza como reino propio y estalla una guerra de rebelion
    //   nivel3_rasgos=1            la pecera marca a la gente con rasgos del Arsenal de worldfall-expansion
    //                              (desterrado, pretendiente, corazon roto). Sin ese mod, addTrait no hace nada.
    //
    // LEIDO del binario build 719: DiplomacyManager.startWar(Kingdom, Kingdom, WarTypeAsset, bool) es INTERNAL
    // (por eso no se puede llamar como startWar(a, b)), y City.makeOwnKingdom(Actor, bool, bool) tambien.
    // Se invocan por reflexion, igual que hace el propio juego en DiplomacyHelpersRebellion.startRebellion.
    // Si una actualizacion cambia esas firmas, la parte afectada se apaga sola y lo dice en el log.
    internal static partial class Mundo
    {
        static MethodInfo mGuerra, mReinoPropio;
        static bool reflejoBuscado;
        static double tGuerras, tCiviles;

        static void Reflejo()
        {
            if (reflejoBuscado) return;
            reflejoBuscado = true;
            try
            {
                mGuerra = AccessTools.Method(typeof(DiplomacyManager), "startWar", new[] { typeof(Kingdom), typeof(Kingdom), typeof(WarTypeAsset), typeof(bool) });
                mReinoPropio = AccessTools.Method(typeof(City), "makeOwnKingdom", new[] { typeof(Actor), typeof(bool), typeof(bool) });
            }
            catch (Exception e) { Fallo("reflejo", e); }
            if (mGuerra == null) Debug.LogWarning("[PeceraWB] nivel3: no encuentro DiplomacyManager.startWar; guerras desactivadas");
            if (mReinoPropio == null) Debug.LogWarning("[PeceraWB] nivel3: no encuentro City.makeOwnKingdom; guerras civiles desactivadas");
        }

        static void TickNivel3Guerras(double wt)
        {
            if (!Nivel3Activo) return;
            if (Cfg.Bool("nivel3_guerras") && wt - tGuerras >= 60) { tGuerras = wt; GuerrasPorOdio(); }
            if (Cfg.Bool("nivel3_guerras_civiles") && wt - tCiviles >= 120) { tCiviles = wt; GuerraCivil(); }
        }

        static void GuerrasPorOdio()
        {
            Reflejo();
            if (mGuerra == null) return;
            try
            {
                var reyes = new List<ReyInfo>();
                var reinoDe = new Dictionary<string, Kingdom>();
                var reinoPorClave = new Dictionary<string, Kingdom>();
                foreach (var kv in Vivos)
                {
                    Actor a = kv.Value;
                    if (a == null || !a.isAlive() || !a.isKing() || a.kingdom == null || a.kingdom.wild || a.kingdom.data == null) continue;
                    string rk = a.kingdom.data.name + "#" + reyes.Count;
                    reyes.Add(new ReyInfo { Id = kv.Key, Reino = rk });
                    reinoDe[kv.Key] = a.kingdom;
                    reinoPorClave[rk] = a.kingdom;
                }
                if (reyes.Count < 2) return;
                var pares = Diplomacia.GuerrasPorOdio(reyes, Afectos.Sentimiento, (ra, rb) =>
                {
                    Kingdom ka = reinoPorClave[ra], kb = reinoPorClave[rb];
                    if (ka == kb || ka.isInWarWith(kb)) return true;
                    return ka.hasAlliance() && kb.hasAlliance() && ka.getAlliance() == kb.getAlliance();
                }, Math.Max(0.2, Cfg.Num("nivel3_guerra_odio")), 1);
                foreach (var p in pares)
                {
                    if (!CupoN3()) { LogN3("tope por hora: no hay guerra entre " + NombreDe(p.Key) + " y " + NombreDe(p.Value)); return; }
                    Kingdom atq = reinoDe[p.Key], def = reinoDe[p.Value];
                    mGuerra.Invoke(World.world.diplomacy, new object[] { atq, def, WarTypeLibrary.normal, true });
                    // El hito «X declara la guerra a Y» lo pone HookGuerra (postfix de startWar).
                    LogN3("GUERRA por odio: " + NombreDe(p.Key) + " (" + atq.data.name + ") contra " + NombreDe(p.Value) + " (" + def.data.name + ")");
                }
            }
            catch (Exception e) { Fallo("guerra_odio", e); }
        }

        static void GuerraCivil()
        {
            Reflejo();
            if (mGuerra == null || mReinoPropio == null || tensionesConocidas.Count == 0) return;
            try
            {
                var tensiones = new List<KeyValuePair<string, string>>();
                foreach (string t in tensionesConocidas)
                {
                    int i = t.IndexOf('|');
                    if (i > 0) tensiones.Add(new KeyValuePair<string, string>(t.Substring(0, i), t.Substring(i + 1)));
                }
                string rebelde = Diplomacia.Rebelde(tensiones, id =>
                {
                    Actor a;
                    if (!Vivos.TryGetValue(id, out a) || a == null || !a.isAlive() || a.kingdom == null || a.kingdom.wild) return null;
                    City c = a.city;
                    return new LiderInfo
                    {
                        Id = id, Reino = a.kingdom.data != null ? a.kingdom.data.name : "",
                        EsRey = a.isKing(), EsLiderCiudad = a.isCityLeader(),
                        CiudadEsCapital = c != null && a.kingdom.capital == c,
                        CiudadesDelReino = a.kingdom.countCities(),
                    };
                });
                if (rebelde == null) return;
                Actor r = Vivos[rebelde];
                if (!CupoN3()) { LogN3("tope por hora: " + NombreDe(rebelde) + " no se alza"); return; }
                City ciudad = r.city;
                Kingdom viejo = r.kingdom;
                string nombreViejo = viejo.data.name;
                // Mismo orden que el juego (DiplomacyHelpersRebellion.startRebellion): fuera el cargo, reino propio, vuelve a su ciudad.
                if (r.isCityLeader()) ciudad.removeLeader();
                var nuevo = mReinoPropio.Invoke(ciudad, new object[] { r, true, false }) as Kingdom;
                if (nuevo == null) { LogN3("la ciudad de " + NombreDe(rebelde) + " no pudo alzarse"); return; }
                r.joinCity(ciudad);
                mGuerra.Invoke(World.world.diplomacy, new object[] { viejo, nuevo, WarTypeLibrary.rebellion, true });
                Hito("guerra_civil", "Guerra civil en " + nombreViejo + ": " + NombreDe(rebelde) + " se alza con su ciudad", 8);
                Cultura.Suceso("venganza", 2);
                LogN3("GUERRA CIVIL: " + NombreDe(rebelde) + " se alza contra " + nombreViejo);
                // La tension ya estallo: no se repite con los mismos lideres.
                tensionesConocidas.RemoveWhere(t => t.Contains(rebelde));
            }
            catch (Exception e) { Fallo("guerra_civil", e); }
        }

        // Rasgos del Arsenal (worldfall-expansion). addTrait devuelve false si el rasgo no existe: sin el otro mod no pasa nada.
        public static void MarcaRasgo(Actor a, string rasgo, string motivo)
        {
            if (!Nivel3Activo || !Cfg.Bool("nivel3_rasgos") || a == null) return;
            try
            {
                if (a.isAlive() && a.addTrait(rasgo, false)) LogN3("RASGO " + rasgo + " a " + NombreDe(Id(a)) + ": " + motivo);
            }
            catch (Exception e) { Fallo("rasgo", e); }
        }
    }
}
