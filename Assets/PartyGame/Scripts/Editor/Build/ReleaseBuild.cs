using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Store builds, driven from the command line so a release is reproducible.
    ///
    /// Signing credentials and version numbers come from the environment, never from the repo,
    /// so nothing secret is ever committed and CI can supply them the same way a person does.
    /// </summary>
    public static class ReleaseBuild
    {
        private const string OutputRoot = "Builds";

        // Environment variables the build reads. All optional except the signing set, which is
        // only required for a signed Android build.
        private const string EnvKeystorePath = "PARTYGAME_KEYSTORE_PATH";
        private const string EnvKeystorePass = "PARTYGAME_KEYSTORE_PASS";
        private const string EnvKeyAlias = "PARTYGAME_KEY_ALIAS";
        private const string EnvKeyAliasPass = "PARTYGAME_KEY_PASS";
        private const string EnvVersion = "PARTYGAME_VERSION";
        private const string EnvBuildNumber = "PARTYGAME_BUILD_NUMBER";

        // ------------------------------------------------------------------ entry points

        [MenuItem("Party Game/Release/Build Android App Bundle", false, 80)]
        public static void AndroidAppBundle()
        {
            BuildAndroid(asAppBundle: true);
        }

        [MenuItem("Party Game/Release/Build Android APK", false, 81)]
        public static void AndroidApk()
        {
            BuildAndroid(asAppBundle: false);
        }

        [MenuItem("Party Game/Release/Export iOS Xcode Project", false, 82)]
        public static void IosXcodeProject()
        {
            if (!Require(BuildTarget.iOS, "iOS Build Support")) return;

            ReleaseSettings.Apply(NamedBuildTarget.iOS);
            ApplyVersion();

            var folder = Path.Combine(OutputRoot, "iOS");
            Directory.CreateDirectory(folder);

            // iOS produces an Xcode project rather than a finished app, and only on macOS.
            Run(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = folder,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            });
        }

        private static void BuildAndroid(bool asAppBundle)
        {
            if (!Require(BuildTarget.Android, "Android Build Support (with OpenJDK and Android SDK & NDK Tools)")) return;

            ReleaseSettings.Apply(NamedBuildTarget.Android);
            ApplyVersion();

            if (!ApplySigning()) return;

            EditorUserBuildSettings.buildAppBundle = asAppBundle;

            var folder = Path.Combine(OutputRoot, "Android");
            Directory.CreateDirectory(folder);
            var extension = asAppBundle ? ".aab" : ".apk";
            var fileName = "OddOneOut-" + PlayerSettings.bundleVersion + "-" +
                           PlayerSettings.Android.bundleVersionCode + extension;

            Run(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = Path.Combine(folder, fileName),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });
        }

        // ------------------------------------------------------------------ configuration

        /// <summary>
        /// Points the build at a keystore described by the environment. Without it the build is
        /// unsigned, which is fine for a local smoke test but cannot be uploaded to Play.
        /// </summary>
        private static bool ApplySigning()
        {
            var keystore = ReadUnquoted(EnvKeystorePath);
            var keystorePassword = ReadUnquoted(EnvKeystorePass);
            var alias = ReadUnquoted(EnvKeyAlias);
            var aliasPassword = ReadUnquoted(EnvKeyAliasPass);

            var provided = new[] { keystore, keystorePassword, alias, aliasPassword };
            if (provided.All(string.IsNullOrEmpty))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("[Party Game] No signing environment set, so this build is UNSIGNED and " +
                                 "cannot be uploaded to Google Play. Set " + EnvKeystorePath + ", " +
                                 EnvKeystorePass + ", " + EnvKeyAlias + " and " + EnvKeyAliasPass + ".");
                return true;
            }

            if (provided.Any(string.IsNullOrEmpty))
            {
                Debug.LogError("[Party Game] Signing is half configured. All four of " + EnvKeystorePath + ", " +
                               EnvKeystorePass + ", " + EnvKeyAlias + " and " + EnvKeyAliasPass + " are required.");
                return false;
            }

            if (!File.Exists(keystore))
            {
                Debug.LogError("[Party Game] Keystore not found at " + keystore);
                return false;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPassword;
            return true;
        }

        /// <summary>
        /// Windows makes it easy to store a value with its surrounding quotes (setx "\"...\"",
        /// or pasting a quoted path), which then fails File.Exists or the keystore password.
        /// </summary>
        private static string ReadUnquoted(string name)
        {
            var value = Environment.GetEnvironmentVariable(name)?.Trim();
            if (value != null && value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);
            return value;
        }

        /// <summary>
        /// Version from the environment when supplied, otherwise the build number simply ticks up
        /// so two local builds never collide in the store.
        /// </summary>
        private static void ApplyVersion()
        {
            var version = Environment.GetEnvironmentVariable(EnvVersion);
            if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version;

            var buildNumber = Environment.GetEnvironmentVariable(EnvBuildNumber);
            if (int.TryParse(buildNumber, out var parsed))
            {
                PlayerSettings.Android.bundleVersionCode = parsed;
                PlayerSettings.iOS.buildNumber = parsed.ToString();
            }
            else
            {
                PlayerSettings.Android.bundleVersionCode += 1;
                PlayerSettings.iOS.buildNumber = PlayerSettings.Android.bundleVersionCode.ToString();
            }

            Debug.Log("[Party Game] Building version " + PlayerSettings.bundleVersion +
                      " (" + PlayerSettings.Android.bundleVersionCode + ")");
        }

        private static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }

        private static bool Require(BuildTarget target, string moduleName)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (BuildPipeline.IsBuildTargetSupported(group, target)) return true;

            Debug.LogError("[Party Game] " + target + " is not installed. Add \"" + moduleName +
                           "\" to Unity 6000.6.2f1 through Unity Hub > Installs > Add modules, then retry.");
            return false;
        }

        private static void Run(BuildPlayerOptions options)
        {
            if (options.scenes.Length == 0)
            {
                Debug.LogError("[Party Game] No scenes enabled in the build settings. " +
                               "Run Party Game > Set Up Project first.");
                return;
            }

            var summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Party Game] Release build succeeded: " + summary.outputPath + " (" +
                          summary.totalSize / (1024 * 1024) + " MB, " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s)");
                return;
            }

            Debug.LogError("[Party Game] Release build " + summary.result + " with " + summary.totalErrors + " errors.");
        }
    }
}
