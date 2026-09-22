using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    public static partial class ContentSeedData
    {
        private static List<PackSeed<TriviaQuestion>> BuildTriviaPacks()
        {
            return new List<PackSeed<TriviaQuestion>>
            {
                CurrentTriviaPack(),
                OddFactsTriviaPack(),
                EntertainmentTriviaPack(),
                HistoryTriviaPack(),
                ScienceTriviaPack(),
                GeographyTriviaPack(),
                FoodTriviaPack(),
                AnimalsTriviaPack(),
                TechTriviaPack(),
                SportTriviaPack(),
                LanguageTriviaPack()
            };
        }
    }
}
