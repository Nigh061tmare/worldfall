// STUBS de Unity, NeoModLoader y Harmony: solo lo que usan los mods de este repo.
// Unity: API publica estable. NML: leida de NeoModLoader.dll (BasicMod, ModDeclare.FolderPath).
using System;
using System.Reflection;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public T GetComponent<T>() { return default(T); }
    }
    public class Transform : Component { public Vector3 position; public void SetParent(Transform p, bool worldPositionStays) { } }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta; }
    public class GameObject : Object
    {
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get { return null; } }
        public T GetComponent<T>() { return default(T); }
        public void SetActive(bool v) { }
    }
    public class Font : Object { }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class Camera : Behaviour { public static Camera main { get { return null; } } }
    public class Sprite : Object { }

    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector3 { public float x, y, z; }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize;
        public TextAnchor alignment;
        public FontStyle fontStyle;
        public GUIStyleState normal = new GUIStyleState();
    }
    public class GUISkin { public GUIStyle label; }
    public class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public string text { get; set; }
    }
    public static class GUI
    {
        public static GUISkin skin { get { return null; } }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static bool Button(Rect position, string text, GUIStyle style) { return false; }
    }

    public static class Screen { public static int width { get { return 0; } } public static int height { get { return 0; } } }
    public static class Mathf
    {
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
    }

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

namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Behaviour { public UnityEngine.Color color; public bool raycastTarget; }
    public class Text : Graphic
    {
        public string text; public UnityEngine.Font font; public int fontSize; public UnityEngine.TextAnchor alignment;
        public bool supportRichText; public UnityEngine.HorizontalWrapMode horizontalOverflow;
    }
}

namespace NeoModLoader.api
{
    public class ModDeclare { public string FolderPath { get; private set; } }

    public abstract class BasicMod<T> : UnityEngine.MonoBehaviour where T : BasicMod<T>
    {
        public static T Instance { get; private set; }
        public ModDeclare GetDeclaration() { return null; }
        public virtual void Init() { }
        protected abstract void OnModLoad();
    }
}

namespace NeoModLoader.General
{
    public static class PowerButtonCreator
    {
        public static PowerButton CreateGodPowerButton(string pGodPowerId, UnityEngine.Sprite pIcon, UnityEngine.Transform pParent = null, UnityEngine.Vector2 pLocalPosition = default(UnityEngine.Vector2)) { return null; }
        public static void AddButtonToTab(PowerButton button, PowersTab tab, int? siblingIndex = null) { }
    }
}

namespace NeoModLoader.General.UI.Tab
{
    public static class TabManager
    {
        public static PowersTab CreateTab(string name, string pTitleKey, string pDescKey, UnityEngine.Sprite pIcon, string pOptionDescKey = "hotkey_tip_tab_other") { return null; }
    }
}

namespace HarmonyLib
{
    public class HarmonyMethod { public HarmonyMethod(MethodInfo method) { } }

    public class Harmony
    {
        public Harmony(string id) { }
        public void PatchAll(Type t) { }
        public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null) { return null; }
    }

    public static class AccessTools
    {
        public static System.Reflection.MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null) { return null; }
        // En Harmony real devuelve «ref F»; aqui sin ref para poder compilar los stubs en C# 5.
        public delegate F FieldRef<in T, F>(T instance);
        public static FieldRef<T, F> FieldRefAccess<T, F>(string fieldName) { return null; }
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
