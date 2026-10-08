using System;
using System.Reflection;
using UnityEngine;

namespace WorldfallExpansion
{
    // Lectura (solo lectura) del estado PUBLICO de Worldfall, por reflexion y sin referenciar su DLL:
    //   FirstPerson.WorldBoxMod.Instance  (public static)
    //     .IsFirstPerson  bool    ¿estas en primera persona?
    //     .Host           Actor   la unidad que controlas
    //     .ViewYaw        float   hacia donde miras (radianes)
    // Leido del binario de Worldfall 0.9.2. Si una actualizacion lo cambia, Disponible queda en false y
    // la brujula sigue en modo dios: nada falla.
    internal static class PuenteWorldfall
    {
        static PropertyInfo pInstancia, pPrimera, pHost, pYaw;
        static float tBusca = -100f;
        static int intentos;
        static bool avisado;

        public static bool Disponible { get { return pInstancia != null; } }

        static void Busca()
        {
            if (pInstancia != null || intentos >= 30) return;
            float real = Time.unscaledTime;
            if (real - tBusca < 10f) return;   // Worldfall puede cargar despues que este plugin
            tBusca = real;
            intentos++;
            try
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = a.GetType("FirstPerson.WorldBoxMod", false);
                    if (t == null) continue;
                    var flags = BindingFlags.Public | BindingFlags.Instance;
                    PropertyInfo inst = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    pPrimera = t.GetProperty("IsFirstPerson", flags);
                    pHost = t.GetProperty("Host", flags);
                    pYaw = t.GetProperty("ViewYaw", flags);
                    if (inst != null && pPrimera != null && pHost != null)
                    {
                        pInstancia = inst;
                        Debug.Log("[WorldfallExp] puente: Worldfall encontrado (" + a.GetName().Name + " " + a.GetName().Version + ")");
                    }
                    else if (!avisado)
                    {
                        avisado = true;
                        Debug.LogWarning("[WorldfallExp] puente: Worldfall ha cambiado (falta Instance/IsFirstPerson/Host); brujula solo en modo dios");
                    }
                    return;
                }
            }
            catch (Exception e) { Estado.Fallo("puente", e); }
        }

        // true si estas en primera persona; host y yaw quedan rellenos.
        public static bool PrimeraPersona(out Actor host, out float yaw)
        {
            host = null; yaw = 0f;
            Busca();
            if (pInstancia == null) return false;
            try
            {
                object wf = pInstancia.GetValue(null, null);
                if (wf == null) return false;
                if (!(bool)pPrimera.GetValue(wf, null)) return false;
                host = pHost.GetValue(wf, null) as Actor;
                if (pYaw != null) yaw = (float)pYaw.GetValue(wf, null);
                return host != null;
            }
            catch (Exception e) { Estado.Fallo("puente_lectura", e); return false; }
        }
    }
}
