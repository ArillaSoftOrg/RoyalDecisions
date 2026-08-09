# Royal Decisions — Codex Instructions

Read this file before every task. Treat it as the binding technical specification.

## 1. Project

Build a portrait, offline, single-player Unity mobile game inspired by the
card-swipe decision genre. The player swipes a card left or right, applies the
chosen consequences, and tries to keep four statistics between critical limits.

Use original code, text, characters, art, audio, branding, and UI. Do not copy
assets or content from Reigns.

## 2. Technical Baseline

- Unity 6.3 LTS; preserve the version already selected by the team.
- C#.
- Android first, iOS later.
- Portrait only.
- uGUI and TextMeshPro.
- Unity Input System with pointer events.
- ScriptableObjects for static card and ending definitions.
- Versioned JSON for runtime saves.
- Unity Test Framework for EditMode and PlayMode tests.
- Root namespace: `RoyalDecisions`.

Do not install packages, upgrade Unity, or edit `ProjectVersion.txt` without
explicit approval.

## 3. MVP

Implement:

- Four stats: `authority`, `people`, `security`, `wealth`.
- Stat range: `0..100`; initial value: `50`.
- One active card with left and right choices.
- Mouse drag in Editor and touch drag on mobile.
- Directional choice preview while dragging.
- Snap back below the decision threshold.
- Confirm once above the threshold.
- Stat deltas, flags, conditions, cooldowns, one-time cards, weights, and forced
  follow-up cards.
- Deterministic card selection using a run seed.
- Game over when any stat reaches `0` or `100`.
- New game, restart, save, and resume.
- Twenty clearly labelled placeholder cards.
- Eight placeholder endings: minimum and maximum for each stat.
- Replaceable placeholder art and silent/missing-audio fallbacks.

Do not implement ads, IAP, backend, accounts, cloud saves, analytics,
achievements, notifications, multiplayer, live services, Addressables, or
runtime AI story generation.

## 4. Content Ownership

All initial story content is temporary test data. The team will later replace:

- speakers and card text;
- left/right choice labels;
- stat effects;
- conditions and weights;
- flags and card chains;
- portraits, audio, animation, and endings.

Final content replacement must not require gameplay code changes.

Generate twenty placeholder cards that collectively test:

- ordinary stat changes;
- required and forbidden flags;
- flag addition and removal;
- one-time cards;
- cooldown cards;
- weighted selection;
- a forced two-card chain;
- `people <= 25`;
- `wealth <= 25`;
- an opening card forced at the start of a run.

Create placeholder content through an idempotent Unity Editor command:

`Tools > Royal Decisions > Generate Placeholder Content`

The generator must only write under
`Assets/_Game/Content/Placeholder/`, must not silently overwrite user content,
and must validate duplicate or missing IDs. Do not hand-author `.asset` YAML.

## 5. Core Loop

1. Load or create `RunState`.
2. Select an eligible card.
3. Present the card and enable input.
4. Preview the choice during drag.
5. Confirm exactly one choice after crossing the threshold.
6. Lock input.
7. Apply stat and flag changes atomically.
8. Save the completed decision.
9. Evaluate endings.
10. Present the ending or next card.

A decision must never resolve or save twice.

## 6. Data

`CardDefinition` should contain:

- unique ID, speaker, body text, portrait;
- left and right `ChoiceDefinition`;
- conditions, selection weight, once-per-run, cooldown;
- optional forced next-card ID.

`ChoiceDefinition` should contain:

- preview text;
- four stat deltas;
- flags to add/remove;
- optional forced next-card ID and audio event ID.

`RunState` must be a serializable runtime model, not a ScriptableObject:

- save version, turn, random seed;
- current stats and flags;
- shown-card history and cooldown data;
- forced/current card IDs and active-run state.

Use stable string IDs for content references. Validate missing and duplicate IDs.

## 7. Architecture

Keep Unity presentation separate from game rules.

- `GameFlowController`: coordinates the loop; does not calculate rules.
- `CardDeckService`: filters and selects cards deterministically.
- `ChoiceResolver`: applies one choice atomically.
- `StatSystem`: owns clamped stat values and change events.
- `ConditionEvaluator`: evaluates card eligibility.
- `GameOverEvaluator`: selects an ending.
- `SaveService`: handles versioned JSON and file errors.
- `CardSwipeController`: handles pointer movement and confirmation only.
- `CardView`, `HUDView`, `GameOverView`: render state; never mutate rules.
- `AudioService`: provides safe audio playback and missing-clip fallback.

Dependency direction:

`Presentation -> Application -> Domain -> Data`

Forbidden:

- domain code referencing `Image`, `Slider`, `TMP_Text`, or scene objects;
- UI changing stats or writing saves directly;
- gameplay rules inside the swipe controller;
- repeated `FindObjectOfType`, `GameObject.Find`, or tag searches;
- global singleton chains;
- direct `UnityEngine.Random` calls across multiple systems.

Inject random, save, and audio boundaries where testing requires it. Avoid
framework-heavy dependency injection and unnecessary abstractions.

## 8. Save Rules

- Save run data as versioned JSON after each completed decision.
- Store settings separately from run data.
- Use a safe temporary-write/replace strategy.
- Detect unsupported save versions.
- Return a controlled failure for corrupt or missing files.
- Never use ScriptableObjects to store player progress.
- Use `PlayerPrefs` only for small preferences, not complete run data.

## 9. Swipe Rules

- Track only the pointer that began the drag.
- Move horizontally and rotate by horizontal distance.
- Fade the relevant choice preview in by direction and distance.
- Use configurable threshold, max rotation, movement multiplier, and durations.
- Snap back when released below threshold.
- Exit the screen when confirmed above threshold.
- Lock input immediately after confirmation.
- Handle cancellation and rapid repeated input safely.

No story, stat, save, or ending logic belongs in `CardSwipeController`.

## 10. Code Standards

- One public type per file; file and type names must match.
- Use English identifiers and `RoyalDecisions.*` namespaces.
- Prefer `[SerializeField] private` over public mutable fields.
- Keep user-facing strings in content data.
- Avoid magic numbers and hidden scene dependencies.
- Subscribe and unsubscribe events symmetrically.
- Do not use silent `catch` blocks.
- Do not use `async void` except Unity event entry points when unavoidable.
- Avoid per-frame allocations in gameplay paths.
- Add comments for reasoning, not obvious syntax.
- Produce no project-code compilation errors or warnings.
- Preserve existing serialized fields and user changes.

## 11. File Boundaries

Preferred structure:

```text
Assets/_Game/
  Art/{Temp,Final}
  Audio/{Temp,SFX,Music}
  Content/{Cards,Endings,Placeholder}
  Prefabs/
  Scenes/
  Scripts/{Data,Domain,Application,Infrastructure,Presentation,Editor}
  Tests/{EditMode,PlayMode}
```

During initial parallel work, you may edit:

- `Assets/_Game/Scripts/`
- `Assets/_Game/Tests/`
- required assembly definitions;
- Editor code for placeholder content generation.

Do not edit unless explicitly requested:

- `.unity`, `.prefab`, or existing `.asset` YAML;
- `ProjectSettings/`;
- `Packages/manifest.json`;
- this `AGENTS.md`;
- unrelated user files.

The user owns Unity scenes, Canvas setup, package installation, Build Profiles,
Player Settings, and Inspector wiring. Record required manual steps in
`MANUAL_UNITY_STEPS.md`.

## 12. Tests

Required EditMode coverage:

- stat increase, decrease, and clamping;
- choice deltas and flag changes;
- required/forbidden flags;
- min/max stat conditions;
- once-per-run and cooldown filtering;
- forced next card;
- deterministic selection;
- no-eligible-card result;
- all min/max ending boundaries;
- save JSON round trip, corrupt save, and unsupported version.

Required PlayMode coverage where practical:

- snap back below threshold;
- one left/right confirmation event;
- input lock after confirmation;
- no duplicate decision during rapid input;
- input re-enabled for the next card.

Never claim tests were run if Unity or its CLI is unavailable. Report exactly
what was run, what failed, and what the user must verify manually.

## 13. Execution Phases

Work in this order and stop after the requested phase:

0. Inspect project, Unity version, packages, repository state, and conflicts.
1. Data/domain models and EditMode tests.
2. Stat, condition, choice, ending, random, and deck services with tests.
3. Placeholder card/ending generator and content validation.
4. Versioned save system and tests.
5. Presentation components with fallback assets.
6. Swipe interaction and PlayMode tests.
7. Game flow integration.
8. Manual Unity wiring and Android test instructions.

For every phase:

1. Inspect related files.
2. Present a short file-level plan.
3. Modify only in-scope files.
4. Run available compilation checks and relevant tests.
5. Report changed files, results, assumptions, and manual Unity steps.

Do not create commits, switch branches, install packages, edit scenes, or
perform unrelated refactors unless asked.

## 14. Definition of Done

The MVP is done only when:

- it runs on an Android device in portrait;
- Safe Area and common screen ratios work;
- mouse and touch swipes work;
- each decision resolves exactly once;
- all four stats and ending boundaries work;
- conditions, flags, cooldowns, one-time cards, weights, and chains work;
- twenty placeholder cards can be replaced without code changes;
- new game, save, resume, and restart work;
- corrupt saves do not block startup;
- missing optional art/audio does not crash;
- relevant tests pass;
- Unity Console has no project-code errors.

## 15. First Command

When first asked to work on this project, perform Phase 0 only:

```text
Read AGENTS.md completely. Inspect the Unity project, selected Unity version,
installed packages, repository status, and current folder structure. Identify
conflicts with this specification. Propose the phased implementation plan and
list the exact files intended for Phase 1. Do not modify any files yet. Do not
edit scenes, prefabs, ProjectSettings, packages, or AGENTS.md.
```
