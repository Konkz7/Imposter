using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    public static partial class ContentSeedData
    {
        // The aim is playful disagreement, not hostility. Everything here is an everyday opinion
        // a table can argue about for five minutes and then forget: nothing political, medical,
        // religious or personal, because the fun comes from arguing badly on purpose.
        //
        // Sentence structure is deliberately varied - flat assertions, comparisons, "should"
        // statements and absolutes - so a round does not feel like a template.

        private static List<PackSeed<DebateStatement>> BuildDebatePacks()
        {
            return new List<PackSeed<DebateStatement>>
            {
                DebateFoodPack(),
                DebateLifePack(),
                DebateCulturePack(),
                DebateTechPack(),
                DebateSocialPack(),
                DebateWorkPack()
            };
        }

        private static PackSeed<DebateStatement> DebateFoodPack()
        {
            return new PackSeed<DebateStatement>("debate-food", "Food and drink",
                "Kitchen table arguments", "FD", 3, new[]
            {
                D("d1", "Pineapple belongs on pizza.", "Food", "pizza,classic"),
                D("d2", "Breakfast is the best meal of the day.", "Food", "breakfast"),
                D("d3", "A cooked meal is always better than a takeaway.", "Food", "cooking"),
                D("d4", "Tea is better than coffee.", "Drink", "tea,coffee"),
                D("d5", "Cereal counts as a dinner.", "Food", "meals"),
                D("d6", "Chocolate should be kept in the fridge.", "Food", "chocolate"),
                D("d7", "Eating the same lunch every day is a good idea.", "Food", "routine"),
                D("d8", "A sandwich cut into triangles tastes better.", "Food", "sandwiches"),
                D("d9", "Dessert is the only course that matters.", "Food", "dessert"),
                D("d10", "Cooking for other people is more fun than eating.", "Food", "cooking"),
                D("d11", "Ketchup does not belong on a cooked breakfast.", "Food", "breakfast,sauce"),
                D("d12", "Fruit has no place in a savoury dish.", "Food", "cooking"),
                D("d13", "The crust is the worst part of the bread.", "Food", "bread"),
                D("d14", "Expensive restaurants are never worth the money.", "Food", "restaurants"),
                D("d15", "Everyone should be able to cook at least five meals from memory.", "Food", "skills"),
                D("d16", "Leftovers taste better the next day.", "Food", "leftovers"),
                D("d17", "There is no excuse for a badly made cup of tea.", "Drink", "tea"),
                D("d18", "Sparkling water is objectively worse than still.", "Drink", "water"),
                D("d19", "A roast dinner is overrated.", "Food", "british"),
                D("d20", "Sharing plates ruin a meal.", "Food", "restaurants"),
                D("d21", "Cheese is the best thing humans have ever made.", "Food", "cheese"),
                D("d22", "Breakfast food should be available all day.", "Food", "breakfast"),
                D("d23", "Microwaves are underrated.", "Food", "cooking"),
                D("d24", "A meal is not a meal without a vegetable.", "Food", "health"),
                D("d25", "Cold pizza is better than reheated pizza.", "Food", "pizza"),
                D("d26", "Nobody actually likes olives, they just say they do.", "Food", "taste"),
                D("d27", "Recipes should be followed exactly.", "Food", "cooking"),
                D("d28", "The best food in the world is simple food.", "Food", "cooking")
            });
        }

        private static PackSeed<DebateStatement> DebateLifePack()
        {
            return new PackSeed<DebateStatement>("debate-life", "Everyday life",
                "Habits, homes and how to live", "LF", 0, new[]
            {
                D("d29", "Working from home is better than working in an office.", "Work", "remote"),
                D("d30", "Summer is better than winter.", "Seasons", "weather"),
                D("d31", "Getting up early is worth it.", "Routine", "sleep"),
                D("d32", "Living in a city beats living in the countryside.", "Home", "city"),
                D("d33", "Money can buy happiness.", "Money", "philosophy"),
                D("d34", "It is better to plan a holiday than to improvise one.", "Travel", "planning"),
                D("d35", "Owning a pet is worth the hassle.", "Home", "pets"),
                D("d36", "Driving is better than public transport.", "Travel", "transport"),
                D("d37", "Tidying up as you go is the only way to live.", "Home", "cleaning"),
                D("d38", "Birthdays stop being fun after a certain age.", "Life", "birthdays"),
                D("d39", "A short holiday every month beats one long one a year.", "Travel", "holidays"),
                D("d40", "Renting is better than owning.", "Home", "money"),
                D("d41", "You should never go to bed on an argument.", "Life", "advice"),
                D("d42", "Naps are a sign of a well organised life.", "Routine", "sleep"),
                D("d43", "It is fine to wear the same outfit two days running.", "Life", "clothes"),
                D("d44", "Everyone should learn to drive.", "Skills", "driving"),
                D("d45", "Second hand is always better than new.", "Money", "shopping"),
                D("d46", "There is no such thing as bad weather, only bad clothing.", "Weather", "outdoors"),
                D("d47", "Open plan living is a mistake.", "Home", "design"),
                D("d48", "Alarm clocks should be banned at weekends.", "Routine", "sleep"),
                D("d49", "You should always take the stairs.", "Health", "exercise"),
                D("d50", "Keeping houseplants alive is harder than it looks.", "Home", "plants"),
                D("d51", "A long commute is worth it for a better house.", "Home", "travel"),
                D("d52", "It is better to be early than on time.", "Life", "punctuality"),
                D("d53", "Everyone should keep a diary.", "Life", "habits"),
                D("d54", "Sunday is the worst day of the week.", "Life", "weekends"),
                D("d55", "You should replace things before they break.", "Money", "habits"),
                D("d56", "Hoarding is just optimism about the future.", "Home", "clutter")
            });
        }

        private static PackSeed<DebateStatement> DebateCulturePack()
        {
            return new PackSeed<DebateStatement>("debate-culture", "Culture and taste",
                "Films, music and what counts as good", "CU", 5, new[]
            {
                D("d57", "Books are better than films.", "Books", "reading"),
                D("d58", "Remakes are never as good as the original.", "Film", "remakes"),
                D("d59", "Listening to music while working helps.", "Music", "focus"),
                D("d60", "Watching a series weekly is better than binge watching.", "TV", "streaming"),
                D("d61", "Live music is always worth the price.", "Music", "concerts"),
                D("d62", "Spoilers do not actually ruin anything.", "Film", "spoilers"),
                D("d63", "Video games are a legitimate art form.", "Gaming", "art"),
                D("d64", "A film over three hours long is too long.", "Film", "length"),
                D("d65", "Reality television has no redeeming qualities.", "TV", "reality"),
                D("d66", "Everyone should learn a musical instrument.", "Music", "skills"),
                D("d67", "Subtitles improve every film.", "Film", "subtitles"),
                D("d68", "The book is only better because you read it first.", "Books", "adaptations"),
                D("d69", "Modern art is a con.", "Art", "modern"),
                D("d70", "A bad ending ruins a good series.", "TV", "endings"),
                D("d71", "Musicals are the highest form of theatre.", "Theatre", "musicals"),
                D("d72", "Audiobooks count as reading.", "Books", "audiobooks"),
                D("d73", "Poetry is better read aloud than on the page.", "Books", "poetry"),
                D("d74", "Nobody needs a physical music collection any more.", "Music", "vinyl"),
                D("d75", "Comedy ages worse than any other genre.", "Film", "comedy"),
                D("d76", "Sequels should be banned.", "Film", "sequels"),
                D("d77", "You should always finish a book you have started.", "Books", "reading"),
                D("d78", "Going to the cinema beats watching at home.", "Film", "cinema"),
                D("d79", "The best music was made before you were born.", "Music", "nostalgia"),
                D("d80", "Documentaries are more entertaining than dramas.", "TV", "documentary"),
                D("d81", "A film should never be longer than the book.", "Film", "adaptations"),
                D("d82", "Board games are better than video games.", "Gaming", "boardgames")
            });
        }

        private static PackSeed<DebateStatement> DebateTechPack()
        {
            return new PackSeed<DebateStatement>("debate-tech", "Technology",
                "Screens, apps and gadgets", "TC", 1, new[]
            {
                D("d83", "Social media has done more harm than good.", "Internet", "social"),
                D("d84", "Group chats are better than phone calls.", "Communication", "messaging"),
                D("d85", "Phones should be banned from the dinner table.", "Etiquette", "phones"),
                D("d86", "Paper books beat e-readers.", "Reading", "ereaders"),
                D("d87", "Smart home gadgets are more trouble than they are worth.", "Gadgets", "smarthome"),
                D("d88", "Voice notes are rude.", "Communication", "messaging"),
                D("d89", "Everyone should switch their phone off one day a week.", "Habits", "detox"),
                D("d90", "Autocorrect has made everyone worse at spelling.", "Language", "phones"),
                D("d91", "Video calls are worse than meeting in person in every way.", "Communication", "video"),
                D("d92", "You should never read the comments.", "Internet", "comments"),
                D("d93", "Notifications should be off by default.", "Gadgets", "phones"),
                D("d94", "Owning fewer gadgets makes life better.", "Gadgets", "minimalism"),
                D("d95", "Email is a worse invention than the fax machine.", "Communication", "email"),
                D("d96", "Wireless headphones were a mistake.", "Gadgets", "audio"),
                D("d97", "Everyone should know a little bit about how computers work.", "Skills", "literacy"),
                D("d98", "Streaming has ruined how we listen to albums.", "Music", "streaming"),
                D("d99", "Reading news on a phone is worse than reading a newspaper.", "News", "reading"),
                D("d100", "Passwords should have been replaced by now.", "Security", "passwords"),
                D("d101", "Screens before bed are not actually that bad.", "Health", "sleep"),
                D("d102", "Online shopping has made high streets better, not worse.", "Shopping", "retail")
            });
        }

        private static PackSeed<DebateStatement> DebateSocialPack()
        {
            return new PackSeed<DebateStatement>("debate-social", "People and manners",
                "Friendship, etiquette and awkwardness", "PM", 6, new[]
            {
                D("d103", "It is fine to cancel plans at the last minute.", "Friendship", "plans"),
                D("d104", "Turning up early to a party is worse than turning up late.", "Etiquette", "parties"),
                D("d105", "You should always split the bill evenly.", "Money", "restaurants"),
                D("d106", "Surprise parties are a bad idea.", "Parties", "surprises"),
                D("d107", "It is rude to arrive at someone's house empty handed.", "Etiquette", "visiting"),
                D("d108", "Small talk is a genuinely useful skill.", "Conversation", "smalltalk"),
                D("d109", "You should reply to a message within a day.", "Communication", "etiquette"),
                D("d110", "Giving money as a present is lazy.", "Gifts", "money"),
                D("d111", "It is acceptable to re-gift a present.", "Gifts", "etiquette"),
                D("d112", "Best friends are overrated.", "Friendship", "friends"),
                D("d113", "You should never lend money to a friend.", "Money", "friendship"),
                D("d114", "Group holidays ruin friendships.", "Travel", "friends"),
                D("d115", "Being fashionably late is just being rude.", "Etiquette", "punctuality"),
                D("d116", "It is fine to leave a party without saying goodbye.", "Etiquette", "parties"),
                D("d117", "Everyone should send thank you notes.", "Etiquette", "manners"),
                D("d118", "Talking about money with friends should be normal.", "Money", "honesty"),
                D("d119", "You should always tell a friend if they have food in their teeth.", "Honesty", "manners"),
                D("d120", "Hosting is more enjoyable than being a guest.", "Parties", "hosting"),
                D("d121", "Keeping in touch is the responsibility of both people equally.", "Friendship", "effort"),
                D("d122", "It is better to be honest than kind.", "Honesty", "philosophy"),
                D("d123", "Nobody should ever have to give a speech at a wedding.", "Weddings", "speeches"),
                D("d124", "Making friends gets harder with age.", "Friendship", "age")
            });
        }

        private static PackSeed<DebateStatement> DebateWorkPack()
        {
            return new PackSeed<DebateStatement>("debate-work", "Work, school and sport",
                "Offices, classrooms and competition", "WK", 4, new[]
            {
                D("d125", "Meetings should have a strict time limit.", "Work", "meetings"),
                D("d126", "A four day week would make everyone more productive.", "Work", "hours"),
                D("d127", "Homework should be abolished.", "School", "homework"),
                D("d128", "School uniforms are a good idea.", "School", "uniform"),
                D("d129", "Exams are a bad way to measure ability.", "School", "exams"),
                D("d130", "Taking part matters more than winning.", "Sport", "competition"),
                D("d131", "Sports day is more stressful than any exam.", "School", "sport"),
                D("d132", "University is not worth the money any more.", "Education", "university"),
                D("d133", "Everyone should have to work in hospitality at least once.", "Work", "experience"),
                D("d134", "Open plan offices are a disaster.", "Work", "offices"),
                D("d135", "Dress codes at work are pointless.", "Work", "clothes"),
                D("d136", "Team building exercises never work.", "Work", "teams"),
                D("d137", "It is better to be good at one thing than decent at many.", "Skills", "career"),
                D("d138", "Referees should be able to explain decisions out loud.", "Sport", "officials"),
                D("d139", "Video technology has ruined football.", "Sport", "football"),
                D("d140", "Extra time should be scrapped in favour of penalties.", "Sport", "football"),
                D("d141", "PE should be optional at school.", "School", "sport"),
                D("d142", "Learning a language is more useful than learning to code.", "Skills", "education"),
                D("d143", "Handwriting should still be taught properly.", "School", "writing"),
                D("d144", "The best manager is one you barely notice.", "Work", "management"),
                D("d145", "Losing teaches you more than winning.", "Sport", "philosophy"),
                D("d146", "Everyone should have to present in front of a group at school.", "School", "confidence")
            });
        }
    }
}
