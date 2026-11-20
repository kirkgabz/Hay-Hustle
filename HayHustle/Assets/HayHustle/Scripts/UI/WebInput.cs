using System.Runtime.InteropServices;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Typing on a phone in the browser: shows a real web text box over the game (so the phone keyboard opens),
    /// then passes the text back. See Plugins/WebGL/HayHustleWeb.jslib.
    /// </summary>
    public static class WebInput
    {
        public const int Text = 0, Password = 1, Email = 2;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void HH_InputOpen(string title, string value, int kind, int maxLength);
        [DllImport("__Internal")] static extern int HH_InputState();
        [DllImport("__Internal")] static extern string HH_InputValue();
        [DllImport("__Internal")] static extern void HH_InputReset();
        public static bool Available => true;
#else
        static void HH_InputOpen(string title, string value, int kind, int maxLength) { }
        static int HH_InputState() => -1;
        static string HH_InputValue() => "";
        static void HH_InputReset() { }
        public static bool Available => false;
#endif

        static System.Action<string> callback;
        public static bool IsOpen => callback != null;

        public static void Open(string title, string value, int kind, int maxLength, System.Action<string> done)
        {
            if (!Available) return;
            callback = done;
            HH_InputReset();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;   // let the keys reach the web text box
#endif
            HH_InputOpen(title, value ?? "", kind, maxLength);
        }

        /// <summary>Called every frame: delivers the text when the player presses OK (Cancel keeps the old text).</summary>
        public static void Poll()
        {
            if (callback == null) return;
            int state = HH_InputState();
            if (state <= 0) return;
            var cb = callback;
            callback = null;
            string value = state == 1 ? HH_InputValue() : null;
            HH_InputReset();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
            if (value != null) cb(value);
        }
    }
}
