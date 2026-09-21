using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// One-command builds, mainly so a development build can be produced from CI or from the
    /// command line as a smoke test that the project still ships.
    /// </summary>
    public static class BuildTools
    {
        private const string OutputRoot = "Builds";

        [MenuItem("Party Game/Build/Development (this platform)", false, 40)]
        public static void BuildDevelopmentForCurrentPlatform()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var extension = target == BuildTarget.Android ? ".apk"
                : target == BuildTarget.StandaloneWindows64 ? ".exe"
                : string.Empty;

            var folder = Path.Combine(OutputRoot, target.ToString());
            Directory.CreateDirectory(folder);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(folder, "OddOneOut" + extension),
                target = target,
                targetGroup = group,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            if (options.scenes.Length == 0)
            {
                Debug.LogError("[Party Game] No scenes are enabled in the build settings. " +
                               "Run Party Game > Set Up Project first.");
                return;
            }

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Party Game] Build succeeded: " + summary.outputPath +
                          " (" + (summary.totalSize / (1024 * 1024)) + " MB, " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s)");
            }
            else
            {
                Debug.LogError("[Party Game] Build " + summary.result + " with " + summary.totalErrors + " errors.");
            }
        }

        /// <summary>Fails loudly if anything the app needs at runtime is missing.</summary>
        [MenuItem("Party Game/Validate Project", false, 41)]
        public static void ValidateProject()
        {
            var problems = 0;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            if (scenes.Count == 0 || scenes[0].path != ProjectSetup.BootstrapScenePath)
            {
                Debug.LogError("[Party Game] The bootstrap scene is not the first enabled scene.");
                problems++;
            }

            var library = AssetDatabase.LoadAssetAtPath<Core.Content.ContentLibrary>(
                "Assets/Resources/PartyGame/ContentLibrary.asset");
            if (library == null)
            {
                Debug.LogError("[Party Game] No content library in Resources.");
                problems++;
            }
            else
            {
                var content = new Core.Content.ContentService(library);
                foreach (var modeId in Core.Modes.GameModeFactory.ImplementedModes)
                {
                    if (content.GetMode(modeId) == null)
                    {
                        Debug.LogError("[Party Game] No definition asset for " + modeId);
                        problems++;
                    }
                    if (content.PacksFor(modeId).Count == 0)
                    {
                        Debug.LogError("[Party Game] No content packs for " + modeId);
                        problems++;
                    }
                }
            }

            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                Debug.LogWarning("[Party Game] TextMeshPro resources are missing; text will use a " +
                                 "runtime generated fallback font.");
            }

            Debug.Log(problems == 0
                ? "[Party Game] Validation passed."
                : "[Party Game] Validation found " + problems + " problem(s).");
        }
    }
}
