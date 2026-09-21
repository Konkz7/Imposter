using System;
using System.IO;
using System.Linq;
using PartyGame.Core.App;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// One-shot project configuration: fonts, scenes, build settings and mobile player
    /// settings. Everything it does is idempotent so it can be re-run on a fresh checkout.
    /// </summary>
    public static class ProjectSetup
    {
        public const string BootstrapScenePath = "Assets/PartyGame/Scenes/Bootstrap.unity";
        private const string TmpFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Party Game/Set Up Project", false, 1)]
        public static void SetUpProject()
        {
            ImportTextMeshProResources();
            ContentBuilder.RebuildContent();
            CreateBootstrapScene();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Party Game] Project set up. Open " + BootstrapScenePath + " and press play.");
        }

        [MenuItem("Party Game/Import TextMeshPro Resources", false, 20)]
        public static void ImportTextMeshProResources()
        {
            if (File.Exists(TmpFontAssetPath))
            {
                Debug.Log("[Party Game] TextMeshPro resources already present.");
                return;
            }

            // The essentials ship as a .unitypackage inside com.unity.ugui. Unity's own importer
            // is asynchronous, so it is extracted directly instead: that works identically from
            // the menu and from a one-shot batch-mode run.
            var packagePath = FindEssentialsPackage();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogWarning("[Party Game] Could not locate the TMP Essential Resources package. " +
                                 "Text will fall back to a runtime generated font asset.");
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            var written = UnityPackageExtractor.Extract(packagePath, projectRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log("[Party Game] Imported " + written + " TextMeshPro resource files.");
        }

        private static string FindEssentialsPackage()
        {
            var roots = new[] { "Library/PackageCache", "Packages" };
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                var match = Directory
                    .GetDirectories(root, "com.unity.ugui*", SearchOption.TopDirectoryOnly)
                    .Select(dir => Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage"))
                    .FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(match)) return match;
            }
            return null;
        }

        [MenuItem("Party Game/Create Bootstrap Scene", false, 21)]
        public static void CreateBootstrapScene()
        {
            var folder = Path.GetDirectoryName(BootstrapScenePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                Directory.CreateDirectory(folder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // A camera is not required to draw a screen space overlay canvas, but having one
            // keeps the render pipeline quiet and gives the app a defined background colour.
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.06f, 0.11f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.cullingMask = 0;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("App Bootstrap", typeof(GameBootstrap));

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AddSceneToBuildSettings(BootstrapScenePath);
            Debug.Log("[Party Game] Bootstrap scene created at " + BootstrapScenePath);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));

            // The template sample scene is not part of the app.
            scenes.RemoveAll(s => s.path.EndsWith("/SampleScene.unity", StringComparison.Ordinal));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Party Game/Configure Player Settings", false, 22)]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Odd One Out Games";
            PlayerSettings.productName = "Odd One Out";
            PlayerSettings.bundleVersion = "1.0.0";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.oddoneoutgames.oddoneout");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.oddoneoutgames.oddoneout");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            AssetDatabase.SaveAssets();
            Debug.Log("[Party Game] Player settings configured for portrait mobile.");
        }
    }
}
