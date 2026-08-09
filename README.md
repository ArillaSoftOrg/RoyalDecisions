# Royal Decisions

A portrait, offline, single-player Unity mobile game inspired by the card-swipe decision
genre. The player swipes a card left or right, applies the resulting consequences, and tries to
keep four statistics between critical limits.

All code, text, characters, art, audio, branding, and UI are original. No assets or content are
copied from *Reigns*.

## Status

Work in progress, built up in phases against the specification in [`CLAUDE.md`](CLAUDE.md).
Outstanding manual Unity Editor steps (scene wiring, Player Settings, package imports, etc.) are
tracked in [`MANUAL_UNITY_STEPS.md`](MANUAL_UNITY_STEPS.md).

## Tech stack

- Unity 6.3 LTS (`6000.3.18f1`)
- C#, root namespace `RoyalDecisions`
- uGUI + TextMeshPro
- Unity Input System (pointer-driven swipe)
- ScriptableObjects for static card/ending definitions, versioned JSON for runtime saves
- Unity Test Framework (EditMode + PlayMode)

## Gameplay

- Four stats — `authority`, `people`, `security`, `wealth` — ranging `0..100`, starting at `50`.
- Drag a card left or right; a directional preview fades in as it crosses the confirmation
  threshold, then snaps back or confirms and exits.
- Card selection is deterministic per run seed, and supports flags, conditions, cooldowns,
  one-time cards, weighted selection, and forced follow-up chains.
- The run ends when any stat hits `0` or `100`.
- New game, restart, save, and resume are all supported via versioned JSON saves.

## Architecture

Presentation code never calculates rules; domain code never touches Unity UI types.

```
Presentation -> Application -> Domain -> Data
```

- `GameFlowController` coordinates the core loop.
- `CardDeckService` / `ConditionEvaluator` select and filter eligible cards deterministically.
- `ChoiceResolver` / `StatSystem` apply a choice atomically and own clamped stat state.
- `GameOverEvaluator` selects an ending.
- `SaveService` handles versioned JSON with a safe write/replace strategy.
- `CardSwipeController` only handles pointer movement and confirmation — no story, stat, or save
  logic lives there.

See `CLAUDE.md` §7 for the full boundary rules.

## Content

Twenty placeholder cards and eight placeholder endings (min/max for each stat) ship as test data,
generated through an idempotent Unity Editor command:

`Tools > Royal Decisions > Generate Placeholder Content`

It only writes under `Assets/_Game/Content/Placeholder/`, never overwrites user content silently,
and validates duplicate or missing IDs. Final story content is meant to be replaced without any
gameplay code changes.

## Repository layout

```
Assets/_Game/
  Art/{Temp,Final}
  Audio/{Temp,SFX,Music}
  Content/{Cards,Endings,Placeholder}
  Prefabs/
  Scenes/
  Scripts/{Data,Domain,Application,Infrastructure,Presentation,Editor}
  Tests/{EditMode,PlayMode}
```

## Getting started

1. Open the project in Unity `6000.3.18f1` (matches `ProjectSettings/ProjectVersion.txt`).
2. Run `Tools > Royal Decisions > Scene Setup > Apply Remaining Setup` to materialize the
   generated scene wiring, then `Tools > Royal Decisions > Scene Setup > Validate` to confirm it.
3. Run `Tools > Royal Decisions > Generate Placeholder Content` if placeholder cards/endings are
   missing.
4. Run the EditMode and PlayMode suites from the Unity Test Runner.
5. Work through the checklist in [`MANUAL_UNITY_STEPS.md`](MANUAL_UNITY_STEPS.md) before shipping.

## License

No license has been chosen yet.
