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
