using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    public static partial class ContentSeedData
    {
        /// <summary>
        /// Every Different Word category. Order here is the order they appear in the picker, so
        /// the broadly recognisable categories come first and the specialist ones follow.
        /// </summary>
        private static List<PackSeed<WordPair>> BuildWordCategories()
        {
            return new List<PackSeed<WordPair>>
            {
                FoodPack(),
                AnimalsPack(),
                PlacesPack(),
                ObjectsPack(),
                FilmPack(),
                TelevisionPack(),
                MusicPack(),
                GamingPack(),
                SportPack(),
                TechnologyPack(),
                BrandsPack(),
                InternetPack(),
                CelebritiesPack(),
                CharactersPack(),
                TravelPack(),
                TransportPack(),
                SchoolPack(),
                JobsPack(),
                SciencePack(),
                HistoryPack(),
                NaturePack(),
                RandomPack()
            };
        }
    }
}
