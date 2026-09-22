using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Player settings a store build needs, applied in one place so a release never depends on
    /// whatever state someone left the inspector in.
    /// </summary>
    public static class ReleaseSettings
    {
        [MenuItem("Party Game/Release/Apply Release Settings", false, 61)]
        public static void ApplyForAllPlatforms()
        {
            Apply(NamedBuildTarget.Android);
            Apply(NamedBuildTarget.iOS);
            AssetDatabase.SaveAssets();
            Debug.Log("[Party Game] Release settings applied for Android and iOS.");
        }

        public static void Apply(NamedBuildTarget target)
        {
            ApplyShared();

            // IL2CPP is required for 64-bit stores and gives a meaningful startup win over Mono.
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);

            // Low stripping only. The content system resolves ScriptableObjects through
            // serialisation, and a more aggressive level risks stripping something the linker
            // cannot see being used.
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Master);

            if (target == NamedBuildTarget.Android) ApplyAndroid();
            if (target == NamedBuildTarget.iOS) ApplyIos();
        }

        private static void ApplyShared()
        {
            PlayerSettings.companyName = "Odd One Out Games";
            PlayerSettings.productName = "Odd One Out";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // The app is passed around a table and never needs to keep working in the background.
            PlayerSettings.runInBackground = false;

            // Nothing reads the accelerometer, and polling it costs battery.
            PlayerSettings.accelerometerFrequency = 0;

            // A party is often already playing music. Taking over the audio session would be rude.
            PlayerSettings.muteOtherAudioSources = false;

            // The splash sits on the same near-black the app opens onto, so the launch does not
            // flash a bright frame. Hiding the Unity logo needs a Pro or Plus licence; on
            // Personal the setting is simply ignored, which is why the style is set to suit a
            // dark background either way.
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.07f, 0.06f, 0.11f, 1f);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        private static void ApplyAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.oddoneoutgames.oddoneout");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Play requires a 64-bit binary. Shipping both in an app bundle costs nothing at
            // install time because Play serves each device only the slice it needs.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.optimizedFramePacing = true;

            // Draw edge to edge and hide the status and navigation bars. The UI already keeps
            // itself clear of cutouts through SafeAreaFitter, so the extra height is usable.
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.None;
            PlayerSettings.Android.systemBarsBehavior = AndroidSystemBarsBehavior.ShowTransientBarsBySwipe;

            // Public symbols let Play symbolicate native crash reports. The replacement API,
            // UnityEditor.Android.DebugSymbols, ships inside the Android build module, so using
            // it directly would stop this file compiling on a machine without that module.
#pragma warning disable CS0618
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
#pragma warning restore CS0618
        }

        private static void ApplyIos()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.oddoneoutgames.oddoneout");

            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.requiresFullScreen = true;

            // Apple rejects builds that ask for a capability they never use. The app has no
            // network, camera, microphone or location code at all.
            PlayerSettings.iOS.locationUsageDescription = string.Empty;
            PlayerSettings.iOS.cameraUsageDescription = string.Empty;
            PlayerSettings.iOS.microphoneUsageDescription = string.Empty;
        }
    }
}
