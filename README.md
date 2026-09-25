# Odd One Out

A collection of five pass-the-phone party games for people in the same room. One phone, no
accounts, no internet: hand it round, keep your secret, and work out who is lying.

Built with **Unity 6000.6.2f1** (URP, uGUI, TextMesh Pro), targeting Android and iOS.

## The games

| Game | Players | The idea |
| --- | --- | --- |
| **Different Word** | 3–12 | Everyone gets the same secret word except the imposter, who gets something close. Describe it without saying it, then vote. |
| **Fib** | 3–10 | A real trivia question. Everyone writes a fake answer; find the truth among the lies and score when people fall for yours. |
| **Number Wavelength** | 3–12 | Everyone has the same number from 1 to 10, except one. Answer a scaled question to match your number's strength. |
| **Devil's Advocate** | 3–10 | Everyone picks a side on a statement. One player is secretly told to argue the opposite. Who actually means it? |
| **The Suspects** | 5–12 | Secret roles for the whole game. Public scenes and clues, private clues for the investigator and witness, one accusation a round. |

## Getting started

1. Open the project in Unity Hub with **Unity 6000.6.2f1**.
2. Run **Party Game → Set Up Project**. It is idempotent and on a fresh checkout it:
   - imports the TextMesh Pro resources
   - generates all content assets from the seed data
   - creates the bootstrap scene and adds it to the build settings
   - applies the mobile player settings
3. Open `Assets/PartyGame/Scenes/Bootstrap.unity` and press Play.

To try it on a phone quickly, use **Party Game → Build → Development (this platform)**.

## Project layout

```
Assets/PartyGame/
  Scenes/              Bootstrap.unity, the only scene
  Scripts/Runtime/
    Core/              app flow, sessions, game-mode framework, content, persistence,
                       audio and monetisation abstractions
    Games/             one folder per game mode
    UI/                screen framework, screens, gameplay views and reusable components
  Scripts/Editor/
    Build/             setup, development and release builds, release checklist, icon generator
    Content/           content builder, validator and the seed data (Seeds/)
  Content/             generated ScriptableObject assets: modes, words, trivia, clues, debates
  Tests/               EditMode and PlayMode tests
  Docs/                release guide, store listing copy, privacy policy
```

## Content

Game content (word pairs, trivia, wavelength scales, debate statements, clues) is written as C#
seed data in `Scripts/Editor/Content/Seeds/` and turned into assets under `Content/`, plus the
library at `Assets/Resources/PartyGame/ContentLibrary.asset`.

- **Party Game → Rebuild Content** regenerates every asset from the seeds.
- **Party Game → Validate Content** checks for gaps and mistakes.

Add new content in the seed files and rebuild, rather than editing the generated assets, so a
rebuild never loses your changes.

## Tests

Run them from **Window → General → Test Runner**:

- **EditMode:** core services, each game mode's rules (through `ModeTestHarness`), and project setup.
- **PlayMode:** the full app flow, plus `ScreenshotTests`, which captures store screenshots into `Screenshots/`.

## Releasing

The full walkthrough is [`Assets/PartyGame/Docs/RELEASE.md`](Assets/PartyGame/Docs/RELEASE.md).
It covers the keystore, versioning, command-line builds, the pre-flight checklist, store assets
and the Play Data Safety answers. Here is the short version.

Signing is read from environment variables, so nothing secret is kept in the project:

| Variable | Purpose |
| --- | --- |
| `PARTYGAME_KEYSTORE_PATH` | full path to the upload keystore, kept outside the repo |
| `PARTYGAME_KEYSTORE_PASS` | keystore password |
| `PARTYGAME_KEY_ALIAS` | key alias, e.g. `oddoneout` |
| `PARTYGAME_KEY_PASS` | key password |
| `PARTYGAME_VERSION` | optional, the version name, e.g. `1.0.0` |
| `PARTYGAME_BUILD_NUMBER` | optional, otherwise it increments on every build |

Unity reads environment variables only at startup, so after changing any of them fully quit Unity
**and Unity Hub** before building. On Windows, don't include the quotes in the value itself:
`setx PARTYGAME_KEYSTORE_PATH "C:\path\key.keystore"` is right, and `"\"C:\...\""` is not.

Then use **Party Game → Release →**:

- **Check Release Readiness**: the pre-flight checklist
- **Build Android APK**: sideload onto a real phone to test
- **Build Android App Bundle**: the Google Play upload
- **Export iOS Xcode Project**: finish on a Mac

Builds land in `Builds/`.

## Privacy

The app is fully offline. It has no networking, analytics or accounts, and it stores only player
names and preferences on the device. The release checklist fails if networking code appears in
the runtime, because the [privacy policy](Assets/PartyGame/Docs/PRIVACY.md) and store listing
both depend on that being true.
