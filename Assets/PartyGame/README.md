# Odd One Out

A local pass-and-play party game app for phones: five social deduction and bluffing games
sharing one device. Built on Unity 6000.6.2f1 (2D/URP template), portrait, offline, no accounts.

## Getting started

1. Open the project in Unity 6000.6.2f1.
2. `Party Game > Set Up Project` (one shot: imports TextMeshPro resources, generates content
   assets, creates the bootstrap scene, applies mobile player settings).
3. Open `Assets/PartyGame/Scenes/Bootstrap.unity` and press Play.

Other menu items:

| Menu | What it does |
| --- | --- |
| `Party Game > Rebuild Content` | Regenerates every content asset from the seed data |
| `Party Game > Validate Project` | Checks scenes, content and fonts are wired up |
| `Party Game > Build/Development (this platform)` | One-command development build |

## How the code is organised

```
Assets/PartyGame/
  Scripts/Runtime/
    Core/
      App/          AppServices (composition root), GameBootstrap
      Session/      PlayerState, PlayerRoster, GameSession
      Modes/        GameStep / StepResult, IGameMode, GameModeBase, ImposterModeBase,
                    GameSettings, GameModeDefinition, GameModeFactory
      Services/     IRandomProvider, Shuffler, ScoreManager, VotingManager,
                    GameTimer, ContentRotation
      Content/      ContentPack + one ScriptableObject per content type, ContentLibrary,
                    ContentService
      Persistence/  IKeyValueStore, SettingsService, ModeSettingsStore
      Monetisation/ IAdService + AdPolicy, IPurchaseService, EntitlementService
      Audio/        AudioManager (procedurally generated placeholder sounds)
      Util/         ValidationResult, TextUtility
    Games/          One folder per mode: the rules, and nothing else
    UI/
      Design/       Theme (all colours, type scale, spacing), UIGraphics (generated sprites)
      Framework/    UIFactory, ScreenBase, ScreenStack, AppController, UIRoot, SafeAreaFitter
      Components/   UiButton, SelectionCard, StepperControl, ToggleSwitch, OptionPicker,
                    TimerBar, PlayerRowView, ToastLayer
      Gameplay/     One view per step kind, plus the hand-the-phone gate
      Screens/      Menu, game select, players, setup, rules, gameplay, results, settings
  Scripts/Editor/   Content generation, project setup, build and validation tools
  Content/          Generated ScriptableObjects (edit these directly to change content)
  Tests/            EditMode (rules) and PlayMode (real UI)
```

### The central idea

A game mode never touches a `GameObject`. It turns session state into a list of `GameStep`
objects - message, private info, text input, choice, discussion, reveal, scoreboard - and
consumes a `StepResult` for each one. `GameplayScreen` renders whichever step is current with
the matching view and hands the result back.

That is what lets five different games share one gameplay screen, makes every rule unit
testable without Unity, and leaves room for a network layer later: steps and results are plain
data, so they could just as easily arrive from another device.

```
UI (views)  ->  GameplayScreen  ->  IGameMode  ->  GameSession state
     ^                                                    |
     +----------------- GameStep --------------------------+
```

### Pass and play

Any step marked `IsPrivate` with an `ActorPlayerId` is preceded by a hand-over gate, and
secret cards stay covered until the owner presses and holds. The gate is skipped only while
consecutive steps belong to the same player, so the phone is never passed twice in a row.
`AppController` keeps `AdPolicy.PrivateInformationVisible` in sync, which hard-blocks every ad
while a secret is on screen.

## Player counts

Four of the five games run from three players upward. `GameModeDefinition` carries a separate
`RecommendedPlayers`, which the cards and the setup screen surface as a suggestion - "Plays with
3, best with 4 or more" - and which never blocks a table from starting. The Suspects is the one
exception: it keeps a minimum of five, because the investigator and witness have to hide among
enough people to be worth hiding.

`PlayerRoster.AbsoluteMinPlayers` is the floor for the whole app, and `MinPlayers` can never
drop below it.

## Content

Content lives in ScriptableObjects under `Assets/PartyGame/Content` and is indexed by
`Assets/Resources/PartyGame/ContentLibrary.asset`. Adding a category means duplicating an asset
and filling it in - no code changes. `ContentBuilder` regenerates them all from the seed data in
`ContentSeedData.cs` if you ever want to start over.

Debate topics are deliberately limited to everyday opinions rather than anything personal or
political. Packs carry an `enabled` flag and a `premium` flag so content can be retired or
gated later without touching gameplay.

## Monetisation

Nothing is monetised today, but the seams are in place:

- `IAdService` with a `NullAdService` that always calls back, so flow never depends on an ad.
- `AdPolicy` is the only thing that decides whether an ad may show. Gameplay contains no
  `if (adFree)` checks.
- `IPurchaseService` with a `NullPurchaseService` that reports the store as unavailable.
- `EntitlementService` is the single source of truth for what the player owns, persisted
  locally. `PurchaseCoordinator` maps `remove_ads` to the `ad_free` entitlement.

## Tests

- **EditMode (113)** - randomisation, scoring, voting and ties, timers, content rotation,
  roster validation, settings clamping, entitlements and ad policy, plus a harness that plays
  every mode end to end and asserts the rules held.
- **PlayMode (10)** - boots the app, builds every screen, and plays all five games through the
  real buttons to the results screen. One test also writes PNG captures of every screen at
  1080x1920 to `Screenshots/`.

```
Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

## Deliberate decisions

- **Two scenes, not seven.** `Bootstrap` starts the app; every screen lives on one canvas and
  is pushed and popped by `ScreenStack`. Scene loads between screens would add load hitches and
  risk a frame where a previous screen's secret is still on screen. Managers exist once.
- **UI built in code.** Screens are composed from `UIFactory` and `Theme` rather than prefabs,
  so there is exactly one definition of a button, a card and a colour, and restyling the app is
  a single-file change.
- **Sprites and sounds are generated at runtime.** Rounded rectangles, rings and UI tones are
  produced in code, so the project has a consistent look with no imported art and real audio
  with no binary assets. Both are replaceable: assign a clip through `AudioManager.OverrideClip`
  or swap the sprite helpers.
