using UnityEngine;

namespace HayHustle
{
    /// <summary>Player settings stored on this device (volume, graphics quality, confirmation popups).</summary>
    public static class Settings
    {
        public static float Volume { get; private set; } = 0.8f;
        public static bool HighQuality { get; private set; } = true;
        public static bool AskConfirm { get; private set; } = true;

        public static void Load()
        {
            Volume = PlayerPrefs.GetFloat("hh_volume", 0.8f);
            HighQuality = PlayerPrefs.GetInt("hh_quality", Application.isMobilePlatform ? 0 : 1) == 1;   // phones start on Low (faster)
            AskConfirm = PlayerPrefs.GetInt("hh_confirm", 1) == 1;
            Apply();
        }

        public static void SetVolume(float v)
        {
            Volume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("hh_volume", Volume);
            AudioListener.volume = Volume;
        }

        public static void SetHighQuality(bool high)
        {
            HighQuality = high;
            PlayerPrefs.SetInt("hh_quality", high ? 1 : 0);
            Apply();
        }

        public static void SetAskConfirm(bool ask)
        {
            AskConfirm = ask;
            PlayerPrefs.SetInt("hh_confirm", ask ? 1 : 0);
        }

        public static void Apply()
        {
            AudioListener.volume = Volume;
            int levels = QualitySettings.names.Length;
            if (levels > 0) QualitySettings.SetQualityLevel(HighQuality ? levels - 1 : 0, true);
            if (WorldBuilder.Sun) WorldBuilder.Sun.shadows = HighQuality ? LightShadows.Soft : LightShadows.None;
            QualitySettings.shadowDistance = HighQuality ? 70f : 0f;
            if (WorldBuilder.PostVolume) WorldBuilder.PostVolume.enabled = HighQuality;
            // Phones: draw the 3D world a bit below screen resolution on Low (much faster; the menus stay sharp).
            if (Application.isMobilePlatform && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
                urp.renderScale = HighQuality ? 1f : 0.75f;
            if (WorldBuilder.CameraData)
                WorldBuilder.CameraData.antialiasing = HighQuality
                    ? UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing
                    : UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
        }
    }
}
