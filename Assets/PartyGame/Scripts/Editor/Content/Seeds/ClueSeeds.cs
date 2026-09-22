using System.Collections.Generic;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    public static partial class ContentSeedData
    {
        // Clue packs are scenarios, not puzzles. The authored text here is flavour only: the
        // factual half of every private clue is generated at runtime from live round state by
        // SocialClueFactory, so a clue can never contradict what actually happened.
        //
        // Scene lines set a situation without naming anybody. InvestigatorPair lines wrap a
        // guaranteed "exactly one of these two" fact. AnonymousHint lines wrap a true but weak
        // group fact. WitnessDetail lines wrap an observation about how the table voted last
        // round, so they promise attention rather than a sighting - the app cannot see the room
        // the players are sitting in, and a clue must never claim otherwise. More lines per
        // scenario means fewer repeated rounds.

        private static List<PackSeed<ClueTemplate>> BuildCluePacks()
        {
            return new List<PackSeed<ClueTemplate>>
            {
                CluesHousePack(),
                CluesOfficePack(),
                CluesHolidayPack(),
                CluesWeddingPack(),
                CluesSchoolPack(),
                CluesCampingPack(),
                CluesRestaurantPack(),
                CluesFlatsharePack()
            };
        }

        private static PackSeed<ClueTemplate> CluesHousePack()
        {
            return new PackSeed<ClueTemplate>("clues-house", "The house party",
                "Something went missing while the music was loud", "HP", 5, new[]
            {
                C("c1", ClueKind.Scene, "The music stopped, the lights came back on, and the cake was gone."),
                C("c2", ClueKind.Scene, "Somebody unplugged the speakers halfway through the night and nobody owned up."),
                C("c3", ClueKind.Scene, "The back door was found wide open at two in the morning."),
                C("c4", ClueKind.Scene, "Every glass in the kitchen had been moved, and one was missing."),
                C("c5", ClueKind.Scene, "The neighbours complained about a noise that nobody in the house remembers making."),
                C("c6", ClueKind.Scene, "A coat that belongs to nobody here is hanging in the hallway."),
                C("c7", ClueKind.Scene, "There is a handprint on the ceiling and no agreed explanation."),
                C("c8", ClueKind.Scene, "The playlist was changed six times in one hour from an unknown device."),
                C("c9", ClueKind.InvestigatorPair, "You were watching the hallway all evening. Exactly one of {a} and {b} is involved."),
                C("c10", ClueKind.InvestigatorPair, "Your notes narrow it down: one of {a} and {b} is in on it, the other is not."),
                C("c11", ClueKind.InvestigatorPair, "You compared stories afterwards. Precisely one of {a} and {b} was lying."),
                C("c12", ClueKind.WitnessDetail, "You have been watching the room all night, not the dancing. {fact}"),
                C("c13", ClueKind.WitnessDetail, "You were on the stairs with a clear view of everybody. {fact}"),
                C("c14", ClueKind.WitnessDetail, "You kept an eye on who was backing whom. {fact}"),
                C("c15", ClueKind.AnonymousHint, "A note was left on the table. {fact}"),
                C("c16", ClueKind.AnonymousHint, "The neighbour phoned with one detail. {fact}"),
                C("c17", ClueKind.AnonymousHint, "Someone shouted it across the room and everyone heard. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesOfficePack()
        {
            return new PackSeed<ClueTemplate>("clues-office", "The office",
                "Something is quietly wrong at work", "OF", 0, new[]
            {
                C("c18", ClueKind.Scene, "The entire team's lunch disappeared from the fridge before midday."),
                C("c19", ClueKind.Scene, "A meeting room was booked all week under a name nobody recognises."),
                C("c20", ClueKind.Scene, "The printer jammed at nine and somebody quietly walked away from it."),
                C("c21", ClueKind.Scene, "Someone replied all to the wrong email and then deleted the evidence."),
                C("c22", ClueKind.Scene, "The office plant has been watered three times today and is not happy about it."),
                C("c23", ClueKind.Scene, "A confidential document was found in the recycling, face up."),
                C("c24", ClueKind.Scene, "Every chair on the third floor has been raised to its maximum height."),
                C("c25", ClueKind.Scene, "The shared calendar now contains an event called Do Not Delete."),
                C("c26", ClueKind.InvestigatorPair, "Security footage is grainy, but exactly one of {a} and {b} was there."),
                C("c27", ClueKind.InvestigatorPair, "The door log narrows it to two people: one of {a} and {b} is involved."),
                C("c28", ClueKind.InvestigatorPair, "You checked the sign in sheet. One of {a} and {b} is not telling the truth."),
                C("c29", ClueKind.WitnessDetail, "You have a clear view of the whole floor from your desk. {fact}"),
                C("c30", ClueKind.WitnessDetail, "You noticed who closed ranks with whom. {fact}"),
                C("c31", ClueKind.WitnessDetail, "You were watching the room rather than the screen. {fact}"),
                C("c32", ClueKind.AnonymousHint, "An anonymous message went round the team. {fact}"),
                C("c33", ClueKind.AnonymousHint, "The cleaner mentioned one thing on the way out. {fact}"),
                C("c34", ClueKind.AnonymousHint, "It was written on the whiteboard and nobody rubbed it off. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesHolidayPack()
        {
            return new PackSeed<ClueTemplate>("clues-holiday", "The group holiday",
                "Eight people, one villa, no trust", "HO", 4, new[]
            {
                C("c35", ClueKind.Scene, "The hire car came back with a scratch that was not there on Monday."),
                C("c36", ClueKind.Scene, "Somebody moved everyone's beach towels and rearranged the sun loungers."),
                C("c37", ClueKind.Scene, "The group kitty is forty euros short and everyone says they paid in."),
                C("c38", ClueKind.Scene, "The villa wifi password was changed overnight."),
                C("c39", ClueKind.Scene, "Someone booked a table for six when there are eight of us."),
                C("c40", ClueKind.Scene, "The good sunglasses have vanished and everyone is being very casual about it."),
                C("c41", ClueKind.Scene, "The air conditioning has been set to sixteen degrees and locked."),
                C("c42", ClueKind.Scene, "There is sand in the bed of somebody who claims not to have gone to the beach."),
                C("c43", ClueKind.InvestigatorPair, "You were awake late. Exactly one of {a} and {b} was not in bed."),
                C("c44", ClueKind.InvestigatorPair, "You heard two people on the stairs. One of {a} and {b} is involved."),
                C("c45", ClueKind.InvestigatorPair, "The receipts narrow it down: exactly one of {a} and {b} paid for something odd."),
                C("c46", ClueKind.WitnessDetail, "You have been watching the group from behind your sunglasses. {fact}"),
                C("c47", ClueKind.WitnessDetail, "You noticed who took whose side at dinner. {fact}"),
                C("c48", ClueKind.WitnessDetail, "You were up early and paying attention. {fact}"),
                C("c49", ClueKind.AnonymousHint, "A photo on the group chat gives one thing away. {fact}"),
                C("c50", ClueKind.AnonymousHint, "The villa owner left a note about it. {fact}"),
                C("c51", ClueKind.AnonymousHint, "Somebody said it out loud at breakfast and then went quiet. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesWeddingPack()
        {
            return new PackSeed<ClueTemplate>("clues-wedding", "The wedding",
                "A big day with a small disaster", "WD", 6, new[]
            {
                C("c52", ClueKind.Scene, "The top tier of the cake is missing and the photographer has gone very quiet."),
                C("c53", ClueKind.Scene, "Somebody rewrote the seating plan an hour before the guests arrived."),
                C("c54", ClueKind.Scene, "The best man's speech notes were found in the car park."),
                C("c55", ClueKind.Scene, "Three people have claimed the same plus one."),
                C("c56", ClueKind.Scene, "The first dance playlist was swapped for something nobody chose."),
                C("c57", ClueKind.Scene, "A gift envelope was opened and sealed again badly."),
                C("c58", ClueKind.Scene, "Somebody has been quietly drinking the top table champagne all afternoon."),
                C("c59", ClueKind.Scene, "The confetti was thrown twenty minutes before the couple came out."),
                C("c60", ClueKind.InvestigatorPair, "You were at the door greeting guests. Exactly one of {a} and {b} slipped away."),
                C("c61", ClueKind.InvestigatorPair, "The seating plan narrows it: one of {a} and {b} was not where they should be."),
                C("c62", ClueKind.InvestigatorPair, "Two people left during the speeches. Exactly one of {a} and {b} is involved."),
                C("c63", ClueKind.WitnessDetail, "You had the best view in the room from the top table. {fact}"),
                C("c64", ClueKind.WitnessDetail, "You noticed which way the room turned. {fact}"),
                C("c65", ClueKind.WitnessDetail, "You were watching faces during the speeches. {fact}"),
                C("c66", ClueKind.AnonymousHint, "It turned up in the wedding photos. {fact}"),
                C("c67", ClueKind.AnonymousHint, "A guest mentioned it to the venue staff. {fact}"),
                C("c68", ClueKind.AnonymousHint, "The DJ announced it by mistake over the microphone. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesSchoolPack()
        {
            return new PackSeed<ClueTemplate>("clues-school", "The school trip",
                "Thirty children, four adults, one incident", "ST", 1, new[]
            {
                C("c69", ClueKind.Scene, "The coach left fifteen minutes early and nobody will say who told the driver."),
                C("c70", ClueKind.Scene, "All the packed lunches were swapped between bags."),
                C("c71", ClueKind.Scene, "Someone pressed every button in the lift at the museum."),
                C("c72", ClueKind.Scene, "The gift shop reports one missing pencil and a great deal of guilt."),
                C("c73", ClueKind.Scene, "A worksheet has been filled in with answers from a completely different trip."),
                C("c74", ClueKind.Scene, "The fire alarm went off during the guided tour and nobody was cooking."),
                C("c75", ClueKind.Scene, "Someone has drawn a moustache on the laminated map."),
                C("c76", ClueKind.Scene, "The head count came back one too many, twice."),
                C("c77", ClueKind.InvestigatorPair, "You did the register twice. Exactly one of {a} and {b} answered late."),
                C("c78", ClueKind.InvestigatorPair, "Two people were last onto the coach. One of {a} and {b} is involved."),
                C("c79", ClueKind.InvestigatorPair, "You checked the buddy pairs. Exactly one of {a} and {b} broke theirs."),
                C("c80", ClueKind.WitnessDetail, "You were at the back of the group where you could see everyone. {fact}"),
                C("c81", ClueKind.WitnessDetail, "You were counting heads and watching who stuck together. {fact}"),
                C("c82", ClueKind.WitnessDetail, "You noticed who took whose side. {fact}"),
                C("c83", ClueKind.AnonymousHint, "It was reported by a very confident nine year old. {fact}"),
                C("c84", ClueKind.AnonymousHint, "The museum guide mentioned it on the way out. {fact}"),
                C("c85", ClueKind.AnonymousHint, "Somebody wrote it in the back of the trip diary. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesCampingPack()
        {
            return new PackSeed<ClueTemplate>("clues-camping", "The camping trip",
                "Wet, cold and somebody is lying", "CT", 2, new[]
            {
                C("c86", ClueKind.Scene, "The last of the firewood vanished while everyone was supposedly asleep."),
                C("c87", ClueKind.Scene, "One tent has been moved three metres to the left overnight."),
                C("c88", ClueKind.Scene, "The only working torch has been found with the batteries removed."),
                C("c89", ClueKind.Scene, "Somebody finished the marshmallows and put the empty bag back."),
                C("c90", ClueKind.Scene, "There are footprints leading to the river and none leading back."),
                C("c91", ClueKind.Scene, "The car keys were in a different pocket to the one they were left in."),
                C("c92", ClueKind.Scene, "Somebody let the fire go out and then loudly blamed the wood."),
                C("c93", ClueKind.Scene, "A tent peg has been used to open a tin and is now unusable."),
                C("c94", ClueKind.InvestigatorPair, "You were up in the night. Exactly one of {a} and {b} was outside their tent."),
                C("c95", ClueKind.InvestigatorPair, "Two head torches were on at 3am. One of {a} and {b} is involved."),
                C("c96", ClueKind.InvestigatorPair, "You counted boots by the fire. Exactly one of {a} and {b} had wet ones."),
                C("c97", ClueKind.WitnessDetail, "You were awake by the fire, watching. {fact}"),
                C("c98", ClueKind.WitnessDetail, "You noticed who sided with whom around the fire. {fact}"),
                C("c99", ClueKind.WitnessDetail, "You have been keeping track of who goes along with what. {fact}"),
                C("c100", ClueKind.AnonymousHint, "Somebody found it written in the campsite log book. {fact}"),
                C("c101", ClueKind.AnonymousHint, "The family in the next tent mentioned one thing. {fact}"),
                C("c102", ClueKind.AnonymousHint, "It came out around the fire and everyone went quiet. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesRestaurantPack()
        {
            return new PackSeed<ClueTemplate>("clues-restaurant", "The restaurant",
                "One table, one bill, one problem", "RS", 3, new[]
            {
                C("c103", ClueKind.Scene, "The bill is thirty pounds more than the sum of what everyone ordered."),
                C("c104", ClueKind.Scene, "A bottle of wine appeared on the table that nobody admits to ordering."),
                C("c105", ClueKind.Scene, "Somebody sent a dish back and then ate most of the replacement."),
                C("c106", ClueKind.Scene, "The reservation was under a name none of us recognises."),
                C("c107", ClueKind.Scene, "A dessert spoon has gone missing and one coat pocket is heavier."),
                C("c108", ClueKind.Scene, "Someone told the waiter it was a birthday and now there is a candle."),
                C("c109", ClueKind.Scene, "The tip was quietly changed after everyone agreed on it."),
                C("c110", ClueKind.Scene, "Two people have ordered the same dish and both claim they did it first."),
                C("c111", ClueKind.InvestigatorPair, "You watched the table all evening. Exactly one of {a} and {b} left their seat."),
                C("c112", ClueKind.InvestigatorPair, "The card machine tells a story: one of {a} and {b} is involved."),
                C("c113", ClueKind.InvestigatorPair, "Two people spoke to the waiter privately. Exactly one of {a} and {b} did."),
                C("c114", ClueKind.WitnessDetail, "You were sat where you could see the whole table. {fact}"),
                C("c115", ClueKind.WitnessDetail, "You caught the room reflected in the specials board. {fact}"),
                C("c116", ClueKind.WitnessDetail, "You noticed who agreed with whom over dinner. {fact}"),
                C("c117", ClueKind.AnonymousHint, "The waiter mentioned it while clearing plates. {fact}"),
                C("c118", ClueKind.AnonymousHint, "It was on the itemised receipt all along. {fact}"),
                C("c119", ClueKind.AnonymousHint, "Somebody said it while paying and immediately regretted it. {fact}")
            });
        }

        private static PackSeed<ClueTemplate> CluesFlatsharePack()
        {
            return new PackSeed<ClueTemplate>("clues-flatshare", "The flat share",
                "Shared kitchen, shared suspicion", "FS", 7, new[]
            {
                C("c120", ClueKind.Scene, "The labelled milk has been used and the label carefully peeled off."),
                C("c121", ClueKind.Scene, "The heating was on all night and nobody will admit to touching the dial."),
                C("c122", ClueKind.Scene, "A single dirty pan has been in the sink for six days."),
                C("c123", ClueKind.Scene, "The bins were not put out and everyone insists it was not their week."),
                C("c124", ClueKind.Scene, "Somebody has been using the good shampoo."),
                C("c125", ClueKind.Scene, "The wifi router has been restarted eleven times this week."),
                C("c126", ClueKind.Scene, "There is a parcel addressed to a former tenant and it has been opened."),
                C("c127", ClueKind.Scene, "The kitchen rota has been rewritten in a different handwriting."),
                C("c128", ClueKind.InvestigatorPair, "You heard the front door twice. Exactly one of {a} and {b} came in late."),
                C("c129", ClueKind.InvestigatorPair, "The shared bill app shows it: one of {a} and {b} is not being straight."),
                C("c130", ClueKind.InvestigatorPair, "Two people were in the kitchen at midnight. Exactly one of {a} and {b} was."),
                C("c131", ClueKind.WitnessDetail, "You were on the landing where you can hear everything. {fact}"),
                C("c132", ClueKind.WitnessDetail, "You noticed who took whose side in the kitchen. {fact}"),
                C("c133", ClueKind.WitnessDetail, "You have been keeping track of who agrees with whom. {fact}"),
                C("c134", ClueKind.AnonymousHint, "It was posted in the house group chat and then deleted. {fact}"),
                C("c135", ClueKind.AnonymousHint, "The landlord mentioned it during the inspection. {fact}"),
                C("c136", ClueKind.AnonymousHint, "Somebody left it written on a sticky note on the fridge. {fact}")
            });
        }
    }
}
