using System.Linq;
using NUnit.Framework;
using PartyGame.Core.App;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PartyGame.Tests
{
    /// <summary>
    /// Guards the things that only break once, silently, and then break everything:
    /// the boot scene, the build list and the content library.
    /// </summary>
    public class ProjectSetupTests
    {
        private const string BootstrapScenePath = "Assets/PartyGame/Scenes/Bootstrap.unity";

        [Test]
        public void TheBootstrapSceneIsFirstInTheBuild()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            Assert.IsNotEmpty(enabled, "No scenes are enabled in the build settings.");
            Assert.AreEqual(BootstrapScenePath, enabled[0].path);
        }

        [Test]
        public void TheBootstrapSceneOpensAndStartsTheApp()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.IsTrue(scene.IsValid(), "The bootstrap scene did not open.");

                var roots = scene.GetRootGameObjects();
                Assert.IsTrue(roots.Any(go => go.GetComponent<GameBootstrap>() != null),
                    "The bootstrap scene has no GameBootstrap.");

                foreach (var root in roots)
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.IsNotNull(behaviour, "The bootstrap scene has a missing script reference.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TheContentLibraryLoadsFromResources()
        {
            var library = Resources.Load<ContentLibrary>(ContentLibrary.ResourcePath);
            Assert.IsNotNull(library, "No ContentLibrary at Resources/" + ContentLibrary.ResourcePath);
            Assert.IsTrue(library.GameModes.All(m => m != null), "The library has a broken game mode reference.");
            Assert.IsTrue(library.WordCategories.All(c => c != null), "The library has a broken word category reference.");
        }

        [Test]
        public void EveryImplementedModeHasAnAvailableDefinition()
        {
            var content = new ContentService(Resources.Load<ContentLibrary>(ContentLibrary.ResourcePath));
            foreach (var modeId in GameModeFactory.ImplementedModes)
            {
                var definition = content.GetMode(modeId);
                Assert.IsNotNull(definition, modeId + " has no definition asset.");
                Assert.IsTrue(definition.Available, modeId + " is marked unavailable.");
                Assert.GreaterOrEqual(definition.MaxPlayers, definition.MinPlayers,
                    modeId + " has an impossible player range.");
            }
        }

        [Test]
        public void TextMeshProResourcesAreImported()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            Assert.IsNotNull(font, "Run Party Game > Import TextMeshPro Resources.");
            Assert.IsNotNull(Shader.Find("TextMeshPro/Distance Field"),
                "The TextMeshPro shaders are missing, so text would not render.");
        }
    }
}
