// STUBS de Unity, NeoModLoader y Harmony: solo lo que usan los mods de este repo.
using System;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { }
    }

    public enum KeyCode { None = 0, F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }

    public static class Input { public static bool GetKeyDown(KeyCode k) { return false; } }

    public static class Time { public static float unscaledTime { get { return 0f; } } }

    public static class Application { public static string persistentDataPath { get { return ""; } } }
}

namespace NeoModLoader.api
{
    public abstract class BasicMod<T> : UnityEngine.MonoBehaviour where T : BasicMod<T>
    {
        protected abstract void OnModLoad();
    }
}

namespace HarmonyLib
{
    public class Harmony
    {
        public Harmony(string id) { }
        public void PatchAll(Type t) { }
    }

    public class Traverse
    {
        public static Traverse Create(object o) { return new Traverse(); }
        public Traverse Field(string name) { return this; }
        public T GetValue<T>() { return default(T); }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type t, string metodo) { }
    }

    [AttributeUsage(AttributeTargets.Method)] public class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class HarmonyPostfix : Attribute { }
}
