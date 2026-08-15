// Minimal stubs so the real CKIEditor sources compile outside Unity.
namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        public static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);
        public static float Floor(float f) => (float)System.Math.Floor(f);
    }
}

namespace Framewerk.UI.List
{
    public interface IListItemDataProvider { }
}

namespace Crosstales.FB
{
    internal static class Placeholder { }
}
