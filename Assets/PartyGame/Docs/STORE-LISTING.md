# Store listing — Odd One Out

Draft copy for Google Play and the App Store. Nothing here claims a feature the app does not
have; check it still matches before each submission.

## App name

**Odd One Out** (11 characters)

If the name is taken, the fallback is **Odd One Out: Party Games** (25 characters, within
Play's 30-character limit).

## Short description (Play, 80 characters max)

> Five party games for one phone. Pass it round, spot the liar. (61)

Alternatives:

> Hand the phone round. One of you is lying. Five games, no internet needed. (73)
> Five social deduction games for a room full of people and one phone. (67)

## Full description (Play, 4000 characters max)

> **One phone. A room full of people. Somebody is lying.**
>
> Odd One Out is a collection of five party games for people in the same room. No accounts, no
> internet, no second device — just pass the phone around and start arguing.
>
> **DIFFERENT WORD**
> Everyone gets the same secret word, except one player, who gets something close but not quite
> right. Take turns describing your word without ever saying it, then vote on who sounded
> slightly off. Choose how much help the odd one out gets: a similar word, just the category, or
> nothing at all.
>
> **FIB**
> A real question appears with an answer nobody has seen. Everyone secretly writes a fake answer,
> then all the lies get shuffled in with the truth. Score for spotting the real one — and score
> again every time somebody falls for yours.
>
> **NUMBER WAVELENGTH**
> Everyone gets the same number from one to ten. Almost everyone. A question appears with a
> scale, and your answer has to match the strength of your number without ever saying it. Find
> whoever is answering from the wrong end of the scale.
>
> **DEVIL'S ADVOCATE**
> A statement appears and everyone privately picks a side. One player is secretly told to argue
> the opposite of whatever they chose. Everyone makes their case out loud. Who actually means it?
>
> **THE SUSPECTS**
> Secret roles for the whole game. Every round the table hears a scene and a clue, while the
> investigator and the witness privately receive something narrower — and always true. Debate,
> accuse, and remove one player a round.
>
> **BUILT FOR PASSING AROUND**
> Secret information is always hidden behind a reveal screen. Hand the phone over first, then
> press and hold to read your card, and it hides again before you pass it on. Nothing private is
> ever on screen while the phone is moving.
>
> **NO INTERNET, NO ACCOUNTS, NO DATA**
> Everything runs offline. The app has no network code at all. Nothing about a round is ever
> saved — your words, roles and votes exist only while you are playing.
>
> • 3 to 12 players (The Suspects needs 5)
> • Over 2,200 word pairs across 22 categories
> • 326 trivia questions, 150 wavelength prompts, 146 debate topics
> • Adjustable rounds, timers, categories and difficulty per game
> • Reduced-motion option and large, readable text

## Artwork to upload

Play validates the sizes strictly, so these are generated at exactly what it asks for.

| Field | File | Size |
| --- | --- | --- |
| App icon | `StoreAssets/store-icon-512.png` | 512x512 |
| Feature graphic | `Screenshots/feature-graphic-1024x500.png` | 1024x500 |
| Phone screenshots | see below | 1080x1920 (9:16) |

Regenerate with **Party Game → Release → Generate Store Artwork** for the icon, and the
`FeatureGraphicTests` play mode test for the feature graphic. Both are drawn from `Theme`, so a
colour change flows through to the listing artwork.

### Screenshots, in this order

Six tells the story and clears Play's four-shot bar for promotion. All come from the
`ScreenshotTests` play mode test.

1. `01-main-menu.png` - what the app is
2. `02-game-select.png` - five games, not one
3. `08-handoff-secret-covered.png` - the hand-over gate, which is the thing no screenshot of a
   quiz app has
4. `09-secret-revealed.png` - a secret card actually revealed
5. `15-suspects-reveal.png` - the vote coming out
6. `14-results.png` - final scores

## Category and tags

- **Category:** Games → Trivia, or Games → Word. Trivia is the better fit for discovery.
- **Tags:** party game, social deduction, pass and play, local multiplayer, offline, word game
- **Content rating:** expect PEGI 3 / ESRB Everyone. No violence, no language, no purchases in
  this version, no user-to-user communication through the app.

## Apple App Store

- **Subtitle (30 characters):** `Pass the phone. Spot the liar.` (30)
- **Promotional text (170):** Five party games for one phone and a room full of people. No
  internet, no accounts, nothing saved. Hand it round and find out who is bluffing.
- **Keywords (100, comma separated, no spaces):**
  `party,deduction,bluff,imposter,word,trivia,friends,offline,local,group,family,game night`

## What still needs a person

- A **feature graphic** (1024x500) for Play. Not generated.
- Nothing further on privacy: the policy is live at
  <https://konkz7.github.io/Imposter/privacy.html> and the listing contact is o.place100@gmail.com.
- Confirmation that **"Odd One Out"** is free to use as a store name in your territories.
