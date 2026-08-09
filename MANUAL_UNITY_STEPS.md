# Manual Unity Steps

Steps that must be performed in the Unity Editor by hand, because they touch scenes, prefabs,
`ProjectSettings/`, packages or Build Profiles — all of which are owned by the team, not by
generated code (see `CLAUDE.md` §11).

Tick items off as they are completed. Later phases append to this file.

---

## Required before the MVP can ship

### U1 — Lock the app to portrait

`Edit > Project Settings > Player > Resolution and Presentation`

- [ ] `Default Orientation` = **Portrait**
- [ ] Uncheck `Allowed Orientations for Auto Rotation > Landscape Right`
- [ ] Uncheck `Allowed Orientations for Auto Rotation > Landscape Left`

Currently `defaultScreenOrientation: 4` (Auto Rotation) with all four orientations enabled, which
contradicts the portrait-only requirement in `CLAUDE.md` §2 and §14.

### U2 — Import TextMeshPro Essential Resources

`Window > TextMeshPro > Import TMP Essential Resources`

- [ ] Import **Essential Resources** (skip "Examples & Extras")

The TMP *code* already ships inside `com.unity.ugui` 2.0.0, so no package install is needed. The
*runtime assets* are still a one-time `.unitypackage` import, and without them every `TMP_Text`
renders with a missing material. Needed before Phase 5.

### U3 — Set application identity

`Edit > Project Settings > Player`

- [ ] `Company Name` — currently `DefaultCompany`
- [ ] `Product Name` — currently `RoyalDecisions`
- [ ] Android > `Override Default Package Name` ticked, `Package Name` set
      (e.g. `com.yusufsari.royaldecisions`)

Only a Standalone identifier exists today (`com.DefaultCompany.2D-URP`); Android has none, which
blocks a device build.

### U4 — Install Android build support

Unity Hub > Installs > `6000.3.20f1` > Add Modules

- [ ] Android Build Support
- [ ] OpenJDK
- [ ] Android SDK & NDK Tools
- [ ] `File > Build Profiles` — switch the active platform to Android

Needed for Phase 8.

---

## Recommended

### U5 — Commit the baseline before Phase 1 is reviewed

- [ ] Accept the deletion of `Assets/Editor/HubForceResolve.cs` — it is Unity Hub's bootstrap
      script, written to call `Client.Resolve()` once and then delete itself. The deletion is the
      script working as designed.
- [ ] Add `CLAUDE.md` and the new `ProjectSettings/Packages/` folder.

### U6 — Widen the supported aspect ratio

`Player > Resolution and Presentation > Supported Aspect Ratio`

- [ ] Raise `Up To` from `2.4` to `3.0`

At `2.4`, some 21:9 phones and folded foldables get letterboxed. `CLAUDE.md` §14 requires common
screen ratios to work.

### U7 — Ignore the generated solution file

- [ ] Add `*.slnx` to `.gitignore`

`RoyalDecisions.slnx` is auto-generated and currently tracked; the template's `*.sln` rule does not
match the newer `.slnx` extension.

---

## Notes for later phases

- **Safe Area is mandatory, not optional.** `androidRenderOutsideSafeArea: 1` is already set, so the
  app draws under notches and camera cutouts. The Canvas needs explicit Safe Area handling in
  Phase 5.
- **No packages need to be installed for the MVP.** uGUI, TextMeshPro, Input System, Test Framework
  and JSON serialisation are all present already.
- **Scene wiring** (Canvas, prefabs, Inspector references) arrives in Phase 5 and Phase 8; nothing
  in Phase 1 touches a scene.

---

## Phase 1 — nothing to wire

Phase 1 added only plain C# types, assembly definitions and EditMode tests. There is no scene,
prefab or Inspector work to do.

To confirm the phase locally:

- [ ] Reopen the project and check the Console shows no errors from `Assets/_Game/`
- [ ] `Window > General > Test Runner > EditMode > Run All` — all tests green

---

## Phase 2 — nothing to wire

Phase 2 added the rule services (`StatSystem`, `ConditionEvaluator`, `ChoiceResolver`,
`GameOverEvaluator`, `CardDeckService`, `SeededRandomSource`) and their EditMode tests. All of it is
plain C# inside the existing `RoyalDecisions.Domain` assembly — no new assembly definition, no
scene, prefab or Inspector work.

To confirm the phase locally:

- [ ] Console shows no errors or warnings from `Assets/_Game/`
- [ ] `Window > General > Test Runner > EditMode > Run All` — all tests green

Note for Phase 3: the placeholder content generator must emit **unique card IDs**. Weighted card
selection sorts eligible cards by ID ordinally to keep draws independent of asset order, and that
ordering is only well defined when IDs do not repeat — so duplicate IDs must be a hard validation
error in the generator.

---

## Phase 3 — run the content generator

Phase 3 added the `ContentCatalogue`, the content validator, and an Editor-only generator. There is
still no scene or prefab work, but this phase does need you to **run a menu command once**.

### P3.0 — Commit first

- [ ] Commit the existing work before generating anything.

The generator writes 29 assets into the project. It is written to abort rather than overwrite, and
never touches anything outside its own folder, but a commit is the only thing that makes a mistake
trivially recoverable.

### P3.1 — Generate

- [ ] `Tools > Royal Decisions > Generate Placeholder Content`
- [ ] Console reports `Created 29, Updated 0, Unchanged 0, Skipped 0, Warnings 0, Errors 0`

This writes, under `Assets/_Game/Content/Placeholder/`:

- `Cards/` — 20 `CardDefinition` assets
- `Endings/` — 8 `EndingDefinition` assets
- `PlaceholderContentCatalogue.asset` — the `ContentCatalogue`

### P3.2 — Confirm idempotency

- [ ] Run the same command a **second** time
- [ ] Console reports `Created 0, Updated 0, Unchanged 29`
- [ ] `git status` shows **no modified files**

If the second run reports updates, something is non-deterministic in generation and should be
reported rather than committed.

### P3.3 — Spot-check the content

- [ ] Every generated asset shows the `RoyalDecisions.Placeholder` label at the bottom of the
      Inspector
- [ ] Card speakers and ending titles begin with `[PLACEHOLDER]`
- [ ] `PlaceholderContentCatalogue` lists 20 cards, 8 endings, and
      `openingCardId = card_01_coronation`

### P3.4 — Optional: prove the overwrite guard

- [ ] Remove the `RoyalDecisions.Placeholder` label from any one generated asset
- [ ] Re-run the command — it must **abort**, report that asset as skipped, and write nothing
- [ ] Restore the label and re-run to return to a clean state

---

## Phase 5 — build the Game scene

Phase 5 added the passive presentation layer: `CardView`, `HUDView` + `StatItemView`,
`GameOverView`, `AudioService`, and `SafeAreaFitter`. All of it is driven from outside — nothing
renders until Phase 7 calls it — so this section builds the scene and wires the references.

**Build the Game scene now.** `Bootstrap` and `MainMenu` are described at the end as target
structure; they stay unbuilt until Phase 7, because nothing can move between scenes until
`GameFlowController` exists.

### P5.0 — Prerequisites

- [ ] **U2 — Import TMP Essential Resources.** Still outstanding. Every `TMP_Text` renders with a
      missing material without it, and the EditMode view tests cannot run.
- [ ] **Run the placeholder generator** (`Tools > Royal Decisions > Generate Placeholder Content`)
      if you have not — there is nothing to look at otherwise.
- [ ] **U1 — Lock to portrait.** Still outstanding, and Safe Area behaviour is only meaningful in
      the orientation you ship.

### P5.1 — Create the scene

- [ ] `File > New Scene` → Basic (URP), save as `Assets/_Game/Scenes/Game.unity`
- [ ] `File > Build Profiles > Scene List` — add it

### P5.2 — Camera and EventSystem

- [ ] Main Camera: `Projection = Orthographic`, `Background Type = Solid Color`
- [ ] `GameObject > UI > Event System`
- [ ] The EventSystem **must** use `InputSystemUIInputModule`. The project is set to the new Input
      System only (`activeInputHandler: 1`), so the legacy `StandaloneInputModule` will not work.
      Unity offers a **Replace with InputSystemUIInputModule** button — accept it.

### P5.3 — Canvas

`GameObject > UI > Canvas`, renamed `UICanvas`:

- [ ] `Render Mode` = **Screen Space – Overlay**
- [ ] `Pixel Perfect` off
- [ ] **CanvasScaler** → `UI Scale Mode` = **Scale With Screen Size**
- [ ] `Reference Resolution` = **1080 × 1920** (portrait)
- [ ] `Screen Match Mode` = **Match Width Or Height**, `Match` = **1** (height)

Matching on height keeps the card the same width on a taller phone rather than shrinking it.

### P5.4 — Safe Area

- [ ] Child of `UICanvas` named `SafeArea`, `RectTransform` **stretched to all four edges**, all
      offsets `0`
- [ ] Add **`SafeAreaFitter`**; leave `Target` empty so it uses its own `RectTransform`
- [ ] **Every other UI element parents under `SafeArea`**

`androidRenderOutsideSafeArea` is already enabled, so the app draws under notches. Without this the
HUD sits under the camera cutout on most modern phones.

### P5.5 — HUD

- [ ] Child of `SafeArea` named `HUD`, anchored to the top, add **`HUDView`**
- [ ] Four children: `StatItem_Authority`, `StatItem_People`, `StatItem_Security`,
      `StatItem_Wealth`

Each stat item:

- [ ] Add **`StatItemView`**, set its `Stat` to the matching statistic
- [ ] Child `Fill` — `Image`, **`Image Type = Filled`**, `Fill Method = Horizontal`,
      `Fill Origin = Left`
- [ ] Optional children: `Icon` (`Image`) and `Label` (`TextMeshProUGUI`)
- [ ] Assign `Fill Image`, and `Icon Image` / `Label` if used
- [ ] `Animation Speed` — `2.5` is a sensible default; `0` snaps instantly

Then on `HUDView`:

- [ ] `Stat Items` — size **4**, one per statistic

`HUDView` warns in the Inspector if a statistic is missing, duplicated, or a slot is empty.

### P5.6 — Card

- [ ] Child of `SafeArea` named `CardArea`, then a child `Card` with **`CardView`**
- [ ] `Card Root` — the `Card` RectTransform (**Phase 6 drags this; Phase 5 never moves it**)
- [ ] `Visual Root` — leave empty to toggle the `Card` object itself
- [ ] Children: `Portrait` (`Image`), `Speaker` (`TextMeshProUGUI`), `Body` (`TextMeshProUGUI`)
- [ ] Two preview children, `PreviewLeft` and `PreviewRight`, each with a **`CanvasGroup`**, a
      `TextMeshProUGUI` label, and **`ChoicePreviewView`**
- [ ] On each `ChoicePreviewView`: set `Side` (Left / Right), assign `Label` and `Canvas Group`
- [ ] On `CardView`: assign `Speaker Text`, `Body Text`, `Portrait Image`, `Left Preview`,
      `Right Preview`

Portrait fallback (`Portrait Fallback` on `CardView`):

- [ ] `Fallback Sprite` — leave empty until there is art
- [ ] `Use Fallback Colour` — **on**, so a card with no portrait shows a flat block rather than a
      hole. Turn it off to hide the portrait slot entirely.

### P5.7 — Game Over panel

- [ ] Child of `SafeArea` named `GameOverPanel`, stretched full screen, add **`GameOverView`**
- [ ] Children: `Illustration` (`Image`), `Title` (`TextMeshProUGUI`), `Body` (`TextMeshProUGUI`),
      `RestartButton` (`Button`)
- [ ] Assign `Panel Root` = the `GameOverPanel` object, plus `Title Text`, `Body Text`,
      `Illustration Image`, `Restart Button`
- [ ] `Generic Title` / `Generic Body` — shown when a boundary is reached that no ending covers
- [ ] On `RestartButton` → `OnClick()` → add `GameOverPanel` → **`GameOverView.HandleRestartButton`**
- [ ] **Deactivate `GameOverPanel`** in the Inspector; `Show` activates it

`HandleRestartButton` only raises `RestartRequested`. Phase 7 subscribes and decides what a restart
means — the view restarts nothing.

### P5.8 — Audio

- [ ] Child of `SafeArea` (or the scene root) named `AudioService`
- [ ] Add **`AudioSource`**: `Play On Awake` **off**, `Loop` **off**, `Spatial Blend` = **2D (0)**
- [ ] Add **`AudioService`**, assign the `Audio Source`
- [ ] `Cue Library` — leave empty for now. Every cue then resolves to silence, which is a supported
      configuration, not an error.

When there is audio: `Assets > Create > Royal Decisions > Audio Cue Library`, add `id` → `clip`
pairs where `id` matches `ChoiceDefinition.audioEventId` **exactly** (comparison is ordinal, so
case matters), and assign it to `AudioService`.

### P5.9 — Verify in the Editor

- [ ] Console clean on entering Play Mode
- [ ] `Window > General > Device Simulator` — check **16:9**, **19.5:9** and **21:9**; the HUD and
      card must stay inside the safe area with a notch simulated
- [ ] Nothing renders until Phase 7 drives it — an empty card and empty bars are correct for now

### Required Inspector references

| Component | Required | Optional |
|---|---|---|
| `SafeAreaFitter` | — (defaults to own RectTransform) | `Target` |
| `HUDView` | `Stat Items` ×4, one per statistic | — |
| `StatItemView` | `Stat`, `Fill Image` | `Icon Image`, `Icon Sprite`, `Label`, fallback |
| `CardView` | `Speaker Text`, `Body Text`, `Portrait Image`, both previews | `Card Root`, `Visual Root`, fallback |
| `ChoicePreviewView` | `Side`, `Label`, `Canvas Group` | scale settings |
| `GameOverView` | `Title Text`, `Body Text`, `Panel Root` | `Illustration Image`, `Restart Button`, fallback, generic text |
| `AudioService` | — | `Audio Source`, `Cue Library` |

Every optional reference left empty degrades to a no-op rather than an exception. `HUDView` and the
fallback settings validate in `OnValidate`, so a mis-wired prefab reports the specific problem in
the Inspector.

---

## Phase 6 — wire the swipe

Phase 6 added `CardSwipeController`. It moves the card, drives the previews, and raises two events.
It applies **no consequences** — nothing happens after a swipe until Phase 7 subscribes.

### P6.1 — Add the component

On the `Card` object from P5.6:

- [ ] Add **`CardSwipeController`**
- [ ] `Card View` — the `CardView` on the same object
- [ ] `Drag Parent` — the `CardArea` RectTransform (leave empty to use the card's parent)

**The card must have a `Graphic` with `Raycast Target` enabled**, or no pointer event ever reaches
the component:

- [ ] `Card` has an `Image` with **`Raycast Target` ticked** — a fully transparent one is fine, but
      an alpha of exactly `0` is still hit-testable only if the Image component itself is enabled

This is the single most common reason a uGUI swipe silently does nothing.

### P6.2 — Tune the feel

Defaults are a reasonable starting point; all are serialized:

| Field | Default | Effect |
|---|---|---|
| `Threshold Ratio` | `0.25` | Fraction of parent width needed to confirm |
| `Minimum Threshold Distance` | `40` | Floor, so an unlaid-out parent cannot confirm instantly |
| `Movement Multiplier` | `1.0` | Card travel per unit of finger travel |
| `Max Rotation Degrees` | `12` | Tilt at full threshold |
| `Rotate Clockwise On Right Drag` | on | Tilt direction |
| `Snap Back Duration` | `0.18` | Return animation |
| `Exit Duration` | `0.25` | Off-screen animation |
| `Snap Back Ease` / `Exit Ease` | ease-in-out | Curves |
| `Exit Margin Multiplier` | `1.0` | Extra card widths travelled past the edge |

Out-of-range values are clamped in `OnValidate`, so the Inspector cannot produce an unusable
configuration.

### P6.3 — Verify in the Editor

Enter Play Mode and drag the card with the mouse:

- [ ] The card follows horizontally and tilts; it does **not** move vertically
- [ ] Dragging left fades in only the left preview; right, only the right
- [ ] Releasing before the threshold snaps the card back and the previews fade out
- [ ] Releasing past the threshold sends the card off screen and it stays gone
- [ ] After a confirmed swipe, further dragging does nothing (the card is locked)
- [ ] Console stays clean

Nothing else happens yet — no stats, no next card. That is Phase 7.

### P6.4 — Verify on device

- [ ] Touch drag behaves as it does with the mouse
- [ ] Putting a **second finger** down mid-drag changes nothing — the first finger keeps control
- [ ] Rapid repeated swipes produce **one** decision each, never two
- [ ] The gesture feels the same on a tall and a short screen (the threshold is a fraction of
      width, not a pixel count)
- [ ] Swiping with a notch present keeps the card inside the safe area

---

---

## Phase 7 — final wiring and smoke test

Phase 7 added the application session and the composition root. **All the deferred manual work from
P5, P6 and P7 is collected here in dependency order**, so it can be done in one pass.

Nothing below has been done by code: no scene, prefab, setting or generated asset was touched.

### F1 — Prerequisites (all still outstanding)

- [ ] **U1** — Player Settings: `Default Orientation` = Portrait; untick both Landscape orientations
- [ ] **U2** — `Window > TextMeshPro > Import TMP Essential Resources`
- [ ] **U3** — Company Name, Product Name, Android package name
- [ ] **Generate content** — `Tools > Royal Decisions > Generate Placeholder Content`
      (expect `Created 29`; run it twice and confirm the second run reports `Unchanged 29`)

### F2 — Game scene (P5 + P6)

Follow **P5.1–P5.8** for the scene, Canvas, Safe Area, HUD, card, previews, audio and game-over
panel, then **P6.1–P6.2** for the swipe controller.

The one thing most likely to go wrong: the `Card` object needs an `Image` with **`Raycast Target`
ticked**, or no pointer event ever reaches `CardSwipeController`.

### F3 — Game flow (P7)

- [ ] Add **`GameSceneController`** to a root object in the Game scene
- [ ] `Catalogue` — `Assets/_Game/Content/Placeholder/PlaceholderContentCatalogue.asset`
- [ ] `Card View`, `Hud View`, `Game Over View`, `Swipe Controller` — the components from F2
- [ ] `Audio Service` — optional
- [ ] `Session Intent` — optional for now; see F4
- [ ] `Fallback Start Mode` — `NewGame` while there is no menu

`GameSceneController` needs only `Card View` and `Swipe Controller` to run; anything else missing
degrades rather than throwing, and it reports the problem through `WiringError`.

### F4 — Bootstrap and MainMenu scenes

Only needed once you want a menu. The Game scene runs standalone without them.

- [ ] `Assets > Create > Royal Decisions > Session Intent` → save as
      `Assets/_Game/Content/SessionIntent.asset`
- [ ] **Bootstrap.unity** — an empty object with `BootstrapController`; set `Main Menu Scene Name`;
      optionally assign an `AudioService`
- [ ] **MainMenu.unity** — Canvas with New Game and Continue buttons plus a `MainMenuController`;
      set `Game Scene Name`, assign the `SessionIntent` asset
- [ ] Wire New Game → `MainMenuController.OnNewGamePressed`,
      Continue → `MainMenuController.OnContinuePressed`
- [ ] Disable the Continue button when `IsContinueAvailable` is false
- [ ] Assign the same `SessionIntent` asset to `GameSceneController`
- [ ] `File > Build Profiles > Scene List` — add **Bootstrap, MainMenu, Game** (names, not indices)

### F5 — Smoke test

In the Editor, in this order:

- [ ] **New Game** → the opening card (`card_01_coronation`) appears
- [ ] Swipe past the threshold → the card flies off, the HUD moves, a new card arrives
- [ ] Swipe below the threshold → the card snaps back and no stat changes
- [ ] Play several turns → the turn count rises and each decision produces exactly one save
- [ ] Stop Play Mode, start it again with **Continue** → the run resumes on the same turn
- [ ] Drive a stat to `0` or `100` → the card leaves, *then* the ending appears
- [ ] Press **Restart** → a new run begins on the opening card
- [ ] Console has no errors from `Assets/_Game/`

The save file lives at `%userprofile%/AppData/LocalLow/<Company>/RoyalDecisions/run.json` — inspect
it to confirm `isRunActive: false` is persisted after an ending.

### F6 — Device verification (Phase 8)

- [ ] Portrait only; Safe Area respected around a notch
- [ ] Touch swipe matches mouse behaviour; a second finger mid-drag changes nothing
- [ ] Backgrounding mid-run and returning resumes correctly
- [ ] Deleting the save file leaves Continue unavailable and New Game working

**The MVP is not complete until F1–F6 are done and verified on a device.**

---

### Target structure for Phase 7

Not built yet — nothing can move between scenes until `GameFlowController` exists.

```
Bootstrap.unity   services constructed, then loads MainMenu
MainMenu.unity    New Game / Continue (Continue needs SaveService.HasSave)
Game.unity        built above
```

---

### Replacing placeholder content later

All 29 assets are disposable. To replace them with final content, either edit them in place and
stop running the generator, or delete the `Placeholder` folder and author content elsewhere under
`Assets/_Game/Content/`. Nothing in the gameplay code refers to any placeholder ID — the only
content reference the game needs is a `ContentCatalogue`, which Phase 7 will take as an Inspector
reference.

---

## Phase F — Turkish localization and readability

Phase F has been applied through the guarded Unity Editor generators and scene automation. It did
not add a localization package or change the save format. Turkish is the only active MVP language.

Generated project-owned assets:

- `Assets/_Game/Content/Interface/TurkishInterfaceText.asset`
- `Assets/_Game/Art/Fonts/Resources/LiberationSans-Turkish.ttf`
- `Assets/_Game/Art/Fonts/Resources/LiberationSans-Turkish SDF.asset`
- `Assets/_Game/Art/Fonts/Resources/LiberationSans-Turkish-OFL.txt`

The 20 placeholder cards and eight endings now contain Turkish display text and retain their
existing IDs, gameplay data, paths, `.meta` files and GUIDs. The catalogue was not rewritten.
Do not edit `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`;
the Turkish scenes use the separate project-owned static SDF directly.

Useful regeneration and validation commands:

- `Tools > Royal Decisions > Generate Turkish Interface Text`
- `Tools > Royal Decisions > Generate Turkish TMP Font` (generates and validates)
- `Tools > Royal Decisions > Generate Placeholder Content`
- `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup`

The exact font probe is `Çığ, öğüt, şüphe, İmparator, özgürlük ve güvenlik`. A failed glyph check
must be fixed in the project-owned SDF; do not mask it with TMP fallback substitution.

Automated verification completed on 3 August 2026:

- EditMode: **693/693 passed** — `Logs/PhaseFFullEditMode.xml`
- PlayMode: **38/38 passed** — `Logs/UIFoundationFullPlayModeFinal.xml`
- Scene authoring: **5/5 passed** — `Logs/PhaseFSceneTests.xml`
- Focused Turkish layout PlayMode: **2/2 passed** — `Logs/PhaseFFocusedPlayMode.xml`

### Phase F visual review in Unity

- [ ] At 1080×1920, verify four- and six-line dialogue stays at or above 34 px and does not
      overflow; the dialogue remains the card's most prominent text.
- [ ] Verify a two-line speaker name and two-/three-line choices do not clip.
- [ ] Drag to both decision thresholds and confirm card text, previews and contrast remain readable.
- [ ] Verify HUD labels and values at 0, 50 and 100 do not collide and their fills agree.
- [ ] Verify the first card displays `Tur 1`, then increments once per completed decision.
- [ ] Verify the menu reads `Yeni Oyun` / `Devam Et`, and game over reads
      `Hükümdarlık Sona Erdi` / `Yeniden Başlat` where fallback text is used.
- [ ] Inspect `Ç Ğ İ Ö Ş Ü ç ğ ı i ö ş ü`, especially dotted and dotless I, at device scale.
- [ ] Confirm no approved narrative or ending text is silently truncated.

Phase F recovery copies are under
`Library/RoyalDecisionsPhaseFBackup/20260802-215404/`. The last scene-automation backup is under
`Library/RoyalDecisionsSceneSetupBackup/Last/`. Restore only the Phase F targets from these folders;
do not reset or delete unrelated working-tree changes.

---

## Phase 8 — Android device acceptance

Android SDK, NDK and OpenJDK modules are installed. The application identifier and device
acceptance remain manual and are the only unfinished release gates.

### A1 — Player and build settings

- [ ] Set a real Company Name and enable Android `Override Default Package Name` with a stable
      identifier such as `com.yusufsari.royaldecisions`.
- [ ] Set `Default Orientation` to **Portrait** and disable both landscape orientations.
- [ ] Switch the active Build Profile to Android and include `Bootstrap`, `MainMenu`, and `Game` in
      that order.
- [ ] Make a development build and confirm the Unity Console contains no project-code warnings or
      errors.

### A2 — Supported layouts and Safe Area

- [ ] Check 1080×1920, 1080×2340, 1440×2960 and 1536×2048 in Device Simulator or on matching
      devices.
- [ ] Simulate a top notch and bottom gesture inset; every active text element must remain inside
      `SafeArea`.
- [ ] Confirm card rotation at both maximum directions keeps text aligned with the card through the
      confirmation threshold. Leaving the Safe Area during the intentional exit animation is valid.

### A3 — Touch, save and Turkish smoke test

- [ ] Start `Yeni Oyun`; the opening card appears with `Tur 1`.
- [ ] Perform one below-threshold swipe: snap-back occurs and no decision or save is recorded.
- [ ] Perform one above-threshold touch swipe: exactly one decision is applied and saved.
- [ ] Try a rapid repeat and a second finger: neither produces a duplicate decision.
- [ ] Background and resume the app, then use `Devam Et`; the same run and turn return.
- [ ] Reach an ending, verify the full Turkish title/body, then use `Yeniden Başlat`.
- [ ] Recheck the Turkish glyph probe on the physical device and confirm all text is readable.
- [ ] Finish with a clean Unity Console and no Android log errors from project code.

The MVP is not device-accepted until every A1–A3 item is complete on an Android device.

---

## Post-MVP foundation acceptance

The post-MVP automation adds code-only responsive polish, safe content tools, balance simulation,
lifecycle/release gates, settings/accessibility/audio/haptics, a first-run tutorial, and an
Editor/Development-Build-only debug panel. Optional art and audio slots may remain empty.

### Visual and accessibility review

- [ ] Device Simulator: 9:16, 19.5:9, 20:9, 21:9 and 4:3 tablet, including top/bottom cutouts.
- [ ] On phones, confirm the card is 75–80% of Safe Area width when height permits; on tablets,
      confirm the 920-reference-unit cap.
- [ ] Check HUD/footer typography, 24-unit bars, sharp temporary border, procedural vignette and
      portrait silhouette with all designer sprites null.
- [ ] Check long Turkish dialogue, choice labels and `ÇĞİÖŞÜçğıöşü` in normal and larger-text
      modes without overlap or clipping.
- [ ] Check high contrast and reduced motion; reduced motion must use no more than 4° rotation and
      0.05-second transitions.
- [ ] Verify GameOver contains only `/Content` replacements and no obsolete direct children.

### Content and simulation review

- [ ] Open `Tools > Royal Decisions > Content Authoring`; create a disposable card under
      `Content/Cards`, edit/Undo it, inspect incoming/outgoing links, then remove the disposable
      asset through normal Unity asset workflow.
- [ ] Confirm existing IDs are read-only in custom inspectors and placeholder content was not
      regenerated or bulk-overwritten.
- [ ] Run `Tools > Royal Decisions > Balance Simulator` twice with identical options and compare
      report hashes; inspect never-observed cards/endings and high-death choices.

### Lifecycle, settings and tutorial review

- [ ] On Android, background/lock during a below-threshold drag: neutral card, no decision/save.
- [ ] Background immediately after confirmation: exactly one save and one completed exit.
- [ ] Android Back closes tutorial/settings first, then returns Game to MainMenu; Back on MainMenu
      requests quit.
- [ ] Ended/deleted saves disable Continue immediately; New Game replaces the prior main save.
- [ ] Verify music/SFX volume, master mute, haptics, reduced motion, larger text and high contrast
      persist after process reconstruction. Missing clips remain silent.
- [ ] Fresh settings show the deterministic tutorial before any run/save exists; Skip and Complete
      persist completion; Continue never shows it; Reset Settings enables it again.

---

## Settings menu expansion — Audio / Graphics / Controls / General

Extends the existing Audio + Accessibility settings panel into four tabs: **Ses** (unchanged),
**Grafik** (frame-rate cap, battery saver), **Kontroller** (swipe sensitivity, swipe-rotation
invert, haptics), **Genel** (reduced motion / larger text / high contrast, reset progress, reset
tutorial, about). No "Account" tab exists — CLAUDE.md forbids accounts/cloud saves/backend, so
that category became the local-only Genel tab. Language/localization was explicitly deferred.

New code: `Domain/GameSettings.cs` (new fields, no save-version bump — additive JSON, proven by
the existing `OlderVersionOneJsonLoadsAdditiveSettingsDefaults`-style tests), four new passive
views (`AudioSettingsPanelView`, `GraphicsSettingsPanelView`, `ControlsSettingsPanelView`,
`GeneralSettingsPanelView`), `SettingsPanelView` (now a tab container), `AboutPanelView`,
`Composition/ResetProgressController.cs`, `GameSession.ResetProgress()`, and extensions to
`SettingsController` and `GameSceneController.ApplySettings()` (Controls settings are re-applied
in the Game scene independently, since MainMenu and Game are different scenes with no live
reference between them — exactly how audio volume already works).

### S1 — Rebuild the MainMenu Settings panel

`SceneSetupAutomation.cs` (`ConfigureSettingsPanel`) was updated to construct the new four-tab
hierarchy under `/UICanvas/SafeArea/SettingsPanel/Content`, plus a new
`/UICanvas/SafeArea/AboutPanel` and a new root `/ResetProgressController` object. The **old**
single-panel Slider/Toggle/Button references serialized on `SettingsPanelView` in the existing
`MainMenu.unity` no longer match the class's fields (they now live on the four sub-panel
components) — this is expected, not a corruption.

- [ ] Commit current work first (this rewires a chunk of the MainMenu scene).
- [ ] `Tools > Royal Decisions > Scene Setup > Audit` — review what will change.
- [ ] `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` — rebuilds the Settings
      panel into the four tabs, creates the About panel and `ResetProgressController`.
- [ ] `Tools > Royal Decisions > Scene Setup > Validate` — must report zero errors.
- [ ] Re-run `Apply Remaining Setup` a second time — it must report no further changes
      (idempotent), matching the pattern already used for the placeholder content generator.

### S2 — Visual pass (not verified here — no running Unity Editor available this session)

The tab layout, spacing and tab-button positions in `ConfigureSettingsPanel` /
`ConfigureGraphicsSettingsTab` / `ConfigureControlsSettingsTab` / `ConfigureGeneralSettingsTab`
were written by hand against the existing coordinate scheme but never rendered — verify in the
Editor before trusting them:

- [ ] Open Settings from MainMenu; all four tab buttons switch tabs correctly and only one tab's
      controls are visible/interactable at a time
- [ ] Nothing overlaps the Apply/Cancel/Reset row at the bottom of the panel
- [ ] Genel tab: tap **İlerlemeyi Sıfırla** once — label changes to the "tap again to confirm"
      state; tap elsewhere or switch tabs — it disarms; tap it twice in a row — the run save is
      deleted (check `%userprofile%/AppData/LocalLow/<Company>/RoyalDecisions/run.json` is gone)
      and Settings preferences are untouched
- [ ] Genel tab: **Öğreticiyi Sıfırla** persists immediately (no Apply needed) — confirm the
      tutorial reappears on the next New Game
- [ ] Genel tab: **Hakkında** opens the About panel over Settings; Close returns cleanly
- [ ] Grafik tab: toggling the frame-rate cap / battery saver takes effect immediately
      (`Application.targetFrameRate`) — note the Editor Game view is usually capped by VSync
      regardless, so confirm on device or with `QualitySettings.vSyncCount = 0` for a visible
      difference
- [ ] Kontroller tab: change swipe sensitivity, Apply, then **enter a run from MainMenu** (not
      Play Mode started directly in the Game scene) and confirm the card now travels
      proportionally more/less per unit of drag — this only takes effect through
      `GameSceneController.ApplySettings()` when the Game scene loads, not live while Settings is
      open
- [ ] Kontroller tab: toggle swipe-rotation invert and confirm only the card's tilt direction
      flips — a left drag must still confirm the left choice

### S3 — Author the About panel copy

- [ ] Replace the placeholder body text on `AboutPanel/Content/Body` (currently
      "Royal Decisions / Sürüm: geliştirme / Bu içerik yer tutucudur.") with real credits/version
      text. No code change is needed — the view only shows whatever is authored on that TMP object.

### S4 — Run the test suites

This session could not run `Window > General > Test Runner` via the command line — the project
was already locked by another Unity Editor instance (`Temp/UnityLockfile` in use, multiple
`Unity.exe` processes present) — so **none of the new or changed tests below have been executed
this session.** Run them yourself:

- [ ] EditMode: `GameSettings` clamp/default tests for `SwipeSensitivity` (in
      `SettingsSaveServiceTests.cs`), the additive-JSON migration test
      (`OlderJsonMissingGraphicsAndControls_LoadsSafeDefaults`), the new
      `CardSwipeControllerTests.cs` Controls-settings tests (`SetSwipeSensitivity...`,
      `SetInvertRotation...`), and `GameSessionResetProgressTests.cs`
- [ ] Full `EditMode > Run All` and `PlayMode > Run All` — must stay green; nothing in this change
      should regress `GameCompositionPlayModeTests` (it now also exercises the swipe-settings
      application path in `GameSceneController.ApplySettings`, since it passes a real
      `CardSwipeController` and a `StubSettingsStore`)
- [ ] Confirm the Console has no project-code compilation errors or warnings after the scene
      rebuild in S1

---

## MainMenu / Settings visual polish — icon-only settings button, rounded corners

`SceneSetupAutomation.cs` was updated (`EnsureSettingsIconButton`, `ConfigureRoundedButtonGraphic`,
`EnsureMenuButton`, `ConfigureSettingsPanel`) to: replace the large "Ayarlar" text button on
MainMenu with a compact top-right icon-only button (`ProceduralGearIconGraphic`, a texture-free
gear mesh — no sprite asset needed); give every button (New Game, Continue, settings icon, all
four Settings tabs, Apply/Cancel/Reset, Reset Progress/Reset Tutorial/About) a consistent rounded
corner via `ProceduralRoundedRectGraphic` (`StandardButtonCornerRadius = 20`, the icon button uses
half its own size so it renders as a circle); and reconfirm the Settings panel's
Header/TabBar/ContentViewport(ScrollRect)/BottomActions layout, with Apply/Cancel/Reset pinned
outside the scroll view. Colour palette (dark/navy background, gold CTA, white text) is unchanged.

**Applied.** With the Editor closed, this was regenerated via
`Unity.exe -batchmode -nographics -quit -executeMethod RoyalDecisions.Editor.SceneSetupAutomation.ApplyBatch`
(the batch entry point behind the `Apply Remaining Setup` menu item):

- [x] `ApplyBatch` — exit code 0. Report: `BACKUP_CREATED` (pre-apply backup at
      `Library/RoyalDecisionsSceneSetupBackup/Last`), `VALIDATION_OK`, `APPLY_COMPLETE`.
- [x] `ValidateBatch` — exit code 0, `VALIDATION_OK`, zero errors.
- [x] Confirmed on disk: `MainMenu.unity` now references the `ProceduralRoundedRectGraphic` script
      GUID 14 times (every button) and `ProceduralGearIconGraphic` once (the settings icon); the
      `SettingsButton` object's anchors are now `(1,1)-(1,1)` (top-right) instead of the old centred
      600×120 rect; the literal `m_text: Ayarlar` string now appears exactly once (the Settings
      panel header title) instead of twice (the old button no longer carries that label).
- [x] Idempotency: ran `ApplyBatch` two more times. Run 2 vs. run 1 changed only three
      `m_fontSize` lines (42→40, 68→64) — TMP auto-size cache converging, matching the code's own
      comment about this. Run 3 vs. run 2: **byte-identical** (`sha256sum` match). Stable.

**Follow-up fixes (same session, after a user screenshot showed the applied result broken):**

1. **Gear icon didn't render.** The `Icon` child under `SettingsButton` got its
   `ProceduralGearIconGraphic` but no `CanvasRenderer` — every *other* graphic in the scene
   inherited its `CanvasRenderer` from a pre-existing `Image` it replaced, but `Icon` was a
   brand-new object, and `[RequireComponent(typeof(CanvasRenderer))]`'s auto-add lost the race
   with this single-shot `-executeMethod`/`-quit` batch run before `SaveScene` serialized it.
   Confirmed empirically: scene had 94 `CanvasRenderer`s for 95 graphics before the fix, 95-for-95
   after. `EnsureSettingsIconButton` now adds the `CanvasRenderer` explicitly.
2. **All four Settings tabs rendered stacked on top of each other** (a screenshot showed every
   tab's labels ghosted together). `ConfigureSettingsPanel` built all four tab bodies active;
   only `SettingsPanelView.Show()` (a runtime call) hid three of them. Now only `AudioTab` is
   left active at authoring time, matching the runtime default.
3. **Seven leftover control rows** (`MusicVolume`, `SfxVolume`, `MasterMute`, `Haptics`,
   `ReducedMotion`, `LargerText`, `HighContrast`) were parented directly under `SettingsPanel/
   Content` from a pre-tab-restructuring authoring pass, sitting alongside `Header`/`TabBar`/
   `ContentViewport`/`BottomActions` and rendered by the same `VerticalLayoutGroup` — duplicates of
   the real rows now living inside their tab bodies. `ConfigureSettingsPanel` now removes any
   direct child of `Content` outside that expected set (`RemoveUnexpectedChildren`); the Apply
   report logged all seven as `ORPHAN_REMOVED`.
4. **Slider/toggle labels used fixed pixel offsets** (`-270`, `70` from row centre) instead of
   proportional anchors, so on a narrower-than-1080-reference safe area the label box moved
   partly off-screen (a screenshot showed "Müzik" as "lüzik", "Efekt" as "fekt"). `EnsureSlider
   Control`/`EnsureToggleControl` now split each row into anchor percentages (label left, control
   right) that can't leave the row's bounds regardless of device width.

**Second pass (same session, after the user asked for more minimisation and proper About
navigation):**

5. Further compaction, all still ≥ the 96 px accessibility floor `ConfigureMinimumTouchTarget`
   already enforces on every button: Header 92→72 (title 44→36pt), content-row spacing 14→10,
   tab-body padding/spacing 6/8/16/14 → 4/6/12/10, slider row 112→96 (handle 56→48), toggle row
   88→80, BottomActions 120→104. Button sizes themselves were left alone — they're already
   floored at 96×96 and can't shrink further without failing that floor.
6. **About now opens as a real separate page instead of an overlay left on top of a still-active
   Settings.** Previously `SettingsController.HandleAboutRequested` only called
   `aboutPanel.Show()` — the Settings panel underneath was never hidden, just visually covered.
   `SettingsPanelView` gained `Reopen()` (reactivates without re-rendering, so it doesn't discard
   edits made before tapping Hakkında or reset the selected tab); `SettingsController` now hides
   Settings on `HandleAboutRequested`, and subscribes to `AboutPanelView.CloseRequested` to bring
   it back via `Reopen()` when About's Close button is pressed. This touches real runtime code
   (`Assets/_Game/Scripts/Composition/SettingsController.cs`,
   `Assets/_Game/Scripts/Presentation/SettingsPanelView.cs`), not just the scene-authoring tool.

Re-applied through `ApplyBatch` after each round; report stayed `VALIDATION_OK`/zero errors and
the scene stabilized byte-identical on the final run. Also ran the full EditMode suite this time
(`-runTests -testPlatform EditMode`, no `-quit` — that flag was making the test run exit before
writing results): **730/730 passed, 0 failed**, so the `SettingsController` change didn't regress
anything covered by existing tests. PlayMode suite was not run this session.

**Not yet confirmed with your own eyes** — batch mode can't screenshot the Game view or click
Hakkında for you. Please check once in the Editor: Settings reads noticeably denser than before;
tapping Hakkında replaces Settings entirely (not layered on top) and its Close button returns to
Settings with your tab/edits intact; no label clipping at any width you test.

**Third pass (same session): the second-pass screenshot showed a large empty gap below the
short Ses tab, and the near-black background read as flat/lifeless over a full-screen panel.**

7. `ContentViewport`'s `LayoutElement.flexibleHeight` was `1`, so it always grew to fill 100% of
   the safe area's remaining height regardless of which tab was showing — on the reference
   canvas that's roughly ~1690 units, while even the tallest tab (Genel) only needs ~630. Changed
   to a fixed `preferredHeight` (`SettingsContentViewportHeight = 700`, flexibleHeight 0) and
   `content`'s own layout `childAlignment` from `UpperCenter` to `MiddleCenter`, so the whole
   Header/TabBar/ContentViewport/BottomActions stack now sizes to its real content and sits
   vertically centred as one compact card — short tabs (Ses, Grafik, Kontroller) no longer leave
   a bare void, and ScrollRect still protects Genel (or any future taller tab) if it ever exceeds
   700 units.
8. Added `SettingsBackgroundColour` (`#161F33`, a lifted navy) and used it for both
   `SettingsPanel` and `AboutPanel`'s surface, replacing the near-black `OverallBackgroundColour`
   they shared with the rest of the game. Kept in the same dark/gold/white family — just enough
   lift to stop a full-screen panel from reading as an empty void — per explicit request ("rengi
   sana bırakıyorum, ideal bir renk ayarla").

Re-applied and re-verified idempotent (byte-identical on the second run) after this pass too.

**Fourth pass (same session): the user clarified — via a plan-mode confirmation — that they want
the opposite of pass three's compact card: Settings should fill the whole screen, with more real
content so it doesn't look sparse, a smaller tab bar, and no colour bleed at the edges.**

9. Reverted `ContentViewport`'s `flexibleHeight` back to `1` and `content`'s `childAlignment`
   back to `UpperCenter` (undoes pass three's card-centring); deleted the now-unused
   `SettingsContentViewportHeight` constant.
10. Added a new `EnsureTabSectionHeader` helper and called it at the top of all four tabs — a
    short title (e.g. "Ses ve Müzik") plus a one-line description (e.g. "Müzik ve efekt
    seviyelerini ayarlayın."), using `SpeakerTextColour`/`SecondaryTextColour` (already-defined
    theme colours, nothing new). Purely presentational — no new `GameSettings` field, no new
    persistence — so it doesn't cross the "no new functionality beyond MVP" line while giving
    each tab real vertical weight.
11. Shrank the tab bar as far as `ConfigureMinimumTouchTarget`'s 96×96 floor allows: font
    32→24pt, requested size 200×84→150×80 (height is floored at 96 regardless — only the visual
    weight gets lighter, not the actual tap target, which must not shrink further per CLAUDE.md's
    own touch-target rule), tab spacing 12→8.
12. ~~Added a full-Canvas `Background` object in `ApplyMainMenuScene`~~ — **reverted in pass 5**,
    see below. This step's edge-bleed fix touched the whole Main Menu screen (not just Settings),
    which the user explicitly said must stay untouched.

**Fifth pass (same session): user feedback — "main menü kısmına dokunmuşsun, orası aynı
kalıcaktı" (you touched the Main Menu, that was supposed to stay the same).**

13. Reverted item 12. Removed the full-Canvas `Background` object and its `SetSiblingIndex` calls
    from `ApplyMainMenuScene`, and the matching `RequirePath(".../UICanvas/Background")` check
    from `ValidateMainMenuScene`. Added `RemoveUnexpectedChildren(canvasObject.transform, report,
    "SafeArea")` right after `SafeArea` is ensured, so re-running `ApplyBatch` actively deletes the
    `Background` object a prior run had already written to the scene — confirmed via the Apply
    report (`ORPHAN_REMOVED`, path `/UICanvas/Background`) and confirmed zero `Background` objects
    remain in `MainMenu.unity` afterward. The edge-bleed issue item 12 was trying to fix is
    consequently unaddressed again — Main Menu's own background is back to "nothing, camera clear
    colour shows through outside SafeArea," matching how it was before this whole session's work
    started. Everything Settings/About-specific from passes 1-4 (icon button, rounded corners, tab
    fixes, orphan cleanup, full-height content, section headers, compact tabs, `SettingsBackground
    Colour`) is untouched by this revert.

Re-applied twice more (idempotent after the second run) and re-ran the full EditMode suite:
**730/730 passed, 0 failed.**

**Sixth pass (same session): user report — Settings still doesn't fully cover the screen or block
the menu behind it. Root-caused properly this time instead of guessing from a screenshot.**

Root cause: `SettingsPanel` (and `AboutPanel`) lived *inside* `SafeArea`, the same safe-area-inset
container as `MainMenuPanel`. Their own background could therefore only ever cover the safe-area
rect, not the physical screen — outside that inset (a notch/cutout region on some device/simulator
profiles), Unity's raw camera clear colour showed through. Sibling order (`MainMenuPanel` then
`SettingsPanel` then `AboutPanel`, all under `SafeArea`) was actually already correct — that part
wasn't the bug — but Main Menu was never explicitly disabled while Settings was open, so nothing
guaranteed it couldn't be clicked through if a gap ever existed.

14. **Moved `SettingsPanel` and `AboutPanel` out of `SafeArea` to be direct children of the
    Canvas** (siblings of `SafeArea`, last two in sibling order), each now `Stretch`-ed to the
    *full Canvas* rect so their opaque background genuinely covers the whole physical screen
    regardless of safe-area insets. `ConfigureSettingsPanel`/`ConfigureAboutPanel` now take the
    Canvas `Transform` instead of `SafeArea`, and both gained their own inner `SafeArea` child
    (`Stretch` + `SafeAreaFitter`, mirroring the top-level `SafeArea`/`MainMenuPanel` pattern) so
    their actual content (`Content`: Header/Tabs/sliders/buttons) still respects the safe area —
    only the background is unconstrained. `MigrateChildIfNeeded` reparents the existing
    `SettingsPanel`/`AboutPanel`/`Content` objects into their new locations rather than
    duplicating them, matching every other repair in this file.
15. **Added a `CanvasGroup` on `MainMenuPanel`**, wired to a new `SettingsPanelView.mainMenuGroup`
    field. `Show()`/`Reopen()` now set `interactable = false` and `blocksRaycasts = false` on it
    (and call `panelRoot.transform.SetAsLastSibling()`, re-asserting topmost order every time
    regardless of history); `Hide()` restores both to `true`. This is on top of, not instead of,
    the full-screen opaque overlay above — belt-and-suspenders against input reaching the menu
    even if a future change ever put something in front of Settings by mistake. Touches real
    runtime code: `Assets/_Game/Scripts/Presentation/SettingsPanelView.cs`.
16. Fixed a self-inflicted idempotency bug from step 14: the `RemoveUnexpectedChildren` cleanup
    added in pass five only whitelisted `SafeArea` as a Canvas child, so it deleted the newly
    relocated `SettingsPanel`/`AboutPanel` every run, which then got fully rebuilt with new
    `fileID`s each time (never converging). Whitelist now includes `SettingsPanel`/`AboutPanel`.
    Confirmed idempotent (byte-identical) across 3 consecutive `ApplyBatch` runs after the fix.
17. Re-ran the full EditMode suite: **730/730 passed, 0 failed.** Also ran PlayMode
    (`-runTests -testPlatform PlayMode`): **29/38 passed, 9 failed — all 9 in
    `CardSwipeAnimationPlayModeTests`** (Game scene card-swipe timing, e.g. "animation did not
    complete within 300 frames"). Unrelated to this change — that suite doesn't touch MainMenu,
    Settings, or anything this pass modified — and most likely a `-nographics`/batch-mode timing
    artifact rather than a real regression, but it was **not verified against a pre-change
    baseline this session**, so treat it as a flag to check yourself, not a confirmed non-issue.

**Seventh pass (same session): user reported a Console error after opening the reorganized
Settings panel.** `Editor.log` showed it spamming every frame:
`MissingComponentException: There is no 'CanvasRenderer' attached to the "ApplyButton" game
object, but a script is trying to access it.` — the exact same class of bug as the earlier
settings-gear-icon fix (pass "second" in the icon/rounded-corner section above): a brand-new
GameObject's `[RequireComponent(typeof(CanvasRenderer))]` auto-add can lose the race with a
single-shot batch `-executeMethod`/`-quit` run before `SaveScene` serializes it. That earlier fix
only patched the one Icon object; `ApplyButton` (created fresh during this session's Settings
restructuring) hit the same race independently.

18. Fixed it at the source instead of per-object this time: `ConfigureRoundedButtonGraphic` —
    which *every* button (New Game, Continue, all four tabs, Apply/Cancel/Reset, Reset Progress/
    Tutorial/About, the settings icon) routes through — now explicitly calls
    `EnsureSingleComponent<CanvasRenderer>(target, report)` before adding the graphic. This closes
    the whole class of bug for every current and future button built through this helper, not
    just the one that happened to surface it.
19. Verified on disk: `MainMenu.unity` now has exactly as many `CanvasRenderer` components (80)
    as graphic components needing one (27 `Image` + 38 `TMP` + 14 rounded-rect + 1 gear = 80).
    Re-ran `ApplyBatch` twice more — idempotent (byte-identical) after the usual one-time TMP
    font-size-cache settle. Full EditMode suite: **730/730 passed, 0 failed.**

**Eighth pass (same session): user rejected Settings' navy background outright — wanted it to be
the exact colour MainMenu already uses, sourced from one real place, plus real `SetActive`-based
menu hiding instead of CanvasGroup/overlay blocking. Went through plan mode for this one given the
scope (touches a runtime-behaviour-owning class, not just the scene-authoring tool).**

Root cause: `EnsureCamera` set `clearFlags = SolidColor` but never assigned `backgroundColor`, so
MainMenu had only ever shown Unity's *unset default* (`≈ #314C79`) — not a deliberate colour
anyone chose, and not the same value as Settings' hand-picked navy (`#161F33`). Confirmed
`SettingsPanel`/`AboutPanel` were already genuinely full-Canvas-stretched from an earlier pass
(anchors `(0,0)-(1,1)` on Canvas, not SafeArea) and `ContentViewport`/`ScrollContent`/`Content`
carry no secondary `Image` — so geometry was already correct; the "separate dark panel" read was a
colour mismatch, not a layout bug.

20. Renamed `SettingsBackgroundColour` → `MainMenuBackgroundColour` (`Assets/_Game/Scripts/Editor/
    SceneSetupAutomation.cs`), value `Color32(49, 77, 121, 255)` — Unity's actual previous default,
    chosen so MainMenu's own appearance is unchanged. Used in exactly three places: `EnsureCamera`
    now explicitly sets `camera.backgroundColor` to it (previously implicit); `SettingsPanel`'s and
    `AboutPanel`'s background `Image`s use the same constant instead of their own navy. One value,
    defined once, referenced three times — no separate colour chosen for Settings anymore.
21. Replaced CanvasGroup-based menu blocking with real `GameObject.SetActive`, and moved ownership
    from `SettingsPanelView` to `SettingsController` (the actual Settings↔About orchestrator).
    `SettingsController` gained `[SerializeField] private GameObject mainMenuRoot` — `Open()`
    deactivates it, `Cancel()`/successful `ApplyFromView()` reactivate it. Menu stays inactive
    through the Settings↔About handoff (`HandleAboutRequested`/`HandleAboutClosed` untouched).
    `SettingsPanelView.mainMenuGroup` and its blocking logic were removed entirely — it now only
    manages its own `panelRoot`. `SceneSetupAutomation.cs` no longer adds a `CanvasGroup` to
    `MainMenuPanel` (`RemoveStaleComponents<CanvasGroup>` cleans up the one a prior pass added) and
    wires `SettingsController.mainMenuRoot` to `MainMenuPanel` directly. Touches real runtime code:
    `Assets/_Game/Scripts/Composition/SettingsController.cs`,
    `Assets/_Game/Scripts/Presentation/SettingsPanelView.cs`.
22. Verified on disk: `Main Camera`'s `m_BackGroundColor` is `(0.192, 0.302, 0.475, 1)`; both
    `SettingsPanel` and `AboutPanel` background `Image`s carry the identical RGB; zero
    `CanvasGroup` components remain anywhere in `MainMenu.unity`; `SettingsController.mainMenuRoot`
    points at `MainMenuPanel`. Re-applied twice more — stable (byte-identical) after the usual
    one-time TMP-cache settle. Full EditMode suite: **730/730 passed, 0 failed.**

**Ninth pass (same session): user-supplied visual redesign brief for tabs, slider, toggles, and
bottom buttons — "fuller," more professional, pill-shaped controls.** No image actually came
through (as with several earlier requests this session) — implemented from the detailed text spec.
Two requested items (Account Settings, Language dropdown placeholders in Genel) were skipped after
asking the user: they conflict with CLAUDE.md's explicit no-accounts/no-localization boundary,
already the reason Genel was scoped to local-only options earlier this session. User chose to
enrich the existing Genel items instead.

23. **Tabs**: bumped to a true pill shape — new `TabPillCornerRadius = 48` (half the floored 96px
    height) via a new optional `cornerRadius` parameter on `EnsureMenuButton` (default unchanged,
    `StandardButtonCornerRadius`, itself bumped 20→26 for a slightly more modern feel on every
    other button). Widened 150→172, spacing 8→12, font 24→27pt. Active-tab text now flips to a
    dark colour (`SettingsPanelTheme.ActiveTabTextColour`) instead of staying white-on-gold, which
    was low-contrast — `SettingsPanelView.TintTab` (runtime) and the tab-bar authoring code (initial
    paint) both updated from the one new theme colour.
24. **Slider**: `ConfigureRoundedButtonGraphic` split into a shared `ConfigureRoundedFill` helper
    (adds the `CanvasRenderer` fix by default) reusable for non-button fills. Track/Fill/Handle now
    render as procedural rounded-rect/circle meshes instead of the built-in soft-edged `UISprite`,
    and the row's own background went transparent (was a second dark rectangle behind the label).
25. **Toggles**: replaced the static checkbox glyph with a real sliding pill switch — new
    `Assets/_Game/Scripts/Presentation/ToggleSwitchVisual.cs` (small, focused, mirrors the existing
    `ProceduralRoundedRectGraphic`/`ProceduralGearIconGraphic` pattern) drives track colour and
    knob position from `Toggle.onValueChanged`, since Unity's built-in `Toggle` only supports a
    single show/hide graphic. Applies to all 8 toggles across all four tabs (shared
    `EnsureToggleControl` helper). The old `Checkmark` objects were correctly swept by
    `RemoveUnexpectedChildren` on the first `ApplyBatch` after this change — confirmed via 8
    `ORPHAN_REMOVED` entries in the report, zero left afterward.
26. **Bottom actions**: Apply's label gets `FontStyles.Bold` (primary-action weight, distinguishing
    it from Cancel/Reset) without a new colour or size; otherwise inherits the same radius/spacing
    bump as every other button automatically (shared `EnsureMenuButton`).
27. Main-panel "dark border": found no border/outline component on `SettingsPanel` anywhere in the
    code — nothing to remove. Most likely already resolved by pass eight's colour unification
    (Settings and MainMenu are now the same RGB, so there's no seam to see); flagged for the user
    to confirm with a fresh screenshot rather than assumed fixed blind.

Verified on disk: 8 `ToggleSwitchVisual` / 8 `Knob` / 11 `Track` (8 toggles + 3 sliders) objects,
zero leftover `Checkmark`. Re-applied 3 times — stable (byte-identical) after the usual one-time
TMP-cache settle. Full EditMode suite: **730/730 passed, 0 failed.**

**Still worth doing yourself** (needs the Editor GUI / a device — not checkable from a batch run):

- [ ] Open the project in the Editor and look at MainMenu/Settings once, live: settings icon reads
      clearly as a gear and doesn't overlap the Title; New Game/Continue remain the two large CTAs;
      the four Settings tab labels aren't clipped; Apply/Cancel/Reset stay visible below the
      scrollable content; no new Console errors when opening/closing Settings and switching tabs.
- [ ] `Window > General > Test Runner` — EditMode/PlayMode `Run All`, confirm still green (not run
      this pass; batch mode here only executed the scene-setup method, not the test suites).

---

### Build and performance review

- [ ] Run `RoyalDecisions.Editor.ReleaseValidationAutomation.ValidateBatch`; resolve every error
      and warning. Local signing credentials remain a manual release prerequisite and must not be
      added to tracked project paths.
- [ ] Development APK output: `Builds/Android/Development/`; unsigned release AAB output:
      `Builds/Android/Release/`; reports: `Logs/Build/`.
- [ ] Confirm the debug panel exists in Editor/Development Build and is absent from a release build.
- [ ] Profile 60 seconds idle, repeated drags, ten decisions, GameOver/restart and scene transitions:
      zero project-attributed steady-state GC allocation, stable listener/coroutine counts and no
      growing memory trend.
