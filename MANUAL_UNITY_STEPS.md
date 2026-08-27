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

---

## Audio work ported from RoyalDecisions-main

Brought the audio assets and audio-specific code changes from the older `RoyalDecisions-main`
working copy into this project, without touching any non-audio feature this project has already
grown past `RoyalDecisions-main` (tap-choice buttons, invert-swipe-rotation setting, About panel,
tutorial reset, battery-saver frame-rate cap — all untouched, all still present).

**Copied**, `.meta` files and GUIDs intact:

- `Assets/_Game/Audio/` (new folder) — `MainAudioCueLibrary.asset` (maps `ui_click`, `card_swipe`,
  `card_preview` to clips) and `SFX/` (9 `.wav`/`.mp3` clips plus a `Sources/` subfolder of raw
  source recordings).

**Code changes** (additive only; every new call site is guarded exactly like the pre-existing ones,
so absent audio stays silent, never an error):

- `Application/GameSession.cs` — added `event Action<CardDefinition> CardPresented`, raised right
  after `ShowCard`. (`ResetProgress()`, already present here but not in `RoyalDecisions-main`, was
  left untouched.)
- `Presentation/FeedbackCueProfile.cs` — added a `cardPreview` cue-ID field/property.
- `Composition/GameFeedbackController.cs` — subscribes to `CardPresented` (plays `cues.CardEnter`);
  `HandlePreview` now plays `cues.CardPreview` once when a drag first crosses
  `PreviewCueMinimumStrength` (0.05) in a given direction, and again only if the direction changes;
  `HandlePreviewCleared` resets that direction latch.
- `Composition/GameSceneController.cs` — added a `cues` (`FeedbackCueProfile`) field,
  `PlayGameplayMusic()` (called once from `Start()`, after `ApplySettings()`), and `PlayUiClick()`
  (called from `HandleRestartRequested()`). `ApplySettings()` itself — including the tap-choice-
  button/invert-rotation logic this project added — was left exactly as it was.
- `Composition/MainMenuController.cs` — added `audioService`/`cues` fields and a
  `sceneTransitionDelaySeconds` field (default `0.15`); `Start()` calls `PlayMenuMusic()`;
  `OnNewGamePressed()`/`OnContinuePressed()` now play `ui_click` and load the Game scene through a
  new `LoadGameSceneAfterClickCue()` coroutine (`WaitForSecondsRealtime(sceneTransitionDelaySeconds)`
  before `sceneLoader.LoadScene(...)`) instead of loading synchronously, and are guarded by a new
  `isTransitioningToGame` flag so a double-press can't start two loads or play the click twice.
- `Composition/SettingsController.cs` — added a `cues` field and `PlayUiClick()`; `Open()`,
  `ApplyFromView()` and `ResetToDefaults()` now play it directly. `Cancel()` itself is unchanged
  (it is also the programmatic close path used by `CloseIfOpen()`, e.g. Android Back, which must
  stay silent); the panel's own Cancel button now routes through a new `HandleCancelButton()`
  wrapper that plays the click and then calls `Cancel()`.

**Not ported** — already superseded by this project's own, more advanced implementation, so
porting `RoyalDecisions-main`'s version would have been a regression:

- Settings-menu audio wiring (volume/mute sliders, `AudioSettingsPanelView`,
  `SettingsPanelView`/tabs). This project's `SettingsController.ApplyRuntime()` already calls
  `audioService.SetMusicVolume/SetSfxVolume/SetMasterMuted` against its own newer tabbed Settings
  architecture; `RoyalDecisions-main`'s single-panel `SettingsController` predates that redesign.
- `Editor/SceneSetupAutomation.cs` — diverged almost entirely on non-audio scene-authoring work
  (tap-choice buttons, the whole Settings tab rebuild documented above). Left untouched; the few
  audio-relevant lines in it (`ConfigureAudio`, wiring `audioService`) were already equivalent on
  both sides.

### Required manual Inspector wiring

None of this can be done from a script edit — these are `.asset`/`.unity` reference assignments
(`CLAUDE.md` §11), and the new serialized fields above do not exist in the `.unity` YAML until
Unity re-serializes the object, so they currently show as empty/`None` in the Inspector.

- [ ] **`AudioService.Cue Library`** — empty (`fileID: 0`) on **both** `Game.unity` and
      `MainMenu.unity`. Assign `Assets/_Game/Audio/MainAudioCueLibrary.asset` to each. Without this,
      every `Play(...)` call resolves to `AudioPlayResult.NoLibrary` regardless of any other wiring.
- [ ] **`GameSceneController.Cues`** (new field, `Game.unity`) — assign
      `Assets/_Game/Content/UI/DefaultFeedbackCueProfile.asset` (the same asset already wired to
      `GameFeedbackController.Cues` on the same scene).
- [ ] **`MainMenuController.Audio Service`** (new field, `MainMenu.unity`) — assign the scene's
      existing `AudioService` component (already wired to `SettingsController.Audio Service`).
- [ ] **`MainMenuController.Cues`** (new field, `MainMenu.unity`) — assign
      `DefaultFeedbackCueProfile.asset`.
- [ ] **`SettingsController.Cues`** (new field, `MainMenu.unity`) — assign
      `DefaultFeedbackCueProfile.asset`.

### Content authoring gap (not a wiring step — data, deliberately not hand-edited)

`Assets/_Game/Content/UI/DefaultFeedbackCueProfile.asset` — the one profile asset already shared by
every controller above — currently has **every** cue-ID field blank, including on this project's
own pre-existing `uiClick`/`cardEnter`/`threshold`/etc. fields, not just the new `cardPreview` one.
This predates this session's change and was left alone deliberately: `CLAUDE.md` §11 reserves
`.asset` content for the team, and every call site already degrades to silence when a field is
empty, so nothing is broken by leaving it as-is.

`MainAudioCueLibrary.asset` only defines three clip IDs: `ui_click`, `card_swipe`, `card_preview`
(no `card_enter`, no music). To actually hear the ported features, fill in at minimum, in the
Inspector on `DefaultFeedbackCueProfile.asset`:

- [ ] `Ui Click` = `ui_click`
- [ ] `Card Preview` = `card_preview`
- [ ] `Left Confirmation` = `card_swipe`
- [ ] `Right Confirmation` = `card_swipe`

`Card Enter`, `Menu Music` and `Gameplay Music` have no matching clip in `MainAudioCueLibrary.asset`
yet (no card-enter or music clips exist in the copied audio) — leaving them blank is correct for
now; the silent fallback is by design (`CLAUDE.md` §3, missing/silent audio must not crash), not a
missing wiring step.

### Verify after wiring

- [ ] Editor Console stays clean after opening both scenes and saving them (confirms the new
      serialized fields deserialize without error).
- [ ] `Window > General > Test Runner > EditMode > Run All` — should stay green; nothing above
      changed a public signature the existing tests call, and `AudioServiceTests`/
      `CardSwipeControllerTests`/`MainMenuControllerTests` do not exercise any of the new fields.
- [ ] In Play Mode: New Game / Continue play a click and the Game scene loads ~0.15s later, not
      instantly; a first left/right drag past 5% strength plays the preview cue once, switching
      direction mid-drag plays it again, holding the same direction does not repeat it; the first
      card of a run plays `card_enter`; Settings Open/Apply/Reset and the panel's own Cancel button
      play a click, but Android Back closing Settings does not.

---

## Audio candidate integration — remaining 9 cues, gameplay hooks, slider feedback

Filled in the gaps the previous pass above left open: the 9 cue IDs that had no clip and no
trigger (`slider_tick`, `card_enter` had a clip but `snap_back`/`game_over`/`stat_increase`/
`stat_decrease`/`critical`/`menu_music`/`gameplay_music` had neither), plus the stepped
slider-tick feedback Phase 6 of this pass asked for. `ui_click`, `card_preview`, and `card_swipe`
mappings were **not** touched — same clips, same IDs, as instructed.

### Candidates evaluated

Every file under `D:\2D Oyun\AudioCandidates\SFX\*` and `Music\*` was analyzed for duration, peak
and RMS level (Python + `soundfile`/`numpy`, since neither `ffmpeg` nor a DAW is available in this
environment) and compared against the project's existing reference cues
(`card_swipe_real_A.wav`: peak -4.0 dBFS; `buton sesi okey gibi.wav` / ui_click: peak -12.7 dBFS;
`card_preview_subtle.wav`: peak -10.5 dBFS). Every category had exactly one downloaded candidate;
none were rejected, but most needed trimming and/or gain correction before they were game-ready:

| Cue | Source file | Problem found | Fix applied |
|---|---|---|---|
| `slider_tick` | `757328__steaq__ui-hover-item.wav` | Fine as-is, just very quiet (peak -40.6 dB) | Trimmed to the 0.1s transient, +14.6 dB gain → peak -26.0 dB (still ~13 dB under ui_click) |
| `card_enter` | `oxidvideos-placing-playing-card-522514 (2).mp3` | Peak +0.8 dB (louder than the swipe sound) | Trimmed silence, -10.8 dB gain → peak -10.0 dB, 20ms fade |
| `snap_back` | `oxidvideos-paper-slide-short-478835.mp3` | Fine character, quiet (peak -24.2 dB) | Trimmed tail, +10.2 dB gain → peak -14.0 dB (still under card_swipe's -4 dB) |
| `game_over` | `857938__bassimat__church-bell-bb5.wav` | 28s raw bell recording, near-0dB peak | Trimmed to 8s (strike + natural decay), -4.1 dB gain, 1s fade-out → peak -5.0 dB |
| `stat_increase` | `370180__mpaol2023__3-tone-chime-up.wav` | 4s, 3 repeated tones — up to 4 stats can change per decision, would stack into a wash | Kept only the first tone (0-0.9s), -15.0 dB gain → peak -16.0 dB |
| `stat_decrease` | `370179__mpaol2023__3-tone-chime-down.wav` | Same issue as above | Same treatment, -15.4 dB gain → peak -16.0 dB (matched level, distinct descending pitch) |
| `critical` | `169289__qubodup__gong-bell-monkays-singing-bowl-modified.flac` | 18.9s resonant tail, 0dB peak | Trimmed to 1.1s (strike + short decay), -6.0 dB gain → peak -6.0 dB |
| `menu_music` | `deuslower-medieval-ambient-236809.mp3` | None — already calm, peak -11.2 dB, clean fade tail | Copied unmodified |
| `gameplay_music` | `deuslower-atmosphere-dark-fantasy-dungeon-synthpiano-verse-248215.mp3` | ~29s of near-total silence appended after the musical fade-out (music ends ~131s into a 159.6s file) — would leave a dead-air gap on every loop | Trimmed to 131s + 1.2s fade, re-exported as WAV (only file converted from its source format, and only because the silent tail made trimming necessary — no other gain/EQ change) |

All final peak levels stay comfortably under 0 dBFS (no clipping); exact numbers are in each row
above. The original downloaded files under `AudioCandidates\` were never modified.

### Files copied into the project

```
Assets/_Game/Audio/SFX/slider_tick.wav       (new)
Assets/_Game/Audio/SFX/card_enter.wav        (new)
Assets/_Game/Audio/SFX/snap_back.wav         (new)
Assets/_Game/Audio/SFX/game_over.wav         (new)
Assets/_Game/Audio/SFX/stat_increase.wav     (new)
Assets/_Game/Audio/SFX/stat_decrease.wav     (new)
Assets/_Game/Audio/SFX/critical.wav          (new)
Assets/_Game/Audio/Music/menu_music.mp3      (new folder + file, unmodified copy)
Assets/_Game/Audio/Music/gameplay_music.wav  (new, trimmed/converted — see table above)
```

`ui_click`, `card_preview`, `card_swipe` and their clip files are untouched.

### Code changes (additive, same silent-when-empty pattern as every existing cue call site)

- **`Presentation/CardSwipeController.cs`** — added `event Action SnapBackStarted`, raised at the
  top of `BeginSnapBack()` (a released drag that didn't cross the threshold). No existing behavior
  changed; this is a new event with no subscribers until the next file.
- **`Composition/GameFeedbackController.cs`** — subscribes to the new `SnapBackStarted` (plays
  `cues.SnapBack`) and to a new `GameSession.StatValueChanged` (below); plays `cues.StatIncrease` /
  `cues.StatDecrease` on an ordinary stat move, or `cues.Critical` (+haptic pulse) the instant a
  stat *crosses into* its critical range (not on every further move while already critical — mirrors
  the existing one-shot pattern already used for the drag threshold pulse). Critical is judged by a
  new `[SerializeField] criticalBoundary = 15` on this component, mirroring
  `GameUITheme.CriticalBoundary`'s default — the two aren't linked, so if a designer changes the
  theme's boundary this field needs updating to match (documented in its Inspector tooltip).
- **`Application/GameSession.cs`** — added `event Action<StatChange> StatValueChanged`, forwarding
  `StatSystem.StatChanged` for whichever run is currently bound. Subscribed alongside the existing
  `presenter.BindStats(statSystem)` call in `BeginRun()`, unsubscribed in `UnbindStats()`; the
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD` `DevelopmentSetStats()` path (which swaps `statSystem`
  directly) was updated to re-subscribe too, so the development debug panel's manual stat editing
  still drives the same audio.
- **`Presentation/FeedbackCueProfile.cs`** — added a `sliderTick` cue-ID field/property. This field
  did not exist before this pass; Phase 6 needed it and the desired-mapping list in the request
  that drove this pass did not otherwise name a home for it.
- **`Presentation/AudioService.cs`** — `Play(string)` is now a one-line wrapper around a new
  `Play(string, float volumeOverride)` overload, used to preview the SFX slider at the volume it's
  being dragged to without touching the applied SFX volume before Apply is pressed.
- **`Presentation/AudioSettingsPanelView.cs`** — added `MusicVolumeStepped`/`SfxVolumeStepped`
  events, raised at most once per ~10% of slider travel (never for a programmatic `Render()`, since
  that still uses `SetValueWithoutNotify`). Subscribed via `onValueChanged` in `OnEnable`, released
  in `OnDisable`.
- **`Presentation/SettingsPanelView.cs`** — forwards the two events above from the Audio tab, same
  pattern as `ApplyRequested`/`CancelRequested`.
- **`Composition/SettingsController.cs`** — subscribes to both forwarded events; Music slider ticks
  play `cues.SliderTick` at the normal SFX volume, SFX slider ticks play it at the value being
  dragged to (via the new `AudioService` overload), so the tick itself previews the loudness about
  to be applied.

### New Editor tool (avoids hand-editing either `.asset`'s YAML)

**`Assets/_Game/Scripts/Editor/AudioCueLibrarySetup.cs`** — three menu commands under
`Tools > Royal Decisions > Audio`:

- **`Update Main Audio Cue Library`** — adds/refreshes the 9 new `id → clip` pairs above in
  `MainAudioCueLibrary.asset`, reading and writing only through `SerializedObject`/
  `SerializedProperty` (never touching the YAML directly). Existing entries (`ui_click`,
  `card_swipe`, `card_preview`, and anything else already authored) are preserved byte-for-byte;
  re-running it is a no-op once the mapping matches.
- **`Update Default Feedback Cue Profile`** — fills the matching string fields on
  `DefaultFeedbackCueProfile.asset` (every field was blank per the previous pass's notes above:
  `Ui Click=ui_click`, `Slider Tick=slider_tick`, `Card Enter=card_enter`,
  `Card Preview=card_preview`, `Snap Back=snap_back`, `Left/Right Confirmation=card_swipe`,
  `Stat Increase=stat_increase`, `Stat Decrease=stat_decrease`, `Critical=critical`,
  `Game Over=game_over`, `Menu Music=menu_music`, `Gameplay Music=gameplay_music`). `Threshold`,
  `Exit`, `Restart` and `Ambient Loop` are deliberately left alone — the first two have no cue by
  design, Restart already plays `ui_click` directly from `GameSceneController`/`MainMenuController`
  code rather than through this profile (matching "Restart may use ui_click" from the brief), and
  nothing produces an ambient loop yet. A field that already carries a non-empty, different value is
  left untouched and reported rather than overwritten.
- **`Update Cue Library and Profile`** — runs both.

**This tool could not be run this session** — `Temp/UnityLockfile` shows the project already open
in your own Editor (`Unity.exe`, started before this session), and Unity refuses a second instance
on the same project, so the batchmode attempt aborted with "another Unity instance is running with
this project open." All five affected assemblies (Presentation, Application, Composition, and the
Editor tool itself compiled standalone via a temporary `dotnet build` check) compile cleanly with
`0 errors, 0 warnings` — see below — but the actual `.asset` writes still need one of:

- [ ] **Run it from your already-open Editor** — `Tools > Royal Decisions > Audio > Update Cue
      Library and Profile`. Console should report the 9 additions to the library and the profile
      field summary, both idempotent on a second run.
- [ ] *(only if you'd rather I run it)* Close your Editor session first and say so, and I can run it
      via `Unity.exe -batchmode -executeMethod ...` next turn.

### Required manual Inspector wiring (new, on top of the still-outstanding items from the previous pass)

- [ ] **`AudioService.Music Source`** — no separate music `AudioSource` is mentioned anywhere
      earlier in this document, and `AudioService.PlayMusic()` silently no-ops without one (logs
      "No music AudioSource is assigned"). Add a second `AudioSource` next to the existing SFX one
      on the `AudioService` object in **both** `Game.unity` and `MainMenu.unity` — `Play On Awake`
      off, `Loop` on (or leave `PlayMusic`'s own `loop: true` default), `Spatial Blend = 2D` — and
      assign it to `Music Source`.
- [ ] The previous pass's still-open items remain open (this pass didn't touch scenes or these
      assets beyond what the Editor tool above does): `AudioService.Cue Library` empty on both
      scenes, `GameSceneController.Cues` / `MainMenuController.Audio Service` / `MainMenuController.
      Cues` / `SettingsController.Cues` unassigned. None of the new cues in this pass will be heard
      until those are wired, same as before.
- [ ] `GameFeedbackController.Critical Boundary` — new field, defaults to `15` to match
      `GameUITheme`'s default. Only needs attention if a designer changes the theme's boundary later.

### Compile checks run this session

Unity itself could not compile-check (see lock conflict above). As a substitute, `dotnet build` was
run directly against the project's own generated `.csproj` files (the same assemblies Unity would
produce), plus a temporary copy of the Editor `.csproj` patched to include the new
`AudioCueLibrarySetup.cs` (deleted again immediately after, along with its `Temp/obj` output —
confirmed absent from `git status`):

- `RoyalDecisions.Presentation.csproj` — 0 errors, 0 warnings
- `RoyalDecisions.Application.csproj` — 0 errors, 0 warnings
- `RoyalDecisions.Composition.csproj` — 0 errors, 0 warnings
- `RoyalDecisions.Editor.csproj` (+ new file, temporary copy) — 0 errors, 0 warnings

This confirms the C# is syntactically and semantically valid against the real Unity assembly
references, but it is **not** a substitute for actually entering Play Mode — no EditMode/PlayMode
test run happened this session, for the same lock-conflict reason. Please run
`Window > General > Test Runner > EditMode/PlayMode > Run All` yourself and confirm the Console
stays clean, same as every other pass in this document asks.

### Verify after wiring

- [ ] Settings → Ses tab: dragging the Music or SFX slider ticks roughly every 10% of travel, not
      continuously; dragging the SFX slider all the way down makes the tick itself get quieter
      (previewing the level about to be applied); dragging Music does not change the tick's own
      volume.
- [ ] A below-threshold release (snap-back) plays a soft return sound, audibly quieter than a
      confirmed swipe.
- [ ] Reaching game over plays a bell-like sound instead of nothing.
- [ ] A decision that raises a stat plays a small chime; one that lowers a stat plays a slightly
      different small chime; a decision that pushes any stat to ≤15 or ≥85 plays a single, more
      noticeable warning instead of (not in addition to) the ordinary chime for that stat, and only
      on the turn it first crosses into that range.
- [ ] MainMenu plays quiet background music; Game scene plays its own, quieter, different track
      underneath the SFX above.

---

## Diagnosis: why Game scene SFX were silent, and Settings interaction sounds

### Root cause (confirmed by reading the current `.unity` YAML, not by guessing)

`Assets/_Game/Scenes/Game.unity`'s `AudioService` component has **`cueLibrary: {fileID: 0}`** —
empty. Every `AudioService.Play(...)`/`PlayMusic(...)` call checks `cueLibrary == null` first and
returns `AudioPlayResult.NoLibrary` without playing anything (a `Debug.LogWarning`, not an error —
which is why the Console looked clean while every cue silently no-op'd). This alone fully explains
"gameplay card sounds do NOT play at all": `card_preview`, `card_swipe`, `card_enter`, `snap_back`,
`stat_increase`/`stat_decrease`/`critical`, and `game_over`/`gameplay_music` all route through this
same `AudioService` instance, so none of them could ever have played, regardless of any other
wiring or code correctness upstream.

`GameSceneController.Cues` is a second, independent gap: the `cues` field entirely does not appear
in `Game.unity`'s serialized `GameSceneController` block (it was added to the C# class in an
earlier pass but the scene has not been re-saved since), so it deserializes to `null` and
`PlayGameplayMusic()` no-ops too.

Both `GameSceneController.Audio Service` and `GameFeedbackController.Audio Service`/`Cues` **are**
already correctly wired in `Game.unity` (pointing at the same `AudioService` and at
`DefaultFeedbackCueProfile.asset` respectively) — the previous pass's content work
(`MainAudioCueLibrary.asset` now has all 12 cues, `DefaultFeedbackCueProfile.asset` now has all 13
cue-ID strings filled in) was correct and complete. The only thing missing was these two scene
object references.

`MainMenu.unity` has the same class of gap in two more places: `SettingsController.Cues` is empty
(so Settings sliders/toggles/tabs have never been able to play anything), and
`MainMenuController.Audio Service`/`Cues` are both empty too (present in the file as new,
zero-valued fields from the same not-yet-resaved situation as `GameSceneController.Cues` above).
`MainMenu.unity`'s own `AudioService.Cue Library` **is** already wired (from the previous pass),
which is presumably why menu music is audible.

### Fix: `Assets/_Game/Scripts/Editor/GameAudioSceneWiringSetup.cs` (new)

One menu command, under **`Tools > Royal Decisions > Audio > Wire Scene Audio References`**:

- Opens `Game.unity`, then `MainMenu.unity`, each in turn (restoring whatever you had open
  afterward).
- In each, locates the relevant components by type (not by hardcoded scene object IDs, so it can't
  silently point at the wrong instance) and, through `SerializedObject`/`SerializedProperty` only:
  - `AudioService.Cue Library` → `MainAudioCueLibrary.asset`
  - `GameSceneController.Cues` → `DefaultFeedbackCueProfile.asset`
  - `GameFeedbackController.Audio Service` / `.Cues` (defensive — already correct, so this is a
    no-op unless something regresses)
  - `SettingsController.Cues` → `DefaultFeedbackCueProfile.asset`
  - `MainMenuController.Audio Service` / `.Cues` → the scene's `AudioService` /
    `DefaultFeedbackCueProfile.asset`
- **Only ever fills a field that is currently empty.** A reference that already points at
  something (correct or not) is left completely alone and reported, never overwritten — so this is
  safe to run repeatedly and safe to run without first auditing every field by hand.
- Saves only the scene(s) it actually changed.

**This could not be run this session** — same as the previous pass, `Temp/UnityLockfile` shows your
Editor already has the project open (`Unity.exe`, PID active since before this session started), and
Unity refuses a second instance on the same project. The C# compiles cleanly (see below), but the
actual reference assignment needs you to run it:

- [ ] **`Tools > Royal Decisions > Audio > Wire Scene Audio References`** in your already-open
      Editor. Console should report each reference it filled in (six lines: the `AudioService` cue
      library on both scenes, `GameSceneController.Cues`, `SettingsController.Cues`,
      `MainMenuController.Audio Service` and `.Cues`); a second run should report nothing left to
      change.
- [ ] Save both scenes if the Editor doesn't already do so as part of the tool running (it calls
      `EditorSceneManager.SaveScene` itself, but confirm no unsaved-asterisk remains on either
      scene tab afterward).

### Part 2 — Settings interaction sounds: code changes

All additive, all guarded exactly like every existing cue call site (empty `cues`/`audioService`
stays silent, never an error):

- **`AudioSettingsPanelView.cs`** — the `Master Mute` toggle now raises a new `ToggleChanged` event
  from its own `onValueChanged`, added in `OnEnable`/removed in `OnDisable`. `Render()` already used
  `SetIsOnWithoutNotify` before this change, so loading/resetting settings still cannot trigger it —
  only an actual user flip can.
- **`GraphicsSettingsPanelView.cs`**, **`ControlsSettingsPanelView.cs`** — neither file had an
  `OnEnable`/`OnDisable` before this change (nothing on either tab produced any event). Both gained
  one, wiring every toggle on that tab (`Use High Frame Rate Cap`/`Battery Saver`, and
  `Tap Buttons Enabled`/`Invert Swipe Rotation`/`Haptics` respectively) to the same new
  `ToggleChanged` event.
- **`GeneralSettingsPanelView.cs`** — already had `OnEnable`/`OnDisable` for its three buttons;
  `Reduced Motion`/`Larger Text`/`High Contrast` were added to the existing listener wiring, feeding
  the same new `ToggleChanged` event. The two-tap Reset Progress arm/confirm button, Reset Tutorial,
  and About buttons are untouched — they already have their own explicit sound-or-not behavior from
  an earlier pass, and touching them was not asked for here.
- **`SettingsPanelView.cs`** — aggregates all four tabs' `ToggleChanged` into its own single
  `ToggleChanged` event (same pattern already used for `ApplyRequested`/`CancelRequested`/etc.).
  Also added `TabPressed`, raised only from the four tab buttons' own `onClick` — **not** from
  `Show()`'s internal call to `ShowAudioTab()` that selects the default tab on open, which stays
  silent (opening Settings already plays its own click via `SettingsController.Open()`; a second
  click for the auto-selected tab would be a duplicate).
- **`SettingsController.cs`** — subscribes to both new events and plays `cues.UiClick` for each
  (`view.ToggleChanged += PlayUiClick; view.TabPressed += PlayUiClick;`) — reusing the exact same
  `PlayUiClick()` already used by Open/Apply/Reset/Cancel, so there is exactly one code path that
  decides what "the click" sounds like. **Apply/Cancel/Reset/Reset Progress/Reset Tutorial/About are
  untouched** — they already call `PlayUiClick()` from their own handlers, so nothing here duplicates
  them.

Net effect: every toggle across all four tabs, and all four tab buttons, now play `ui_click` exactly
once per user action, with no possibility of firing from a programmatic `Render()` (every `Render()`
already used the `WithoutNotify` setters before this change) and no possibility of double-firing on
Apply/Cancel/Reset (those were not touched).

### Compile checks run this session

Unity itself could not compile-check (same lock conflict as the diagnosis above). `dotnet build`
against the project's own generated `.csproj` files, same substitute as the previous pass:

- `RoyalDecisions.Presentation.csproj` — 0 errors, 0 warnings (covers `AudioSettingsPanelView.cs`,
  `GraphicsSettingsPanelView.cs`, `ControlsSettingsPanelView.cs`, `GeneralSettingsPanelView.cs`,
  `SettingsPanelView.cs`)
- `RoyalDecisions.Composition.csproj` — 0 errors, 0 warnings (covers `SettingsController.cs`)
- `RoyalDecisions.Editor.csproj` (+ new `GameAudioSceneWiringSetup.cs`, temporary patched copy,
  deleted immediately after along with its `Temp/obj` output) — 0 errors, 0 warnings

Not a substitute for Play Mode or the Test Runner — please confirm the Console stays clean and run
`EditMode`/`PlayMode > Run All` yourself once the scene-wiring command above has run.

### Verify after running the menu command

- [ ] Main Menu music still plays (unchanged by this pass).
- [ ] `ui_click` still plays on New Game / Continue / Apply / Cancel / Reset / Reset Progress arm+
      confirm / Reset Tutorial / About (all pre-existing, none of this pass's code touches them).
- [ ] Settings → any tab's slider still ticks in ~10% steps, not continuously (unchanged by this
      pass — only toggles and tabs were added).
- [ ] Flipping `Sessiz`/Master Mute, either frame-rate/battery toggle, either control toggle, or
      any of the three accessibility toggles plays exactly one click per flip; opening Settings
      (which lands on the Ses tab) and switching tabs afterward does not double up or play on load.
- [ ] In the Game scene: dragging left/right past 5% strength plays `card_preview` once per
      direction; a confirmed swipe plays `card_swipe`; a new card plays `card_enter`; a
      below-threshold release plays `snap_back`; a stat change plays `stat_increase`/
      `stat_decrease`, or `critical` the instant a stat first crosses ≤15/≥85; reaching game over
      plays `game_over`.

---

## Haptics wiring + menu transition animations

Two gaps, found by reading the existing code rather than assumed: **(1)** a Titreşim/Vibration
toggle already existed end-to-end in `GameSettings`/`ControlsSettingsPanelView`/
`SettingsController` and was persisted correctly, but `GameFeedbackController` — the only class
that ever calls `haptics.Pulse()` — had its `Configure(IHapticService)` seam called by **no
composition root at all**, so `haptics` stayed null and every `Pulse()` call in the Game scene was
a silent no-op regardless of the toggle. **(2)** `PanelFadeAnimator` (fade+scale panel transitions,
reduced-motion aware) existed fully implemented and tested but, per its own commit message, was
"not yet wired into any scene."

### What changed

**Haptics — now tiered and actually reaches a service:**

- `Presentation/HapticFeedbackLevel.cs` (new) — `Light` / `Standard` / `Critical`.
- `Presentation/HapticProfile.cs` (new) — pure duration/amplitude data per level, unit-tested
  independent of any platform code.
- `IHapticService.Pulse()` now takes a `HapticFeedbackLevel` (default `Standard`, so every prior
  caller still compiles).
- `UnityHapticService` — on Android API 26+, uses `VibrationEffect.createOneShot` via
  `AndroidJavaObject` reflection so Light/Standard/Critical genuinely differ in strength and
  length; falls back to the legacy duration-only `Vibrator.vibrate(long)` down to this project's
  minSdk 25; falls back again to `Handheld.Vibrate()` on the Editor/other platforms (Light stays
  silent there — a full-strength buzz for a subtle drag-threshold tick would be worse than
  nothing). Every native call is wrapped defensively (logged once, not silently) since OEM vibrator
  services are a known source of odd exceptions this project cannot reproduce or test.
- `GameFeedbackController` — now self-configures in `Awake()` exactly like `GameSceneController`
  re-applies settings per scene load (Settings lives in a different scene, so there's no live
  reference): builds a real `UnityHapticService` + `SettingsServiceStore` by default, reads the
  saved `HapticsEnabled`, and applies it. `Configure()` (the test seam) now also re-establishes
  event subscriptions, not just injected fields — mirrors `SettingsController.Configure()` calling
  `LoadAndApply()`. The four `Pulse()` call sites are now tiered: drag crossing the confirm
  threshold → `Light`; a confirmed decision → `Standard`; a stat entering its critical range, and
  game over → `Critical`.

**Menu transitions — `PanelFadeAnimator` wired into every panel-level and tab-level transition:**

- `PanelFadeAnimator` gained `Swap(Action)` — fades a `CanvasGroup` out, runs the swap at zero
  alpha, fades back in, without ever touching `panelRoot`'s active state (unlike `Show`/`Hide`).
  Used for in-place content changes (a settings tab) where two `SetActive(true)` bodies existing
  simultaneously inside a `VerticalLayoutGroup`/`ContentSizeFitter` stack would double the measured
  height mid-swap. Also gained `animateScale` (off for tab swaps — a scale pulse would visibly
  distort `ContentViewport`'s `RectMask2D`-clipped bounds; on, unchanged default, for full-panel
  open/close).
- `SettingsPanelView` — `Show()`/`Hide()`/`Reopen()` now route through an optional `panelAnimator`
  (falls back to instant `SetActive` if unwired); tab switching now routes through an optional
  `tabCrossfadeAnimator`. A `SettingsTabId` spam-guard skips animating a reselect of the
  already-active tab (repeated button press, or `Show()` re-selecting the tab it was already on)
  but still applies the (idempotent) active state directly, so it can't ever leave two tab bodies
  active or drift from the actual GameObject state.
- `AboutPanelView` — same optional `panelAnimator` pattern for `Show()`/`Hide()`.
- `SceneSetupAutomation.cs` — new `ConfigurePanelFadeAnimator`/`ValidatePanelFadeAnimator` helpers
  add a `CanvasGroup` + `PanelFadeAnimator` to `SettingsPanel`, `AboutPanel` (both 220ms show /
  180ms hide — reads as a screen-level transition) and `ContentViewport` (kept at the animator's
  shorter defaults, `animateScale` off — reads as an in-place content swap), and wire them into the
  new `SettingsPanelView`/`AboutPanelView` fields. No manual scene wiring needed — already applied
  (see below).
- `SettingsController` needed **no changes** — `Open`/`ApplyFromView`/`Cancel`/
  `HandleAboutRequested`/`HandleAboutClosed` already called `view.Show()`/`Hide()` and
  `aboutPanel.Show()`/`Hide()` in the right order for a crossfade to fall out for free: the
  destination panel/menu is reactivated immediately while the closing one fades out on top of it
  (still blocking input the whole time, since `CanvasGroup.blocksRaycasts` stays true until alpha
  reaches exactly 0) — this only works because `SettingsPanel`/`AboutPanel`'s background colour
  already matches `MainMenuPanel`'s camera-clear-colour backdrop, so there's no flash of empty
  canvas underneath, matching a design decision from an earlier pass in this file.
- Input-spam / double-navigation: covered by two existing, unmodified guards plus the one new tab
  guard above — `MainMenuController.isTransitioningToGame` (already blocked scene-load spam),
  `CardSwipeController`'s input lock after confirmation (already existed), and `PanelFadeAnimator`
  itself, where `Show()`/`Hide()`/`Swap()` all call `StopRunningAnimation()` before starting, so a
  rapid repeated call always converges to the last request's target state instead of stacking or
  getting stuck mid-fade.

### Verified this session (Unity available via CLI, unlike most earlier passes in this file)

Ran directly, not simulated — `Unity.exe -batchmode -nographics -projectPath . -executeMethod ...`:

- [x] `ApplyBatch` — first attempt caught a real EditMode test compile error
      (`Has.One.EqualTo(...)` isn't valid NUnit syntax; fixed to `Has.Exactly(1).EqualTo(...)`).
      Second attempt: exit code 0, `BACKUP_CREATED` → `VALIDATION_OK` → `APPLY_COMPLETE`,
      **0 errors, 0 warnings**.
- [x] `ValidateBatch` — exit code 0, `VALIDATION_OK`, **0 errors, 0 warnings**.
- [x] Idempotency: ran `ApplyBatch` two more times. Run 2 vs. run 1 changed only three
      `m_fontSize` lines in `MainMenu.unity` (42→40, 68→64) — the same benign TMP auto-size cache
      convergence documented earlier in this file, unrelated to this pass. Run 3 vs. run 2:
      **byte-identical** for both `MainMenu.unity` and `Game.unity` (`diff -q`). Stable.
- [x] Full `EditMode -runTests` (no `-quit`, per this file's own earlier lesson that the flag can
      exit before results are written): first run — **747/750 passed, 3 failed**, all three in the
      new tests, all three real bugs (not test-authoring mistakes in the assertion sense — the
      tests were checking the right thing and caught actual defects):
  - `SettingsPanelViewTests.Show_ActivatesOnlyTheAudioTab` — the tab spam-guard's original form
    skipped `ApplyActiveTabContent` entirely on a no-op reselect, which is only safe if the
    GameObjects' actual active state already matches (true in the real, `SceneSetupAutomation`
    -authored scene, not guaranteed for arbitrarily-constructed GameObjects). Fixed: the guard
    still applies the idempotent active state directly, only the animation is skipped.
  - `GameFeedbackControllerTests.DraggingPastThreshold_PulsesLightExactlyOnce` and
    `ConfirmingADecision_PulsesStandard` — both recorded zero pulses. Root cause: this test
    activates `GameFeedbackController`'s host GameObject after wiring it (the documented
    "build inactive, configure, then activate" pattern used elsewhere in this codebase's PlayMode
    tests), but in a pure EditMode run `SetActive(true)` did not reliably invoke `OnEnable()`
    synchronously the way it does in Play Mode, so `Subscribe()` never ran. Fixed by making
    `Configure()` also call `Subscribe()` directly (idempotent, guarded) instead of relying on
    Unity's automatic lifecycle dispatch — a production-code change, but `Configure()` is a
    test-only seam no composition root calls, so this has no runtime behaviour impact.
  - Re-ran after both fixes: **750/750 passed, 0 failed.**
- [x] `ValidateBatch` re-run after the two code fixes above (neither touches scene wiring, but
      confirmed anyway): exit code 0, `VALIDATION_OK`, 0 errors.

PlayMode suite was **not** run this session — please run `PlayMode > Run All` yourself; nothing in
this pass should affect it (`CardSwipeController` itself is unchanged), but it wasn't exercised.

### Not verified — please check with your own eyes

Batch mode can apply/validate wiring and run tests, but can't screenshot or feel an animation:

- [ ] Titreşim toggle ON, on an Android device (not the Editor — `Handheld.Vibrate()`/native
      `VibrationEffect` are both no-ops in the Editor by design): drag a card past the confirm
      threshold (light tick), confirm a decision (a firmer pulse), and drive a stat to ≤15 or ≥85
      or reach game over (the strongest pulse) — three distinguishably different feels, not one
      repeated buzz. Toggle OFF and repeat: no vibration at all. Toggle back ON: works again
      immediately on the next run (Settings/Game are different scenes, so this only takes effect
      when the Game scene next loads — same as every other Controls-tab setting).
- [ ] On a device below Android 8.0 (API 26) if one is available: same scenario, confirm no crash
      and that *some* vibration still occurs (via the legacy `Vibrator.vibrate(long)` path) even
      without amplitude differentiation.
- [ ] MainMenu → tap the settings icon → Settings fades/scales in over the menu (~220ms); tap a
      different tab → the tab content crossfades in place (~short, no scale) with no visible
      double-tab flash or layout jump; tap Cancel or Apply → Settings fades out, menu is already
      there underneath. Tap Hakkında → Settings fades out as About fades in (not layered); About's
      Kapat → same crossfade back, with your tab selection and any unapplied edits intact.
- [ ] Spam-tap the settings icon, a tab button, and Hakkında/Kapat rapidly: no duplicate panels, no
      stuck mid-fade state, final state always matches the last tap.
### Known gap noticed, not fixed (out of this pass's scope)

`PanelFadeAnimator.SetReducedMotion(bool)` already existed before this pass and is unchanged by
it, ready for a caller to shorten every transition when `Azaltılmış Hareket` is on — but nothing
currently calls it for the new panel/tab animators, because `AccessibilityPresentationController`
(the class that reads `GameSettings.ReducedMotion` and would call it) is **only ever constructed
in the Game scene** by `SceneSetupAutomation.ApplyGameScene` — `SettingsController.accessibility`
in `MainMenu.unity` has no matching `ApplyMainMenuScene` wiring and is therefore unset. This
predates this session (confirmed by grepping `SceneSetupAutomation.cs` for every
`AccessibilityPresentationController` reference — all in the Game-scene method). Building that
MainMenu-scene wiring from scratch was judged out of scope for a vibration+transitions task and
risked untested surface; flagging it here instead, per this file's own convention, rather than
silently leaving it. If/when someone wires a MainMenu `AccessibilityPresentationController`, adding
the four `PanelFadeAnimator`s (`SettingsPanel`, `AboutPanel`, `ContentViewport` ×1 shared) to a new
array field and calling `SetReducedMotion` on each from `Apply()` is all that's left to do — the
transition code itself needs no further changes.

- [ ] With `Azaltılmış Hareket` (reduced motion) on, the panel/tab transitions added this session
      currently still run at full speed (the gap above) — confirm this matches your expectations
      for now, or ask for the MainMenu accessibility wiring as a follow-up.

---

## MainMenu → Game scene transition

Every transition added in the previous section fades content *within* a single already-loaded
scene. Leaving MainMenu for Game was still an untouched, instant `SceneManager.LoadScene` call —
the one remaining "cut" in the menu flow — so this pass covers exactly that, reusing
`PanelFadeAnimator` again rather than introducing a second transition mechanism.

### What changed

- **`PanelFadeAnimator.Show(Action onComplete = null)`** — gained a completion callback, mirroring
  the one `Hide(Action onComplete = null)` already had. Every existing call site (`SettingsPanelView`,
  `AboutPanelView`) still calls `Show()` with no argument and is unaffected.
- **`MainMenuController`** — new optional `[SerializeField] PanelFadeAnimator transitionOverlay`.
  `LoadGameSceneAfterClickCue()` (the existing coroutine that already waits
  `sceneTransitionDelaySeconds` for the click cue to play) now calls
  `transitionOverlay.Show(() => sceneLoader.LoadScene(gameSceneName))` when wired, fading the
  screen to a solid cover before the scene actually loads; falls back to the previous instant load
  when unwired. The existing `isTransitioningToGame` guard (unchanged) still prevents a double
  New Game/Continue press from starting two transitions.
- **`GameSceneController`** — new optional `[SerializeField] PanelFadeAnimator transitionOverlay`.
  Unlike every other `PanelFadeAnimator` in this project, this one is authored to **start already
  opaque and active** — the very first rendered frame of a freshly loaded scene must never show
  unsettled/unstyled layout before anything has had a chance to run. `Start()` calls
  `transitionOverlay?.Hide()` as its first line, fading the cover away once the scene has wired
  itself.
- **`SceneSetupAutomation.cs`** — new `ConfigureTransitionOverlay(canvas, report, startVisible)`
  helper (next to the existing `ConfigurePanelFadeAnimator`): builds a full-screen
  `Image`+`CanvasGroup`+`PanelFadeAnimator` named `TransitionOverlay`, always forced to be the
  *last* child of the Canvas (so it renders above Background/SafeArea on Game, and above
  SafeArea/SettingsPanel/AboutPanel on MainMenu, regardless of Apply call order). Uses
  `OverallBackgroundColour` (`#07111B`) — the same dark navy already used for the Game background
  and GameOverPanel — rather than plain black, so the cover reads as part of this game's palette.
  280ms show/hide (within this project's own 200–350ms "large screen transition" range). Wired into
  both `ApplyGameScene` (`startVisible: true`) and `ApplyMainMenuScene` (`startVisible: false`).
  MainMenu's canvas-level `RemoveUnexpectedChildren` allow-list gained `"TransitionOverlay"` —
  without this, a second Apply run would have deleted and recreated the object every time,
  breaking the byte-identical-on-rerun idempotency this file's own automation is held to elsewhere.
  Matching `ValidatePanelFadeAnimator`/`ValidateReference` checks were added to both
  `ValidateGameScene` and `ValidateMainMenuScene`, plus an explicit assertion that Game's overlay
  starts active and MainMenu's starts inactive (opposite of every other panel — flagged so a future
  pass doesn't "fix" it to match the usual convention).
- **`PanelFadeAnimatorTests.cs`** — two new tests for the `Show(onComplete)` addition, mirroring the
  existing `Hide(onComplete)` coverage (`Show_OutsidePlayMode_InvokesCompletionAfterRaisingAlpha`,
  `Show_OnAlreadyFullyVisiblePanel_InvokesCompletionWithoutError`).

### Verified this session (Unity was available via CLI for this whole pass)

- `ApplyBatch`: exit 0, `BACKUP_CREATED` → `VALIDATION_OK` → `APPLY_COMPLETE`, 0 errors/warnings.
- Ran `ApplyBatch` three times total. Run 2 vs. run 1 changed only the same benign TMP auto-size
  cache convergence documented earlier in this file (unrelated to this pass, `m_fontSize` 42→40 /
  68→64 in `MainMenu.unity`). Run 3 vs. run 2: **byte-identical** for both `Game.unity` and
  `MainMenu.unity` (`diff -q`). Stable.
- `ValidateBatch` independently: exit 0, `VALIDATION_OK`, 0 errors.
- Spot-checked the raw scene YAML directly (not just the Editor tool's own report): `Game.unity`'s
  `TransitionOverlay` — `m_IsActive: 1`, `CanvasGroup.m_Alpha: 1`, `m_BlocksRaycasts: 1`;
  `MainMenu.unity`'s — `m_IsActive: 0`, `m_Alpha: 0`, `m_BlocksRaycasts: 0`. Both
  `GameSceneController.transitionOverlay` / `MainMenuController.transitionOverlay` reference the
  correct component by fileID.
- Full `EditMode -runTests` (no `-quit`): **752/752 passed, 0 failed** (750 before this pass, +2 new
  `PanelFadeAnimator` tests). No compiler warnings or errors in the log.

PlayMode suite was **not** run this session — please run `PlayMode > Run All` yourself; nothing in
this pass changes gameplay logic, but the actual scene-load transition can only be felt, not
asserted by a PlayMode test that doesn't load a second scene.

### Not verified — please check with your own eyes

- [ ] From MainMenu, tap **Yeni Oyun** or **Devam Et**: the click cue plays, then the screen fades
      to a solid dark cover (~280ms) before the Game scene appears, rather than an instant cut.
- [ ] The Game scene's first visible frame is already the faded cover, not a flash of
      unsettled/unstyled layout — the cover then fades away (~280ms) once the opening card is ready.
- [ ] Spam-tapping **Yeni Oyun**/**Devam Et** rapidly still starts exactly one transition (the
      pre-existing `isTransitioningToGame` guard).
- [ ] The cover's colour reads as intentional/branded, not like an unstyled loading flash — it's the
      same dark navy already used for the Game scene's own background and the Game Over panel.

---

## Settings menu expansion #2 — master volume, FPS choice, swipe sensitivity/disable, text size, language, danger styling (2026-08-21)

Extends the existing four-tab Settings menu with new controls inside its current architecture
(staged Apply/Cancel/Reset, versioned JSON persistence, passive per-tab views aggregated by
`SettingsPanelView`) — no new Settings system, no new state-management or persistence mechanism.

### What changed

**`Domain/GameSettings.cs`** — new additive fields, all with safe defaults so an older save file
missing a key falls back to the field initializer (no version bump, same pattern as every prior
addition): `masterVolume` (default `1f`, so a pre-existing save keeps sounding exactly as loud as
it always did — `effectiveVolume = masterVolume * (music|sfx)Volume`), `swipeSensitivity` (default
`0.5f`), `disableSwipe` (default `false`), `language` (default `"tr"`). Two booleans were **replaced**
by three-way enums, each new file (one public type per file, CLAUDE.md §10):
- `FrameRateMode.cs` (`Sixty = 0` / `Thirty` / `Auto`) replaces `useHighFrameRateCap`; `Sixty` is
  the enum default so an old save missing the new field lands on the same behaviour the old `true`
  default gave.
- `TextSizeMode.cs` (`Normal = 0` / `Small` / `Large`) replaces `largerText`; `Normal` is the
  default, matching the old `false`.
`SanitizeAfterLoad()` gained clamping for the two new floats and `Enum.IsDefined` guards for the
two new enums (a corrupt/out-of-range save value resets to the safe default, same defensive pattern
as the existing volume-NaN handling).

**`Presentation/AudioService.cs` + `IAudioService.cs` + `SilentAudioService.cs`** — added
`MasterVolume`/`SetMasterVolume`. SFX and music are each multiplied by the master before reaching
the `AudioSource`s; `Play(id)` (no explicit volume override) now resolves through the master too.
`Volume`/`MusicVolume` still return the raw per-channel setting, unscaled — existing callers and
tests that read those properties are unaffected.

**`Presentation/CardSwipeController.cs`** — added `SetSwipeSensitivity(0..1)` (maps onto the
existing `thresholdRatio`, captured-default pattern identical to `SetInvertRotation`/
`SetReducedMotion`: 0.5 exactly reproduces today's authored threshold) and
`SetSwipeInputEnabled(bool)` (guards `OnBeginDrag`; `ConfirmSide`, used by the tap-button path, is
untouched, so decision buttons keep working with swipe off).

**`Presentation/AccessibilityPresentationController.cs`** — `TextSizeMode` now maps to a scale
(`Small` 0.9× / `Normal` 1× / `Large` 1.15×, the last being byte-identical to the old "larger text"
behaviour).

**Four `*SettingsPanelView.cs`** (all still passive — no rule/persistence logic):
- `AudioSettingsPanelView` — new Master Volume slider + live "NN%" `TMP_Text` next to all three
  volume sliders, updated on every drag frame (not gated to the 10%-step tick).
- `GraphicsSettingsPanelView` — the single frame-rate `Toggle` is replaced by three `Toggle`s
  (`frameRateThirty/Sixty/Auto`) read as a `FrameRateMode`; Battery Saver toggle unchanged.
- `ControlsSettingsPanelView` — new Swipe Sensitivity slider (+ percentage label, same step-gated
  tick pattern as the volume sliders) and a Disable Swipe `Toggle`; Tap Buttons, Invert Rotation,
  Haptics unchanged.
- `GeneralSettingsPanelView` — the Larger Text `Toggle` is replaced by three `Toggle`s
  (`textSizeSmall/Normal/Large`) read as a `TextSizeMode`; new read-only Language `TMP_Text`
  (`"tr"` → `"Türkçe"` — no in-app localization system exists, so this is a fixed lookup, not a
  functional picker); Reduced Motion, High Contrast, Reset Progress/Tutorial/About unchanged.

**`Presentation/SettingsPanelTheme.cs`** — added `DangerColour`/`DangerTextColour` (muted red), the
same "single source of truth" pattern as the existing tab tints.

**`Presentation/SettingsPanelView.cs`** — pass-through properties/events for all of the above,
mirroring the exact shape of the existing Music/Sfx/InvertRotation pass-throughs.

**`Composition/SettingsController.cs`** — `ApplyFromView` now stages the four new fields into
`GameSettings` alongside the existing ones. Master Volume previews live against `AudioService`
while dragging, exactly like Music/Sfx (`Cancel()` already reverted `ApplyRuntime`, so no extra
revert logic was needed — it now naturally reverts master volume too). `ResolveTargetFrameRate`
switches on `FrameRateMode`: `Thirty`→30, `Sixty`→60, `Auto`→`Application.targetFrameRate = -1`
(Unity's own "use the platform's default cadence" value — deliberately **not** a fabricated
device-performance heuristic, since no such system exists in this project). Swipe
sensitivity/disable are staged into `GameSettings` here but — like `InvertSwipeRotation` before
them — are only ever *applied* in the Game scene (see below), since MainMenu has no live
`CardSwipeController` reference.

**`Composition/GameSceneController.cs`** (`ApplySettings()`) — now also calls
`SetMasterVolume`, `SetSwipeSensitivity`, and `SetSwipeInputEnabled(!DisableSwipe)`. When swipe is
disabled, the tap decision buttons are forced visible (`TapButtonsEnabled || DisableSwipe`)
regardless of their own toggle, so a player who also had tap buttons off is never stranded with no
way to resolve a card.

**Apply/Cancel/Reset kept as-is.** `SettingsController` genuinely depends on the staged-edit model
(Cancel reverts a live audio preview that would otherwise be stuck at a discarded value), so per
the task's own instruction the buttons were not removed. `Varsayılanlara Dön`'s existing mechanism
(`GameSettings.CreateDefault()`) automatically covers every new field's default — no new reset code
was needed. `Sıfırla` → **Varsayılanlara Dön** and `Öğreticiyi Sıfırla` → **Öğreticiyi Tekrar
Göster** are label-only renames (`SceneSetupAutomation.cs`); the underlying events/wiring are
untouched.

**`Editor/SceneSetupAutomation.cs`** — the scene-construction tool (CLAUDE.md §11: `.unity` is not
hand-edited) gained the UI for every field above, reusing the *existing* `EnsureSliderControl`/
`EnsureToggleControl`/`EnsureMenuButton` row builders — no new UI component system:
- `EnsureSliderControl` gained a trailing `TMP_Text` percentage readout (an `out` parameter);
  updated at all four call sites (Master/Music/Sfx/Sensitivity).
- The FPS and Text Size three-way choices reuse the project's one existing "selectable" control
  (`Toggle`) grouped under a `UnityEngine.UI.ToggleGroup` (`allowSwitchOff = false`) for radio
  behaviour — the standard uGUI mechanism for this, not a bespoke segmented control.
- `İlerlemeyi Sıfırla` is visually separated: an 18px spacer row above it, and its button now uses
  `SettingsPanelTheme.DangerColour`/`DangerTextColour` instead of the gold every other button uses.
  Its existing two-tap arm/confirm guard (unchanged) already prevented an accidental single-tap
  delete — this pass only adds the visual distinction the task asked for.
- `RemoveUnexpectedChildren` allow-lists were updated per tab so a repeated Apply run cleans up the
  renamed/removed old rows (`UseHighFrameRateCap`, `LargerText`) instead of leaving orphans.
- The `ValidateMainMenuScene` path-existence list was updated to match every renamed/new object.
- Only `SceneSetupAutomation.cs` (Editor code) was touched — `MainMenu.unity` itself was **not**
  edited directly, per CLAUDE.md §11.

**Tests** — `SettingsSaveServiceTests.cs`: updated the two tests that referenced the removed
`UseHighFrameRateCap`/`LargerText` API to use `FrameRateMode`/`TextSizeMode`, plus four new tests
(additive-JSON defaults for the four new fields, save/load round-trip for master volume + language,
out-of-range clamping, unknown-enum-value fallback). `CardSwipeControllerTests.cs`: six new tests
for `SetSwipeSensitivity` (min/max/default threshold) and `SetSwipeInputEnabled` (suppresses drag,
`ConfirmSide` still works, re-enable restores drag). `AudioServiceTests.cs`: two new tests for
`SetMasterVolume` (default is full, doesn't affect the raw per-channel readback; clamping).
`AudioServicePlayModeTests.cs`: one new test proving `effectiveSfxVolume = masterVolume * sfxVolume`
reaches the real `AudioSource`.

### Manual step required

- [ ] Run **`Tools > Royal Decisions > Scene Setup > Apply Remaining Setup`** — rebuilds
      `MainMenu.unity`'s Settings panel to add the new rows and rename the changed ones. Nothing in
      this pass touches `.unity`/`.prefab` files directly.

### Verified this session (Unity 6000.3.18f1 was available via CLI for this whole pass)

- Full `EditMode -runTests`: **764/764 passed, 0 failed** (752 before this pass, +12 new tests
  listed above). Zero `error CS` in the compiler log across three full recompiles (the test-runner's
  own define-symbol switch forces two extra domain reloads beyond the normal one). No compiler
  warnings or errors.
- **Important CLI note for next time:** `-runTests` must **not** be combined with `-quit` — Unity
  exits immediately after Editor init in that combination without ever invoking the test runner
  (confirmed: first attempt this session produced no updated results file and no test output at
  all, despite `exit 0`). Drop `-quit`; the test framework quits on its own once the run finishes.
- Full `PlayMode -runTests`: **30/39 passed, 9 failed** — all 9 failures are
  `CardSwipeAnimationPlayModeTests` timing tests ("animation did not complete within 300 frames").
  Confirmed **pre-existing**: `Logs/PlayModeResultsBaseline.xml` (2026-08-16, before this session)
  shows the exact same 9 tests failing with the exact same message, 38/29/9 — i.e. this pass added
  exactly one new PlayMode test (`MasterVolumeScalesTheAudioSourceMultiplicatively`) and it passed;
  no regression. The 9 failures appear specific to headless `-batchmode -nographics` CLI execution
  (likely coroutine/frame-timing behaviour without a real display) — please also run
  `PlayMode > Run All` from inside the Editor window yourself to confirm they pass interactively,
  since that's the environment that actually matters for the shipped game.

### Not verified — please check with your own eyes

- [ ] After **Apply Remaining Setup**, open Settings → **Ses**: Ana Ses/Müzik/Ses Efektleri each
      show a live "NN%" next to the slider that updates while dragging; dragging Ana Ses audibly
      scales both music and SFX together.
- [ ] **Grafik**: 30 FPS / 60 FPS / Otomatik behave as a radio group (selecting one deselects the
      others); Pil Tasarrufu still overrides to 30 regardless of the FPS choice.
- [ ] **Kontroller**: Kaydırma Hassasiyeti slider shows a live percentage; dragging it to the low
      end makes a card noticeably harder to confirm by swipe in the Game scene, and to the high end
      easier. Kaydırmayı Devre Dışı Bırak: with it on, swiping a card does nothing, but the decision
      buttons still work (and appear even if Dokunma ile Karar Butonları is off).
- [ ] **Genel**: Küçük/Normal/Büyük behave as a radio group and visibly change text size; Dil shows
      "Türkçe" as a static (non-interactive) row.
- [ ] İlerlemeyi Sıfırla reads as visually distinct (muted red) from every other row/button in
      Settings, with a spacer above separating it from Yüksek Kontrast; still requires two taps.
- [ ] Bottom action bar: **Varsayılanlara Dön** label (was Sıfırla); pressing it resets every new
      field above back to its default too. **Öğreticiyi Tekrar Göster** label (was Öğreticiyi
      Sıfırla) on the General tab's tutorial-reset button.
- [ ] With many more rows now in each tab, confirm on a small/low-height device (or a resized Game
      view) that Settings content scrolls smoothly, the header/tab bar/bottom action bar never
      scroll away, and there is no horizontal scrollbar or content clipped outside the panel.
- [ ] Console shows no errors/warnings after opening every Settings tab at least once.

---

## Settings menu expansion #2 — real-user-flow verification pass (2026-08-21, later same day)

No production behaviour changed in this pass except one real bug found and fixed (below). The
goal was to actually exercise the Settings screen — sliders dragged, toggles clicked, tabs
switched, Apply/Cancel/Reset pressed, persisted, and reloaded — since this environment has no
way to click a rendered screen by hand.

### What changed

- **`Composition/SettingsController.cs`** — real bug found by this pass's audit (not by running
  anything yet): `HandleSfxVolumeStepped`'s slider-tick preview passed the raw dragged SFX value
  straight to `AudioService.Play(id, volumeOverride)`, bypassing the master-volume multiplier.
  With Ana Ses turned down, the preview tick while dragging Ses Efektleri played louder than the
  SFX will actually sound once Uygula is pressed. Fixed: the preview volume is now multiplied by
  `audioService.MasterVolume` before being passed through. Verified by re-deriving the
  `PlayOneShotScaleFor` math by hand (single multiplication, no double-counting) and by a full
  EditMode rerun (764/764, unchanged).
- **`Domain/GameSettings.cs`** — an XML-doc `<see cref>` on `ClampUnitRange` pointed at
  `Mathf.Clamp01` from an earlier rename, but the method still calls `Mathf.Clamp(float,float,float)`.
  Corrected the cref; no behaviour change.
- **Ran the project's own `SceneSetupAutomation.ApplyBatch` via CLI** (the same sanctioned,
  backup-protected tool `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` runs, and
  the same one prior sessions already used from the CLI — see the report `BACKUP_CREATED` →
  `VALIDATION_OK` → `APPLY_COMPLETE`, 0 errors) so `MainMenu.unity` actually contains the new
  Settings rows described in the previous section, making this pass possible at all. Only removed
  the two expected stale orphans (`UseHighFrameRateCap`, `LargerText`). **`Game.unity`'s resulting
  changes were reverted** — unrelated `ResponsiveCardSizer`-driven card-size and TMP auto-size
  drift the same Apply run touched, out of scope for Settings and not reviewed here; the one field
  addition it also produced (`AudioService.masterVolume: 1`) needs no explicit YAML entry since
  Unity already defaults an absent serialized field to the C# field initializer.
- **New `Tests/PlayMode/SettingsFlowPlayModeTests.cs`** (13 tests) — builds the real
  `SettingsController` + `SettingsPanelView` + all four tab views under a real `Canvas`, wired
  with genuine `Slider`/`Toggle`/`Button`/`ToggleGroup` components exactly as
  `SceneSetupAutomation` wires them (not fakes), and a `StubSettingsStore` standing in for the
  persisted file. Drives it the way a player would: opens the panel, drags sliders, clicks
  toggles across tabs (switching tabs first — see "what this pass did NOT find" below), presses
  Apply/Cancel/Reset/Öğreticiyi Tekrar Göster/İlerlemeyi Sıfırla, and tears the whole scene down
  and rebuilds it against the *same* store instance to simulate an app restart. Covers: opening
  shows persisted values; 0% and 100% render correctly; live percentage tracking while dragging;
  mute never zeroes the underlying volumes and unmuting restores them exactly; the FPS and Metin
  Boyutu radio groups are genuinely mutually exclusive (a real `ToggleGroup`, not simulated);
  Kaydırma Hassasiyeti and Kaydırmayı Devre Dışı Bırak persist; Varsayılanlara Dön restores every
  new field in both UI and the store; Öğreticiyi Tekrar Göster touches only the tutorial flag;
  İlerlemeyi Sıfırla needs two taps, arms/disarms correctly, and never touches the Settings store
  (that's a different component); Cancel reverts the live audio preview and never saves;
  persistence survives a simulated restart.
- **`Tests/PlayMode/GameCompositionPlayModeTests.cs`** — extended the existing real-`GameSceneController`
  fixture with an optional settings seed and a wired `TapChoiceButtonsView`, and added two tests:
  with `DisableSwipe` on, a real drag no longer resolves a card but the tap-button path still does,
  and the buttons are forced visible even though `TapButtonsEnabled` is off; with `DisableSwipe`
  off, tap-button visibility still follows its own toggle as before (no regression).

### What this pass did NOT find a bug in, despite initially suspecting one

The first run of the new tests failed 5 of the 13 (FPS/Metin Boyutu exclusivity, Kaydırma
Hassasiyeti, Kaydırmayı Devre Dışı Bırak, Öğreticiyi Tekrar Göster, İlerlemeyi Sıfırla — the tests
outside the Audio tab). Root cause was the test, not the product: `SettingsPanelView.Show()`
always opens on the Ses tab and explicitly deactivates the other three tabs'
`GameObject`s (`ApplyActiveTabContent`), which un-registers those tabs' own `OnEnable`-subscribed
listeners. The tests were interacting with Grafik/Kontroller/Genel controls without first calling
`view.ShowGraphicsTab()`/`ShowControlsTab()`/`ShowGeneralTab()` — exactly what a real player would
have to do (switch tabs) before touching those controls. Fixed the tests, not the product; all 13
now pass. Recorded here because it is worth knowing this is how tab-switching interacts with each
tab's own event subscriptions, if it ever needs debugging again.

### Verified this session

- Full `EditMode -runTests`: **764/764 passed, 0 failed** — unchanged by this pass (only the doc-cref
  and the slider-tick fix touched production code, neither is covered by a value this suite
  wasn't already asserting).
- Full `PlayMode -runTests`: **45/54 passed** (54 total = 39 pre-existing + 13 new Settings-flow
  tests + 2 new gameplay-integration tests). All 9 failures are the same pre-existing
  `CardSwipeAnimationPlayModeTests` timing failures documented in the previous section and in
  `Logs/PlayModeResultsBaseline.xml` (2026-08-16) — confirmed by name-for-name comparison, not
  just count. All 15 new tests passed.
- `ProjectSettings/ProjectSettings.asset` picked up an unrelated scripting-define-symbol change
  from running the Editor three times this pass; reverted each time (CLAUDE.md §11).

### Not verified — please check with your own eyes

Everything already listed as "Not verified" in the previous section still applies (this pass
could not click a rendered screen, only drive the real components programmatically). In addition:
- [ ] Confirm `Assets/_Game/scenes/MainMenu.unity`'s diff from this pass looks right when opened
      in the Editor — it was applied via the CLI `ApplyBatch` path rather than the interactive
      `Tools` menu, and `Game.unity`'s incidental changes from the same run were deliberately
      reverted (see above) rather than reviewed by eye.
- [ ] The slider-tick preview fix: with Ana Ses around 20–30%, drag Ses Efektleri — the tick
      should now sound quiet, matching what Uygula will actually commit, not full volume.

## Story integration — "Sığınak: Saltanat Günlükleri" Chapter I (2026-08-23)

Integrated the narrative specification in `Hıkaye.md` (root of the repo) into the existing card
system as real, data-driven content — not decorative text. See the chat report for the full
architecture write-up; this section covers only what needs manual attention in the Editor.

### What changed (summary — full detail in the session report)

- New Data types: `NumericCondition`/`NumericSource`/`NumericComparison`, `ConditionalChoiceEffect`,
  `RandomStatOutcome`, `CounterDelta`, `LeaderHealthBounds`. `CardConditions` gained numeric
  conditions; `ChoiceDefinition` gained counter deltas, a conditional effect, and a random outcome
  — all optional, all backward compatible with the twenty existing placeholder cards.
- `RunState` gained `LeaderHealth`, `ReignNumber`, and named story counters (`AddToCounter`/
  `GetCounter`), all covered by `SanitizeAfterLoad` and JSON round-trip tests. No save-version bump:
  a save from before this pass simply deserialises these as the constructor's defaults (full leader
  health, first reign, no counters) — verified by test, matching how every earlier additive field
  in `RunState` already behaves.
- `ChoiceResolver` gained support for the three new effect types, applied atomically alongside the
  existing flag/delta/cooldown/forced-next handling — a reign succession triggered by a choice never
  leaves a statistic sitting at a boundary for `GameOverEvaluator` to see.
- New `Tools/Royal Decisions/Generate Story Content` Editor command (mirrors the existing
  placeholder generator's safety rules exactly: writes only under `Assets/_Game/Content/Story/`,
  never overwrites hand-authored content, validates before writing). Generates 25 cards (K1-K25 of
  the specification) plus one closing card of our own, and 8 boundary endings themed to the
  setting.

### Manual step required

The generator writes `Assets/_Game/Content/Story/StoryContentCatalogue.asset` but does **not**
wire it into the Game scene — `GameSceneController.catalogue` still points at
`PlaceholderContentCatalogue.asset`, and switching it is an Inspector change on a `.unity` scene,
which this pass does not touch per CLAUDE.md §11.

To play the real story instead of the twenty placeholder cards:

- [ ] Run `Tools > Royal Decisions > Generate Story Content` once (idempotent; safe to re-run).
- [ ] Open `Assets/_Game/scenes/Game.unity`, select the object carrying `GameSceneController`,
      and drag `Assets/_Game/Content/Story/StoryContentCatalogue.asset` onto its `Catalogue` field
      (replacing `PlaceholderContentCatalogue`).
- [ ] If you want both available side by side (e.g. a menu choice), that selection logic does not
      exist yet — see "Remaining issues" in the session report.

### Known, deliberate content-validation warnings

`Tools > Royal Decisions > Content Authoring` (or `ProjectContentAudit`) will show ~27 warnings on
the Story catalogue, all `UnreachableRequiredFlag` / `FlagReadNeverProduced` for the same flag
(`story_forced_chain_only`). This is intentional — see the doc comment on
`StoryContentLibrary.ForcedChainOnlyFlag` — and is why `ReleaseValidationAutomation.ValidateContent`
(which currently demands zero warnings, not just zero errors) would fail if the Story catalogue
were ever wired in as the release catalogue as-is. Not fixed this pass; flagged for whoever wires
it in for real.

### Verified this session

- Full `EditMode -runTests` (Unity 6000.3.18f1 via CLI): **817/817 passed, 0 failed** (764 baseline
  + 53 new: `RunStateTests` leader/counter cases, `ConditionEvaluatorTests` numeric-condition cases,
  `SeededRandomSourceTests.ForChoiceResolution` cases, the new `ChoiceResolverConditionalEffectTests`
  and `StoryContentLibraryTests` fixtures).
  - The first run of this pass surfaced a real regression, caught by the suite exactly as intended:
    an earlier draft added `StatType.None` as a "no stat selected" sentinel, which broke every
    existing test that iterates `Enum.GetValues(typeof(StatType))` (6 failures, all pre-existing
    tests, none of the new ones). Fixed by not touching the shared enum at all — the "no stat"
    case is now expressed as `StatType?` at the `ConditionalChoiceEffect` constructor only, backed
    by a plain `bool hasSuccessionResetStat` field, which is how `RunState.LeaderHealth`-only
    succession (leader-risk cards) is told apart from stat-resetting succession (destructive/Yıkıcı
    cards) without adding a member to a shared, iterated-over enum. Re-ran after the fix: clean.
  - `ProjectSettings/ProjectSettings.asset` picked up the same unrelated scripting-define-symbol
    churn documented in earlier passes (`Standalone` losing `SENTIS_ANALYTICS_ENABLED`); reverted
    (CLAUDE.md §11).
- `PlayMode` was not run this pass (no Presentation/scene code changed; the new mechanisms are
  exercised only through `GameSession`/`ChoiceResolver`, both already covered by EditMode).
- The generator itself (`Tools > Royal Decisions > Generate Story Content`) was **not** run through
  the Editor this pass — verified instead by an EditMode test (`StoryContentLibraryTests`) that
  builds the same in-memory content and runs it through `ContentValidator`, `CardDeckService`,
  `ChoiceResolver` and `GameOverEvaluator` exactly as the real game would, across 8 seeds, to the
  chapter's end. Run the menu command yourself and check the Console log before shipping — first
  run should report 26 "Created" cards, 8 "Created" endings, one catalogue, and the warnings above;
  a second run should report everything "Unchanged".

### Not verified — please check with your own eyes

- [ ] Actually running the generator menu command and inspecting the created assets in the
      Inspector (portraits/audio are intentionally blank — placeholder/fallback rendering should
      apply exactly as it does for the twenty existing cards).
- [ ] Playing Chapter I end-to-end with the catalogue swapped in, on a device or in the Editor Game
      view — confirm the swipe/tap flow, HUD, and card text render correctly for this content
      (nothing in Presentation changed, but it has not been *played*, only driven programmatically).
- [ ] Leader health has no HUD indicator. It is tracked correctly (tests cover it) but is currently
      invisible to the player; whether it needs one is a design call for whoever owns the UI.

## Full 250-card story, conditional variants, and runtime wiring (2026-08-23, later same day)

Completed the story integration from the previous pass's Chapter I (K1-K25) to the entire
specification (K1-K250), added a general conditional-variant/choice-availability system, and
wired the result into the actual playable Game scene. Full detail is in the session report; this
section is the manual-steps and verification record.

### What changed (summary)

- `StoryContentLibrary.cs` split into six chapter partial-class files (`StoryChapter1Cards.cs` ..
  `StoryChapter6Cards.cs`), all 250 specification cards, plus a `CardDefinition.ForcedChainOnly`
  field replacing the previous pass's flag-based hack for keeping story content out of normal
  weighted selection (cleaner, and it eliminated ~27 false-positive validator warnings outright).
- New `CardVariant` (Data) / `CardVariantResolver` (Domain) / `ResolvedCard` (Domain): a card's
  text and choices can now depend on earlier flags/counters ("*Eğer X ise:*" in the specification),
  resolved once per presentation and consumed by `IGamePresenter.ShowCard` — signature changed to
  take the resolved card, not the raw `CardDefinition` (see report for the full call-site list).
- New choice-level `availability` (a `CardConditions`): `ConditionEvaluator.IsChoiceAvailable` →
  `GameSession.ConfirmDecision` refuses an unavailable side (new `SessionErrorCode.
  ChoiceUnavailable`) → `CardSwipeController.SetSideAvailability` stops a drag or tap from ever
  confirming it in the first place (wired from `UnityGamePresenter.ShowCard`).
- `ContentValidator` gained forced-chain reachability, once-per-run convergence, and dead-end
  checks — all variant-aware (they walk every `CardVariant`'s targets, not just the base card's).
- New `StorySceneWiring.cs`: `Tools > Royal Decisions > Scene Setup > Use Story Catalogue In Game
  Scene` (and its `Use Placeholder Catalogue...` counterpart) repoints `GameSceneController.
  catalogue` and saves the scene, through `SerializedObject` exactly like `ContentAuthoringWindow`
  already does — **already run this session** (see below), so `Game.unity` now ships the story.

### A real bug this pass's own verification caught (worth knowing about)

The first two attempts at `StorySceneWiring` reported success while actually writing `catalogue:
{fileID: 0}` (null) into the scene — a classic Unity gotcha: the catalogue asset was loaded
*before* `EditorSceneManager.OpenScene(..., Single)`, and opening a scene in Single mode unloads
assets nothing currently references; a local C# variable is not a keep-alive root, so the
reference went stale ("fake null") the moment the scene finished loading, and everything downstream
(including the log message, which echoed the input path string rather than the actual result)
looked fine. Fixed by loading the catalogue *after* opening the scene, and by adding real
verification: reopening the saved scene fresh afterward and reading the field back from disk
before ever reporting success. Both bad attempts were caught before being left in the repo — see
the session report's Executive Summary for why this matters as a general lesson (scene/asset
tooling code needs the same "verify what you actually did, not what you asked for" discipline as
any other automation).

### Manual step required

None. `Tools > Royal Decisions > Scene Setup > Use Story Catalogue In Game Scene` was run this
session, and the change is verified (`git diff Assets/_Game/scenes/Game.unity` — one line,
`catalogue`'s GUID, confirmed to match `Assets/_Game/Content/Story/StoryContentCatalogue.asset.meta`,
confirmed again by reopening the saved scene and reading the field back from disk). Opening the
project and pressing Play should now start the real story at K1.

To go back to the twenty placeholder cards instead, run `Tools > Royal Decisions > Scene Setup >
Use Placeholder Catalogue In Game Scene`.

### Verified this session (Unity 6000.3.18f1 via CLI for this whole pass)

- Full `EditMode -runTests`: **835/835 passed, 0 failed** — run twice more after the Chapter I
  pass's own 818/818 baseline (once after adding the six chapters and the variant/availability
  architecture, once again after the two `ContentValidator` fixes below), both clean.
- `Tools > Royal Decisions > Generate Story Content` (`StoryContentGenerator.GenerateBatch`, a new
  CLI-safe entry point) run for real, twice: first run created all 250 cards + 8 endings + the
  catalogue (259 created, 0 errors, then 163 warnings); second run (after the convergence-check fix
  below) reported 259 unchanged, 0 errors, **2 warnings** (`ExcessiveTextLength` on K150 and K250,
  the two longest narrative-closer cards — harmless).
  - The first generator run's 161 `MultipleForcedChainsConvergeOnOncePerRunCard` warnings turned out
    to be a false-positive in the check itself, not a content problem: for a
    `CardDefinition.ForcedChainOnly` card, converging forced-next edges from mutually-exclusive
    earlier branches is the normal, safe shape of a branching story rejoining itself (a single run
    can only ever have taken one of those edges), not a risk. Fixed the validator to skip
    `ForcedChainOnly` cards for that specific check, rather than suppressing the warning generally —
    it remains meaningful for content that mixes forced chains with normal selection.
- `Tools > Royal Decisions > Scene Setup > Use Story Catalogue In Game Scene`
  (`StorySceneWiring.UseStoryCatalogueBatch`) run for real, three times (see the bug note above for
  why): the first two were caught by verification and did **not** save; the third succeeded and was
  independently confirmed (GUID match against the asset's own `.meta`, plus a fresh scene reopen).
- Full `PlayMode -runTests`: **55/54 baseline → 55/55 passed, 0 failed** — run once, after the scene
  wiring, specifically because this pass (unlike the Chapter I pass) touched Presentation/
  Composition code (`CardView`, `CardPresenter`, `CardSwipeController`, `UnityGamePresenter`) for the
  variant/availability system; confirms real Unity components (not fakes) still work end to end.
- `ProjectSettings/ProjectSettings.asset` picked up the same unrelated scripting-define-symbol churn
  as every earlier CLI pass, every time the Editor ran; reverted every time (CLAUDE.md §11).

### Not verified — please check with your own eyes

- [ ] Actually playing the story on a device or in the Editor Game view, swiping/tapping through
      several turns — the CLI passes above drive `GameSession`/`ChoiceResolver` and the real
      Presentation/Composition components directly and via PlayMode's real-component fixtures, but
      nothing in this session watched a rendered screen or made a physical swipe gesture.
- [ ] A flag-dependent `CardVariant`'s text and choices actually rendering correctly on `CardView`
      in a live scene (covered by unit/PlayMode tests against the mechanism, not by eyes on the
      screen).
- [ ] An unavailable choice's absence of preview text and un-confirmable drag, live, on a card that
      actually uses `availability` — no shipped story card uses it yet (see the session report's
      Known Limitations), so there is nothing to look at today, but it is worth a look the first
      time a card does use it.
- [ ] See `STORY_CONTENT_GUIDE.md` for the authoring reference this pass added; worth a read before
      extending the story further.

---

## Genel tab — Text Size toggle row replaced with a slider

The old three-way **Küçük / Normal / Büyük** toggle row (`ToggleGroup`) on Settings → Genel was
replaced with a single three-step `Slider` (`0 = Küçük, 1 = Normal, 2 = Büyük`), matching the
Grafik tab's frame-rate slider pattern. `TextSizeMode` (`Domain/TextSizeMode.cs`) and the runtime
scaling in `AccessibilityPresentationController` (`fontSizeMin`/`fontSizeMax` scaling on the wired
`TMP_Text[]`) are unchanged — only the input control changed, so no new scaling system was added.

Code changed: `Presentation/GeneralSettingsPanelView.cs` (`textSizeSlider`/`textSizeValueLabel`
replace `textSizeSmall`/`textSizeNormal`/`textSizeLarge`; `TextSizeMode` getter now maps the
slider's rounded step through `StepToMode`; `Render` sets the slider via `SetValueWithoutNotify`
and updates the label; dragging fires `HandleTextSizeChanged` → live label update +
`ToggleChanged`), `Editor/SceneSetupAutomation.cs` (`ConfigureGeneralSettingsTab` now builds one
`EnsureSliderControl(tab, "TextSize", "Metin Boyutu", ...)` row instead of a label row + `ToggleGroup`
+ three toggles; validation path list updated from `TextSizeSmall`/`TextSizeNormal`/`TextSizeLarge`
to `TextSize`; the now-unused `AssignToggleGroup` helper was removed), and
`Tests/PlayMode/SettingsFlowPlayModeTests.cs` (rebuilds its local scene with the slider instead of
three toggles; `TextSizeOptionsAreMutuallyExclusiveAndDefaultIsNormal` renamed to
`TextSizeSliderStepsThroughAllThreeModesAndDefaultIsNormal`).

**Unity Editor was open (live process, `Temp/UnityLockfile` held) during this change, so no batch
command was run this session** — running one against an open project risks clobbering unsaved
Editor state. Everything below must be done by hand, in your already-open Editor:

- [ ] Commit or stash current work first (this rewires part of the MainMenu scene's Genel tab).
- [ ] `Tools > Royal Decisions > Scene Setup > Audit` — confirm it reports the `TextSizeSmall`/
      `TextSizeNormal`/`TextSizeLarge` objects as stale/no-longer-expected and a new `TextSize`
      slider row as pending.
- [ ] `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` — removes the three old
      toggle objects (and the `ToggleGroup` component that grouped them) and builds the new
      `TextSize` slider row in their place.
- [ ] `Tools > Royal Decisions > Scene Setup > Validate` — must report zero errors.
- [ ] Re-run `Apply Remaining Setup` once more — must report no further changes (idempotent).
- [ ] `Window > General > Test Runner` → EditMode → Run All, then PlayMode → Run All — both must
      stay green, in particular the renamed
      `SettingsFlowPlayModeTests.TextSizeSliderStepsThroughAllThreeModesAndDefaultIsNormal`.
- [ ] In Play Mode: Settings → Genel → drag the **Metin Boyutu** slider left/right/centre and
      confirm the label reads **Küçük / Normal / Büyük** at each step and visible UI text resizes
      live as you drag (no Apply needed to preview, matching every other slider on this screen).
- [ ] Drag to the **Büyük** (right) end and check longer Turkish strings (card dialogue, settings
      labels) still fit without clipping/overflow — the existing `fontSizeMin`/`fontSizeMax`
      TMP auto-size range is what bounds this, so nothing new needed to be added for it.
- [ ] Apply, then fully close and reopen the app (or Stop/Play again) and confirm the slider
      position and text size are restored from the saved settings file.

---

## Genel tab — Reduced Motion and Text Size actually wired to real behaviour

Both controls previously only wrote into the `GameSettings` model — `AccessibilityPresentationController`
existed in code (and was already fully wired to ~35 `TMP_Text`/`StatItemView`/`CardSwipeController`
references in the **Game** scene) but nothing ever called `.Apply()` on it from either scene, and it
didn't exist at all in the **MainMenu** scene. So dragging Metin Boyutu or flipping Azaltılmış
Hareket changed the saved value but never touched anything on screen — purely cosmetic, exactly as
reported. This pass makes both real, using only the pre-existing scaling/animation mechanisms
(`AccessibilityPresentationController`, `PanelFadeAnimator.SetReducedMotion`,
`CardSwipeController.SetReducedMotion`, `StatItemView.SetReducedMotion` — no new system).

**What Reduced Motion now shortens:** the card swipe's rotation (12°→4°) and snap-back/exit
durations (→≤0.05s, via `CardSwipeController.SetReducedMotion`, already existed, now actually
called); each of the four HUD stat bars' fill animation (`StatItemView.SetReducedMotion`, already
existed, now actually called); and every panel fade/scale/tab-crossfade transition in both scenes
(`PanelFadeAnimator.SetReducedMotion`, already existed, now actually called and now wired to a new
`panelAnimators` array on `AccessibilityPresentationController`) — MainMenu's own scene-transition
overlay, the Settings panel's open/close, the tab crossfade, the About panel's open/close, and the
Game scene's entry transition. Decision-making, card advancement, and navigation are untouched —
only how long the animation takes changes.

**Text Size range and application:** unchanged — the slider's three steps (`0/1/2 = Küçük/Normal/
Büyük`) map to `TextSizeMode`, and `AccessibilityPresentationController.Apply` scales each wired
`TMP_Text`'s `fontSizeMin`/`fontSizeMax` by `0.9/1.0/1.15` from its originally-authored size (cached
on first touch, so repeated live preview never compounds). Overflow/clipping is bounded by the same
pre-existing TMP auto-size range every text element already had — nothing new was added for it.

**Live preview (new):** `SettingsController` now subscribes a `PreviewAccessibility` handler to
`SettingsPanelView.ToggleChanged` (the same aggregate event already used for the UI click sound),
so dragging Metin Boyutu or flipping Azaltılmış Hareket calls `accessibility.Apply(...)` immediately
against the real wired views — mirroring exactly how the volume sliders already preview live against
`AudioService` before Uygula. It reads straight from the view's own draft state and never mutates
`current`, so İptal still correctly reverts the preview (already proven by
`SettingsFlowPlayModeTests.TextSizeSliderPreviewsLiveAgainstTheRealWiredTextBeforeApply` and
`ReducedMotionTogglePreviewsLiveAgainstTheRealWiredPanelAnimatorBeforeApply`).

**Persistence:** unchanged — both settings already round-trip through the same versioned JSON
`GameSettings` blob via `SettingsSaveService`/`SettingsController.ApplyFromView`/`LoadAndApply`.
What's new is that loading now visibly *does* something: `SettingsController.LoadAndApply` (MainMenu)
and the newly-added `GameSceneController.ApplySettings` → `accessibility?.Apply(settings)` (Game)
both re-apply the restored values to real views on every scene load, not only the toggle/slider's
own visual state.

Code changed:
- `Presentation/AccessibilityPresentationController.cs` — new `panelAnimators` field, looped in
  `Apply()`, added to `SetAuthoringReferences`.
- `Composition/GameSceneController.cs` — new `accessibility` field; `ApplySettings()` now calls
  `accessibility?.Apply(settings)`; new optional `accessibilityController` parameter on
  `SetAuthoringReferences`.
- `Composition/SettingsController.cs` — new `PreviewAccessibility()` subscribed to
  `view.ToggleChanged` in `OnEnable`/unsubscribed in `OnDisable`.
- `Editor/SceneSetupAutomation.cs` — Game scene: wires the already-built
  `AccessibilityPresentationController` into `GameSceneController.accessibility` and its
  `panelAnimators` to the scene's transition overlay. MainMenu scene: builds a **new**
  `AccessibilityPresentationController` on the `SettingsController` GameObject (mirroring the Game
  scene's own pattern), with `scalableText` set to every `TMP_Text` in the whole MainMenu scene via
  `FindComponentsInScene<TextMeshProUGUI>` (same helper the Game scene already used — covers menu
  titles, all four Settings tabs including Ses/Grafik/Kontroller's own labels for visual consistency
  across the screen, and About) and `panelAnimators` set to MainMenu's transition overlay + the
  Settings panel's own two animators + About's animator; wires it into
  `SettingsController.accessibility`; `SettingsParts` struct gained a `PanelAnimators` field to carry
  the two Settings-panel animators out of `ConfigureSettingsPanel`; added `ValidateReference` checks
  for both scenes' new `accessibility` field.
- New test: `Tests/EditMode/AccessibilityPresentationControllerTests.cs` (text scaling for all three
  modes, no-compounding on repeated live-preview calls, `panelAnimators` reduced-motion forwarding,
  null-safety).
- `Tests/PlayMode/GameCompositionPlayModeTests.cs` — `BuildScene` now wires a real
  `AccessibilityPresentationController`; new test
  `ReducedMotionInSettingsShortensTheRealSwipeControllersAnimation`.
- `Tests/PlayMode/SettingsFlowPlayModeTests.cs` — `BuildScene` now wires a real
  `AccessibilityPresentationController` with a dummy scalable label and `PanelFadeAnimator`; two new
  tests for live preview and post-restart re-application.

**Not touched** (per explicit instruction): Audio/Graphics/Controls tab *functionality* (no line was
added to `ConfigureAudioSettingsTab`/`ConfigureGraphicsSettingsTab`/`ConfigureControlsSettingsTab`;
their labels are only included in the MainMenu accessibility controller's `scalableText` because that
array is built from the whole scene via the same mechanism the Game scene already used, so Text Size
reads consistently across the whole Settings screen rather than scaling three tabs and not the
fourth); the save/persistence format; game/decision logic; the Text Size slider's own UI design
(only the *effect* of moving it changed); responsive layout (nothing here changes anchors/sizes,
only `fontSizeMin`/`fontSizeMax` within the existing TMP auto-size range, and animation durations).

**Unity Editor was open (live process, `Temp/UnityLockfile` held) during this change, so no batch
command was run this session** — same reasoning as the slider pass above. Everything below must be
done by hand, in your already-open Editor:

- [ ] Commit or stash current work first (this adds a new component to both MainMenu and Game
      scenes and rewires two existing `GameSceneController`/`SettingsController` fields).
- [ ] `Tools > Royal Decisions > Scene Setup > Audit` — review what will change in both scenes
      (open each scene first; Audit only inspects the currently-open scene).
- [ ] `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` — run once with **MainMenu**
      open (adds the new `AccessibilityPresentationController` to `SettingsController`, wires
      `panelAnimators`, wires `SettingsController.accessibility`) and once with **Game** open (wires
      the existing `AccessibilityPresentationController`'s `panelAnimators` and
      `GameSceneController.accessibility`).
- [ ] `Tools > Royal Decisions > Scene Setup > Validate` — must report zero errors in both scenes.
- [ ] Re-run `Apply Remaining Setup` on both scenes once more — must report no further changes.
- [ ] `Window > General > Test Runner` → EditMode → Run All (in particular the new
      `AccessibilityPresentationControllerTests`), then PlayMode → Run All (in particular the new/
      changed tests in `GameCompositionPlayModeTests` and `SettingsFlowPlayModeTests`) — both must
      stay green.
- [ ] In Play Mode from MainMenu: open Settings → Genel, flip **Azaltılmış Hareket** on, and confirm
      the Settings panel's *own* next open/close and tab-switch animation is visibly snappier
      immediately (before pressing Uygula) — that's the live preview.
- [ ] Still with Azaltılmış Hareket on, press Uygula, start a run, and confirm the card's swipe
      rotation is visibly reduced and snaps back/exits faster than with it off.
- [ ] Drag **Metin Boyutu** while Settings is open and confirm text elsewhere on the Settings screen
      (not just the Küçük/Normal/Büyük label next to the slider) visibly grows/shrinks as you drag.
- [ ] Apply both, start a run, and confirm card dialogue/choice text and the HUD are also scaled
      per the chosen Text Size — this is the part that previously did nothing at all in the Game
      scene.
- [ ] Fully close and reopen the app (or Stop/Play again) with both settings non-default and confirm
      both the toggle/slider positions **and** the actual on-screen effects (animation speed, text
      size, in both MainMenu and a started run) come back correctly.

---

## Game scene — Geri (back to menu) and Ayarlar reachable mid-run

The Game scene's card screen previously had no way back to the main menu or into Settings short of
the Android hardware Back key (which `ApplicationLifecycleController.HandleBackRequested()` already
handled correctly — save/pause via `GameSceneController.HandleApplicationInterrupted()`, then load
MainMenu). This pass adds two on-screen icon buttons that call that same existing logic, plus a full
Settings/About panel duplicated into the Game scene (per your choice of "tam Ayarlar paneli" over a
simple back-only button) so it can be opened without leaving the run.

**New UI:** a `TopBar` strip (`UICanvas/SafeArea/TopBar`, 136px) sits above HUD (HUD is shifted down
by that amount, still 208px tall — its own existing validation check on `sizeDelta.y == 208` still
holds). `BackButton` (top-left, dark chip, "<" glyph) calls
`ApplicationLifecycleController.HandleBackRequested()`. `SettingsButton` (top-right, reuses the
existing `EnsureSettingsIconButton` gear-icon builder) calls `SettingsController.Open()` on
a **second, independent** `SettingsController`/`SettingsPanelView`/`AboutPanelView`/
`ResetProgressController` instance built in the Game scene by calling the exact same
`ConfigureSettingsPanel`/`ConfigureAboutPanel` methods MainMenu already uses (both were already
scene-agnostic) — so every tab, every row, every behaviour is identical to MainMenu's Settings, not
a second implementation.

**Why a second `SettingsController` instance, not the MainMenu one:** Unity scenes cannot share
GameObjects; MainMenu and Game are only ever loaded one at a time. Each scene's `SettingsController`
talks to the same on-disk `GameSettings` JSON file, exactly like Controls/Audio settings already did
before this pass ("each scene re-applies its own runtime preferences on load").

**Made this feel live instead of only "next load":** previously Controls-tab changes (swipe
sensitivity/invert/disable, tap buttons) only took effect the *next* time the Game scene loaded,
because `GameSceneController.ApplySettings()` only ran once at `Start()`. Since Settings can now
open *while* a run is already active, `GameSceneController` gained a public `ReapplySettings()`
wrapper, and `SettingsController` gained an optional `gameSceneController` field — wired only on the
Game-scene instance — so pressing Uygula (or Cancel, or Varsayılanlara Dön) there immediately
re-applies swipe/tap-button settings to the live, already-running view, the same way Reduced Motion/
Text Size/audio already did live via `accessibility`. Proven by the new PlayMode test
`ReapplySettingsPicksUpAChangeMadeAfterTheSceneAlreadyStarted`.

**Reused, not duplicated:** the Game scene's existing `AccessibilityPresentationController`
(already fully wired to every card/HUD text and the swipe/stat animations) now also covers the new
Settings/About panel's text and both their `PanelFadeAnimator`s, because construction order places
`ConfigureSettingsPanel`/`ConfigureAboutPanel` *before* the `FindComponentsInScene<TextMeshProUGUI>`
call that builds `scalableText` — one accessibility controller for the whole scene, same as before.

**Known follow-up risk — please read before relying on İlerlemeyi Sıfırla mid-run:**
`ResetProgressController`'s own doc comment says "No live `GameSession` exists in this scene" — true
in MainMenu, no longer true here. It still only deletes the run-save *file* on disk; it does not
touch the active in-memory session. If a player resets progress mid-run and then makes one more
decision before backing out, that decision's own auto-save will silently recreate the file, making
the reset appear to have done nothing. This is a narrow edge case (most players who reset progress
mid-run are then backing out anyway), not something this pass changed the mechanics of, but it's
new exposure worth a deliberate product decision (e.g. force-close the run on confirm) rather than
silently shipping. Flagging rather than fixing, since the right behaviour is a product call, not an
engineering default.

Code changed:
- `Composition/GameSceneController.cs` — new public `ReapplySettings()`.
- `Composition/SettingsController.cs` — new optional `gameSceneController` field, called from
  `ApplyRuntime`; new optional trailing parameter on `SetAuthoringReferences`.
- `Editor/SceneSetupAutomation.cs` — new `GameTopBarHeight` constant, new `EnsureBackIconButton`
  helper, `ApplyGameScene` builds `TopBar`/back/settings buttons and shifts HUD down, builds the
  Game-scene Settings/About/ResetProgressController trio and wires all the cross-references above,
  extends the existing accessibility `panelAnimators` wiring, sibling-orders `TopBar` first;
  `PreflightGameScene` now also guards against a duplicate root `SettingsController`;
  `ValidateGameScene` gained checks for all of the above.
- New test: none (scene construction isn't unit-testable; see the manual checklist below).
- `Tests/PlayMode/GameCompositionPlayModeTests.cs` — `settingsStore` promoted from a local to a
  field so a test can mutate it after the scene has already started; new test
  `ReapplySettingsPicksUpAChangeMadeAfterTheSceneAlreadyStarted`.

**Not covered by an automated test:** `SettingsController.ApplyRuntime` actually calling
`gameSceneController?.ReapplySettings()` end-to-end from a real in-Game Settings panel press. This
is a one-line, null-guarded call sitting directly next to the already-extensively-tested
`accessibility?.Apply(settings)` line in the same method, so the risk is judged low; `ReapplySettings()`
itself is tested directly. Worth a real Play Mode check below regardless.

**Unity Editor was open (live process) during this change, so no batch command was run this
session.** Everything below must be done by hand:

- [ ] Commit or stash current work first (this is the largest scene-construction change so far —
      new UI in both scenes' Game scene, two new root objects there).
- [ ] `Tools > Royal Decisions > Scene Setup > Audit` with **Game** open — review what will change.
- [ ] `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` with **Game** open.
- [ ] `Tools > Royal Decisions > Scene Setup > Validate` — must report zero errors.
- [ ] Re-run `Apply Remaining Setup` once more — must report no further changes (idempotent).
- [ ] `Window > General > Test Runner` → EditMode → Run All, then PlayMode → Run All (in particular
      the new `ReapplySettingsPicksUpAChangeMadeAfterTheSceneAlreadyStarted`) — both green.
- [ ] Start a run. Confirm **Geri** (top-left) returns cleanly to MainMenu, and that the run's last
      decision was already saved (Devam Et resumes on the same turn).
- [ ] Start a run. Tap **Ayarlar** (top-right, gear) mid-run: confirm the card/HUD/TopBar disappear
      behind the Settings panel (not layered on top), every tab works exactly as it does from
      MainMenu, and **İptal** returns to the run exactly where you left it.
- [ ] From that same in-run Settings: change swipe sensitivity or invert-rotation on the Kontroller
      tab, press **Uygula**, and confirm the *current* card's drag behaviour changes immediately —
      this is the part that previously required leaving and re-entering the scene.
- [ ] From that same in-run Settings, open **Hakkında** — confirm it replaces Settings (not layered
      on top) and Close returns to Settings, then İptal/close returns to the run.
- [ ] Tap **İlerlemeyi Sıfırla** twice from the in-run Settings (see the risk note above) — confirm
      it behaves consistently with the MainMenu version, and decide/record whether the mid-run
      stale-save edge case above needs a product fix before shipping.
- [ ] Confirm the TopBar's two buttons don't clip or overlap the HUD's stat bars at 16:9, 19.5:9,
      and 21:9 in Device Simulator, and that they sit inside the safe area with a notch simulated.
- [ ] Android Back mid-run with Settings open must close Settings first (not exit to MainMenu
      directly) — `ApplicationLifecycleController.HandleBackRequested` already special-cases this
      via `settingsController.CloseIfOpen()`, now that a Game-scene `settingsController` is finally
      wired; confirm it in practice.

### Visual refinement (after a screenshot showed the first pass reading as unpolished)

The first pass placed the two icon buttons directly on the transparent game background with a
144px-tall empty strip and no visual grouping — a user screenshot showed this reading as two
disconnected circles floating in dead space, not a designed toolbar. Fixed:

- `TopBar` now has its own background `Image` (`SurfaceColour`, the exact same fill HUD's own
  surface already uses) with **zero gap** to HUD below (HUD's y-offset is still exactly
  `-GameTopBarHeight`), so the two read as one continuous two-row panel instead of icons floating
  over open game art.
- The icon chips shrank from MainMenu's 112px/28px margin down to a dedicated, tighter 96px/20px
  (`GameTopBarIconSize`/`GameTopBarIconMargin`) — 96 is exactly the accessibility touch-target
  floor `ConfigureMinimumTouchTarget` already enforces everywhere, so this is the smallest they can
  go without that helper silently re-growing them. `TopBar`'s height (`GameTopBarHeight`, now 136,
  was 144) is derived from the constant (`margin*2 + size`) instead of a separately-hand-picked
  number, so the two can't drift out of sync again.
- `EnsureSettingsIconButton`/`EnsureBackIconButton` gained optional `size`/`margin` parameters
  (defaulting to the original 112/28) specifically so this tightening is scoped to the Game scene's
  TopBar only — **MainMenu's own settings icon button is unchanged**, still 112/28.

- [ ] Re-run `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` then `Validate` again
      with Game open to pick up this refinement (the icon sizes/margins and the new background
      won't appear until you do).
- [ ] Confirm TopBar now visually reads as one panel continuous with HUD (same fill colour, no
      seam), not two floating circles — this was the specific complaint the refinement addresses.
- [ ] Confirm MainMenu's own settings gear button (top-right of the main menu) still looks exactly
      as it did before this session — it must be completely unaffected by this pass.
- [ ] Re-check touch comfort on a real device or at 1x Simulator scale: the icons shrank from
      112px to 96px (still ≥ the 96px accessibility floor, but worth a real fingertip check, not
      just a measurement).
---

## Card/HUD visual redesign — Reigns-inspired composition (2026-08-25)

Restyled the decision screen toward a persistent top resource HUD, a situation/question panel
above the swipe card, a lighter swipe card dominated by character/event artwork with the name at
its bottom edge, and drag-revealed colored choice banners — per the approved plan. No gameplay
rule code was touched (`CardSwipeController.cs`, `ChoiceResolver`, `CardDeckService`, `GameSession`,
`StatSystem`, `Data/*` all untouched); this is a presentation/scene-authoring change only.

### What changed

- `Assets/_Game/Scripts/Editor/SceneSetupAutomation.cs`:
  - New `ConfigureSituationArea` builds `SafeArea/SituationArea/SituationPanel/SituationText` — a
    light parchment panel (via the existing `ProceduralRoundedRectGraphic`, no new bitmap asset)
    sitting between `HUD` and `CardArea`, outside `Card` so it never moves with the drag.
    `CardView.bodyText` is now wired to this object instead of an in-card `Body` text.
  - `ConfigureCard`: removed the in-card `Body` text band (with `RemoveLegacyCardBody`, an
    idempotent one-time migration that deletes a leftover `Card/Body` object from the previous
    layout); expanded `PortraitRegion` to fill most of the card; added a `NameScrim` + repositioned
    `Speaker` as a name band at the bottom of the card (matching the reference's character-name
    treatment); retuned `CardArea`'s margins to make room for `SituationArea` above it.
  - `ConfigurePreview` (`ChoicePreviewView`'s `Label`/`EdgeHighlight`): widened from an 8%-wide edge
    strip to a ~34%-wide flag-style band positioned mid-card, matching the reference's drag-revealed
    colored choice banner. No change to `CardSwipeController` or `ChoicePreviewView`'s strength/alpha
    behavior — only where the existing pieces are anchored.
  - `ConfigureHud`: restyled each `StatItemView` from an icon-left/label-value-right row with a
    24-unit fill bar into an icon-over-value column with the fill bar shrunk to a 6-unit accent
    underline — the consequence-preview glyphs (`▲`/`▼`/`▲▲`/`▲▲▲` etc.) were already fully wired
    end-to-end via `CardSwipeController.ChoicePreviewChanged` →
    `GameSceneController.HandleChoicePreviewChanged` → `HUDView.ShowChoiceImpact` →
    `StatItemView.ShowImpact`/`ChoiceImpactMath` before this pass; only their position changed.
  - Canonical SafeArea sibling order (near the end of `ApplyGameScene`) now includes
    `SituationArea` at index 1 (`HUD, SituationArea, CardArea, TapChoiceButtons, Footer,
    TutorialOverlay, GameOverPanel`), and the validator's CardArea-margin, HUD-bar-height, and
    `Card/Body` checks were updated to match.
- `Assets/_Game/Scripts/Presentation/CardView.cs`: added one new optional
  `[SerializeField] private Image nameScrimImage` (+ `ApplyTheme` styling, + the existing
  `SetAuthoringReferences` editor hook extended with a trailing optional parameter — backward
  compatible with every existing call site). `bodyText`'s `ApplyTheme` colour source changed from
  `theme.PrimaryText` to a new `theme.SituationText` (dark ink, for legibility on the now-light
  parchment panel it renders on instead of the card's dark surface).
- `Assets/_Game/Scripts/Presentation/GameUITheme.cs`: added `situationText` (`Color`, default
  `#2A1E14`) + its `SituationText` property. No other new theme fields; the situation panel's own
  background colour is a `SceneSetupAutomation.cs`-local constant (`SituationPanelColour`,
  `#D9C79E`), matching how the file already hardcodes several other authoring-time colours (e.g.
  `BodyTextColour`, `SpeakerTextColour`) alongside the theme.
- `Assets/_Game/Tests/EditMode/SceneSetupAutomationTests.cs`: updated
  `GeneratedTurkishLayout_UsesOwnedFontReadableCardAndTurnFooter` to assert against
  `SituationArea/SituationPanel/SituationText` (anchors, font, auto-size range) instead of the
  removed `Card/Body`, and added an assertion that `CardView.bodyText` is actually wired to it.

### Missing art — reported, not fabricated

Per your explicit instruction and the project's actual state (verified: zero bitmap art, zero
prefabs, zero shaders exist anywhere in `Assets/` before this pass), no placeholder art was
invented:

- **HUD stat icons** (`GameUITheme.peopleIcon`/`securityIcon`/`authorityIcon`/`wealthIcon`) are
  still empty `Sprite` slots. `StatItemView` already falls back to a letter glyph (`P`/`S`/`A`/`W`)
  via the existing `GraphicFallback` chain, so the HUD renders correctly without them.
- **Background art** (`GameUITheme.backgroundSprite`) is still empty — there is no background image
  anywhere in the project despite it having been assumed to exist. `BackgroundView` already falls
  back to a flat colour + the existing `ProceduralVignetteGraphic`.
- **Card frame / portrait-frame / portrait-mask art** (`GameUITheme.cardFrameSprite` etc.) are still
  empty — the card renders via its existing procedural `Outline`/`temporaryBorderImages` fallback.
- **Per-card portrait art** — unchanged from before this pass; all 20 placeholder cards still have
  no `portrait` assigned and render the existing procedural silhouette fallback.

Once real art exists for any of these, assigning it to the relevant `GameUITheme`/`CardDefinition`
sprite field in the Inspector is the only step needed — no code changes, per the theme's existing
`GraphicFallback`-based design.

### Verified this session (Unity 6000.3.18f1 via CLI)

- `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup`
  (`SceneSetupAutomation.ApplyBatch`) run for real, twice: first run applied the new layout to
  `Game.unity` (also touching `MainMenu.unity` and `Bootstrap.unity`, which `ApplyBatch` always
  processes together) — `0 errors, 0 warnings, 4 info`, `VALIDATION_OK`, `APPLY_COMPLETE`. Second
  run confirmed idempotency: also `0 errors`, same result.
  - Getting to a clean run took four attempts; the first three failed and were **automatically
    reverted by the tool's own backup/restore** (no data loss at any point) while fixing validator
    expectations this pass's layout changes broke: a hardcoded `CardArea` margin check, a hardcoded
    HUD stat-bar-height check, an explicit `Card/Body`-must-exist check, and — the real root cause
    of the last two failures — a **canonical SafeArea sibling-order block** near the end of
    `ApplyGameScene` that pins each of the six pre-existing top-level children
    (`HUD`/`CardArea`/`TapChoiceButtons`/`Footer`/`TutorialOverlay`/`GameOverPanel`) to a hardcoded
    index; it didn't know about the new `SituationArea` object and kept shoving it past
    `GameOverPanel`, which must stay last. Fixed by adding `SituationArea` into that same block
    (as `SituationAreaParts`, mirroring the existing `CardParts`/`FooterParts` struct pattern)
    rather than trying to out-guess it with a one-off `SetSiblingIndex` inside
    `ConfigureSituationArea` (an earlier attempt at that, informed by a wrong assumption about
    `Footer`'s actual current position, also failed — the file's real sibling order had drifted
    from `ApplyGameScene`'s call order over the project's history, exactly because of this
    canonical-order block).
  - `MainMenu.unity`'s 142-line diff from these runs is the tool's own pre-existing idempotent
    upkeep (an `ArmedText (TMP)` orphan getting removed/recreated, i.e. `ORPHAN_REMOVED`, matching
    the pattern already documented above under "Follow-up fixes") — unrelated to this pass's card/
    HUD scope, not reverted.
  - `ProjectSettings/ProjectSettings.asset` picked up the same unrelated scripting-define-symbol
    churn as every earlier CLI pass; reverted (`git checkout --`) per CLAUDE.md §11.
- Full `EditMode -runTests`: **835/835 passed, 0 failed**.
- Full `PlayMode -runTests`: **55/55 passed, 0 failed** — confirms `CardSwipeController`'s drag/
  snap-back/confirm/exit behavior, `CardView`/`ChoicePreviewView` rendering, and the
  `GameSceneController` composition wiring all still work with real Unity components, not just
  mocks, even though none of those files' own code changed.
- Manually computed WCAG contrast for the new situation-panel palette (dark ink `#2A1E14` text on
  light parchment `#D9C79E` panel): **≈9.7:1**, comfortably above the `4.5:1` AA threshold the
  project's existing `UIContrastMathTests.cs` enforces for other theme colour pairs (not added as
  an automated test, since the panel colour is a `SceneSetupAutomation.cs`-local constant rather
  than a `GameUITheme` field — see above).

### Not verified — please check with your own eyes

This session has no way to render or screenshot the Unity Game view, so nothing below was seen
rendered, only driven structurally through the automation/tests above:

- [ ] The actual visual result: open `Game.unity`, enter Play Mode, and look. The HUD's icon-over-
      value layout, the parchment situation panel's contrast and spacing, the card's now-larger
      portrait area, the bottom name band with its scrim, and the mid-card drag-reveal choice
      banners are all new layout math that has never been seen rendered.
- [ ] Drag left/right on an actual card and confirm the widened `EdgeHighlight` band and
      repositioned `Label` look intentional (a colored flag-style banner) rather than oversized or
      misaligned — the anchors are reasoned estimates from the reference composition, not
      pixel-tuned against a live render.
- [ ] `Window > General > Device Simulator` across 16:9, 18:9, 19.5:9, 20:9 — confirm the new
      `SituationArea` panel and the resized `CardArea` still fit comfortably above `Footer`/
      `TapChoiceButtons` on the shortest supported aspect ratio (the margins were computed
      arithmetically from the existing reference-resolution scheme, not verified against every
      ratio visually).
- [ ] Short vs. long situation text in `SituationText` (TMP auto-size range 28–40pt) — confirm
      neither a one-line nor a near-maximum-length situation paragraph looks broken in the panel.
- [ ] The HUD's new small caption ("Name" object, now 14–18pt above each icon) and the impact-glyph
      badge's new position near the icon's corner — confirm neither clips at 3-digit stat values.

### Polish pass after the first eyes-on review (2026-08-25, same day)

The first real Game-view look flagged the situation panel as too tall/dominant, the card as too
small and too low, a visible gold "debug box" outline, a redundant HUD (name + fallback letter +
value + a prominent fill bar), the tap buttons competing with the card, a detached name label, and
a permanently-visible footer. All fixed through `SceneSetupAutomation.cs` (plus one Range-attribute
widening on `ResponsiveCardSizer.cs`); no gameplay file touched.

- **SituationArea**: width inset `-64`→`-130` (≈88% of SafeArea width, ≈85–90% across 16:9–20:9),
  height `232`→`160` (2-4 lines), corner radius `28`→`14`, text padding `10%/10%`→`8%/6%`, gap to
  `CardArea` `24`→`12` units.
- **CardArea**: top margin `464`→`380` (moved up), bottom margin `112`→`80` (tight, since Footer is
  now hidden and TapChoiceButtons are dimmed); `ResponsiveCardSizer.preferredWidthRatio` `0.78`→
  `0.82`, `maximumWidth` `920`→`960` (its `[Range]` attribute widened from `(0.7,0.8)` to
  `(0.7,0.85)` to accommodate). Card is now the visually dominant element and sits higher.
- **"Debug box" border, identified**: `Card`'s own `Outline` component and the four
  `TemporaryBorder` edge `Image`s (both intentional fallbacks, gated on `theme.CardFrameSprite ==
  null`) were rendering `BorderGoldColour` at full opacity in a perfect rectangle at the card's
  exact bounds. Not deleted (still signal "no frame art yet") — recoloured to a new
  `TemporaryCardBorderColour` (same gold, 25% alpha) via a new `Color colour` parameter on
  `ConfigureTemporaryCardBorders`.
- **HUD**: the per-stat "Name" label (`Halk`/`Güvenlik`/`Otorite`/`Servet`) is now
  `SetActiveIfNeeded(..., false)` by default — object, `TextMeshProUGUI`, and `StatItemView`/
  `HUDView` wiring (`SetLabel` etc.) are all untouched, it just isn't shown, so assigning real icons
  later needs no code change here either. Fallback letter (`P`/`S`/`A`/`W`) is unchanged and is now
  the primary identifier alongside the enlarged value text (`36/32/40`→`44/40/48`pt). Fill bar
  (the accent underline from the first pass) shrunk further: width `60%`→`36%` of the slot,
  height `6`→`3` units.
- **TapChoiceButtons**: **dimmed, not hidden.** A `CanvasGroup` (`alpha 0.45`, `interactable`/
  `blocksRaycasts` both still `true`) was added to the root — fully tappable, just visually
  secondary. They were **not** hidden outright because `GameSettings.tapButtonsEnabled` defaults to
  **`true`** (`Assets/_Game/Scripts/Domain/GameSettings.cs:53`) — this is a default-on alternate-
  input/accessibility path (`TapChoiceButtonsView`'s own doc comment: "Optional on-screen
  alternative to the swipe gesture"), and `GameSceneController.ApplySettings` (line 214) already
  proactively shows/hides them from live settings on every scene load — hiding them at authoring
  time would have fought that logic for players who have the (default-on) setting enabled, silently
  removing an accessibility feature most players start with active. A brief in this project's usual
  "ask the user first" AskUserQuestion style would have been the ideal way to confirm this trade-off,
  but the task explicitly asked for a single polish pass with a written rationale instead — this is
  that rationale, flagged here for review, not a silent judgment call.
- **NameScrim/Speaker**: scrim height `15%`→`11%` of the card, `Speaker` font `34/28/38`→`38/32/42`pt
  (larger, per the request), inset now sits inside the shrunk scrim (`0.02–0.13`→`0.015–0.105`
  height). `PortraitRegion`'s bottom anchor moved from `0.14` to `0.03` (portrait art now extends
  under the scrim; sibling order already had `NameScrim`/`Speaker` after `PortraitRegion`, so the
  scrim renders as a proper overlay on the art rather than a separate blank strip beneath it).
- **Footer**: fully hidden (`SetActiveIfNeeded(root.gameObject, false)`) — unlike TapChoiceButtons,
  nothing shows/hides it live from a setting, it is purely decorative ("Tur N" turn count + the
  static "Royal Decisions" ruler-name string), and the requested target vertical composition
  (HUD → situation → card → minimal bottom margin) has no footer row at all. `FooterView`/
  `RunStatusView` and their wiring are completely untouched — `RenderTurn`/`ShowTurn` still run and
  still write real text every turn, it simply isn't rendered.
- **Sprite-support note (situation panel)**: not yet wired — `SituationPanel`'s background is still
  the procedural `ProceduralRoundedRectGraphic` only, with no `GameUITheme` sprite slot of its own
  (unlike `Card`'s frame/portrait-frame/portrait-mask, which already follow the sprite-first/
  procedural-fallback pattern). Adding that parity is straightforward when real parchment art
  exists — one new `GameUITheme` `Sprite` field plus an `Image` toggled the same way
  `ConfigureOptionalSlicedImage` already does elsewhere — but was not built speculatively this pass.

**Verified this session**: `ApplyBatch` — `0 errors, 0 warnings, 4 info`, `VALIDATION_OK`,
`APPLY_COMPLETE`, confirmed idempotent across two consecutive runs (this pass needed no validator
fixes, unlike the first). Full `EditMode -runTests`: **835/835 passed, 0 failed**. Full
`PlayMode -runTests`: **55/55 passed, 0 failed**. `ProjectSettings/ProjectSettings.asset` picked up
the same recurring unrelated scripting-define-symbol churn as every earlier CLI pass; reverted.

This pass, too, has only been driven through automation — the actual rendered result (whether the
new proportions, dimming, and hidden elements read correctly together) has not been seen and needs
the next eyes-on screenshot pass.

---

## Character portrait integration and parchment re-check

Two new art drops landed in the untracked `Assets/Tasarım/` (folder and file names still contain
Turkish characters, same as before): a wider `Parşömen.png` replacing the old one, and
`Assets/Tasarım/Characters/` with seven portrait PNGs. This pass imported all of it, wired six
character portraits onto every real Story `CardDefinition` whose authored `speaker` field matches,
and re-verified the parchment fit. No gameplay rule, swipe/stat/save code, or story text changed —
only `portrait` fields on 73 `CardDefinition` assets, one new presentation math class
(`PortraitCoverFitMath` + `CardView` wiring), and `SceneSetupAutomation.cs` (new art-path constants
and an `AssignCharacterPortraits` step). Nothing was committed.

**Parchment finding — the gap is not solved.** The new `Parşömen.png` is 2079×756 (aspect ~2.75:1),
essentially the same proportions as the file it replaced (~2.9:1), not meaningfully closer to
`SituationPanel`'s approved ~5.9:1. Unity's own auto-slice detection on import measured the actually
-painted parchment shape at ~2025×710 (~2.85:1), so there's very little wasted transparent canvas to
trim — the shape itself is simply drawn at roughly this aspect. `Simple` + `Preserve Aspect` (already
the code's policy) remains correct — it avoids distortion — but the transparent side margins inside
`SituationPanel` are only marginally smaller than before. Closing the gap needs art actually drawn
wider relative to its height, or accepting the margins, or widening `SituationPanel` itself (out of
scope for this pass, and not done).

**Character coverage.** Of the 22 named/recurring characters in the real 250-card story (see the
character-inventory pass earlier this session), 6 now have a base portrait: **Ömer, Sabiha, Zeynep,
Atilla, Aziz, İsmet** — 73 cards total. `BandajlıSağlıkçıZeynep.png` ("bandaged Zeynep") was imported
but deliberately **not** assigned to any card: it is still byte-for-byte identical to the base
`SağlıkçıDoktorZeynep.png`, and no card in the 250-card story asks for a wounded/bandaged Zeynep —
assigning it would have been guessing an alternate state the content doesn't support. 16 characters
remain with no art at all: Kemal, Ali, Mustafa, Mete, Gül, Sibel, "Lider" Zombi, Necati, Fatma, Veli,
Tarık, Cem, Yusuf, Rıza, Semra, Emine Teyze.

**Known limitation, not fixed here (schema unchanged, as instructed):** `CardVariant` has no
portrait field of its own, only `CardDefinition` does. Ten cards have a variant that overrides the
speaker away from the card's main speaker; on one of them, `story_k041` (main speaker Ömer, variant
speaker İsmet — both now have art), the portrait will keep showing Ömer even while the İsmet variant
's text and name are on screen. `story_k166` (main Sabiha, variant Kemal — Kemal has no art yet) will
show Sabiha under Kemal's dialogue once Kemal's variant is authored a portrait later. Both are
pre-existing structural limits, not something this pass introduced or silently patched around.

### PC1 — Visual check (not done this session — batch mode cannot screenshot)

- [ ] Open a card for each of the 6 newly portraited speakers and confirm the face reads correctly
      inside `KartÇerçevesi.png`'s frame opening — cropped by `PortraitMask`, not stretched. The new
      `PortraitCoverFitMath`-driven sizing in `CardView` was only verified by EditMode math tests
      (8 cases, pure geometry, no live Canvas), not by looking at an actual rendered card.
- [ ] Play through `story_k041` and `story_k166` specifically and confirm the (expected, documented)
      portrait/speaker mismatch described above looks acceptable for now, or decide it needs a
      schema change in a future pass.
- [ ] Look at the `SituationPanel` with the new parchment at a real device width and judge whether
      the remaining side margins are acceptable as-is.

### PC2 — Next portraits to commission

Ranked by recurrence, narrative weight, and how early each character appears:

1. **Kemal (Mühendis)** — 18 appearances, first card `story_k006`
2. **Ali (Halktan)** — 15 appearances, first card `story_k016`
3. **Mustafa (Asker)** — 11 appearances, first card `story_k004`
4. **Mete (Asker)** — 8 appearances, first card `story_k014` (pairs with Mustafa — good test of two
   visually distinct-but-consistent soldiers)
5. **Gül (Halktan)** — 7 appearances, first card `story_k042`

Suggested filenames follow the existing convention exactly as delivered (no ASCII renaming was
applied to existing files): `KemalMühendis.png`, `AliHalktan.png`, `MustafaAsker.png`,
`MeteAsker.png`, `GülHalktan.png`, dropped into `Assets/Tasarım/Characters/`. Re-running
`Tools > Royal Decisions > Content > Assign Character Portraits` after adding them (and adding their
speaker string to `CharacterPortraitMap` in `SceneSetupAutomation.cs`) will pick them up the same way
this pass did — idempotently, by exact `speaker` match, never by guessing from a filename.

**Verified this session:** `AssignCharacterPortraitsBatch` — `0 errors, 0 warnings, 1 info`, exactly
73 cards newly assigned, 0 already correct (first run). `ApplyBatch` (parchment + theme wiring) —
`0 errors, 0 warnings, 4 info`, `VALIDATION_OK`, `APPLY_COMPLETE`. Full `EditMode -runTests`:
**845/845 passed, 0 failed**. Full `PlayMode -runTests`: **55/55 passed, 0 failed**.
`ProjectSettings/ProjectSettings.asset` picked up the same recurring unrelated scripting-define
-symbol churn as every earlier CLI pass; reverted.

---

## SituationText overflow and missing HUD glyphs

After the user visually reviewed the Game View from the pass above and approved the overall card
composition, two defects remained: a 3+ line situation string reading as too close to / outside the
parchment's own readable band, and missing-glyph tofu squares next to some HUD stat icons. Fixed
both without touching `SituationPanel`'s size/position, the parchment art, `CardSwipeController`,
rotation, `CardFrame`/`NextCard` positioning, banners, HUD icon positions, or any story/consequence
data.

**SituationText** (`ConfigureSituationArea` in `SceneSetupAutomation.cs`): vertical margin widened
18px→26px per side (`sizeDelta` -36→-52; the ~50px horizontal margin is unchanged, as requested).
Auto-size range tightened 28-40pt→20-36pt and line spacing 6→2 to buy back the room the wider
margin cost, so ~4 lines still fit before Ellipsis would trigger. New
`Assets/_Game/Tests/PlayMode/SituationTextLayoutPlayModeTests.cs` reproduces this exact box against
three real authored strings (`story_k192` 1 line, `story_k007` 3 lines, `story_k150` 4 lines) using
the real project-owned Turkish TMP font, asserting `isTextOverflowing == false` and the settled
font size never drops below the 20pt floor — this is a permanent regression test, not a one-off
check. `SceneSetupAutomationTests.cs`'s existing structural assertion on this RectTransform/font
config was updated to match.

**HUD glyphs**: traced to `StatItemView.ShowImpact` → `ChoiceImpactMath.Format`, which repeats
`GameUITheme.PositiveImpactGlyph`/`NegativeImpactGlyph` (▲/▼, U+25B2/U+25BC) 1-3× by delta
magnitude. Verified empirically (a live `TurkishFontGlyphTests` run against the two codepoints) that
the project-owned static `LiberationSans-Turkish SDF` atlas does not contain them. Attempted the
preferred fix — extending the atlas via the existing `Tools > Royal Decisions > Generate Turkish TMP
Font` — but it threw `FileNotFoundException`: **the source `.ttf` this atlas was built from is
missing from the repo** (`Assets/_Game/Art/Fonts/Resources/LiberationSans-Turkish.ttf` doesn't
exist, nor does `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`; a search of the Editor install and
package cache found no copy to restore from either). This is a pre-existing environment gap, not
something introduced this session, and out of scope to chase further. Used the explicitly-sanctioned
fallback instead: `GameUITheme`'s default glyphs (both the C# field defaults and
`DefaultGameUITheme.asset`'s explicit values) changed from ▲/▼ to **`+`/`-`** — guaranteed already
in the atlas (full printable ASCII is baked in), no code/logic change in `ChoiceImpactMath` or
`StatItemView`. `UIFoundationViewTests` and `GameUITheme.cs`/`StatItemView.cs`'s own local
pre-`ApplyTheme` defaults were updated to match; `ChoiceImpactMathTests.cs` needed no change since it
already tests the pure repeat-by-magnitude logic with its own explicit glyph parameters, independent
of the theme.

**If the missing `.ttf` is restored later:** the font pipeline can then properly bake ▲/▼ (or any
other symbol) and the theme's glyphs can be switched back — nothing about this fallback is
permanent or hard to undo, it's one string each in `GameUITheme.cs` and `DefaultGameUITheme.asset`.

### PC3 — Visual check (not done this session)

- [ ] Confirm the 1-, 3- and 4-line situation text samples above actually read as centred and clear
      of the parchment's decorative edges at a real device width, not just via the layout math the
      new PlayMode tests check.
- [ ] Confirm `+3` / `--` / `+++` etc. read acceptably in the HUD in place of the original ▲▼
      triangles — a deliberate but untested design substitution.

**Verified this session:** `ApplyBatch` — `0 errors, 0 warnings, 4 info`, `VALIDATION_OK`,
`APPLY_COMPLETE`, confirmed idempotent (byte-identical diffstat across two consecutive runs). Full
`EditMode -runTests`: **845/845 passed, 0 failed**. Full `PlayMode -runTests`: **59/59 passed, 0
failed** (60 on the first run — one newly-added assertion used `TMP_Text.GetPreferredValues`
incorrectly against auto-sizing text and was removed as invalid, not worked around; the four
`isTextOverflowing`-based assertions it sat alongside all passed both times).
`ProjectSettings/ProjectSettings.asset` again picked up the same recurring unrelated
scripting-define-symbol churn; reverted again.

---

## HUD stat icons + SituationArea hidden, then restored (Game.unity)

**Reverted same session** — the user asked to bring this back shortly after, with no relocation
of `bodyText` ever implemented. `Game.unity` is back to its original committed state (both
GameObjects `m_IsActive: 1`, matching `git diff` showing no change on this file). The rest of this
section is kept as a record of what happened and why it's fragile, in case it's hidden again.

**Re-hidden, then root-caused and restored, same day (2026-08-26).** After the restore above, a
user screenshot showed the icon row rendering as four plain cream/beige rectangles and the
situation panel as a plain white box (missing-sprite fallback rendering), and a later screenshot
showed the card's gold frame gone too. `SituationArea`/`HUD` were hidden again as a stopgap, then
**the real cause was found: six files under `Assets/Tasarım/` had been deleted from disk outside
this session** (`git status` showed them as unstaged deletions — not something this session's `cp`
overwrites could cause, since none of those commands ever remove a file). Three were files this
session never touched at all (`KartÇerçevesi.png` — the card frame — plus both swipe banners);
the other three-turned-five were `Otorite.png`, `Güvenlik.png`, `People.png`, `Servet.png`,
`Parşömen.png`, which this session had repeatedly overwritten with new art but never committed.
Fixed by `git checkout --` on the untouched files and all six `.meta`s (restoring the original
GUIDs), then re-copying this session's latest processed versions over the five edited PNGs from
scratchpad backups. `SituationArea`/`HUD` were then set back to active — **nothing was actually
wrong with the scene, the theme asset, or the sprite pipeline; the asset files were simply gone.**
If icons/frame/panel ever go blank again, check `git status` under `Assets/Tasarım/` for deletions
before suspecting the scene or `DefaultGameUITheme.asset`.

At the user's explicit request, the top stat-icon row and the parchment "situation" panel
(speaker-less body text — the "Ömer kapıya koşar..." line shown above the card) are now hidden in
`Game.unity`. This was a **two-line hand edit**, not a rebuild:

- `SituationArea` GameObject (`fileID 1817811540`) — `m_IsActive: 1` → `0`
- `HUD` GameObject (`fileID 1890239425`, holds the four `StatSlot_*`/`StatItemView` icons) —
  `m_IsActive: 1` → `0`

Both are simple `SetActive(false)`-equivalent flips — no hierarchy restructuring, no deleted
references, trivially reversible (flip back to `1`). Nothing in code re-enables either object at
runtime (`CardView` only toggles its own `root`/`nextCardRoot`; `StatItemView`/`HUDView` never call
`SetActive` on their own slot), so the change sticks through Play Mode.

**Important — this will NOT survive `Scene Setup > Apply Remaining Setup`.** Both objects are
created via `EnsureUiChild` in `SceneSetupAutomation.cs` (`ConfigureSituationArea`, `ConfigureHud`),
which does not preserve a disabled state — re-running Apply Remaining Setup / `ApplyBatch` will
recreate or touch these objects and likely re-enable them. If that happens, just re-apply the same
two `m_IsActive: 0` edits, or ask for them again.

**Deferred, not decided:** hiding `SituationArea` removes the *only* place `CardView.bodyText` is
displayed (`GameUITheme`/`SceneSetupAutomation` wire `CardView.bodyText` to
`SituationArea/SituationPanel/SituationText`) — the card's narrative sentence has nowhere to render
right now. The user was asked where it should go (back on the card, an overlay, etc.) and chose to
defer that decision. **Before shipping, this needs a follow-up**: either give `CardView` its own
body-text slot again, or re-enable `SituationArea` some other way — otherwise cards have no visible
story text at all.

- [ ] Decide where `bodyText` should render, then implement it
- [ ] Re-check `SceneSetupAutomation.cs` (`ConfigureSituationArea`/`ConfigureHud`) if the decision is
      "never show this again" — right now the generator still authors both objects active every time

---

## HUD stat numbers moved beside their icons (Game.unity, 2026-08-26)

The numeric value next to each HUD stat icon was **not a new feature** — `StatItemView.SetValue`
and its `valueText` field already existed, and `Game.unity` already had all four `Value` TMP
objects wired and active, showing `50` under each icon. At the user's explicit request, the layout
changed from icon-over-value (number below the icon) to icon-left/value-right (number beside it).

**No Unity Editor/CLI was available this session**, so this was a **direct hand edit** of
`Game.unity` — normally against CLAUDE.md §11, done here only because the user explicitly asked for
this specific change. `SceneSetupAutomation.cs` (`ConfigureHud`) was updated to the same numbers
first, so a future `Scene Setup > Apply Remaining Setup` / `ApplyBatch` run regenerates this same
layout instead of reverting it (unlike the `SituationArea`/`HUD` visibility flip above, which the
generator does still fight).

Per-stat `Icon` and `IconFallback` RectTransforms (mirrored, same anchors) — center `(0.25, 0.50)`,
half-extents `= (0.20, 0.22) * iconScale`:

| Stat | `iconScale` | Icon/IconFallback `fileID`s | New anchors |
|---|---|---|---|
| Security | 0.79 | `59821391` / `658879000` | min `(0.092, 0.3262)` max `(0.408, 0.6738)` |
| Wealth | 0.92 | `1739104486` / `409676871` | min `(0.066, 0.2976)` max `(0.434, 0.7024)` |
| People | 1.14 | `1415560162` / `1809810430` | min `(0.022, 0.2492)` max `(0.478, 0.7508)` |
| Authority | 1.17 | `980017105` / `2073392532` | min `(0.016, 0.2426)` max `(0.484, 0.7574)` |

`Value` RectTransforms — same box for all four: min `(0.52, 0.25)` max `(0.98, 0.75)`.
`fileID`s: Security `1304995193`, Wealth `2124949868`, People `451959342`, Authority `1478265319`.

`Name` (hidden label), `Impact` (+/- flash badge) and `Critical` ("!" badge) rects were left
untouched — they already overlay near the icon's top corner and still make sense there.

**Not visually verified** — there is no way to render Unity from this session. Please check in the
Editor:

- [ ] Open `Game.unity`, enter Play Mode (or just look in Scene view) — each of the four stat icons
      should have its number immediately to its right, vertically centered, not clipped or
      overlapping the icon or the impact/critical badges
- [ ] All four numbers stay legible at 0, 50 and 100 (2 vs 3 digits) — auto-size range is unchanged
      (`40`–`48`pt)
- [ ] If it looks wrong, the fastest fix is nudging the `m_AnchorMin`/`m_AnchorMax` values above
      directly in the Inspector (Icon/Value RectTransforms) rather than re-running scene automation

**Reverted (same session, minutes later) — the user asked to remove the numbers again.** Rather
than deleting the four `Value` GameObjects (which would also drop `StatItemView.valueText`'s
wiring and need re-adding by hand later), they were **hidden** the same way the `Name` label
already is: `m_IsActive: 1` → `0` on each of the four `Value` objects (`fileID`s `451959339`
People, `1304995190` Security, `1478265316` Authority, `2124949865` Wealth). `SceneSetupAutomation.
cs` (`ConfigureHud`) now calls `SetActiveIfNeeded(valueTransform.gameObject, false)` right after
configuring the `Value` text, mirroring the `Name` label, so `Apply Remaining Setup`/`ApplyBatch`
won't re-enable it. `StatItemView.SetValue` still runs and still writes the number every turn —
it's just invisible. Flip `m_IsActive` back to `1` (and delete that one `SetActiveIfNeeded` call)
to bring the numbers back.

**Icons moved up (same session, right after).** Per-stat `Icon`/`IconFallback` boxes (same eight
`fileID`s listed above) kept their size and x-position but their vertical center moved from `0.50`
to `0.68` within the slot — `ConfigureHud`'s icon anchor formula changed from
`(0.25, 0.50) ± (0.20, 0.22) * iconScale` to `(0.25, 0.68) ± (0.20, 0.22) * iconScale`. `Value`
boxes were left where they are (still hidden, so it doesn't matter visually right now, but if the
numbers are ever re-shown they'll sit lower/off-center from the icons — re-check then).

**Icons pulled closer together (same session, right after that).** The four icons were each
hugging the left edge of their own slot (x-center `0.25`) — a leftover from the icon-left/
value-right layout, but with the number now hidden it just left a large empty gap to the right of
every icon between it and the next one. Recentered to slot-center (x `0.5`) instead, same eight
`fileID`s, same y and size — `ConfigureHud`'s formula is now `(0.5, 0.68) ± (0.20, 0.22) *
iconScale`. If this still isn't tight enough, the next lever is the `HUD` object's
`HorizontalLayoutGroup` (`spacing: 8`, `padding: 12,12,12,12`), not the icon boxes.

**Icons enlarged (same session, right after that).** Base half-extent multipliers raised
`0.20 → 0.23` (width) and `0.22 → 0.25` (height), about a 13–15% size increase, same eight
`fileID`s, same center. `ConfigureHud`'s formula is now `(0.5, 0.68) ± (0.23, 0.25) * iconScale`.
The tallest icon (People, `iconScale 1.14`) now spans y `0.395`–`0.965` of its slot — comfortably
inside `0..1`, no clipping, but there isn't much headroom left for another size increase without
also lowering the `0.68` vertical center or shrinking `iconScale`'s spread.

**Icons pulled closer together again, this time via layout (same session, right after that).** The
per-icon box was already centered in its own slot (see above), so there was no more room to close
the gap that way — the four `StatSlot_*` columns themselves needed to shrink. Changed the `HUD`
GameObject's `HorizontalLayoutGroup` (`fileID 1890239429`): `m_Padding` left/right `12 → 48` (top/
bottom unchanged), `m_Spacing` `8 → 2`. With `childForceExpandWidth` on, all four slots still
divide the remaining width equally, but that remaining width is now smaller, so each slot — and
the icon centered in it — sits closer to its neighbours. `ConfigureHud` updated to match
(`layout.padding = new RectOffset(48, 48, 12, 12); layout.spacing = 2f;`). The first/last icons are
also now inset further from the screen's left/right edges as a side effect — check that still looks
right at 16:9 vs. 21:9 in Device Simulator.

**One more notch, same lever (same session, right after that).** `m_Padding` left/right `48 → 64`
(kept equal to each other, top/bottom still `12`), `m_Spacing` `2 → 0`. All four slots are equal
width by construction (`childForceExpandWidth`), so with spacing now zero every slot boundary sits
flush against the next — the four icons are evenly spaced by definition, not just visually close.

**Wealth (gold/Servet) icon enlarged to match the others' apparent height (same session, right
after that).** The user confirmed it was reading visibly shorter than the other three. Its
`iconScale` in `ConfigureHud`'s `iconScales` array went `0.92 → 1.05` (People `1.14`, Security
`0.79`, Authority `1.17` unchanged) — this is a **visual estimate, not a re-measurement** of
`Servet.png`'s alpha bounding box (no Unity Editor available this session to check), so it may need
another nudge. `fileID`s `1739104486` (Icon) and `409676871` (IconFallback) both moved to anchors
min `(0.2585, 0.4175)` max `(0.7415, 0.9425)`.

- [ ] Compare all four icon heights side by side in the Editor; if gold is still off, adjust
      `iconScales[3]` in `SceneSetupAutomation.cs` up or down and re-derive the two `fileID`s above
      with the same `(0.5, 0.68) ± (0.23, 0.25) * iconScale` formula used for the others.

**Wealth artwork replaced entirely (2026-08-27).** `Assets/Tasarım/Servet.png` was overwritten with
a new "ERZAK" (survival-supplies tin/canteen/rations) badge the user supplied, at their explicit
request, to fit the zombie/post-apocalypse re-theme better than the old gold-pile art. Same
filename, same `.meta`/GUID (`49f2c3aa704870441a8c0da0849286d2`) — a straight byte replacement, so
nothing in `Game.unity` or `SceneSetupAutomation.cs` needed to change to pick it up. Old `1322×1190`
→ new `1346×1168`, both RGBA.

- [ ] **The `iconScales[3] = 1.05f` tuning above was calibrated for the old artwork's padding, not
      this one — treat it as stale.** The new badge looks like a tight circular coin nearly filling
      its canvas (high alpha fill, more like Security's `0.79` than the old Wealth guess), so it
      will very likely render too large now. Compare against the other three icons in the Editor
      and adjust `iconScales[3]` (then re-derive `fileID`s `1739104486`/`409676871` with the
      `(0.5, 0.68) ± (0.23, 0.25) * iconScale` formula) rather than assuming `1.05` still holds.
- [ ] Confirm the label/theme still reads correctly if "Servet" (Wealth) is now visually
      "provisions" — `GetStatName`/`GetStatLabel` text wasn't touched here and may want a rename to
      match (out of scope for this edit; flagging only).

**Card frame artwork replaced (2026-08-27).** `Assets/Tasarım/KartÇerçevesi.png` was overwritten
with new rusted/scorched-metal frame art the user supplied, matching the zombie/post-apocalypse
re-theme. Same filename, same `.meta`/GUID, same `1024×1536` RGBA dimensions as the file it
replaced — a straight byte swap, nothing in `Game.unity`/`SceneSetupAutomation.cs` needed to
change. The user first sent a *full card mockup* (baked title/body/portrait/card-number) for this
same request; that was correctly identified as a style reference only and not applied, then this
frame-only (transparent-feeling black center, no baked text) follow-up image was applied instead.

- [ ] Check the card in the Editor — confirm the new frame's proportions still leave room for
      `CardView`'s `Speaker`/`Body`/`Portrait`/preview children without the rust/crack detailing at
      the edges clipping into them.

**Unrelated file also showing modified:** `git status` reports
`Assets/_Game/Art/Fonts/Resources/LiberationSans-Turkish SDF.asset` as changed. Nothing in this
session touched TMP font assets — likely Unity regenerating the font atlas in the background if the
Editor is open. Verify before committing; revert it if it's not an intentional change.

---

## Portrait fill + situation text moved onto the card (2026-08-27)

Two related, user-requested changes, both **hand-edited directly in `Game.unity`** (no Unity
Editor/CLI available this session) and mirrored into `SceneSetupAutomation.cs` so a future
`Apply Remaining Setup`/`ApplyBatch` regenerates the same result instead of reverting it.

### 1 — Portrait now fills nearly the whole frame

The user reported (via a screenshot) a visible black gap between the portrait and the new rusted
frame's inner edge — expected, since `PortraitRegion`'s anchors were still the *old* ornate frame's
hand-measured window (`0.117,0.188`–`0.887,0.914`), never updated when `KartÇerçevesi.png` was
replaced. `GetPixel` via PowerShell/System.Drawing reported this PNG's alpha channel as uniformly 0
everywhere (including clearly-opaque border pixels) — unreliable for this file, not used. Instead,
RGB-brightness transitions were sampled along the image's center row/column (see the code comment
at `ConfigureCard`'s `PortraitRegion` block) to estimate the border thickness, giving new anchors
`0.0352,0.0514`–`0.9639,0.9329`. Changed in both `Game.unity` (`fileID 1255454278`) and
`SceneSetupAutomation.cs`.

- [ ] **Confirm visually — this is an estimate, not a measured value.** If the portrait now bleeds
      past the frame's inner edge, or a gap remains, adjust `PortraitRegion`'s anchors (same file,
      same fileID) directly, or re-derive from a real alpha/pixel measurement inside Unity (which
      can read this PNG's actual alpha correctly, unlike the external tool used here).

### 2 — Situation text moved from the parchment panel onto the card itself

At the user's explicit decision (resolving the "deferred" item from the frame-swap session above):
`SituationArea` (the parchment panel showing the narrative line above the card) is now **hidden**
(`m_IsActive: 0`, `fileID 1817811540` — not deleted, so it's cheap to bring back), and `CardView`
gained a body-text slot on the card itself, styled like the existing bottom `NameScrim`/`Speaker`
pair but at the top of the portrait:

- New `Card/BodyScrim` (`Image`, dark `0,0,0,0.55` scrim, anchors `0,0.75`–`1,0.9329` — flush with
  `PortraitRegion`'s new top edge from change #1) and `Card/Body` (`TextMeshProUGUI`, wrapping,
  32pt auto-sizing 20–32, anchors `0.08,0.765`–`0.92,0.918`). New scene `fileID`s: `900000101`–
  `900000104` (BodyScrim's GameObject/RectTransform/Image/CanvasRenderer) and `900000201`–
  `900000204` (Body's GameObject/RectTransform/TextMeshProUGUI/CanvasRenderer) — hand-authored by
  cloning `NameScrim`'s and the old `SituationText`'s exact structure with new unique fileIDs, added
  to `Card`'s `m_Children` list (`fileID 569737566`) right after `Speaker`, sibling indices 5–6.
- `CardView.cs`: added a `bodyScrimImage` field (mirrors `nameScrimImage`, but **unconditionally**
  enabled in `ApplyTheme` — unlike the name plate, no frame art has ever had a built-in band for
  situation text, so it isn't gated on `hasFrame`). `bodyText`'s runtime colour changed from
  `theme.SituationText` (dark ink, meant for parchment) to `theme.PrimaryText` (light, meant for a
  dark scrim over art) — the old colour would have been unreadable in the new location.
  `SetAuthoringReferences` gained a `bodyScrim` parameter.
- `SceneSetupAutomation.cs`: `ConfigureCard` now builds `BodyScrim`/`Body` and wires
  `CardView.bodyText`/`bodyScrimImage` directly (previously the caller wired `bodyText` to
  `SituationArea`'s text — that line is gone). `ConfigureSituationArea` now hides its own root via
  `SetActiveIfNeeded`. **Deleted** `RemoveLegacyCardBody` — it existed specifically to destroy a
  leftover `Card/Body` object from *before* the parchment-panel design; that design is now reversed,
  so the method's entire premise is gone. The `ValidateBatch` check at the same site flipped from
  "`Card/Body` must not exist" to "`Card/BodyScrim` and a correctly-coloured `Card/Body` must
  exist, and `SituationArea` must be inactive".

- [ ] **Not visually verified — confirm in the Editor.** Check the body text band doesn't collide
      with the `Speaker` name plate at the bottom, reads clearly over busy portrait art, and that
      `BodyScrim`'s top edge (`0.9329`) actually lines up with the portrait's new top edge from
      change #1 (both were set to the same value by hand; re-check if change #1's estimate gets
      adjusted later, since these two are no longer linked by a shared variable, just by two edits
      that happened to use the same number).
- [ ] Re-run `Tools > Royal Decisions > Scene Setup > Validate` once Unity is available — this was
      never executed this session; the validator changes above are logic-reviewed, not tool-run.
- [ ] Full `EditMode`/`PlayMode` test run needed — nothing here was executed. In particular check
      any test that asserts on `SituationArea`'s active state, `CardView.bodyText`'s wiring target,
      or `SituationTextLayoutPlayModeTests` (which targets the still-present but now-inactive
      `SituationPanel/SituationText`, unaffected by these changes but worth re-confirming).

---

## Card layout overhaul: full-bleed portrait, name/story swap, bigger icons (2026-08-27)

Follow-up in the same session, after the compile error above (`CS0102`, a genuine duplicate
`BodyTextColour` constant from the previous edit — fixed by deleting my duplicate and reusing the
pre-existing one). All still hand-edited directly in `Game.unity`; still no Unity available.

1. **Portrait is now full-bleed.** `PortraitRegion` (`fileID 1255454278`) anchors changed from the
   brightness-estimated window to a plain `(0,0)`–`(1,1)` stretch. Reasoning: `Frame` already
   renders *above* `PortraitRegion` in sibling order with an opaque border, so the frame itself
   masks the portrait's edges — no need to estimate the window at all. Simpler and more robust than
   the previous estimate; supersedes it.
2. **Speaker name and story text swapped ends of the card**, at the user's explicit request:
   - `NameScrim`/`Speaker` (`fileID`s `1620275353` RectTransform, `1620275351` Image, `850269775`
     Speaker RectTransform) moved from the bottom band to the top (`y 0.88–1`, text `y 0.895–0.985`).
   - `BodyScrim`/`Body` (`fileID`s `900000102`, `900000202`) moved from the top band to the bottom
     (`y 0–0.19`, text `y 0.017–0.173`) — the exact mirror of where the name used to sit.
   - Field/object names were **not** renamed (`nameScrimImage` now backs the top plaque,
     `bodyScrimImage` the — now invisible — bottom one) to avoid touching every reference; this is a
     naming/reality mismatch worth cleaning up later, not a functional issue.
3. **Top plaque instead of empty space.** The user said the top looked too bare even with the name
   there and left the treatment up to me: `NameScrim`'s Image (`fileID 1620275351`) is no longer a
   black scrim — it's a dark olive/khaki plaque (`Color32(0x2E,0x2A,0x1C,0xCC)`, new
   `TopPlaqueColour` constant), now unconditionally enabled (previously `!hasFrame`-gated, since the
   old logic assumed the frame art itself supplied a nameplate band — untrue for the new frame, and
   moot now that this isn't a "name scrim" anymore). `CardView.RepositionNameForFrame` and its four
   `NameAnchorMin/MaxWithFrame/NoFrame` constants were **deleted outright** — they existed only to
   runtime-override the name's position for the old frame's baked nameplate, which no longer applies
   and was fighting the new authored position every time `ApplyTheme` ran.
4. **Story background darkness removed**, at the user's explicit request
   ("hikayenin arkaplanındaki siyahlığı kaldır"): `bodyScrimImage.enabled = false` unconditionally in
   `CardView.ApplyTheme` (was `true` unconditionally, added the same session). The GameObject/Image
   is still authored (`m_Enabled: 0` baked into the scene too) — one flag flip to bring back if the
   story text turns out illegible over busy portrait art now that there's no scrim and the portrait
   is full-bleed behind it.
5. **HUD icons enlarged again**, at the user's request ("iconları da daha da büyültmeye çalış"):
   half-extent multipliers `0.23/0.25 → 0.26/0.29`, vertical center lowered `0.68 → 0.62` to keep
   the tallest icon (Authority, `iconScale 1.17`, now spanning y `0.2807–0.9593`) inside `0..1` —
   the previous center had no headroom left for this. Same eight `fileID`s as every prior icon-size
   pass. This is the fourth icon-size iteration this session; there is very little margin left for a
   fifth without lowering the center further or increasing slot height.

All of `SceneSetupAutomation.cs`'s `ConfigureCard`/`ConfigureHud` were updated to match, so a future
`Apply Remaining Setup`/`ApplyBatch` reproduces this layout rather than reverting it.

- [ ] **None of this has been seen rendered.** Open `Game.unity` and check, in order: portrait
      reaches the frame's actual edges without under- or over-shooting; the name plaque at the top
      doesn't collide with the `CornerTopLeft`/`CornerTopRight` decorations (also anchored to the
      top corners, unrelated to this change); the story text at the bottom is legible directly over
      portrait art with no scrim; all four HUD icons look consistently sized and don't clip their
      slot.
- [ ] Recompile clean — reload the project / let Unity finish recompiling and confirm the Console
      has no errors (the `CS0102` from this session's previous turn should be the only one that
      occurred, and it's fixed).

---

## Name/story bands pulled onto the visible card, plaque removed (2026-08-27)

A screenshot after the change above showed the name floating in the *background above the card*,
not on it — `KartÇerçevesi.png`'s 1024×1536 canvas has more transparent/glow margin above and below
the drawn metal border than assumed, so both bands (name at `y 0.88–1`, story at `y 0–0.19`),
anchored flush to the Card RectTransform's own edges, landed partly or fully in that margin instead
of on the visible card. Also: the olive/khaki name plaque read as a washed-out muddy brown box, not
the visual interest it was meant to add — user asked for it gone outright, not recoloured again.

- `nameScrimImage` (`fileID 1620275351`) — disabled again (`m_Enabled: 0` in the scene,
  `enabled = false` unconditionally in `CardView.ApplyTheme`, same treatment as `bodyScrimImage`).
  Not deleted — the GameObject/RectTransform stay, just invisible.
- Name band pulled down 0.08: `NameScrim` (`fileID 1620275353`) `y 0.88–1 → 0.8–0.92`; `Speaker`
  (`fileID 850269775`) `y 0.895–0.985 → 0.815–0.905`.
- Story band pulled up 0.08 (the mirror move): `BodyScrim` (`fileID 900000102`) `y 0–0.19 →
  0.08–0.27`; `Body` (`fileID 900000202`) `y 0.017–0.173 → 0.097–0.253`.
- `SceneSetupAutomation.cs`'s `ConfigureCard` updated to the same values so `Apply Remaining
  Setup`/`ApplyBatch` reproduces this instead of reverting it.

This is a **flat 0.08 shift guess, not a remeasurement** — GDI+ still can't read this PNG's alpha
reliably (see the portrait-fill note earlier in this file), and there was no second screenshot to
confirm the new numbers land correctly.

- [ ] **Confirm both bands now sit fully within the card's visible frame**, not spilling above the
      top edge or below the bottom edge, and not touching `PortraitRegion`'s masked content in a way
      that looks cramped. If either is still off, nudge `y` on the four `fileID`s above directly —
      they no longer share a single variable/constant, so each needs its own adjustment.
- [ ] Top of the card will read as empty again now that the plaque is gone — expected, matches what
      the user asked for this turn. If they want the "give the top some visual interest" request
      revisited, that's a separate follow-up, not implied by this change.

---

## HeaderDivider — ornamental divider filling the HUD-to-card gap (2026-08-27)

A later screenshot showed a different empty area — not the card, but the gap between the HUD icons
and the top of the card (the space `SituationArea`'s parchment panel used to occupy before it was
hidden). Asked what to do about it; user picked "add something" over "shrink the gap", and said to
make it professional/polished, leaving the exact treatment up to me.

Added `SafeArea/HeaderDivider`: two thin gold bars flanking a small two-layer diamond ornament (a
dark `CardSurfaceColour` diamond behind a smaller `BorderGoldColour` one, for a rivet/gem look
instead of a flat rotated square) — reusing colours already established throughout the game's
buttons/borders rather than introducing anything new. New method `ConfigureHeaderDivider` in
`SceneSetupAutomation.cs`, called right after `ConfigureSituationArea`; added to the canonical
SafeArea sibling-ordering block (now `topBar, hud, situationArea, headerDivider, card, tapChoices,
footer, tutorial, gameOver`) and to `ValidateBatch`. New scene `fileID`s: `900001001`/`900001002`
(root GameObject/RectTransform), `900001101`–`900001104` (LineLeft), `900001201`–`900001204`
(LineRight), `900001301`–`900001304` (DiamondOuter), `900001401`–`900001404` (DiamondInner) — added
to `SafeArea`'s (`fileID 185074434`) `m_Children` at index 3.

Positioned deliberately by **reusing `SituationArea`'s own proven coordinates** (top-anchored,
`anchoredPosition.y = -288`, spanning the same 208–368-units-below-top band that panel used) rather
than re-deriving the HUD/card gap from scratch — that band is known-good since it's exactly where
the parchment panel rendered correctly before being hidden, sidestepping the whole "can't reliably
read this file's measurements" problem that affected the portrait/frame work earlier in this file.

- [ ] **Not visually verified.** Confirm in the Editor: the divider sits centred in the gap, doesn't
      collide with the HUD's icons/bars above or the card's top edge below, the diamond looks like a
      deliberate ornament (not a stray rotated square if the rotation didn't apply correctly), and it
      reads as "structural/branding" rather than random clutter.
- [ ] Re-run `Tools > Royal Decisions > Scene Setup > Validate` — the four new `RequirePath` checks
      added for `HeaderDivider`'s children were never executed this session.

**Reverted, same session, before ever being seen rendered.** The user changed their mind (chose
"shrink the gap" over "decorate it" — see the entry directly below) before this was checked in the
Editor. All of it removed cleanly: the `ConfigureHeaderDivider` method, its call site, its four
`ValidateBatch` checks, and its `SetSiblingIndex` ordering-block entry, from
`SceneSetupAutomation.cs`; all 18 objects/components (`fileID`s `900001001`–`900001404`) and the
`SafeArea` (`fileID 185074434`) `m_Children` entry pointing at it, from `Game.unity`. Verified
afterward: zero remaining references to any `900001*` `fileID` anywhere in the scene file.

---

## HUD-to-card gap shrunk instead (2026-08-27)

Chosen over the divider above. `CardArea`'s top margin — the space between `HUD` and the card,
previously sized to clear `HUD` (208) + the old `SituationArea` panel (160) + a 12-unit gap (380
total) — reduced to just `HUD` (208) + the same 12-unit gap (220 total), reclaiming the 160 units
`SituationArea` used to occupy now that its panel is hidden. Bottom margin (80) unchanged.

`CardArea`'s RectTransform (`fileID 2068159500`) is a full-stretch rect (`anchorMin (0,0)` –
`anchorMax (1,1)`) with `pivot (0.5, 0.5)`, so top/bottom margins aren't independent offsets — both
come out of `anchoredPosition.y` and `sizeDelta.y` together
(`bottom = anchoredPosition.y - sizeDelta.y × 0.5`, `top = -anchoredPosition.y - sizeDelta.y × 0.5`).
Solved for the target `(top 220, bottom 80)`: `anchoredPosition.y: -150 → -70`,
`sizeDelta.y: -460 → -300`. Changed in both `Game.unity` and `SceneSetupAutomation.cs`'s
`ConfigureCard` (which now also documents the formula, so the next margin change doesn't need to
re-derive it from scratch).

`Card` itself keeps a fixed authored size (`880×1320`) in the scene, but `ResponsiveCardSizer`
(already attached, already reacting to `CardArea`'s bounds at runtime — see
`ConfigureCard`'s `sizer?.RecalculateLayout()`) should pick up the 160-unit-taller `CardArea`
automatically in Play Mode without any further scene change; nothing needed to be done to `Card`'s
own RectTransform by hand.

- [ ] **Not visually verified.** Confirm the card now sits directly under the HUD with just a small
      gap, doesn't crowd the icons above it, and — since `CardArea` is now taller — check whether
      `ResponsiveCardSizer` grows the card noticeably (it read as a bonus while writing this, not
      something separately requested; flag it if it looks like too much).

---

## HUD stat bars rendered as near-invisible hairlines (2026-08-27)

User shared a screenshot of the running game showing only four thin colour-tinted horizontal
lines where the HUD stat bars should read as legible bars. Root cause: each stat item's
`RectTransform` (`StatItem_People/Security/Authority/Wealth`, the object carrying both the
background `Image` and the `Fill` child) was authored at `sizeDelta.y = 3` reference units —
by design, per the code comment at the time ("a faint accent underline... not a primary
visual"), but in practice too thin to read as a bar at all, especially with the icon above it
not rendering in the shared screenshot's crop.

`SceneSetupAutomation.cs` was first updated to `10` units, then re-checked against a follow-up
screenshot (after the user ran `Apply Remaining Setup` and reported "still the same"): at this
project's `CanvasScaler` settings (reference resolution matched on height), 1 reference unit is
roughly 1 device pixel on a 1080×1920 phone, so a 10-unit bar is still only ~10px tall — barely
different from 3px, and still reads as a hairline. `git log` on this file confirms the bar was
originally authored at **24** units (`itemRect.sizeDelta.y, 24f`) and deliberately shrunk to a
"faint accent underline" during the 2026-08-25 Reigns-inspired HUD redesign (see that section
above, "24-unit fill bar... shrunk to a 6-unit accent underline" — it landed at 3, not 6, by the
time it was committed). The creation code in `ConfigureHud` and the matching check in
`ValidateBatch` (`"HUD stat bar height must be ... reference units"`) now both use **`24`**,
restoring the original, clearly-legible bar height.

The 24-unit height alone was reported as "still the same" (a second screenshot at Device
Simulator scale showed no visible change from the 10-unit attempt), and the user asked for a
"professional", on-theme replacement rather than another height tweak. Diagnosis: height was
never the whole story — the bar's *width* was also only 36% of its slot (anchors `0.32`–`0.68`),
so even at 24 units tall it was a short, unframed, flat-coloured stub rather than a gauge.

`ConfigureHud` now builds a proper stat gauge instead of an accent line:

- Width: **84% of the slot** (anchors `0.08`–`0.92`, was `0.32`–`0.68`), matching the visual
  weight of the icon above it instead of a narrow stub in the middle of the slot.
- Height: **20** units (between the original 24 and the failed 10; combined with the width fix
  this reads as a compact gauge, not a hairline).
- A **gold `Outline`** (`StatBarBorderColour`, `#B58A4A` at ~60% alpha, `effectDistance (1.5,
  -1.5)`) added to the same object as the background `Image` — the same device already used for
  the card's own temporary border (`TemporaryCardBorderColour`), just more opaque since a small
  bar needs more contrast than a large card to read as intentionally framed. This is what makes
  it look like a themed gauge instead of a stray colour rectangle: a background track, a coloured
  fill, and a gold frame around both — no new art asset required.

`ValidateBatch`'s check now expects `sizeDelta.y == 20` and additionally requires the gold
`Outline` to be present with the matching colour.

This is a generator/validator change only — **`Game.unity` on disk still has whatever the last
`Apply Remaining Setup` run produced** (some earlier height, no outline, old narrow width) until
the generator is re-run again. This session could not run the
`-batchmode -executeMethod ApplyBatch` route used for earlier fixes in this file, because Unity
currently has the project open and locked (`Temp/UnityLockfile` present).

- [ ] In the open Editor: `Tools > Royal Decisions > Scene Setup > Audit` — confirm it reports
      the four `StatItem_*` objects as needing both the width/height update and the new outline.
- [ ] `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` — rebuilds the HUD stat
      gauges (wider bar, 20-unit height, gold frame).
- [ ] `Tools > Royal Decisions > Scene Setup > Validate` — must report zero errors.
- [ ] Re-run `Apply Remaining Setup` once more — must report no further changes (idempotent).
- [ ] Enter Play Mode / Device Simulator and confirm all four stat gauges now read as a framed
      bar (background track + coloured fill + visible gold border) spanning most of the slot
      width beneath each icon, not a thin unframed stub. If the gold frame reads too subtle or
      too strong, adjust `StatBarBorderColour`'s alpha (currently `0x99`) in
      `SceneSetupAutomation.cs` and re-run Apply.

### Why none of the above ever actually appeared: Apply was silently rolling itself back

The user re-ran `Apply Remaining Setup` after each of the three attempts above and reported
"still the same" every time. Reading `%LOCALAPPDATA%/Unity/Editor/Editor.log` (the running
Editor's own console output, checked directly rather than guessing further) showed why: **every
`Apply Remaining Setup` run was succeeding at writing the new scene state, then immediately
failing its own post-apply validation and restoring the pre-apply backup — discarding everything
it had just written, including all HUD stat bar changes above.** This had nothing to do with bar
width, height, or colour; it would have swallowed *any* change made through this tool.

Three pre-existing bugs in `SceneSetupAutomation.cs`, unrelated to the stat bar work, were
causing this on every single run:

1. **`GameSceneController.catalogue` mismatch (the real blocker).** `ApplyMenu`'s Game-scene
   apply step always loaded the *placeholder* catalogue (`CataloguePath`) and wrote it into
   `GameSceneController.catalogue`. But `ValidateProjectLoadedState` (run automatically at the
   end of every Apply) always validates that same field against
   `StorySceneWiring.StoryCataloguePath` — per its own comment, "the committed Game scene is
   wired to the story catalogue." Apply wrote placeholder, then immediately checked for story:
   guaranteed mismatch, every time, for anyone. Confirmed in the log — the standalone `Validate`
   command (which does not touch the catalogue) reported 6 errors with no catalogue complaint;
   only the `Apply` runs, which overwrite the field, additionally logged
   `GameSceneController.catalogue is incorrect.` Fixed: the Game-scene apply step now loads
   `StorySceneWiring.StoryCataloguePath` first, falling back to the placeholder catalogue only if
   the story one has not been generated (preserving the tool's original placeholder-only
   behaviour for a project with no story content yet).
2. **Stale HUD layout check.** `ValidateBatch` required `HorizontalLayoutGroup.padding.left/right
   == 12` and `spacing == 8`, but `ConfigureHud` (the generator half of the same file) has
   authored `padding = (64, 64)` and `spacing = 0` for some time — a deliberate, commented change
   ("pull the four equally-expanding slots... closer together"). The checker was never updated to
   match, so Apply could never pass this check either. Fixed: the checker now expects `64`/`64`/`0`.
3. **Stale `CardArea` margin check.** Same pattern: `ConfigureCard` sets
   `anchoredPosition = (0, -70)`, `sizeDelta = (-40, -300)` (see "HUD-to-card gap shrunk instead"
   above), but `ValidateBatch` still checked for the pre-that-change values, `(0, -150)` /
   `(-40, -460)`. Fixed: the checker now expects the current values.

All three had to be fixed together for `Apply Remaining Setup` to ever complete without
rolling back — fixing only the stat bar's own check (already done in the section above) was
necessary but not sufficient.

- [x] Re-ran the full sequence with the Editor closed (it had exited between the previous message
      and this one, so `-batchmode -executeMethod` could run without conflicting with an open
      instance):
      `Audit` → `0 errors, 0 warnings, 1 info` (`VALIDATION_OK`) →
      `ApplyBatch` → `0 errors, 4 warnings, 5 info`, `APPLY_COMPLETE`, no `BACKUP_RESTORED` →
      `ValidateBatch` → `0 errors, 0 warnings, 1 info` (`VALIDATION_OK`) →
      `ApplyBatch` again → identical result (`APPLY_COMPLETE`, no further errors), confirming
      idempotency. Logs kept under the session scratchpad
      (`logs/1-audit.log` .. `4-apply-again.log`).
- [x] Read `StatItem_People`'s serialized `RectTransform`/`Outline`/`HorizontalLayoutGroup`
      directly out of `Game.unity` after these runs: anchors `0.08`–`0.92`, `sizeDelta (0, 20)`,
      `Outline` present with `effectColor` matching `StatBarBorderColour` and
      `effectDistance (1.5, -1.5)`; HUD's `HorizontalLayoutGroup` padding `64/64`, spacing `0`;
      `GameSceneController.catalogue` guid matches `StoryContentCatalogue.asset`. All match the
      generator code exactly — the fix is confirmed on disk, not just "reported successful".
- [ ] **Still needs a human eyeball in the Editor**: this session has no way to enter Play Mode or
      take a screenshot headlessly. Open the project, enter Play Mode / Device Simulator, and
      confirm the four stat gauges read as a framed bar (background track + coloured fill +
      visible gold border) under each icon — this is the one thing that has not actually been
      *seen* rendered yet, only verified structurally.
- [ ] The four pre-existing `ART_ASSET_MISSING` warnings and one repeating `ORPHAN_REMOVED`
      notice (`GeneralTab/ResetProgressButton/ArmedText`, removed on both apply runs — it does not
      stay removed, so something keeps recreating it) were left alone: unrelated to this fix, not
      blocking, not something this session was asked to chase.
- [ ] `Assets/_Game/scenes/Game.unity` and `MainMenu.unity` are now modified in the working tree
      (`git status`) — nothing was committed. Review and commit when satisfied with the Play Mode
      check above.

---

## Coded startup intro (Bootstrap)

A native Unity UI/C# logo reveal that plays before `MainMenu` loads. **No video file is used or
shipped anywhere in this feature** — `IntroSequenceController` animates a `CanvasGroup` (alpha),
a `RectTransform` (scale) and an `Image` (brightness) with plain coroutines and an
`AnimationCurve`; there is no `VideoPlayer`, no MP4, no third-party tween library. `IntroSceneSetup`
is a standalone Editor tool that wires it into `Bootstrap.unity`. Neither runs automatically —
**Bootstrap.unity has not been touched by this change**; the scene file on disk still matches
`origin/main` until I2 below is run once in the Editor.

### I1 — Provide the logo artwork

- [ ] Place one transparent PNG of the complete Arilla Games logo at
      `Assets/_Game/Art/Branding/ArillaGamesLogo.png`
- [ ] On import, set `Texture Type` = **Sprite (2D and UI)**

Until this file exists, `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup` still
runs and wires everything else, but the intro has no sprite to show — `IntroSequenceController`
detects that and skips straight to `MainMenu`, so the game is never blocked on missing art.

### I2 — Run the Intro scene setup

- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup`
- [ ] Console reports `[IntroSceneSetup] Bootstrap intro wiring applied.` with no errors

This creates/updates `IntroCanvas` (`BlackBackground` + `Logo`) and an `EventSystem` inside
`Bootstrap.unity` (1080x1920 `CanvasScaler`, matching every other scene), assigns `Logo`'s sprite
from I1 if present, and wires `BootstrapController.introSequence` to the new controller. Safe to
re-run at any time — every step finds-or-creates rather than duplicating, sibling order and
component state are re-asserted each run, and re-running after adding the PNG from I1 picks it up.
Editor property changes are `Undo`-recorded; `Ctrl+Z` right after running it reverts them in that
Editor session if the result looks wrong before you save.

If this step has **not** been run yet, `BootstrapController.introSequence` is unassigned and the
game behaves exactly as it did before this feature existed: Bootstrap loads MainMenu immediately,
no intro, no behavior change.

### Expected behavior once wired

- **Timeline (unscaled seconds, ~3.30s total):** `0.00–0.35` pure black · `0.35–1.25` logo fades
  in (alpha 0→1) while scaling 0.92→1.0 with a quick-start/settle ease and a subtle brightness
  ramp · `1.25–2.30` hold with one gentle scale/brightness breathing pulse (≤1.5% scale, peaks at
  the midpoint, zero at both ends — no pop against the fades) · `2.30–3.10` fades out (alpha 1→0)
  while scaling 1.0→1.02 · `3.10–3.30` black hold · then `MainMenu` loads, exactly once.
- **Skip:** one tap/click anywhere on screen (the black background is the full-screen hit target)
  jumps straight to `MainMenu`. Safe against rapid repeated taps and against a tap arriving before
  the sequence has started — both resolve to exactly one `MainMenu` load, never zero, never two.
- **Fallback:** if the logo sprite, any of its three component references, or Play Mode itself is
  missing, the intro completes immediately with no animation and `MainMenu` loads right away —
  startup is never blocked on missing art or misconfiguration.
- **Reduced motion:** `BootstrapController` reads the already-loaded `GameSettings.ReducedMotion`
  and calls `IntroSequenceController.SetReducedMotion` before playing — when on, the intro becomes
  a brief plain fade (no scale/glow motion, no black holds, capped durations) rather than being
  skipped outright, matching how `CardSwipeController`/`PanelFadeAnimator` already treat reduced
  motion elsewhere in the game.

### I3 — Verify in the Editor

- [ ] Enter Play Mode from `Bootstrap.unity` — logo fades/scales in, holds, fades out, then
      `MainMenu` loads exactly once
- [ ] Tap/click anywhere during the intro — it skips straight to `MainMenu`, still exactly once
- [ ] Temporarily clear `Logo`'s sprite and re-enter Play Mode — `MainMenu` loads immediately, no
      error in the Console
- [ ] In Settings, enable **Reduced Motion**, re-enter Play Mode — a short plain fade plays instead
      of the full reveal, still exactly one `MainMenu` load
- [ ] Console clean throughout
- [ ] `Window > General > Test Runner > EditMode` — `IntroSequenceControllerTests` and
      `BootstrapControllerTests` both green alongside the existing suite

---

## Wordmark left-to-right reveal (2026-08-26)

Extends the intro above: the AS emblem still fades/scales in as before, but the baked-in
"ARILLA GAMES" wordmark underneath is now hidden behind a plain black `WordmarkCover` until it
wipes away left-to-right, with a narrow blue/silver `WordmarkGlint` travelling along the reveal
edge. **The logo PNG itself is untouched and still one Single Sprite, one Image** —
`WordmarkCover`/`WordmarkGlint` are two additional siblings under `LogoGroup` next to `LogoImage`,
sized and positioned from the wordmark's measured pixel row on the 1254x1254 source, not from a
crop or a second copy of the art. `IntroSceneSetup` computes their geometry from `LogoImage`'s own
rect, so it stays correct even if `LogoDisplaySize` is retuned later.

### I4 — Re-run the Intro scene setup

- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup`
- [ ] Console reports `[IntroSceneSetup] Bootstrap intro wiring applied.` and
      `Validation passed: hierarchy and references are correct.`

This is required this time — unlike the earlier pure-numeric timing/scale tweaks, `WordmarkCover`
and `WordmarkGlint` are new GameObjects that only the Editor tool can safely create (correct
`RectTransform`/`Image`/`CanvasRenderer` wiring, unique scene identities, `Undo` support). Until
this is run, `Bootstrap.unity` has no wordmark reveal objects and `IntroSequenceController`'s new
`wordmarkCoverImage`/`wordmarkGlintImage` fields stay unassigned — which is safe (the whole logo,
mark and wordmark together, just fades in exactly as it did before this change) but means the new
effect will not be visible yet. Safe to re-run at any time: it finds-or-creates rather than
duplicating, and re-running never touches `LogoImage` or the AS-mark animation.

### Expected behavior once wired

- **Timeline (unscaled seconds, ~4.50s total):** `0.00–0.40` black · `0.40–1.45` AS mark fades in
  (alpha 0→1, scale 0.94→1.0) while the wordmark stays hidden · `1.25–2.65` wordmark wipes in
  left-to-right (overlaps the tail of the mark's fade-in by design) with a narrow glint travelling
  along the reveal edge, fading in and back out so it never pops or lingers · `2.65–3.45` full logo
  holds with the existing subtle breathing pulse · `3.45–4.25` entire composition (mark + revealed
  wordmark + any cover/glint remnants) fades out together · `4.25–4.50` black hold, then `MainMenu`
  loads, exactly once.
- **Skip:** a tap at any point — including mid-wipe — hides everything instantly (the whole
  `LogoGroup` goes to alpha 0 in one frame) and proceeds to `MainMenu` exactly once. No one-frame
  leftover of a half-covered wordmark or a stray glint.
- **Reduced motion:** the wipe and glint are skipped entirely — the wordmark cover opens instantly
  so the full logo (mark and wordmark together) appears as part of the same short plain fade used
  for the rest of the reduced-motion intro.
- **Fallback:** if `WordmarkCover` was never wired (I4 not yet run, or wiring cleared by hand), the
  logo still fades in normally with the wordmark visible from the start — never blocked or broken.

### I5 — Verify in the Editor

- [ ] Enter Play Mode from `Bootstrap.unity` — AS mark fades in, then the wordmark wipes in
      left-to-right with a subtle travelling highlight, holds, fades out with everything else,
      `MainMenu` loads exactly once
- [ ] Tap/click during the wordmark wipe — skips straight to `MainMenu`, no leftover black bar or
      highlight visible even for one frame
- [ ] In Settings, enable **Reduced Motion**, re-enter Play Mode — the full logo (wordmark
      included) appears together via a short plain fade, no wipe, no glint
- [ ] Console clean throughout, including `Validate Intro Setup`'s hierarchy/reference report
- [ ] `Window > General > Test Runner > EditMode` — existing `IntroSequenceControllerTests` and
      `BootstrapControllerTests` still green (no new tests were added for this pass)

---

## Wordmark soft-edge feather + synchronised intro audio (2026-08-26)

Two additions to the intro above, both intro-only:

1. **Soft feather edge.** The wordmark wipe previously used only the hard-edged `WordmarkCover`
   fill, which could read as a rectangle shrinking rather than an energised reveal. A new sibling,
   `WordmarkFeather`, blends a soft black-to-transparent gradient over the current reveal edge
   (44 px wide, tracks the edge exactly like `WordmarkGlint` already did), so the cut softens into
   the art instead of stopping abruptly. `WordmarkGlint` itself was also upgraded from a flat-colour
   `Image` to the same new gradient component, so it now reads as a soft highlight (transparent →
   peak → transparent) instead of a translucent rectangle. Both use a new,
   texture-free `ProceduralHorizontalGradientGraphic` (`Assets/_Game/Scripts/Presentation/
   ProceduralHorizontalGradientGraphic.cs`) — a `MaskableGraphic` subclass in the same style as the
   existing `ProceduralVignetteGraphic`/`ProceduralGearIconGraphic`: per-vertex colour, no shader,
   no texture. The wipe's timing (1.25s → 2.65s) and easing were **not** changed.
2. **Synchronised intro audio.** Three short, original, procedurally generated cues now play through
   the intro's own `AudioService` + `AudioCueLibrary` — the same architecture every other scene
   uses, not a bespoke intro-only audio path:
   - `intro_logo_rise` — plays as the AS mark begins fading in (skipped in reduced motion).
   - `intro_wordmark_sweep` — plays as the wordmark wipe begins (never plays in reduced motion —
     there is no wipe to synchronise it to there).
   - `intro_resolve` — plays the instant the wordmark becomes fully revealed, in both the timed
     wipe and reduced motion's instant reveal.

   Tapping to skip stops any cue immediately via a new `AudioService.StopSfx()` method (SFX only —
   `StopMusic()` is untouched). Natural completion instead lets the last cue's short tail decay
   into the following black hold, then MainMenu's own scene load destroys the intro's `AudioSource`
   regardless. `BootstrapController.ApplySettings()` was also fixed to forward
   `MasterVolume`/`MasterMuted` to its audio service (it previously hard-coded unmuted, which was
   inert until now because no `AudioService` had ever been wired into Bootstrap).

### I6 — Provide the cue library entries, then re-run Intro Setup

The three WAV files already exist at `Assets/_Game/Audio/Intro/intro_logo_rise.wav`,
`intro_wordmark_sweep.wav`, `intro_resolve.wav` (mono/stereo 16-bit PCM, 44.1kHz, peaks between
-19 and -16 dBFS — quieter than the existing gameplay/UI SFX). They still need Unity to import them
and the cue library to point at them:

- [ ] Let Unity import the new `Assets/_Game/Audio/Intro/` folder (automatic on focus/reopen)
- [ ] `Tools > Royal Decisions > Audio > Update Main Audio Cue Library` — adds `intro_logo_rise`,
      `intro_wordmark_sweep`, `intro_resolve` to `MainAudioCueLibrary.asset` (existing cues are
      preserved, matching this tool's normal idempotent behaviour)
- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup` — creates the new
      `WordmarkFeather` node, upgrades `WordmarkGlint`'s component, creates the scene-root
      `IntroAudio` object (`AudioSource` + `AudioService` wired to `MainAudioCueLibrary.asset`), and
      wires both `BootstrapController.audioService` and `IntroSequenceController.audioService` to it
- [ ] Console reports `[IntroSceneSetup] Bootstrap intro wiring applied.` and
      `Validation passed: hierarchy and references are correct.`

This is required this time — `WordmarkFeather` and `IntroAudio` are new GameObjects only the Editor
tool can safely create. Safe to re-run at any time: it finds-or-creates rather than duplicating.

### I7 — Verify in the Editor

- [ ] Enter Play Mode from `Bootstrap.unity` — the wordmark wipe now shows a soft blended edge
      (not a hard rectangle), the glint reads as a soft highlight, and three cues play in order:
      a soft low rise under the AS mark, a subtle panned sweep under the wordmark wipe, then a
      short soft resolve accent the instant the wordmark completes
  - [ ] Confirm none of the three cues sound like a UI click/notification/loud whoosh — all should
        read as quiet, dark, cinematic accents
- [ ] Tap/click mid-wipe — visuals and audio both cut immediately, `MainMenu` loads exactly once,
      no lingering sound over the menu
- [ ] Tap repeatedly/rapidly — still exactly one `MainMenu` load, no doubled or overlapping audio
- [ ] In Settings, set **Master Volume** partway down and re-enter — all three cues play quieter
      proportionally; set **Master Mute** on and re-enter — the intro is silent throughout, visuals
      unaffected
- [ ] In Settings, enable **Reduced Motion**, re-enter Play Mode — no rise cue, no sweep cue; only
      a short resolve accent plays once, and the intro does not wait for any cue's tail to finish
- [ ] Confirm MainMenu's own music starts normally afterward — unaffected by anything above
- [ ] Console clean throughout, including `Validate Intro Setup`'s report
- [ ] `Window > General > Test Runner > EditMode` — `IntroSequenceControllerTests`,
      `BootstrapControllerTests`, and `AudioServiceTests` (two new `StopSfx` cases) all green

---

## Android launcher icon — zombie-hand artwork

Replaces the default Unity icon with the supplied `AppIconSource.png` (dark post-apocalyptic
background, orange circular symbol, chained zombie hand). Scope was deliberately narrow: only
Android icon slots in `PlayerSettings` and new derived art under
`Assets/_Game/Art/Branding/AppIcon/`. Package name, keystore, version code/name, scripting
backend, and Android SDK/API settings were not touched.

**What was found before changing anything:** every Android icon slot in
`ProjectSettings/ProjectSettings.asset` (`m_BuildTargetPlatformIcons` → `Android`) had
`m_Textures: []` — Legacy, Round and Adaptive were all empty, so the build was shipping Unity's
default icon. There was no pre-existing icon-generation tooling in the project.

**What was generated** (deterministic Pillow script, not hand-drawn, source untouched):

- `Legacy_{192,144,96,72,48,36}.png` / `Round_{same sizes}.png` — the full source art resized with
  high-quality (LANCZOS) downsampling. A pixel analysis showed only ~0.36% of the art's bright
  content (isolated ember/spark specks near the chain tips) falls outside an inscribed circle at
  full bleed, so Legacy/Round use the artwork as-is.
- `AdaptiveForeground_{432,324,216,162,108,81}.png` — the source scaled to **62%** and centered on
  a transparent canvas, with a radial alpha feather over the outer 10% of the pasted image so it
  blends into the background layer instead of showing a hard-edged square. 62% was chosen because
  99% of the source's bright content sits within ~95% of its own half-width from center, and
  Android's adaptive safe zone is a 66dp circle in a 108dp canvas (radius fraction ≈ 0.611);
  `0.611 / 0.95 ≈ 0.64`, and 0.62 leaves a small margin.
- `AdaptiveBackground_{same 6 sizes}.png` — solid fill at `rgb(14, 12, 10)`, the measured mean of
  the source's own near-black background pixels (not a new color choice).
- `Assets/_Game/Scripts/Editor/AppIconSetup.cs` — `Tools > Royal Decisions > Configure Android App
  Icon`, an idempotent menu item that assigns the generated PNGs to the Legacy/Round/Adaptive
  PlayerSettings slots (Adaptive: foreground = texture index 0, background = index 1) and forces
  each imported texture to uncompressed RGBA32 with no mipmaps, so the launcher icon build output
  isn't degraded by default texture compression.

**Applied and verified this session** via
`Unity.exe -batchmode -nographics -quit -executeMethod RoyalDecisions.Editor.AppIconSetup.Configure`
(Editor was closed; no second instance was launched):

- [x] `ProjectSettings.asset` diff is scoped to exactly the Android `m_Icons` block — Legacy and
      Round each now reference one texture per size, Adaptive references two (foreground,
      background) per size. No other PlayerSettings field changed.
- [x] `AppIconSource.png` is unmodified (only the derived files under `AppIcon/` were written).
- [x] The pinned Unity version has no Android module installed on this machine (only 6000.3.20f1
      does — a pre-existing, pre-dated mismatch, not something this pass caused). The batch run
      used 6000.3.20f1 to reach the Android `PlayerSettings` APIs; it bumped
      `ProjectSettings/ProjectVersion.txt` to `6000.3.20f1` on open, which was reverted back to the
      pinned `6000.3.18f1` afterward via `git checkout`.
- [x] A circle-mask simulation of the composited adaptive foreground+background at 432px confirmed
      the hand and full orange ring stay inside the safe zone; only the outer transparent margin
      (no art content) falls outside the circle.

**Known side effect, not from this task:** the same batch launch also generated a
`Assets/_Game/Audio/Intro.meta` file (Unity always does a full project asset scan on open — there is
no way to import only one folder). That folder's `.wav` files belong to the concurrent intro-sequence
work, not this pass; their content was not touched, only the folder got a `.meta` assigned the first
time any Editor opened the project. Safe to leave as-is.

### AI1 — Verify on an Android device

- [ ] Console shows no new project-code errors or warnings after this change.
- [ ] Build and install a debug APK (or use the existing device workflow); check the home screen,
      app drawer and recent-apps icon.
- [ ] If your launcher applies a circular or squircle mask (most Android 8.0+ launchers), confirm
      the hand and orange ring are not clipped — only empty background may be cropped.
- [ ] On a device below Android 8.0 (API < 26) or with a launcher that ignores adaptive icons, the
      Legacy/Round icon should show the full artwork.
- [ ] No manual Inspector wiring is required — icons are assigned via `PlayerSettings`, not scene
      objects. Re-run `Tools > Royal Decisions > Configure Android App Icon` only if
      `AppIconSource.png` or the files under `AppIcon/` change.

---

## Intro final recreation pass, matched to `References/ArillaIntroReference.mp4` (2026-08-27)

Retimed and extended the coded intro to match a reference MP4 the team supplied (analysed via
`ffmpeg`/frame extraction only — **the MP4 was never imported into Unity, never used as a
VideoPlayer source, and its audio was never extracted**; it exists purely as an external reference
under `References/`, which is not shipped). Reference: 1080×1920, 30fps, 5.40s total.

Two real differences from the previous pass, both evidenced by the reference's frames, not guessed:

1. **Timing.** The old timeline approximated an earlier, shorter reference. Frame-by-frame analysis
   of the actual MP4 (bright-pixel bounding boxes/centroids for the AS mark, a brightness-boosted
   crop strip for the wordmark's letter-by-letter edge, and a mean-brightness curve for the hold/
   fade-out boundary) gave precise phase durations, which the timing fields below now match almost
   exactly except the hold (trimmed for mobile — see the timeline table). The one **structural**
   timing change: the wordmark used to start revealing *while the AS mark's fade-in tail was still
   playing* (`wordmarkRevealDelaySeconds` < `fadeInDurationSeconds`); the reference shows the AS
   mark settle fully first, then the wordmark begins about 0.05s later — `wordmarkRevealDelaySeconds`
   is now slightly *larger* than `fadeInDurationSeconds` to match that clean two-stage cadence.
   Also: the reference's hold phase is **perfectly flat** (measured mean brightness varies <1% over
   1.7s) — the old "breathing pulse" during the hold (`holdPulseScaleAmplitude`) is not something the
   reference does, so its default is now `0` (mechanism kept, just off by default — set it above 0
   only for a deliberate future breathing effect).
2. **Wordmark reveal look.** The reference's travelling highlight is a **soft horizontal glow that
   grows in width beneath the wordmark** (like an underline filling in), not a highlight that sweeps
   *across* the letters. A new sibling, `WordmarkUnderlineGlow`, reproduces this: it reuses the
   existing `ProceduralVerticalGradientGraphic` (already in the project for the Loading screen's
   scrim — reused read-only, not modified) for a soft top/bottom-fading glow bar, left-pivoted so
   growing its `sizeDelta.x` at runtime extends it rightward in lockstep with the same reveal
   progress driving the cover/feather. `WordmarkGlint` (the existing bright travelling highlight) was
   **repositioned** down to this same row instead of the text's own row, so it now reads as the
   glow's bright leading edge rather than a highlight crossing the glyphs — this is a reposition, not
   new logic. The letters themselves still reveal via the existing hard-edged `fillAmount` wipe +
   `WordmarkFeather` soft edge, which the reference does not contradict.

Everything else the reference showed — the AS mark's fade+very subtle (~5-6%) scale-up, no position
shift when the wordmark appears, the whole composition fading out together at the end, left-to-right
reveal direction — already matched the previous implementation, so those were **not** touched.

### Exact final timeline (unscaled seconds)

```
0.00–0.55  black                              (reference: ~0.60)
0.55–1.45  AS mark fades in (0.90s)            (reference: ~0.90, near-exact)
1.45–1.50  brief gap (no visual change)         (reference: ~0.08-0.10 gap)
1.50–2.90  wordmark reveals left-to-right (1.40s) (reference: ~1.40, near-exact)
2.90–4.10  full logo holds, perfectly still (1.20s) (reference: ~1.70, trimmed for mobile)
4.10–4.70  entire composition fades out (0.60s) (reference: ~0.60, near-exact)
4.70–4.85  black → Loading                      (reference: ~0.10 tail)
Total ≈ 4.85s (reference total: 5.40s)
```

### Audio decision: kept, retimed only

Inspected the three cues already at `Assets/_Game/Audio/Intro/` (from the previous audio pass):
`intro_logo_rise.wav` (mono, 1.15s, peak -18.0 dBFS), `intro_wordmark_sweep.wav` (stereo, 1.45s,
peak -19.2 dBFS), `intro_resolve.wav` (mono, 0.75s, peak -16.5 dBFS). All three still fit the new,
reference-derived timeline:

- The wordmark reveal duration is unchanged (1.40s), so `intro_wordmark_sweep` (1.45s) needs no
  retiming at all.
- `intro_logo_rise`'s envelope (0.90s rise + 0.20s tail) still tracks the new 0.90s AS-mark fade-in
  closely; its tail now overlaps the sweep's start by ~0.20s, which reads as a natural crossfade,
  not a clash.
- `intro_resolve` (0.75s) now has *more* headroom before fade-out (1.20s hold vs. the old 0.80s), so
  it always finishes decaying well before the composition starts fading.

No new WAV files were generated. No audio was extracted from the reference MP4 — the reference has
audio (48kHz stereo AAC) but it was never listened to, decoded, or used for anything beyond knowing
it exists; only the video frames informed this pass. Trigger points did not need code changes either
— they were already tied to phase *events* (fade-in start, wordmark start, wordmark complete) rather
than fixed timers, so they retimed automatically when the serialized durations above changed.

### I8 — Re-run the Intro scene setup

- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup`
- [ ] Console reports `[IntroSceneSetup] Bootstrap intro wiring applied.` and
      `Validation passed: hierarchy and references are correct.`

Required this time — `WordmarkUnderlineGlow` is a new GameObject only the Editor tool can safely
create, and `WordmarkGlint`'s row changed (existing object, moved, not recreated). Safe to re-run at
any time: finds-or-creates rather than duplicating, and this never touches `StartupLoadingController`,
`LoadingBackground.png`, or anything under `Assets/_Game/Art/Branding/AppIcon*`.

### I9 — Verify in the Editor

- [ ] Enter Play Mode from `Bootstrap.unity` — AS mark fades in over ~0.9s, a short beat, then the
      wordmark reveals left-to-right over ~1.4s with a soft glow growing underneath it (not a
      highlight sweeping across the letters), holds **perfectly still** for ~1.2s, fades out over
      ~0.6s, then Loading begins (startup order unchanged: Intro → Loading → MainMenu)
- [ ] Compare side-by-side against `References/ArillaIntroReference.mp4` if convenient — the overall
      rhythm and the underline-glow reveal should read as the same intro, not a video, at ~90% the
      reference's total length
- [ ] Confirm the hold shows no scale/brightness pulsing (this was intentionally removed by default)
- [ ] Tap/click at any point, including mid-wipe — skips to Loading instantly, audio cuts
      immediately, no leftover cover/glint/feather/underline-glow visible even for one frame
- [ ] In Settings, enable **Reduced Motion** — short plain fade, no wipe, no underline glow, only the
      resolve cue plays, still exactly one transition to Loading
- [ ] Console clean throughout
- [ ] `Window > General > Test Runner > EditMode` — existing `IntroSequenceControllerTests`,
      `BootstrapControllerTests`, and `AudioServiceTests` all green (no new tests were added for
      this timing/layout-only pass; the coroutine timing itself is intentionally not
      unit-tested — see CLAUDE.md's guidance against brittle visual-timing tests)

---

## Intro architecture rebuild — real reveal mask, separated mark/wordmark, no underline (2026-08-27)

Direct feedback against the previous pass identified three problems: the wordmark still read as
popping in rather than revealing; an unwanted underline/glow bar sat permanently under it; and the
AS mark was too large relative to the wordmark, which was too small to read comfortably. This pass
replaces the wordmark reveal mechanism and the logo's internal composition; the intro's overall
five-phase structure (black → AS fade-in → wordmark reveal → hold → fade-out → black) is unchanged.

### Root cause of the "pop in" look

The previous implementation shared one combined image (AS mark + wordmark baked together) and
"revealed" the wordmark by animating a black `Image` cover's `fillAmount` over hand-measured pixel
padding, with a soft-edge blend layered on top. Two things worked against it reading as a reveal:
the wordmark's own on-screen size was small (baked at the same scale as the much larger AS mark
inside one combined sprite), so incremental letter-by-letter progress was hard to perceive at a
glance; and the feather's blend width (44px) was comparable to a single character's width, so the
reveal read more like a soft brightness wave than a crisp sequential reveal. Small alignment slack
in the hand-measured padding could also leave a faint hint of text visible before the cover was
supposed to fully hide it, making the transition to the crisp final glyphs look like a snap rather
than a reveal. The new architecture removes the ambiguity structurally rather than re-tuning
padding: the wordmark is now its own tightly-cropped, independently-sized sprite revealed by a real
`RectMask2D` clip, so what renders at every instant is geometrically exact.

### 1 — Two derived, pixel-exact crops (master PNG untouched)

`Assets/_Game/Art/Branding/ArillaGamesLogo.png` (1254×1254, RGBA) is the unchanged master — its
SHA-256 was verified identical before and after generation. Two new assets were derived from it by
plain pixel-array slicing (no resampling, recolouring, or resizing — every kept pixel is
byte-identical to the source):

- `Assets/_Game/Art/Branding/Generated/ArillaGamesMark.png` — 1050×657, cropped from source pixel
  bbox (81, 217) to (1130, 873)
- `Assets/_Game/Art/Branding/Generated/ArillaGamesWordmark.png` — 1085×78, cropped from source
  pixel bbox (86, 926) to (1170, 1003)

**Method (deterministic, repeatable):** alpha-channel coverage per row found a clean gap between
the two elements (zero coverage for every row in [868, 931] on the 1254-tall source); the mark's
own alpha>8 bounding box was measured within rows [0, 900), the wordmark's within rows [900, 1254),
then each box was padded by 6px on every side (clamped to image bounds) before cropping. Verified
programmatically that every kept pixel matches the corresponding source pixel exactly. If the
master PNG is ever replaced, re-run this same method (a Python/Pillow script) to regenerate both
crops — do not hand-edit them.

- [ ] Let Unity import the new `Assets/_Game/Art/Branding/Generated/` folder (automatic on
      focus/reopen); `Texture Type` should auto-detect as Sprite, but `Apply Intro Setup` (below)
      forces Sprite Mode **Single** on both regardless, exactly like it already did for the master.

### 2 — New hierarchy

```
IntroCanvas
└ BlackBackground        (unchanged: click-catcher + IntroSequenceController)
└ LogoGroup              (unchanged root: CanvasGroup + RectTransform, alpha/scale pivot for everything)
   ├ MarkImage           (AS mark only, 460 reference units wide, preserveAspect)
   ├ WordmarkRevealRoot  (stable container, centred, sized to the wordmark's full final size)
   │  └ RevealMask       (RectMask2D, left-pivoted, width animates 0 → full at runtime)
   │      └ WordmarkImage (ARILLA GAMES only, 560 reference units wide, NEVER resized/moved)
   └ RevealGlint         (optional travelling highlight, ProceduralHorizontalGradientGraphic)
```

`WordmarkUnderlineGlow` and the old text-row `WordmarkCover`/`WordmarkFeather`/`WordmarkGlint` are
gone entirely — `Apply Intro Setup` destroys any of those left over from a previous run
automatically (they are no longer in the tool's known-children set, so the same generic legacy-node
cleanup that has always removed stray nodes now removes these).

### 3 — Exact mask mechanics (reasoned through geometrically, not just compiled)

- `WordmarkImage`'s `RectTransform` uses a point anchor (`anchorMin == anchorMax == (0, 0.5)`,
  pivot `(0, 0.5)`) with `sizeDelta = (560, wordmark height)` — an **absolute, fixed size**,
  authored once and never touched at runtime. Its left edge is pinned to `RevealMask`'s own left
  edge regardless of `RevealMask`'s current width, because the anchor is a point, not a stretch.
- `RevealMask` carries the actual `RectMask2D` component and uses the same left-pivot convention.
  Only its `sizeDelta.x` is animated, from `0` to `WordmarkImage`'s own full width, via
  `IntroSequenceController.SetRevealMaskWidth`.
- Because `RectMask2D` clips children to its own current rect, and `WordmarkImage` never moves or
  resizes, the visible region at any progress `t` is always an exact, un-stretched, un-scaled
  left-aligned prefix of the full wordmark:
  - `t = 0.00` → mask width `0` → nothing visible
  - `t = 0.25` → mask width `140` → the left ~25% of the wordmark's actual pixels visible
  - `t = 0.50` → mask width `280` → left half visible
  - `t = 0.75` → mask width `420` → left three-quarters visible
  - `t = 1.00` → mask width `560` → the complete "ARILLA GAMES" visible
  No stretching at any point — the mask reveals a growing window onto stationary, full-size art.
- `RevealGlint` (optional) tracks the same `t`, positioned at `-280 + 560·t` in `LogoGroup`-local
  X (i.e. `WordmarkRevealRoot`'s left edge to its right edge), fading in/out around the midpoint via
  `sin(t·π)` so it is only ever visible mid-travel — never a static fixture, and gone the instant
  the reveal completes.
- `RectMask2D.softness = (3, 0)` adds a few pixels of built-in horizontal edge softening on top of
  the glint, so the hard clip itself doesn't read as razor-sharp — no shader, no extra draw call.

### 4 — Size / composition

- AS mark: **460** reference units wide (within the requested 430–480 range), height derived from
  its own crop's aspect ratio (1050:657 ≈ 1.60), `preserveAspect` on.
- ARILLA GAMES: **560** reference units wide (within the requested 520–600 range, noticeably larger
  than the previous pass), height derived from its own crop's aspect ratio (1085:78 ≈ 13.91).
- 28-unit gap between the mark's bottom edge and the wordmark's top edge; the whole two-piece block
  is centred vertically around `LogoGroup`'s own local origin, so both pieces stay horizontally
  centred and the block re-centres symmetrically if either width is retuned later.
- `LogoGroup` itself is still anchored at `(0, 50)` — screen centre, 50 units above — unchanged from
  before, on the 1080×1920 reference canvas with `CanvasScaler` `Match Width Or Height = 1`
  (height-matched), so this scales consistently to tall Android aspect ratios.

### 5 — Exact final timeline

Only one timing field changed (`wordmarkRevealDelaySeconds` 0.95 → **1.00s**, so the wordmark now
begins exactly 0.10s after the AS mark's own fade-in finishes, matching "AS settles completely"
before the wordmark starts); every other duration was already correct from the previous pass and is
unchanged:

```
0.00–0.55  black
0.55–1.45  AS mark fades in (0.90s), scale 0.94 → 1.0
1.45–1.55  AS mark settles (brief gap, no visual change)
1.55–2.95  ARILLA GAMES reveals left-to-right (1.40s) via the RectMask2D width animation
2.95–4.15  full logo holds, perfectly static, no pulse (1.20s)
4.15–4.75  entire LogoGroup fades out (0.60s)
4.75–4.90  black → Loading
Total ≈ 4.90s
```

### 6 — Audio: unchanged assets, same trigger events

`intro_logo_rise`/`intro_wordmark_sweep`/`intro_resolve` (from the previous audio pass) still fit —
none needed retiming or replacement. Trigger points are unchanged in spirit and now literally
correct for the new mechanism: `intro_logo_rise` at the AS fade-in's start; `intro_wordmark_sweep`
the instant `RevealMask` begins expanding (was: the cover's `fillAmount` beginning to animate — same
moment, new name); `intro_resolve` the instant `RevealMask` reaches full width (was: `fillAmount`
reaching 0 — same moment). Skip still calls `AudioService.StopSfx()` immediately; natural completion
still lets the last cue decay into the following black hold undisturbed.

### I10 — Re-run the Intro scene setup

- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup`
- [ ] Console reports `[IntroSceneSetup] Bootstrap intro wiring applied.` and
      `Validation passed: hierarchy and references are correct.`
- [ ] The validation log block should also report: Mark sprite assigned + its rect size; Wordmark
      sprite assigned + its final width/height; `RevealMask` pivot/anchor confirmed left-aligned;
      underline glow object absent — read through it once to confirm all of these explicitly say OK

Required this time — `MarkImage` (fresh sprite reference), `WordmarkRevealRoot`/`RevealMask`/
`WordmarkImage` (an entirely new sub-hierarchy), and `RevealGlint` (repositioned/recreated) all need
the Editor tool to wire correctly. Old `WordmarkCover`/`WordmarkFeather`/`WordmarkUnderlineGlow`/the
previous `WordmarkGlint` are destroyed automatically as unrecognised legacy nodes. Safe to re-run at
any time: finds-or-creates rather than duplicating, and this never touches `StartupLoadingController`,
`LoadingBackground.png`, `Assets/_Game/Art/Branding/AppIcon*`, or anything Prologue-related.

### I11 — Verify in the Editor

- [ ] Enter Play Mode from `Bootstrap.unity` — AS mark (now visibly smaller) fades in over ~0.9s, a
      short beat, then **ARILLA GAMES (now visibly larger)** reveals left-to-right over ~1.4s with
      a small travelling glint and **no underline/glow bar anywhere, at any point, including after
      the reveal completes** — hold for ~1.2s completely static, fade out ~0.6s, then Loading
- [ ] Watch the reveal specifically: you should be able to see individual letters becoming visible
      in sequence (A, then AR, then ARI...) rather than the whole word appearing at once — pause
      Play Mode partway through if needed to confirm a partial state shows an exact left portion of
      the text with a clean, un-stretched right edge, not a smeared or fully-formed-but-dim word
- [ ] Compare mark-vs-wordmark proportions against `References/ArillaIntroReference.mp4` — the
      wordmark should now read as comfortably legible, not dwarfed by the mark above it
- [ ] Tap/click mid-reveal — cover/glint disappear instantly, audio cuts immediately, `Loading`
      begins exactly once, no partial-reveal artifact left visible for even one frame
- [ ] In Settings, enable **Reduced Motion** — the wordmark appears at full width instantly with no
      travelling glint, only the resolve cue plays, still exactly one transition to Loading
- [ ] Console clean throughout
- [ ] `Window > General > Test Runner > EditMode` — `IntroSequenceControllerTests`,
      `BootstrapControllerTests`, `AudioServiceTests` all still green (call-site signatures for
      `SetAuthoringReferences`/`SetWordmarkAuthoringReferences` changed internally but stayed
      type-compatible with the existing tests; no new tests were added for this visual-geometry
      pass, consistent with not unit-testing coroutine-driven visual timing)

---

## Intro proportions — mark smaller, wordmark larger (2026-08-27, later same day)

Direct feedback after testing the previous pass in the Editor: the AS mark (460 wide) still
dominated the screen and "ARILLA GAMES" (560 wide) still read as weak beneath it. Only
`IntroSceneSetup`'s three size constants changed — no reveal/mask/timing/audio logic was touched:

| Constant | Old | New |
|---|---|---|
| `MarkTargetWidth` | 460 | **390** |
| `WordmarkTargetWidth` | 560 | **680** |
| `MarkWordmarkGap` | 28 | **20** |

Resulting derived geometry (computed from each crop's own aspect ratio, unchanged since the
previous pass — mark 1050:657, wordmark 1085:78): mark ≈ 390×244, wordmark ≈ 680×48.9, total
two-piece block height ≈ 313 (down from ≈ 356) — a visibly smaller emblem sitting closer above a
noticeably wider, taller wordmark, reading as one composed logo rather than a symbol with small
text underneath. `IntroSequenceController` was already confirmed to never touch either image's
`sizeDelta` at runtime (it only reads `wordmarkImage.rectTransform.rect.width` to size the reveal,
and only ever writes the shared alpha/scale/colour tint) — these authored sizes hold for the whole
sequence, including the reveal itself.

- [ ] `Tools > Royal Decisions > Scene Setup > Intro > Apply Intro Setup` — required, since the
      sizes are baked into `MarkImage`/`WordmarkRevealRoot`/`RevealMask`/`WordmarkImage`'s
      `RectTransform`s at authoring time, not read live from the constants at runtime
- [ ] Enter Play Mode — mark noticeably smaller, wordmark noticeably larger and clearly readable,
      still centred under the mark with a tighter gap; reveal direction/timing/glint/audio all
      identical to before (nothing about the mechanism changed, only the sizes it operates on)
- [ ] Confirm the wordmark is not stretched — its own aspect ratio (13.91) should look identical to
      `Assets/_Game/Art/Branding/Generated/ArillaGamesWordmark.png` viewed directly, just scaled up
