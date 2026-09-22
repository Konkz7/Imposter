using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Everything that would get a build rejected, or shipped broken, checked in one pass.
    /// Run it before every store upload; it exits non-zero in batch mode so CI can gate on it.
    /// </summary>
    public static class ReleaseChecklist
    {
        private static readonly List<string> Problems = new List<string>();
        private static readonly List<string> Warnings = new List<string>();

        [MenuItem("Party Game/Release/Check Release Readiness", false, 62)]
        public static void Run()
        {
            Problems.Clear();
            Warnings.Clear();

            CheckBuildContents();
            CheckIdentity();
            CheckIcons();
            CheckAndroid();
            CheckPrivacyClaims();

            foreach (var warning in Warnings) Debug.LogWarning("[Release] " + warning);
            foreach (var problem in Problems) Debug.LogError("[Release] " + problem);

            if (Problems.Count == 0)
            {
                Debug.Log("[Release] Ready to build. " + Warnings.Count + " warning(s).");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            Debug.LogError("[Release] NOT ready: " + Problems.Count + " problem(s) to fix first.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static void CheckBuildContents()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            if (scenes.Count == 0)
            {
                Problems.Add("No scenes are enabled in the build settings.");
            }
            else if (scenes[0].path != ProjectSetup.BootstrapScenePath)
            {
                Problems.Add("The first enabled scene is " + scenes[0].path + ", not the bootstrap scene.");
            }

            var library = AssetDatabase.LoadAssetAtPath<ContentLibrary>(
                "Assets/Resources/PartyGame/ContentLibrary.asset");
            if (library == null)
            {
                Problems.Add("No ContentLibrary in Resources. Run Party Game > Rebuild Content.");
                return;
            }

            var content = new ContentService(library);
            foreach (var modeId in GameModeFactory.ImplementedModes)
            {
                var definition = content.GetMode(modeId);
                if (definition == null)
                {
                    Problems.Add("No definition asset for " + modeId + ".");
                    continue;
                }
                if (!definition.Available) Warnings.Add(modeId + " is marked unavailable and will be hidden.");
                if (content.PacksFor(modeId).Count == 0) Problems.Add("No content packs for " + modeId + ".");
            }

            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                Problems.Add("TextMeshPro resources are missing, so text will not render. " +
                             "Run Party Game > Import TextMeshPro Resources.");
        }

        private static void CheckIdentity()
        {
            if (string.IsNullOrWhiteSpace(PlayerSettings.productName) ||
                PlayerSettings.productName == "New Unity Project")
                Problems.Add("Product name is not set.");

            if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) ||
                PlayerSettings.companyName == "DefaultCompany")
                Problems.Add("Company name is still the Unity default.");

            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                var id = PlayerSettings.GetApplicationIdentifier(target);
                if (string.IsNullOrWhiteSpace(id) || id.Contains("DefaultCompany") || id.Contains("com.Company"))
                    Problems.Add(target.TargetName + " application identifier is still a placeholder (" + id + ").");
            }

            if (!Version.TryParse(PlayerSettings.bundleVersion, out _))
                Warnings.Add("Version \"" + PlayerSettings.bundleVersion + "\" is not a dotted number. " +
                             "Stores sort releases by this.");
        }

        private static void CheckIcons()
        {
            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                PlatformIconKind[] kinds;
                try
                {
                    kinds = PlayerSettings.GetSupportedIconKinds(target);
                }
                catch (Exception)
                {
                    Warnings.Add("Could not read icon slots for " + target.TargetName +
                                 "; its build module may not be installed.");
                    continue;
                }

                var missing = 0;
                foreach (var kind in kinds)
                foreach (var icon in PlayerSettings.GetPlatformIcons(target, kind))
                    if (icon.GetTextures().All(t => t == null)) missing++;

                if (missing > 0)
                    Problems.Add(target.TargetName + " has " + missing + " empty icon slot(s). " +
                                 "Run Party Game > Release > Generate App Icons.");
            }
        }

        private static void CheckAndroid()
        {
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                Problems.Add("ARM64 is not enabled. Google Play requires a 64-bit binary.");

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                Problems.Add("Android is not set to IL2CPP, which 64-bit builds require.");

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
                Warnings.Add("Android minimum SDK is below 26; adaptive icons and modern APIs expect 26+.");

            if (EditorUserBuildSettings.development)
                Problems.Add("Development Build is ticked. Turn it off before a store build.");

            if (!PlayerSettings.Android.useCustomKeystore)
                Warnings.Add("No custom keystore is configured, so a build now would be unsigned. " +
                             "The release build reads one from the environment.");
        }

        /// <summary>
        /// The store listing and privacy policy both claim the app is fully offline and collects
        /// nothing. That claim has to stay true, so it is checked rather than trusted.
        /// </summary>
        private static void CheckPrivacyClaims()
        {
            if (PlayerSettings.Android.forceInternetPermission)
                Problems.Add("Android is forcing the INTERNET permission, which contradicts the " +
                             "privacy policy and triggers extra Play Data Safety questions.");

            var runtime = Directory.Exists("Assets/PartyGame/Scripts/Runtime")
                ? Directory.GetFiles("Assets/PartyGame/Scripts/Runtime", "*.cs", SearchOption.AllDirectories)
                : Array.Empty<string>();

            var networkApis = new[] { "UnityWebRequest", "System.Net", "Analytics.", "new WebClient" };
            foreach (var file in runtime)
            {
                var text = File.ReadAllText(file);
                foreach (var api in networkApis)
                {
                    if (!text.Contains(api)) continue;
                    Problems.Add("Runtime code uses " + api + " in " + Path.GetFileName(file) +
                                 ", which contradicts the offline privacy claim.");
                }
            }
        }
    }
}
