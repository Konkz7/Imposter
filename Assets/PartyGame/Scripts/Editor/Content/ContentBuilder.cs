using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using UnityEditor;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Regenerates every content asset and the library that points at them. Run it once to
    /// populate a fresh checkout; after that the generated assets can be edited by hand and
    /// only re-run when the seed data changes.
    /// </summary>
    public static class ContentBuilder
    {
        private const string ContentRoot = "Assets/PartyGame/Content";
        private const string LibraryFolder = "Assets/Resources/PartyGame";
        private const string LibraryPath = LibraryFolder + "/ContentLibrary.asset";

        [MenuItem("Party Game/Rebuild Content", false, 10)]
        public static void RebuildContent()
        {
            EnsureFolders();

            var modes = BuildGameModes();
            var words = BuildPacks<WordCategoryData, WordPair>(ContentSeedData.WordCategories, "Words",
                (asset, entries) => asset.SetPairs(entries));
            var trivia = BuildPacks<TriviaPackData, TriviaQuestion>(ContentSeedData.TriviaPacks, "Trivia",
                (asset, entries) => asset.SetQuestions(entries));
            var wavelength = BuildPacks<WavelengthPackData, WavelengthQuestion>(ContentSeedData.WavelengthPacks, "Wavelength",
                (asset, entries) => asset.SetQuestions(entries));
            var debate = BuildPacks<DebatePackData, DebateStatement>(ContentSeedData.DebatePacks, "Debate",
                (asset, entries) => asset.SetStatements(entries));
            var clues = BuildPacks<CluePackData, ClueTemplate>(ContentSeedData.CluePacks, "Clues",
                (asset, entries) => asset.SetClues(entries));

            var library = LoadOrCreate<ContentLibrary>(LibraryPath);
            library.SetContents(modes, words, trivia, wavelength, debate, clues);
            EditorUtility.SetDirty(library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Party Game] Content rebuilt: " + modes.Count + " modes, " + words.Count + " word categories, " +
                      trivia.Sum(t => t.EntryCount) + " trivia questions, " +
                      wavelength.Sum(w => w.EntryCount) + " wavelength questions, " +
                      debate.Sum(d => d.EntryCount) + " debate statements, " +
                      clues.Sum(c => c.EntryCount) + " clue templates.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(LibraryFolder);
            EnsureFolder(ContentRoot);
            foreach (var folder in new[] { "Modes", "Words", "Trivia", "Wavelength", "Debate", "Clues" })
                EnsureFolder(ContentRoot + "/" + folder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            // A file that exists but will not load has a broken script reference; replacing it
            // is the only way to recover, and losing generated content costs nothing.
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static List<TAsset> BuildPacks<TAsset, TEntry>(IEnumerable<PackSeed<TEntry>> seeds, string folder,
            Action<TAsset, List<TEntry>> assign) where TAsset : ContentPack
        {
            var result = new List<TAsset>();
            var expected = new HashSet<string>();

            foreach (var seed in seeds)
            {
                var path = ContentRoot + "/" + folder + "/" + seed.Id + ".asset";
                expected.Add(seed.Id);

                var asset = LoadOrCreate<TAsset>(path);
                asset.Configure(seed.Id, seed.Name, seed.Description, seed.Glyph, seed.Accent,
                    seed.Freshness, seed.ReviewBy);
                assign(asset, seed.Entries);
                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            RemoveRetiredPacks(folder, expected);
            return result;
        }

        /// <summary>
        /// Deletes generated packs that the seed data no longer describes, so renaming or
        /// splitting a category does not silently leave the old asset behind for the library to
        /// keep serving.
        /// </summary>
        private static void RemoveRetiredPacks(string folder, HashSet<string> expected)
        {
            var directory = ContentRoot + "/" + folder;
            if (!AssetDatabase.IsValidFolder(directory)) return;

            foreach (var guid in AssetDatabase.FindAssets("t:ContentPack", new[] { directory }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var id = Path.GetFileNameWithoutExtension(path);
                if (expected.Contains(id)) continue;

                Debug.Log("[Party Game] Removing retired content pack: " + path);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static List<GameModeDefinition> BuildGameModes()
        {
            var definitions = new List<GameModeDefinition>();

            // Most of these work fine at three players and simply get better with more, so the
            // cards suggest a size rather than enforce one. The Suspects is the exception: it
            // needs enough people to hide the special roles among.
            definitions.Add(Mode(GameModeId.DifferentWord, "Different Word",
                "One of you has a different word. Find them.",
                "Everybody is given the same secret word except the imposter, who gets something close but " +
                "not quite right. Take turns describing your word without ever saying it, then vote on who " +
                "sounded slightly off. The imposter wins by surviving the vote.",
                "WD", 0, minPlayers: 3, recommended: 4, maxPlayers: 12, minutes: 5));

            definitions.Add(Mode(GameModeId.Fib, "Fib",
                "Invent a believable lie and spot the truth.",
                "A real question appears with an answer nobody has seen. Everybody secretly writes a fake " +
                "answer, then the whole list is shuffled together with the real one. Score for finding the " +
                "truth, and score again every time somebody falls for your lie.",
                "FB", 1, minPlayers: 3, recommended: 4, maxPlayers: 10, minutes: 6));

            definitions.Add(Mode(GameModeId.Wavelength, "Number Wavelength",
                "Everyone has the same number. Almost everyone.",
                "Everybody secretly receives the same number from one to ten, except one player. A question " +
                "appears with a scale, and each answer has to match the strength of your number. Work out " +
                "whose answer sits at the wrong end of the scale.",
                "1-10", 4, minPlayers: 3, recommended: 4, maxPlayers: 12, minutes: 5));

            definitions.Add(Mode(GameModeId.DevilsAdvocate, "Devil's Advocate",
                "Someone is arguing against their own opinion.",
                "A statement appears and everybody privately picks a side. One player is secretly told to " +
                "argue the opposite of whatever they chose. Everybody makes their case out loud, then the " +
                "group votes on who is arguing for a side they do not believe.",
                "DA", 5, minPlayers: 3, recommended: 4, maxPlayers: 10, minutes: 7));

            definitions.Add(Mode(GameModeId.SocialDeduction, "The Suspects",
                "Secret roles, true clues and one accusation a round.",
                "Everybody gets a secret role for the whole game. Each round the table hears a scene and a " +
                "public clue, while the investigator and the witness privately receive a narrow, always-true " +
                "clue. Debate, accuse, and remove one player a round.",
                "SD", 2, minPlayers: 5, recommended: 6, maxPlayers: 12, minutes: 10));

            return definitions;
        }

        private static GameModeDefinition Mode(GameModeId id, string name, string tagline, string description,
            string glyph, int accent, int minPlayers, int recommended, int maxPlayers, int minutes)
        {
            var path = ContentRoot + "/Modes/" + id + ".asset";
            var asset = LoadOrCreate<GameModeDefinition>(path);
            asset.Configure(id, name, tagline, description, glyph, accent, minPlayers, recommended,
                maxPlayers, minutes, new List<string>());
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
