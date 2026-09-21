using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Modes;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// The only place that knows how content assets are stored. Everything else asks for
    /// packs by mode, so swapping ScriptableObjects for a downloaded pack later is local.
    /// </summary>
    public class ContentService
    {
        private readonly ContentLibrary _library;

        public bool HasLibrary => _library != null;
        public ContentLibrary Library => _library;

        public ContentService(ContentLibrary library)
        {
            _library = library;
        }

        public static ContentService LoadFromResources()
        {
            var library = Resources.Load<ContentLibrary>(ContentLibrary.ResourcePath);
            if (library == null)
            {
                Debug.LogWarning("[Content] No ContentLibrary found at Resources/" + ContentLibrary.ResourcePath +
                                 ". Run Party Game > Rebuild Content to generate it.");
            }
            return new ContentService(library);
        }

        public IReadOnlyList<GameModeDefinition> GameModes
        {
            get
            {
                if (_library == null) return new List<GameModeDefinition>();
                return _library.GameModes.Where(m => m != null && m.Available).ToList();
            }
        }

        public GameModeDefinition GetMode(GameModeId id)
        {
            if (_library == null) return null;
            return _library.GameModes.FirstOrDefault(m => m != null && m.ModeId == id);
        }

        /// <summary>Every enabled, non-empty pack a mode can draw from.</summary>
        public IReadOnlyList<ContentPack> PacksFor(GameModeId modeId)
        {
            if (_library == null) return new List<ContentPack>();
            IEnumerable<ContentPack> packs;
            switch (modeId)
            {
                case GameModeId.DifferentWord:
                    packs = _library.WordCategories;
                    break;
                case GameModeId.Fib:
                    packs = _library.TriviaPacks;
                    break;
                case GameModeId.Wavelength:
                    packs = _library.WavelengthPacks;
                    break;
                case GameModeId.DevilsAdvocate:
                    packs = _library.DebatePacks;
                    break;
                case GameModeId.SocialDeduction:
                    packs = _library.CluePacks;
                    break;
                default:
                    packs = new List<ContentPack>();
                    break;
            }
            return packs.Where(p => p != null && p.Enabled && p.EntryCount > 0).ToList();
        }

        /// <summary>
        /// Resolves the ids a player selected. An empty or unmatched selection means
        /// "everything", so a bad save or a retired pack can never leave a mode with no content.
        /// </summary>
        public IReadOnlyList<T> ResolveSelection<T>(GameModeId modeId, IReadOnlyList<string> selectedIds) where T : ContentPack
        {
            var all = PacksFor(modeId).OfType<T>().ToList();
            if (all.Count == 0) return all;
            if (selectedIds == null || selectedIds.Count == 0) return all;

            var chosen = all.Where(p => selectedIds.Contains(p.Id)).ToList();
            return chosen.Count > 0 ? chosen : all;
        }

        public IReadOnlyList<CluePackData> AllCluePacks
        {
            get
            {
                if (_library == null) return new List<CluePackData>();
                return _library.CluePacks.Where(p => p != null && p.Enabled).ToList();
            }
        }
    }
}
