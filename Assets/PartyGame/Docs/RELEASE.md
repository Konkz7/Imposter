# Releasing Odd One Out

Everything needed to get a store build out, in the order you need it.

## 0. Prerequisites (not yet met on this machine)

Unity 6000.6.2f1 currently has only **WebGL** and **Windows** build support installed. Mobile
builds are impossible until you add the modules:

**Unity Hub → Installs → 6000.6.2f1 → ⚙ → Add modules**

- **Android Build Support**
  - **OpenJDK** (tick it)
  - **Android SDK & NDK Tools** (tick it)
- **iOS Build Support** — only useful alongside a Mac; Unity on Windows cannot produce an
  Xcode build, it can only export a project that a Mac then compiles.

You also need, one time:

| Thing | Where |
| --- | --- |
| Google Play developer account | one-off fee, identity verification takes a few days |
| Apple Developer Program | annual fee, plus a Mac for the final build and upload |
| An upload keystore | created by you, see below |

## 1. Create the upload keystore (once, and never lose it)

This is the only step nobody else can do for you: it needs a password, and if the file is lost
you cannot ever update the app under the same listing.

```bash
keytool -genkeypair -v -keystore oddoneout-upload.keystore -alias oddoneout -keyalg RSA -keysize 2048 -validity 10000
```

Keep the file **outside the repository** — somewhere backed up, such as a password manager's
file attachment. `*.keystore` and `*.jks` are gitignored so an accidental `git add` cannot
commit it.

Opt into **Play App Signing** when you create the listing. Google then holds the real signing
key and this keystore is only an upload key, which can be reset if it is ever lost.

## 2. Point the build at the keystore

The build reads signing from the environment so nothing secret is ever written into the project.

```bash
export PARTYGAME_KEYSTORE_PATH="/secure/path/oddoneout-upload.keystore"
export PARTYGAME_KEYSTORE_PASS="..."
export PARTYGAME_KEY_ALIAS="oddoneout"
export PARTYGAME_KEY_PASS="..."
```

PowerShell:

```powershell
$env:PARTYGAME_KEYSTORE_PATH = "C:\secure\oddoneout-upload.keystore"
$env:PARTYGAME_KEYSTORE_PASS = "..."
$env:PARTYGAME_KEY_ALIAS = "oddoneout"
$env:PARTYGAME_KEY_PASS = "..."
```

Without these the build still runs but is **unsigned** and Play will reject the upload.

## 3. Versioning

`PARTYGAME_VERSION` sets the name people see (`1.0.0`). `PARTYGAME_BUILD_NUMBER` sets the
integer Play and Apple sort by. Leave the build number unset and it increments automatically,
so two local builds never collide.

```bash
export PARTYGAME_VERSION="1.0.0"
```

Play rejects an upload whose build number is not higher than the last one, so never reuse one.

## 4. Build

From the editor: **Party Game → Release →** the item you want.

From the command line:

```bash
Unity.exe -batchmode -quit -nographics -projectPath . -logFile build.log -executeMethod PartyGame.EditorTools.ReleaseBuild.AndroidAppBundle
```

| Method | Output | Use |
| --- | --- | --- |
| `ReleaseBuild.AndroidAppBundle` | `Builds/Android/*.aab` | Google Play upload |
| `ReleaseBuild.AndroidApk` | `Builds/Android/*.apk` | sideload onto a real phone to test |
| `ReleaseBuild.IosXcodeProject` | `Builds/iOS/` | open in Xcode on a Mac, archive, upload |
| `BuildTools.BuildDevelopmentForCurrentPlatform` | `Builds/<platform>/` | quick local smoke test |

Always test the **APK on a real phone** before uploading the AAB. The editor cannot tell you
how the hand-over gate feels when the phone is actually moving between people.

## 5. Pre-flight

```bash
Unity.exe -batchmode -quit -nographics -projectPath . -logFile check.log -executeMethod PartyGame.EditorTools.ReleaseChecklist.Run
```

Exits non-zero if anything would be rejected. It checks scenes and content, the app identity and
version, that every icon slot is filled, that ARM64 and IL2CPP are on, that Development Build is
off, and that the runtime code still contains no networking — because the store listing and the
privacy policy both claim the app is fully offline, and that claim has to stay true.

## 6. Store assets

| Asset | Size | How |
| --- | --- | --- |
| App icon | 512x512 (Play), 1024x1024 (Apple) | **Party Game → Release → Generate App Icons** writes `Assets/PartyGame/Art/Icons/app-icon.png` at 1024 |
| Phone screenshots | 1080x1920 | run the `ScreenshotTests` play-mode test; output lands in `Screenshots/` |
| Feature graphic | 1024x500 | **not generated** - needs a designer or a simple banner |
| Listing copy | - | `STORE-LISTING.md` |
| Privacy policy | - | `PRIVACY.md`, published at <https://konkz7.github.io/Imposter/privacy.html> |

Play needs a minimum of two phone screenshots; four to eight tells the story better. The capture
test already produces the main menu, game select, a covered secret card, a revealed one, the
vote, the reveal and the final scores.

## 7. Play Console: Data Safety

Answer **no data collected, no data shared**. The app has no network code, no analytics, no ads
integrated and no account system. It stores only:

- player names, sound/music/vibration/animation preferences and per-game settings, in
  `PlayerPrefs`, on the device
- an `ad_free` entitlement flag, on the device

None of it leaves the phone. `ReleaseChecklist` re-verifies the no-networking claim on every run.

Content rating: the questionnaire should come out at **PEGI 3 / ESRB Everyone** or close to it.
The debate pack is deliberately everyday opinions - food, weather, films - with nothing
political, medical or sexual. If you add packs later, redo the rating questionnaire.

## 8. When ads and the purchase are switched on

Both are already abstracted, so switching them on is an implementation swap, not a rewrite.
But the moment either ships, the answers above change:

- Data Safety must declare whatever the ad SDK collects, which is usually a device identifier.
- You need a consent flow for GDPR and the Play Families policy if the rating stays low.
- `AdPolicy` already refuses to show anything while a secret is on screen; keep that.
- The privacy policy needs rewriting. It currently says the app collects nothing.

## 9. What is not set up

- **CI.** Building in GitHub Actions needs a Unity licence activated on the runner and the
  signing secrets in the repository's secret store. The build methods already take everything
  from the environment, so the workflow itself is short, but nothing is wired up yet.
- **Feature graphic and promotional art.** Needs a designer.
- **iOS.** Needs a Mac, an Apple Developer account and the iOS module.

## The privacy policy is published in two places

`Assets/PartyGame/Docs/PRIVACY.md` is the source of truth. The same text is served as HTML from
the `gh-pages` branch at <https://konkz7.github.io/Imposter/privacy.html>, which is the URL the
store listing points at. They are separate files, so **change both together** - a store listing
whose policy contradicts the app is a compliance problem, not a typo.

The policy states that the app collects nothing, requests no permissions and contains no ad SDK.
That is true today, and `ProjectSetupTests` fails if an advertising, analytics or purchasing
package is added to the manifest, or if any ad service other than `NullAdService` appears. Any release that adds advertising,
analytics or in-app purchases has to update the policy, the Play Data safety form and the Apple
privacy labels *before* it ships.
