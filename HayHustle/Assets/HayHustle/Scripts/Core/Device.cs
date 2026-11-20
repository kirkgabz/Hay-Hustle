using UnityEngine;

namespace HayHustle
{
    /// <summary>What the game is running on: browser or not, touch screen or mouse and keyboard.</summary>
    public static class Device
    {
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>0 = automatic, 1 = always touch controls, 2 = never (mouse and keyboard).</summary>
        public static int TouchMode { get; private set; }
        static bool sawTouch;

        public static void Load() => TouchMode = Mathf.Clamp(PlayerPrefs.GetInt("hh_touch", 0), 0, 2);

        public static void SetTouchMode(int mode)
        {
            TouchMode = Mathf.Clamp(mode, 0, 2);
            PlayerPrefs.SetInt("hh_touch", TouchMode);
            PlayerPrefs.Save();
        }

        /// <summary>Called when a finger touches the screen (so touch laptops/tablets switch to touch controls).</summary>
        public static void NoteTouch() => sawTouch = true;

        /// <summary>Use touch controls and the larger phone layout.</summary>
        public static bool Touch => TouchMode == 1 || (TouchMode == 0 && (Application.isMobilePlatform || sawTouch));

        /// <summary>A phone held upright: the game asks to turn it sideways.</summary>
        public static bool Portrait => Touch && Screen.height > Screen.width;

        public static bool IsPhoneOrTablet => Application.isMobilePlatform;
    }
}
