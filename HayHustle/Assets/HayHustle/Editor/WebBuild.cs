using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HayHustle.EditorTools
{
    /// <summary>
    /// Menu: Hay Hustle > Build for Web (Vercel).
    /// Builds the browser version into the WebBuild folder next to the Unity project (the repo root),
    /// with the Hay Hustle web page (phone friendly, installable to the home screen) and a vercel.json.
    /// Push the repo to GitHub and Vercel publishes it.
    /// </summary>
    public static class WebBuild
    {
        static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "WebBuild"));

        [MenuItem("Hay Hustle/Build for Web (Vercel)")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Build Hay Hustle for the web",
                    "This switches the project to Web (WebGL) and builds it into:\n" + OutputPath +
                    "\n\nThe first time can take 10-30 minutes. Keep Unity open until it finishes.", "Build", "Cancel"))
                return;
            Build(true);
        }

        /// <summary>For command-line builds: Unity -batchmode -quit -executeMethod HayHustle.EditorTools.WebBuild.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            if (!Build(false)) EditorApplication.Exit(1);
        }

        static bool Build(bool showDialogs)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Fail("Could not switch to Web. In Unity Hub, add the \"Web Build Support\" module to this Unity version.", showDialogs);
                return false;
            }

            PrepareForWeb();
            PlayerSettings.WebGL.template = "PROJECT:HayHustle";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;      // works on any web host without special settings
            PlayerSettings.WebGL.dataCaching = true;                // the phone keeps the game files: faster next start
            PlayerSettings.WebGL.nameFilesAsHashes = true;          // new file names per build: phones never keep an old version
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.runInBackground = false;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

            string output = OutputPath;
            if (Directory.Exists(output))
            {
                // Keep nothing from older builds (old hashed files would pile up).
                foreach (var d in Directory.GetDirectories(output)) Directory.Delete(d, true);
                foreach (var f in Directory.GetFiles(output)) File.Delete(f);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail($"The web build failed ({report.summary.totalErrors} errors). See the Console for details.", showDialogs);
                return false;
            }

            if (!File.Exists(Path.Combine(output, "index.html")) || !Directory.Exists(Path.Combine(output, "Build")))
            {
                Fail("Unity finished, but no web files were made. If the Console says \"Build target 'WebGL' not supported\", " +
                     "close Unity completely and open the project again (the Web module only loads when Unity starts), then build again.", showDialogs);
                return false;
            }
            WriteVercelConfig(output);
            long bytes = Directory.GetFiles(output, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            string msg = $"Done! The web version is in:\n{output}\n\nSize: {bytes / (1024f * 1024f):0.0} MB\n\n" +
                         "Next: commit and push to GitHub - Vercel publishes it automatically.";
            Debug.Log("Hay Hustle: " + msg.Replace("\n", " "));
            if (showDialogs) EditorUtility.DisplayDialog("Hay Hustle web build", msg, "OK");
            return true;
        }

        /// <summary>
        /// The farm is built from code at runtime, so Unity can't see what it needs and would leave parts out of the web build:
        /// - colliders (the ground!) -> keep all engine code;
        /// - the URP Lit shader and its emission/fog variants -> two material assets in Resources keep them in.
        /// </summary>
        static void PrepareForWeb()
        {
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);

            const string folder = "Assets/HayHustle/Resources/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/HayHustle/Resources", "Materials");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("Hay Hustle: URP Lit shader not found"); return; }
            MakeMaterial(folder + "/HH_Lit.mat", lit, false);
            MakeMaterial(folder + "/HH_LitEmissive.mat", lit, true);

            // Keep the linear fog variants (fog is switched on from code, not in the scene).
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs != null && gs.Length > 0)
            {
                var so = new SerializedObject(gs[0]);
                var strip = so.FindProperty("m_FogStripping");
                var keep = so.FindProperty("m_FogKeepLinear");
                if (strip != null) strip.intValue = 1;   // custom
                if (keep != null) keep.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
        }

        static void MakeMaterial(string path, Shader shader, bool emissive)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
        }

        static void WriteVercelConfig(string output)
        {
            // Game files have unique names per build, so phones may cache them for a long time;
            // the page itself is always checked for a newer version.
            const string json = @"{
  ""headers"": [
    {
      ""source"": ""/Build/(.*)"",
      ""headers"": [ { ""key"": ""Cache-Control"", ""value"": ""public, max-age=31536000, immutable"" } ]
    },
    {
      ""source"": ""/(.*)\\.(html|webmanifest)"",
      ""headers"": [ { ""key"": ""Cache-Control"", ""value"": ""no-cache"" } ]
    },
    {
      ""source"": ""/"",
      ""headers"": [ { ""key"": ""Cache-Control"", ""value"": ""no-cache"" } ]
    }
  ]
}
";
            File.WriteAllText(Path.Combine(output, "vercel.json"), json);
        }

        static void Fail(string message, bool showDialogs)
        {
            Debug.LogError("Hay Hustle: " + message);
            if (showDialogs) EditorUtility.DisplayDialog("Hay Hustle web build", message, "OK");
        }
    }
}
