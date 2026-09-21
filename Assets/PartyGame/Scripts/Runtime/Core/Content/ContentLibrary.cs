using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Modes;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// Single asset that points at every content pack and game mode definition in the app.
    /// Loaded from Resources at boot so nothing needs wiring up in a scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Party Game/Content Library", fileName = "ContentLibrary")]
    public class ContentLibrary : ScriptableObject
    {
        public const string ResourcePath = "PartyGame/ContentLibrary";

        [SerializeField] private List<GameModeDefinition> gameModes = new List<GameModeDefinition>();
        [SerializeField] private List<WordCategoryData> wordCategories = new List<WordCategoryData>();
        [SerializeField] private List<TriviaPackData> triviaPacks = new List<TriviaPackData>();
        [SerializeField] private List<WavelengthPackData> wavelengthPacks = new List<WavelengthPackData>();
        [SerializeField] private List<DebatePackData> debatePacks = new List<DebatePackData>();
        [SerializeField] private List<CluePackData> cluePacks = new List<CluePackData>();

        public IReadOnlyList<GameModeDefinition> GameModes => gameModes;
        public IReadOnlyList<WordCategoryData> WordCategories => wordCategories;
        public IReadOnlyList<TriviaPackData> TriviaPacks => triviaPacks;
        public IReadOnlyList<WavelengthPackData> WavelengthPacks => wavelengthPacks;
        public IReadOnlyList<DebatePackData> DebatePacks => debatePacks;
        public IReadOnlyList<CluePackData> CluePacks => cluePacks;

        public void SetContents(
            IEnumerable<GameModeDefinition> modes,
            IEnumerable<WordCategoryData> words,
            IEnumerable<TriviaPackData> trivia,
            IEnumerable<WavelengthPackData> wavelength,
            IEnumerable<DebatePackData> debate,
            IEnumerable<CluePackData> clues)
        {
            gameModes = modes.Where(m => m != null).ToList();
            wordCategories = words.Where(w => w != null).ToList();
            triviaPacks = trivia.Where(t => t != null).ToList();
            wavelengthPacks = wavelength.Where(w => w != null).ToList();
            debatePacks = debate.Where(d => d != null).ToList();
            cluePacks = clues.Where(c => c != null).ToList();
        }
    }
}
