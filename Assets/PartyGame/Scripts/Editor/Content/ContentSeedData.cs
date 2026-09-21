using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    /// <summary>Shape of one authored pack before it becomes a ScriptableObject.</summary>
    public class PackSeed<T>
    {
        public string Id;
        public string Name;
        public string Description;
        public string Glyph;
        public int Accent;
        public List<T> Entries = new List<T>();

        public PackSeed(string id, string name, string description, string glyph, int accent, IEnumerable<T> entries)
        {
            Id = id;
            Name = name;
            Description = description;
            Glyph = glyph;
            Accent = accent;
            Entries = new List<T>(entries);
        }
    }

    /// <summary>
    /// The initial content library, kept as plain data so it can be regenerated into assets in
    /// one pass. After generation the .asset files are the source of truth and can be edited
    /// in the inspector like any other content.
    /// </summary>
    public static class ContentSeedData
    {
        private static WordPair P(string a, string b, string hint) => new WordPair(a, b, hint);

        public static readonly List<PackSeed<WordPair>> WordCategories = new List<PackSeed<WordPair>>
        {
            new PackSeed<WordPair>("food", "Food", "Meals, snacks and everything on the table", "FD", 3, new[]
            {
                P("Pizza", "Burger", "You usually eat it with your hands"),
                P("Sushi", "Sandwich", "It comes in neat little pieces"),
                P("Pancake", "Waffle", "Breakfast, often with syrup"),
                P("Curry", "Stew", "Slow cooked and served hot"),
                P("Ice cream", "Milkshake", "Cold, sweet and dairy"),
                P("Chips", "Crisps", "Potato, salt and regret"),
                P("Spaghetti", "Noodles", "Long and best twirled"),
                P("Cheese", "Butter", "It lives in the fridge door"),
                P("Chocolate", "Toffee", "Sweet and best shared"),
                P("Salad", "Soup", "Somebody always calls it healthy"),
                P("Roast dinner", "Barbecue", "A whole afternoon of cooking"),
                P("Doughnut", "Croissant", "Best bought fresh in the morning"),
                P("Popcorn", "Peanuts", "Snack food eaten by the handful")
            }),
            new PackSeed<WordPair>("animals", "Animals", "Pets, wildlife and things with paws", "AN", 2, new[]
            {
                P("Dog", "Wolf", "Four legs and a very good nose"),
                P("Cat", "Lion", "It lands on its feet"),
                P("Horse", "Donkey", "You can put a saddle on it"),
                P("Shark", "Dolphin", "It never stops swimming"),
                P("Eagle", "Owl", "Sharp eyes and sharper claws"),
                P("Snake", "Lizard", "Cold blooded and scaly"),
                P("Rabbit", "Hamster", "Small, fluffy and fast"),
                P("Elephant", "Rhino", "Enormous and grey"),
                P("Penguin", "Seal", "Happier in cold water"),
                P("Bee", "Wasp", "Small, striped and not to be annoyed"),
                P("Frog", "Toad", "It starts life in the water"),
                P("Monkey", "Squirrel", "Excellent at climbing")
            }),
            new PackSeed<WordPair>("places", "Places", "Buildings, rooms and where people go", "PL", 4, new[]
            {
                P("Beach", "Swimming pool", "You go there to get wet"),
                P("Library", "Museum", "Keep your voice down"),
                P("Hospital", "Dentist", "Nobody goes there for fun"),
                P("Airport", "Train station", "People arrive and leave all day"),
                P("Cinema", "Theatre", "Everyone faces the same way"),
                P("Supermarket", "Market", "You leave with bags"),
                P("Park", "Garden", "Green and outdoors"),
                P("Hotel", "Campsite", "You sleep somewhere unfamiliar"),
                P("Gym", "Playground", "It is full of equipment"),
                P("Kitchen", "Bathroom", "A room with taps"),
                P("Castle", "Cathedral", "Old, stone and full of tourists"),
                P("Nightclub", "Pub", "It gets loud after dark")
            }),
            new PackSeed<WordPair>("screen", "Film and TV", "Things you watch", "TV", 5, new[]
            {
                P("Horror film", "Thriller", "You watch it with the lights on"),
                P("Cartoon", "Anime", "Drawn rather than filmed"),
                P("Soap opera", "Sitcom", "It runs for years"),
                P("Documentary", "News", "It says it is telling you the truth"),
                P("Superhero film", "Action film", "Expect explosions"),
                P("Talent show", "Quiz show", "There is a panel of judges"),
                P("Romcom", "Musical", "Somebody ends up in love"),
                P("Western", "War film", "Lots of dust and shouting"),
                P("Cliffhanger", "Plot twist", "It happens at the end of an episode"),
                P("Trailer", "Advert", "Short and trying to sell you something"),
                P("Box set", "Series finale", "You lose a whole weekend to it"),
                P("Subtitles", "Voiceover", "Words added to the picture")
            }),
            new PackSeed<WordPair>("sport", "Sport", "Games, teams and sweating", "SP", 6, new[]
            {
                P("Football", "Rugby", "Two teams and a pitch"),
                P("Tennis", "Badminton", "There is a net in the middle"),
                P("Swimming", "Diving", "You need a pool"),
                P("Marathon", "Sprint", "Running until it hurts"),
                P("Boxing", "Wrestling", "Two people, one ring"),
                P("Cycling", "Skateboarding", "It has wheels"),
                P("Golf", "Snooker", "Aim carefully and stay quiet"),
                P("Basketball", "Netball", "The ball goes through a hoop"),
                P("Skiing", "Ice skating", "Cold and slippery"),
                P("Cricket", "Baseball", "Someone throws, someone swings"),
                P("Gymnastics", "Dance", "Very fit people making it look easy"),
                P("Darts", "Archery", "Hit the target")
            }),
            new PackSeed<WordPair>("school", "School and work", "Lessons, offices and deadlines", "SC", 0, new[]
            {
                P("Homework", "Revision", "It happens in the evening"),
                P("Exam", "Interview", "Nervous people in a quiet room"),
                P("Teacher", "Manager", "They tell you what to do"),
                P("Assembly", "Meeting", "Everyone sits and listens"),
                P("Detention", "Overtime", "Stuck there longer than planned"),
                P("Playtime", "Lunch break", "The best part of the day"),
                P("Report card", "Appraisal", "Somebody grades you"),
                P("Textbook", "Handbook", "Nobody reads all of it"),
                P("Register", "Timesheet", "It records who was there"),
                P("Whiteboard", "Projector", "Everyone looks at it"),
                P("Uniform", "Dress code", "Rules about what you wear"),
                P("Head teacher", "Chief executive", "The person at the top")
            }),
            new PackSeed<WordPair>("technology", "Technology", "Gadgets, apps and cables", "TC", 1, new[]
            {
                P("Phone", "Tablet", "It has a touchscreen"),
                P("Laptop", "Games console", "Plugged in and expensive"),
                P("Headphones", "Speaker", "It makes sound"),
                P("Wi-Fi", "Bluetooth", "It connects things without wires"),
                P("Password", "Passcode", "Do not tell anyone"),
                P("Email", "Text message", "You send it and wait"),
                P("Camera", "Webcam", "It points at you"),
                P("Charger", "Battery", "Everything dies without it"),
                P("Smart watch", "Fitness tracker", "Worn on the wrist"),
                P("Satnav", "Map app", "It tells you where to turn"),
                P("Cloud storage", "Hard drive", "Where your files live"),
                P("Video call", "Voice note", "Talking through a screen")
            }),
            new PackSeed<WordPair>("objects", "Everyday objects", "The stuff lying around the house", "OB", 7, new[]
            {
                P("Umbrella", "Raincoat", "Only useful in bad weather"),
                P("Kettle", "Microwave", "It heats things up"),
                P("Pillow", "Duvet", "It belongs on a bed"),
                P("Mirror", "Window", "You look at it, or through it"),
                P("Broom", "Vacuum cleaner", "It deals with the floor"),
                P("Wallet", "Keyring", "Do not leave the house without it"),
                P("Candle", "Torch", "It gives off light"),
                P("Scissors", "Stapler", "It lives in a drawer"),
                P("Toothbrush", "Comb", "Used every morning"),
                P("Ladder", "Step stool", "It helps you reach"),
                P("Clock", "Calendar", "It tells you you are late"),
                P("Suitcase", "Rucksack", "You pack things into it")
            }),
            new PackSeed<WordPair>("travel", "Travel", "Holidays and getting there", "TR", 4, new[]
            {
                P("Aeroplane", "Helicopter", "It leaves the ground"),
                P("Passport", "Boarding pass", "Do not lose it at the airport"),
                P("Road trip", "Cruise", "The journey is the holiday"),
                P("Hotel room", "Hostel", "You sleep there for a few nights"),
                P("Souvenir", "Postcard", "Proof you went"),
                P("Sunburn", "Jet lag", "It ruins the first two days"),
                P("Tour guide", "Travel agent", "They know where you should go"),
                P("Sightseeing", "Hiking", "A lot of walking"),
                P("Currency", "Exchange rate", "You worry about it abroad"),
                P("Delay", "Cancellation", "Bad news on the departure board"),
                P("Ferry", "Tram", "Public transport with a view"),
                P("Backpacking", "Camping", "Cheap and slightly uncomfortable")
            }),
            new PackSeed<WordPair>("games", "Games and toys", "Boards, cards and controllers", "GM", 6, new[]
            {
                P("Chess", "Draughts", "A board with squares"),
                P("Jigsaw", "Crossword", "You do it quietly at a table"),
                P("Poker", "Blackjack", "Cards and a poker face"),
                P("Hide and seek", "Tag", "Children running around"),
                P("Lego", "Building blocks", "You build things and stand on them"),
                P("Video game", "Board game", "Everyone argues about the rules"),
                P("Karaoke", "Charades", "Someone is going to embarrass themselves"),
                P("Dominoes", "Jenga", "Everything collapses eventually"),
                P("Dice", "Spinner", "It decides what happens next"),
                P("Quiz night", "Bingo", "Held in a pub or a hall"),
                P("Playing cards", "Tarot cards", "A deck you shuffle"),
                P("Teddy bear", "Action figure", "A toy you keep for years")
            }),
            new PackSeed<WordPair>("music", "Music", "Instruments, gigs and playlists", "MU", 5, new[]
            {
                P("Guitar", "Ukulele", "It has strings"),
                P("Drums", "Piano", "You hit it to make a sound"),
                P("Festival", "Concert", "Loud, crowded and expensive"),
                P("Playlist", "Album", "A collection of songs"),
                P("Choir", "Band", "A group performing together"),
                P("Chorus", "Verse", "Part of a song"),
                P("Headliner", "Support act", "They are on the poster"),
                P("Vinyl", "Cassette", "Older than streaming"),
                P("Busker", "DJ", "They play music in public"),
                P("Opera", "Musical", "Singing that tells a story"),
                P("Earworm", "Anthem", "It gets stuck in your head"),
                P("Microphone", "Amplifier", "It makes things louder")
            }),
            new PackSeed<WordPair>("nature", "Nature", "Weather, plants and the outdoors", "NA", 2, new[]
            {
                P("Thunderstorm", "Hurricane", "The weather turns nasty"),
                P("Snow", "Hail", "It falls from the sky and is cold"),
                P("Forest", "Jungle", "Full of trees"),
                P("Volcano", "Earthquake", "The ground is not behaving"),
                P("River", "Canal", "Water going somewhere"),
                P("Sunset", "Sunrise", "Everyone takes a photo of it"),
                P("Rainbow", "Lightning", "You look up to see it"),
                P("Mountain", "Hill", "It takes effort to get to the top"),
                P("Desert", "Beach", "Sand as far as you can see"),
                P("Cave", "Tunnel", "Dark and underground"),
                P("Oak tree", "Pine tree", "It has been there a long time"),
                P("Rose", "Tulip", "You might buy someone a bunch")
            })
        };

        public static readonly List<PackSeed<TriviaQuestion>> TriviaPacks = new List<PackSeed<TriviaQuestion>>
        {
            new PackSeed<TriviaQuestion>("history", "History", "People and dates worth arguing about", "HS", 4, new[]
            {
                new TriviaQuestion("h1", "What was the original name of the search project that became Google?", "BackRub", 2),
                new TriviaQuestion("h2", "Which city hosted the first modern Olympic Games in 1896?", "Athens", 1),
                new TriviaQuestion("h3", "In which year did the Berlin Wall come down?", "1989", 1),
                new TriviaQuestion("h4", "Who was the first person to walk on the Moon?", "Neil Armstrong", 1),
                new TriviaQuestion("h5", "Which country gave the Statue of Liberty to the United States?", "France", 1),
                new TriviaQuestion("h6", "What was Germany's currency before the euro?", "The Deutsche Mark", 2),
                new TriviaQuestion("h7", "Which ship sank on its maiden voyage in 1912?", "The Titanic", 1)
            }),
            new PackSeed<TriviaQuestion>("science", "Science", "Facts from the natural world", "SI", 1, new[]
            {
                new TriviaQuestion("s1", "What is the chemical symbol for gold?", "Au", 1),
                new TriviaQuestion("s2", "How many bones are there in an adult human body?", "206", 2),
                new TriviaQuestion("s3", "What is the hardest naturally occurring substance on Earth?", "Diamond", 1),
                new TriviaQuestion("s4", "Which gas do plants take in from the air?", "Carbon dioxide", 1),
                new TriviaQuestion("s5", "What is the largest organ in the human body?", "The skin", 2),
                new TriviaQuestion("s6", "At what Celsius temperature does water boil at sea level?", "100", 1),
                new TriviaQuestion("s7", "What does DNA stand for?", "Deoxyribonucleic acid", 3)
            }),
            new PackSeed<TriviaQuestion>("geography", "Geography", "Maps, capitals and very large rivers", "GE", 2, new[]
            {
                new TriviaQuestion("g1", "What is the capital of Australia?", "Canberra", 2),
                new TriviaQuestion("g2", "What is the largest hot desert in the world?", "The Sahara", 1),
                new TriviaQuestion("g3", "Which is the smallest country in the world by area?", "Vatican City", 1),
                new TriviaQuestion("g4", "Which mountain range separates Europe from Asia?", "The Urals", 2),
                new TriviaQuestion("g5", "What is the capital of Canada?", "Ottawa", 2),
                new TriviaQuestion("g6", "Which is the longest river in South America?", "The Amazon", 1),
                new TriviaQuestion("g7", "Mount Kilimanjaro is in which country?", "Tanzania", 2)
            }),
            new PackSeed<TriviaQuestion>("entertainment", "Entertainment", "Films, books and bands", "EN", 5, new[]
            {
                new TriviaQuestion("e1", "Which band released the album Abbey Road?", "The Beatles", 1),
                new TriviaQuestion("e2", "What was the first feature-length Disney animated film?", "Snow White", 2),
                new TriviaQuestion("e3", "Which planet is Superman originally from?", "Krypton", 1),
                new TriviaQuestion("e4", "What is the name of Sherlock Holmes's assistant?", "Doctor Watson", 1),
                new TriviaQuestion("e5", "Which hobbit carries the ring in The Lord of the Rings?", "Frodo Baggins", 1),
                new TriviaQuestion("e6", "Who wrote the play Romeo and Juliet?", "William Shakespeare", 1),
                new TriviaQuestion("e7", "In which fictional town is The Simpsons set?", "Springfield", 1)
            }),
            new PackSeed<TriviaQuestion>("oddfacts", "Odd facts", "Things that sound made up but are not", "OD", 6, new[]
            {
                new TriviaQuestion("o1", "What is a group of crows called?", "A murder", 2),
                new TriviaQuestion("o2", "What is the only mammal that can truly fly?", "The bat", 1),
                new TriviaQuestion("o3", "How many hearts does an octopus have?", "Three", 2),
                new TriviaQuestion("o4", "What colour is an octopus's blood?", "Blue", 2),
                new TriviaQuestion("o5", "Which letter appears in no US state name?", "Q", 3),
                new TriviaQuestion("o6", "What is a group of flamingos called?", "A flamboyance", 3),
                new TriviaQuestion("o7", "How many stomach compartments does a cow have?", "Four", 2)
            }),
            new PackSeed<TriviaQuestion>("foodtrivia", "Food and drink", "Kitchen knowledge", "FD", 3, new[]
            {
                new TriviaQuestion("f1", "Which country does the dish paella come from?", "Spain", 1),
                new TriviaQuestion("f2", "What is the main ingredient in traditional hummus?", "Chickpeas", 1),
                new TriviaQuestion("f3", "Which spice is the most expensive by weight?", "Saffron", 2),
                new TriviaQuestion("f4", "Which fruit is cider made from?", "Apples", 1),
                new TriviaQuestion("f5", "What is tofu made from?", "Soya beans", 1),
                new TriviaQuestion("f6", "Which nut is marzipan made from?", "Almonds", 2)
            }),
            new PackSeed<TriviaQuestion>("techtrivia", "Technology", "Computers and the internet", "TC", 0, new[]
            {
                new TriviaQuestion("t1", "What does the www in a web address stand for?", "World Wide Web", 1),
                new TriviaQuestion("t2", "What does CPU stand for?", "Central processing unit", 2),
                new TriviaQuestion("t3", "How many bits are there in a byte?", "Eight", 2),
                new TriviaQuestion("t4", "Which 1972 Atari game was the first commercial hit?", "Pong", 2),
                new TriviaQuestion("t5", "What does HTTP stand for?", "Hypertext transfer protocol", 3),
                new TriviaQuestion("t6", "Which company makes the iPhone?", "Apple", 1)
            })
        };

        public static readonly List<PackSeed<WavelengthQuestion>> WavelengthPacks = new List<PackSeed<WavelengthQuestion>>
        {
            new PackSeed<WavelengthQuestion>("wave-everyday", "Everyday life", "Ordinary things, rated out of ten", "EV", 0, new[]
            {
                new WavelengthQuestion("w1", "Describe a place to eat", "an awful place", "an incredible place"),
                new WavelengthQuestion("w2", "Describe a way to spend a Saturday", "a wasted day", "a perfect day"),
                new WavelengthQuestion("w3", "Describe a job", "a terrible job", "a dream job"),
                new WavelengthQuestion("w4", "Describe a holiday destination", "a grim trip", "the trip of a lifetime"),
                new WavelengthQuestion("w5", "Describe a way to travel to work", "the worst commute", "the best commute"),
                new WavelengthQuestion("w6", "Describe a birthday present", "a thoughtless gift", "an amazing gift"),
                new WavelengthQuestion("w7", "Describe a breakfast", "barely edible", "worth getting up for"),
                new WavelengthQuestion("w8", "Describe a place to live", "somewhere miserable", "somewhere wonderful"),
                new WavelengthQuestion("w9", "Describe a way to spend an evening", "deeply boring", "brilliant fun"),
                new WavelengthQuestion("w10", "Describe a piece of clothing", "an embarrassment", "extremely stylish")
            }),
            new PackSeed<WavelengthQuestion>("wave-opinions", "Strong opinions", "Rate it and defend it", "OP", 5, new[]
            {
                new WavelengthQuestion("w11", "Describe a film", "unwatchable", "a masterpiece"),
                new WavelengthQuestion("w12", "Describe a song", "skip it immediately", "on repeat all week"),
                new WavelengthQuestion("w13", "Describe a sport to watch", "dull beyond belief", "gripping"),
                new WavelengthQuestion("w14", "Describe a board game", "never again", "a classic"),
                new WavelengthQuestion("w15", "Describe a television series", "switched off after one episode", "watched twice"),
                new WavelengthQuestion("w16", "Describe a book", "a slog", "unputdownable"),
                new WavelengthQuestion("w17", "Describe a biscuit", "the last one in the tin", "the first one gone"),
                new WavelengthQuestion("w18", "Describe a famous landmark", "not worth the queue", "worth crossing the world for"),
                new WavelengthQuestion("w19", "Describe a hobby", "a complete waste of time", "genuinely rewarding"),
                new WavelengthQuestion("w20", "Describe a form of exercise", "pure misery", "surprisingly enjoyable")
            }),
            new PackSeed<WavelengthQuestion>("wave-scale", "How much?", "Size, difficulty and effort", "SC", 3, new[]
            {
                new WavelengthQuestion("w21", "Name something scary", "not scary at all", "terrifying"),
                new WavelengthQuestion("w22", "Name something expensive", "practically free", "eye-wateringly costly"),
                new WavelengthQuestion("w23", "Name something difficult to learn", "learned in an afternoon", "takes a lifetime"),
                new WavelengthQuestion("w24", "Name something useful", "completely pointless", "could not live without it"),
                new WavelengthQuestion("w25", "Name something noisy", "silent", "deafening"),
                new WavelengthQuestion("w26", "Name something fast", "painfully slow", "blindingly fast"),
                new WavelengthQuestion("w27", "Name a household chore", "barely counts as a chore", "the worst job in the house"),
                new WavelengthQuestion("w28", "Name something tiring", "restful", "completely exhausting"),
                new WavelengthQuestion("w29", "Name something impressive", "nobody would notice", "everyone would be amazed"),
                new WavelengthQuestion("w30", "Name something risky", "perfectly safe", "genuinely dangerous")
            }),
            new PackSeed<WavelengthQuestion>("wave-social", "Around the table", "Party-friendly prompts", "PT", 6, new[]
            {
                new WavelengthQuestion("w31", "Describe a party", "everyone left early", "talked about for years"),
                new WavelengthQuestion("w32", "Describe a first date idea", "a disaster", "a brilliant idea"),
                new WavelengthQuestion("w33", "Describe a group holiday activity", "nobody wants to do it", "everyone is in"),
                new WavelengthQuestion("w34", "Describe a house rule", "unreasonable", "completely fair"),
                new WavelengthQuestion("w35", "Describe a way to say sorry", "makes it worse", "completely forgiven"),
                new WavelengthQuestion("w36", "Describe a small act of kindness", "barely noticed", "made someone's week"),
                new WavelengthQuestion("w37", "Describe a group chat message", "instantly ignored", "gets forty replies"),
                new WavelengthQuestion("w38", "Describe a way to wake someone up", "cruel", "lovely")
            })
        };

        public static readonly List<PackSeed<DebateStatement>> DebatePacks = new List<PackSeed<DebateStatement>>
        {
            new PackSeed<DebateStatement>("debate-food", "Food and drink", "Kitchen table arguments", "FD", 3, new[]
            {
                new DebateStatement("d1", "Pineapple belongs on pizza."),
                new DebateStatement("d2", "Breakfast is the best meal of the day."),
                new DebateStatement("d3", "A cooked meal is always better than a takeaway."),
                new DebateStatement("d4", "Tea is better than coffee."),
                new DebateStatement("d5", "Cereal counts as a dinner."),
                new DebateStatement("d6", "Chocolate should be kept in the fridge."),
                new DebateStatement("d7", "Eating the same lunch every day is a good idea."),
                new DebateStatement("d8", "A sandwich cut into triangles tastes better."),
                new DebateStatement("d9", "Dessert is the only course that matters."),
                new DebateStatement("d10", "Cooking for other people is more fun than eating.")
            }),
            new PackSeed<DebateStatement>("debate-life", "Everyday life", "Habits, homes and routines", "LF", 0, new[]
            {
                new DebateStatement("d11", "Working from home is better than working in an office."),
                new DebateStatement("d12", "Summer is better than winter."),
                new DebateStatement("d13", "Getting up early is worth it."),
                new DebateStatement("d14", "Living in a city beats living in the countryside."),
                new DebateStatement("d15", "Money can buy happiness."),
                new DebateStatement("d16", "It is better to plan a holiday than to improvise one."),
                new DebateStatement("d17", "Owning a pet is worth the hassle."),
                new DebateStatement("d18", "Driving is better than public transport."),
                new DebateStatement("d19", "Tidying up as you go is the only way to live."),
                new DebateStatement("d20", "Birthdays stop being fun after a certain age."),
                new DebateStatement("d21", "A short holiday every month beats one long one a year.")
            }),
            new PackSeed<DebateStatement>("debate-culture", "Culture and taste", "Films, music and opinions", "CU", 5, new[]
            {
                new DebateStatement("d22", "Books are better than films."),
                new DebateStatement("d23", "Remakes are never as good as the original."),
                new DebateStatement("d24", "Listening to music while working helps."),
                new DebateStatement("d25", "Watching a series weekly is better than binge watching."),
                new DebateStatement("d26", "Live music is always worth the price."),
                new DebateStatement("d27", "Spoilers do not actually ruin anything."),
                new DebateStatement("d28", "Video games are a legitimate art form."),
                new DebateStatement("d29", "A film over three hours long is too long."),
                new DebateStatement("d30", "Reality television has no redeeming qualities."),
                new DebateStatement("d31", "Everyone should learn a musical instrument.")
            }),
            new PackSeed<DebateStatement>("debate-tech", "Technology", "Screens, apps and gadgets", "TC", 1, new[]
            {
                new DebateStatement("d32", "Social media has done more harm than good."),
                new DebateStatement("d33", "Group chats are better than phone calls."),
                new DebateStatement("d34", "Phones should be banned from the dinner table."),
                new DebateStatement("d35", "Paper books beat e-readers."),
                new DebateStatement("d36", "Smart home gadgets are more trouble than they are worth."),
                new DebateStatement("d37", "Voice notes are rude."),
                new DebateStatement("d38", "Everyone should switch their phone off one day a week.")
            })
        };

        public static readonly List<PackSeed<ClueTemplate>> CluePacks = new List<PackSeed<ClueTemplate>>
        {
            new PackSeed<ClueTemplate>("clues-house", "The house party", "Something went missing at a party", "HP", 5, new[]
            {
                new ClueTemplate("c1", ClueKind.Scene, "The music stopped, the lights came back on, and the cake was gone."),
                new ClueTemplate("c2", ClueKind.Scene, "Somebody unplugged the speakers halfway through the night and nobody owned up."),
                new ClueTemplate("c3", ClueKind.Scene, "The back door was found wide open at two in the morning."),
                new ClueTemplate("c4", ClueKind.Scene, "Every glass in the kitchen had been moved, and one was missing."),
                new ClueTemplate("c5", ClueKind.InvestigatorPair, "You were watching the hallway all evening. Exactly one of {a} and {b} is involved."),
                new ClueTemplate("c6", ClueKind.InvestigatorPair, "Your notes narrow it down: one of {a} and {b} is in on it, the other is not."),
                new ClueTemplate("c7", ClueKind.WitnessDetail, "You only caught a glimpse of them. {fact}"),
                new ClueTemplate("c8", ClueKind.WitnessDetail, "You were coming down the stairs when you saw them. {fact}"),
                new ClueTemplate("c9", ClueKind.AnonymousHint, "A note was left on the table. {fact}"),
                new ClueTemplate("c10", ClueKind.AnonymousHint, "The neighbour phoned with one detail. {fact}")
            }),
            new PackSeed<ClueTemplate>("clues-office", "The office", "Something is wrong at work", "OF", 0, new[]
            {
                new ClueTemplate("c11", ClueKind.Scene, "The entire team's lunch disappeared from the fridge before midday."),
                new ClueTemplate("c12", ClueKind.Scene, "A meeting room was booked all week under a name nobody recognises."),
                new ClueTemplate("c13", ClueKind.Scene, "The printer jammed at nine and somebody quietly walked away from it."),
                new ClueTemplate("c14", ClueKind.Scene, "Someone replied all to the wrong email and then deleted the evidence."),
                new ClueTemplate("c15", ClueKind.InvestigatorPair, "Security footage is grainy, but exactly one of {a} and {b} was there."),
                new ClueTemplate("c16", ClueKind.InvestigatorPair, "The door log narrows it to two people: one of {a} and {b} is involved."),
                new ClueTemplate("c17", ClueKind.WitnessDetail, "You looked up from your desk at the wrong moment. {fact}"),
                new ClueTemplate("c18", ClueKind.WitnessDetail, "You passed them in the corridor. {fact}"),
                new ClueTemplate("c19", ClueKind.AnonymousHint, "An anonymous message went round the team. {fact}"),
                new ClueTemplate("c20", ClueKind.AnonymousHint, "The cleaner mentioned one thing on the way out. {fact}")
            }),
            new PackSeed<ClueTemplate>("clues-holiday", "The group holiday", "Eight people, one villa, no trust", "HO", 4, new[]
            {
                new ClueTemplate("c21", ClueKind.Scene, "The hire car came back with a scratch that was not there on Monday."),
                new ClueTemplate("c22", ClueKind.Scene, "Somebody moved everyone's beach towels and rearranged the sun loungers."),
                new ClueTemplate("c23", ClueKind.Scene, "The group kitty is forty euros short and everyone says they paid in."),
                new ClueTemplate("c24", ClueKind.Scene, "The villa wifi password was changed overnight."),
                new ClueTemplate("c25", ClueKind.InvestigatorPair, "You were awake late. Exactly one of {a} and {b} was not in bed."),
                new ClueTemplate("c26", ClueKind.WitnessDetail, "You saw someone on the balcony after midnight. {fact}"),
                new ClueTemplate("c27", ClueKind.AnonymousHint, "A photo on the group chat gives one thing away. {fact}")
            })
        };
    }
}
