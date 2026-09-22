using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    public static partial class ContentSeedData
    {
        // A Wavelength prompt only works if a 3 and an 8 would produce genuinely different
        // answers. Anything where almost everyone would name the same thing is dead weight, so
        // every prompt below names an explicit low and high end of the scale.

        private static List<PackSeed<WavelengthQuestion>> BuildWavelengthPacks()
        {
            return new List<PackSeed<WavelengthQuestion>>
            {
                WaveEverydayPack(),
                WaveOpinionsPack(),
                WaveScalePack(),
                WaveSocialPack(),
                WaveWorkPack(),
                WaveCulturePack()
            };
        }

        private static PackSeed<WavelengthQuestion> WaveEverydayPack()
        {
            return new PackSeed<WavelengthQuestion>("wave-everyday", "Everyday life",
                "Ordinary things, rated out of ten", "EV", 0, new[]
            {
                W("w1", "Name a place to eat", "an awful place", "an incredible place", Difficulty.Easy, "food"),
                W("w2", "Name a way to spend a Saturday", "a wasted day", "a perfect day", Difficulty.Easy, "leisure"),
                W("w3", "Name a job", "a terrible job", "a dream job", Difficulty.Easy, "work"),
                W("w4", "Name a holiday destination", "a grim trip", "the trip of a lifetime", Difficulty.Easy, "travel"),
                W("w5", "Name a way to get to work", "the worst commute", "the best commute", Difficulty.Easy, "travel"),
                W("w6", "Name a birthday present", "a thoughtless gift", "an amazing gift", Difficulty.Easy, "gifts"),
                W("w7", "Name a breakfast", "barely edible", "worth getting up for", Difficulty.Easy, "food"),
                W("w8", "Name a place to live", "somewhere miserable", "somewhere wonderful", Difficulty.Easy, "home"),
                W("w9", "Name a way to spend an evening", "deeply boring", "brilliant fun", Difficulty.Easy, "leisure"),
                W("w10", "Name a piece of clothing", "an embarrassment", "extremely stylish", Difficulty.Easy, "fashion"),
                W("w11", "Name a household chore", "barely counts as a chore", "the worst job in the house", Difficulty.Easy, "home"),
                W("w12", "Name a drink to order at a bar", "a questionable choice", "a perfect order", Difficulty.Easy, "food"),
                W("w13", "Name a pet", "a terrible idea", "the ideal companion", Difficulty.Easy, "animals"),
                W("w14", "Name a way to wake up", "a horrible start", "a lovely start", Difficulty.Easy, "home"),
                W("w15", "Name a sandwich filling", "an insult to bread", "genuinely excellent", Difficulty.Easy, "food"),
                W("w16", "Name a way to spend a rainy afternoon", "utterly bleak", "secretly brilliant", Difficulty.Medium, "leisure"),
                W("w17", "Name a takeaway order", "regret in a box", "a triumph", Difficulty.Medium, "food"),
                W("w18", "Name a supermarket own brand product", "avoid at all costs", "better than the real thing", Difficulty.Medium, "shopping"),
                W("w19", "Name a piece of furniture", "should be on a skip", "a genuine investment", Difficulty.Medium, "home"),
                W("w20", "Name a kitchen gadget", "used once and forgotten", "used every single day", Difficulty.Medium, "home"),
                W("w21", "Name a way to travel a long distance", "an ordeal", "a pleasure", Difficulty.Medium, "travel"),
                W("w22", "Name a haircut", "a serious mistake", "a great decision", Difficulty.Medium, "fashion"),
                W("w23", "Name a wedding gift", "thoughtless", "genuinely generous", Difficulty.Medium, "gifts"),
                W("w24", "Name a way to spend a lunch break", "a complete waste", "the best part of the day", Difficulty.Medium, "work"),
                W("w25", "Name a phone habit", "mildly annoying", "unforgivable", Difficulty.Medium, "technology"),
                W("w26", "Name a thing to keep in a glovebox", "pointless clutter", "a lifesaver", Difficulty.Medium, "home"),
                W("w27", "Name a length of afternoon nap", "not worth it", "perfectly judged", Difficulty.Medium, "home"),
                W("w28", "Name a breakfast cereal", "cardboard", "a genuine treat", Difficulty.Medium, "food"),
                W("w29", "Name a way to spend a bank holiday", "a total write off", "the ideal long weekend", Difficulty.Medium, "leisure"),
                W("w30", "Name a thing to do on a first day in a new job", "a disaster", "exactly right", Difficulty.Hard, "work")
            });
        }

        private static PackSeed<WavelengthQuestion> WaveOpinionsPack()
        {
            return new PackSeed<WavelengthQuestion>("wave-opinions", "Strong opinions",
                "Rate it and then defend it", "OP", 5, new[]
            {
                W("w31", "Name a film", "unwatchable", "a masterpiece", Difficulty.Easy, "film"),
                W("w32", "Name a song", "skip it immediately", "on repeat all week", Difficulty.Easy, "music"),
                W("w33", "Name a sport to watch", "dull beyond belief", "gripping", Difficulty.Easy, "sport"),
                W("w34", "Name a board game", "never again", "a genuine classic", Difficulty.Easy, "games"),
                W("w35", "Name a television series", "switched off after one episode", "watched twice", Difficulty.Easy, "tv"),
                W("w36", "Name a book", "a slog", "unputdownable", Difficulty.Easy, "books"),
                W("w37", "Name a biscuit", "the last one left in the tin", "the first one gone", Difficulty.Easy, "food"),
                W("w38", "Name a famous landmark", "not worth the queue", "worth crossing the world for", Difficulty.Easy, "travel"),
                W("w39", "Name a hobby", "a complete waste of time", "genuinely rewarding", Difficulty.Easy, "leisure"),
                W("w40", "Name a form of exercise", "pure misery", "surprisingly enjoyable", Difficulty.Easy, "fitness"),
                W("w41", "Name a pizza topping", "an outrage", "essential", Difficulty.Medium, "food"),
                W("w42", "Name a musical instrument", "unbearable to listen to", "beautiful", Difficulty.Medium, "music"),
                W("w43", "Name a film sequel", "should never have been made", "better than the original", Difficulty.Medium, "film"),
                W("w44", "Name a social media platform", "delete it today", "genuinely useful", Difficulty.Medium, "internet"),
                W("w45", "Name a fashion trend", "deeply regrettable", "timeless", Difficulty.Medium, "fashion"),
                W("w46", "Name a type of music", "switch it off", "put it louder", Difficulty.Medium, "music"),
                W("w47", "Name a British institution", "past its best", "a national treasure", Difficulty.Medium, "culture"),
                W("w48", "Name a way to cook an egg", "ruined", "perfect", Difficulty.Medium, "food"),
                W("w49", "Name a type of weather", "miserable", "glorious", Difficulty.Medium, "nature"),
                W("w50", "Name a season of the year", "endure it", "live for it", Difficulty.Medium, "nature"),
                W("w51", "Name a video game", "uninstall immediately", "a hundred hours well spent", Difficulty.Medium, "gaming"),
                W("w52", "Name a cheese", "keep it away from me", "worth the smell", Difficulty.Medium, "food"),
                W("w53", "Name a famous painting", "overrated", "genuinely moving", Difficulty.Hard, "art"),
                W("w54", "Name a cover version", "an insult to the original", "better than the original", Difficulty.Hard, "music"),
                W("w55", "Name a film ending", "completely ruined it", "perfect", Difficulty.Hard, "film")
            });
        }

        private static PackSeed<WavelengthQuestion> WaveScalePack()
        {
            return new PackSeed<WavelengthQuestion>("wave-scale", "How much?",
                "Size, difficulty, cost and effort", "SC", 3, new[]
            {
                W("w56", "Name something scary", "not scary at all", "terrifying", Difficulty.Easy, "feelings"),
                W("w57", "Name something expensive", "practically free", "eye-wateringly costly", Difficulty.Easy, "money"),
                W("w58", "Name something difficult to learn", "learned in an afternoon", "takes a lifetime", Difficulty.Easy, "skills"),
                W("w59", "Name something useful", "completely pointless", "could not live without it", Difficulty.Easy, "objects"),
                W("w60", "Name something noisy", "silent", "deafening", Difficulty.Easy, "sound"),
                W("w61", "Name something fast", "painfully slow", "blindingly fast", Difficulty.Easy, "speed"),
                W("w62", "Name something tiring", "restful", "completely exhausting", Difficulty.Easy, "energy"),
                W("w63", "Name something impressive", "nobody would notice", "everyone would be amazed", Difficulty.Easy, "skills"),
                W("w64", "Name something risky", "perfectly safe", "genuinely dangerous", Difficulty.Easy, "risk"),
                W("w65", "Name something embarrassing", "nobody would care", "you would never live it down", Difficulty.Medium, "feelings"),
                W("w66", "Name something luxurious", "extremely basic", "absurd extravagance", Difficulty.Medium, "money"),
                W("w67", "Name something chaotic", "perfectly ordered", "total chaos", Difficulty.Medium, "feelings"),
                W("w68", "Name something popular", "nobody has heard of it", "everybody knows it", Difficulty.Medium, "culture"),
                W("w69", "Name something healthy", "actively bad for you", "genuinely good for you", Difficulty.Medium, "health"),
                W("w70", "Name something heavy", "you could lift it with one finger", "you could not move it", Difficulty.Medium, "objects"),
                W("w71", "Name something old", "brand new", "ancient", Difficulty.Medium, "time"),
                W("w72", "Name something complicated", "a child could do it", "requires years of training", Difficulty.Medium, "skills"),
                W("w73", "Name something messy", "spotless", "an absolute state", Difficulty.Medium, "home"),
                W("w74", "Name something addictive", "easy to put down", "impossible to stop", Difficulty.Medium, "habits"),
                W("w75", "Name something overrated", "underrated", "wildly overrated", Difficulty.Medium, "opinions"),
                W("w76", "Name something formal", "completely casual", "black tie", Difficulty.Medium, "social"),
                W("w77", "Name something fragile", "indestructible", "breaks if you look at it", Difficulty.Medium, "objects"),
                W("w78", "Name something time consuming", "over in a second", "swallows a whole weekend", Difficulty.Medium, "time"),
                W("w79", "Name a smell", "revolting", "wonderful", Difficulty.Medium, "senses"),
                W("w80", "Name something spicy", "no heat at all", "genuinely painful", Difficulty.Medium, "food"),
                W("w81", "Name something worth queuing for", "not worth two minutes", "worth an hour", Difficulty.Hard, "patience"),
                W("w82", "Name something sustainable", "terrible for the planet", "genuinely green", Difficulty.Hard, "environment"),
                W("w83", "Name something nostalgic", "no feeling at all", "instantly takes you back", Difficulty.Hard, "feelings"),
                W("w84", "Name something intimidating", "completely approachable", "genuinely daunting", Difficulty.Hard, "feelings"),
                W("w85", "Name something that ages well", "ruined within a year", "better after twenty years", Difficulty.Hard, "time")
            });
        }

        private static PackSeed<WavelengthQuestion> WaveSocialPack()
        {
            return new PackSeed<WavelengthQuestion>("wave-social", "Around the table",
                "People, manners and awkward moments", "PT", 6, new[]
            {
                W("w86", "Name a party", "everyone left early", "talked about for years", Difficulty.Easy, "social"),
                W("w87", "Name a first date idea", "a disaster", "a brilliant idea", Difficulty.Easy, "social"),
                W("w88", "Name a group holiday activity", "nobody wants to do it", "everyone is in", Difficulty.Easy, "social"),
                W("w89", "Name a house rule", "unreasonable", "completely fair", Difficulty.Easy, "home"),
                W("w90", "Name a way to say sorry", "makes it worse", "completely forgiven", Difficulty.Easy, "social"),
                W("w91", "Name a small act of kindness", "barely noticed", "made someone's week", Difficulty.Easy, "social"),
                W("w92", "Name a group chat message", "instantly ignored", "gets forty replies", Difficulty.Easy, "internet"),
                W("w93", "Name a way to wake someone up", "cruel", "lovely", Difficulty.Easy, "social"),
                W("w94", "Name a thing to say in a job interview", "instantly disqualifying", "gets you hired", Difficulty.Medium, "work"),
                W("w95", "Name a housemate habit", "mildly irritating", "grounds for moving out", Difficulty.Medium, "home"),
                W("w96", "Name a way to end a phone call", "rude", "charming", Difficulty.Medium, "social"),
                W("w97", "Name a thing to bring to a dinner party", "an insult to the host", "the perfect contribution", Difficulty.Medium, "social"),
                W("w98", "Name a conversation starter", "kills the conversation", "gets everyone talking", Difficulty.Medium, "social"),
                W("w99", "Name a thing to do at a wedding", "a serious faux pas", "exactly right", Difficulty.Medium, "social"),
                W("w100", "Name a way to give bad news", "brutal", "as kind as possible", Difficulty.Medium, "social"),
                W("w101", "Name an excuse for being late", "nobody believes it", "completely reasonable", Difficulty.Medium, "social"),
                W("w102", "Name a thing to do on public transport", "wildly antisocial", "considerate", Difficulty.Medium, "travel"),
                W("w103", "Name a way to split a restaurant bill", "causes an argument", "entirely fair", Difficulty.Medium, "money"),
                W("w104", "Name a nickname", "genuinely offensive", "affectionate", Difficulty.Medium, "social"),
                W("w105", "Name a thing to do when a guest stays over", "makes them uncomfortable", "makes them feel at home", Difficulty.Medium, "social"),
                W("w106", "Name a text you could send at midnight", "unacceptable", "completely fine", Difficulty.Medium, "social"),
                W("w107", "Name a way to leave a party", "memorably bad", "perfectly judged", Difficulty.Hard, "social"),
                W("w108", "Name a thing to say to a neighbour", "starts a feud", "starts a friendship", Difficulty.Hard, "social"),
                W("w109", "Name a favour to ask a friend", "asking far too much", "completely reasonable", Difficulty.Hard, "social"),
                W("w110", "Name a family tradition", "a chore everyone dreads", "the highlight of the year", Difficulty.Hard, "family")
            });
        }

        private static PackSeed<WavelengthQuestion> WaveWorkPack()
        {
            return new PackSeed<WavelengthQuestion>("wave-work", "Work and money",
                "Offices, bosses and the cost of things", "WK", 4, new[]
            {
                W("w111", "Name a workplace perk", "meaningless", "worth staying for", Difficulty.Easy, "work"),
                W("w112", "Name a meeting", "a total waste of an hour", "genuinely useful", Difficulty.Easy, "work"),
                W("w113", "Name a boss behaviour", "unbearable", "the sign of a great manager", Difficulty.Easy, "work"),
                W("w114", "Name an email sign off", "cold", "warm", Difficulty.Easy, "work"),
                W("w115", "Name a way to spend a bonus", "gone in a weekend", "genuinely sensible", Difficulty.Easy, "money"),
                W("w116", "Name a thing to spend money on", "a waste", "worth every penny", Difficulty.Medium, "money"),
                W("w117", "Name a subscription", "cancel it now", "worth every month", Difficulty.Medium, "money"),
                W("w118", "Name a work deadline", "completely unreasonable", "generous", Difficulty.Medium, "work"),
                W("w119", "Name an office noise", "barely noticeable", "impossible to work through", Difficulty.Medium, "work"),
                W("w120", "Name a reason to call in sick", "transparently fake", "entirely legitimate", Difficulty.Medium, "work"),
                W("w121", "Name a career change", "reckless", "the best decision they ever made", Difficulty.Medium, "work"),
                W("w122", "Name a thing to negotiate for in a job offer", "not worth asking", "always worth asking", Difficulty.Medium, "work"),
                W("w123", "Name a work social event", "a duty", "genuinely fun", Difficulty.Medium, "work"),
                W("w124", "Name a way to save money", "barely makes a difference", "genuinely transformative", Difficulty.Medium, "money"),
                W("w125", "Name a thing to put on a CV", "actively harmful", "gets you the interview", Difficulty.Medium, "work"),
                W("w126", "Name a working pattern", "exhausting", "ideal", Difficulty.Medium, "work"),
                W("w127", "Name an office rule", "petty", "completely sensible", Difficulty.Hard, "work"),
                W("w128", "Name a thing worth paying extra for", "never worth it", "always worth it", Difficulty.Hard, "money"),
                W("w129", "Name a side hustle", "not worth the effort", "genuinely lucrative", Difficulty.Hard, "money"),
                W("w130", "Name a retirement plan", "leaves you struggling", "completely secure", Difficulty.Hard, "money")
            });
        }

        private static PackSeed<WavelengthQuestion> WaveCulturePack()
        {
            return new PackSeed<WavelengthQuestion>("wave-culture", "Culture and taste",
                "Films, food and things people judge you for", "CU", 1, new[]
            {
                W("w131", "Name a guilty pleasure", "nothing to be ashamed of", "would never admit it", Difficulty.Easy, "culture"),
                W("w132", "Name a way to eat a takeaway", "barbaric", "entirely civilised", Difficulty.Easy, "food"),
                W("w133", "Name a Christmas film", "not festive at all", "essential viewing", Difficulty.Easy, "film"),
                W("w134", "Name a karaoke song", "clears the room", "gets everyone singing", Difficulty.Easy, "music"),
                W("w135", "Name a museum exhibit", "walk straight past", "worth the trip alone", Difficulty.Medium, "culture"),
                W("w136", "Name a tourist activity", "a tourist trap", "genuinely special", Difficulty.Medium, "travel"),
                W("w137", "Name a book adaptation", "butchered the book", "did it justice", Difficulty.Medium, "film"),
                W("w138", "Name a British food", "an acquired taste", "genuinely delicious", Difficulty.Medium, "food"),
                W("w139", "Name a dance move", "deeply embarrassing", "genuinely impressive", Difficulty.Medium, "social"),
                W("w140", "Name a podcast topic", "would not last one episode", "could listen for hours", Difficulty.Medium, "culture"),
                W("w141", "Name a sequel to a video game", "ruined the series", "improved on everything", Difficulty.Medium, "gaming"),
                W("w142", "Name an art form", "anyone could do it", "takes real genius", Difficulty.Medium, "art"),
                W("w143", "Name a musical", "unbearable", "life changing", Difficulty.Medium, "theatre"),
                W("w144", "Name a fictional villain", "not remotely threatening", "genuinely terrifying", Difficulty.Medium, "film"),
                W("w145", "Name a talent worth having", "a party trick", "a genuine gift", Difficulty.Medium, "skills"),
                W("w146", "Name a way to end a TV series", "an insult to the fans", "a perfect ending", Difficulty.Hard, "tv"),
                W("w147", "Name a piece of classical music", "background noise", "genuinely moving", Difficulty.Hard, "music"),
                W("w148", "Name a poem", "meaningless", "says it perfectly", Difficulty.Hard, "books"),
                W("w149", "Name a work of architecture", "an eyesore", "a masterpiece", Difficulty.Hard, "art"),
                W("w150", "Name a cultural export", "embarrassing abroad", "something to be proud of", Difficulty.Hard, "culture")
            });
        }
    }
}
