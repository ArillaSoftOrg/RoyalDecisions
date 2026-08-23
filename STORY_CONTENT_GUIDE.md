# Story Content Guide

How the "Sığınak: Saltanat Günlükleri" story is represented in code, and how to extend it. This
covers the *mechanics*; the narrative itself lives in `Hıkaye.md` at the repo root.

Everything described here is ordinary data on `RoyalDecisions.Data` types — adding, editing or
correcting a card never requires touching `RoyalDecisions.Domain`, `Application`, `Composition` or
`Presentation` code. See `CLAUDE.md` §4 and §7 for why that boundary matters.

## Where the content lives

`Assets/_Game/Scripts/Editor/StoryContentLibrary.cs` plus its six chapter partners
(`StoryChapter1Cards.cs` .. `StoryChapter6Cards.cs`, one per Bölüm) build the 250-card catalogue in
memory. `StoryContentGenerator.cs` writes that in-memory content to
`Assets/_Game/Content/Story/` as real `CardDefinition`/`EndingDefinition` assets, via
`Tools > Royal Decisions > Generate Story Content` — the same idempotent, overwrite-guarded,
never-hand-edit-YAML pattern the placeholder generator already uses (`CLAUDE.md` §4).

Run the generator again after any change to the chapter files; it updates existing assets in place
(no new GUIDs, so scene/Inspector references survive) and reports created/updated/unchanged counts.

## Adding a card

Call the shared `Card(...)` helper from the appropriate chapter file:

```csharp
Card(number, "Speaker (Role)", "Body text.",
    Choice("Left preview", authority: 1, forcedNext: nextNumber),
    Choice("Right preview", wealth: -1, forcedNext: otherNumber))
```

- `number` becomes the ID `story_k{number:D3}` (matching the specification's `Kn`).
- Every story card is authored `OncePerRun` and `ForcedChainOnly` automatically — see "Forced
  chains" below for why.
- The four named stat parameters on `Choice(...)` map to the specification's resources: see the
  mapping table in `StoryContentLibrary.cs`'s class remarks (`Wealth` = Erzak, `Security` =
  Barınak, `People` = Toplum Sağlığı, `Authority` = Toplum Morali).

## Forced chains (story progression)

The story is driven entirely by `ChoiceDefinition.ForcedNextCardId` — `Choice(..., forcedNext: 42)`
sets the next card regardless of normal selection. This is why every story card is authored
`ForcedChainOnly`: without it, cards from a branch the current run never took would sit in the
catalogue, never having been shown, and once the forced chain ran out normal (non-forced) weighted
selection would start drawing them completely out of narrative order. `ForcedChainOnly` makes
normal selection skip the whole story unconditionally, so running out of forced-next cards produces
a clean "no eligible card" stop — the correct way to end a hand-authored branching chapter — instead
of a scrambled continuation. See `CardDefinition.ForcedChainOnly`'s doc comment.

Leave `forcedNext` at its default (`0`) only on the specification's actual final card (today,
K250) — that is what makes reaching it the story's genuine end. Everywhere else, a missing
forced-next is very likely a bug: `ContentValidator` reports it as `TerminalForcedChainOnlyCard`
(information) so you can tell an intentional ending from a forgotten one.

## Story flags

Plain string flags via `flagsAdd`/`flagsRemove` on a `Choice(...)`, exactly like the placeholder
content. Reused for two purposes:

- **Gating a `CardVariant`** (see below) — `RequiresFlag("some_flag")` / `VariantIfFlag(...)`.
- **Gating card or choice eligibility** — `CardConditions` on `Card(...)`'s `conditions` parameter,
  or a choice's `availability` parameter (see "Conditional choice availability" below).

Convention used throughout the story chapters: when the specification describes a choice as
"A) sets flag X" / "B) (implicitly, the opposite)", only flag X is created — the "otherwise" case is
expressed as the *absence* of X, not as a second flag. Keep following this unless a later card
genuinely needs to distinguish three or more prior states, in which case give each its own flag.

## Story counters

For values that accumulate rather than merely being present or absent (the specification's
`pharma_arastirma`, `vertak_ipucu`): `counterDeltas: Counter(StoryContentLibrary.CounterX, delta)`
on a `Choice(...)`. Read one back with `RequiresCounterAtLeast(counterId, minimum)`, the counter
equivalent of `RequiresFlag`. Declare new counter IDs as `public const string` fields on
`StoryContentLibrary` next to the existing ones — never a raw string literal at the call site.

## Conditional card variants

When the specification's text (and/or choices) for a card differ depending on an earlier decision
("*Eğer flag ise:* ... *değilse:*"), author the "otherwise" branch as the card's own base fields and
the flagged branch as one `CardVariant`, passed via `Card(...)`'s `variants` parameter:

```csharp
Card(28, "Ömer (Gözcü)", "<base/otherwise body text>",
    Choice("Artır", ...), Choice("Gerek yok", ...),
    variants: new[]
    {
        VariantIfFlag("cit_yaklastik_evet", "<flagged body text>",
            Choice("Ateş et", ...), Choice("Dinle", ...))
    })
```

Resolution (`RoyalDecisions.Domain.CardVariantResolver`, used by `GameSession` at presentation
time) is **first-matching-variant-wins**, in the order the array lists them; a card with no
matching variant renders its own base fields untouched. A variant may override only *some* fields —
pass `null` for `bodyText`, `left`/`right` to keep the base card's — see K32 or K155 in
`StoryChapter2Cards.cs`/`StoryChapter5Cards.cs` for a choice-only override, and K133 in
`StoryChapter4Cards.cs` for several variants layered by specificity ahead of a base fallback
(compound flag conditions first, single-flag conditions after).

`CardConditions` (used both for variant conditions and card/choice eligibility) supports required
flags, forbidden flags, stat ranges, and numeric conditions (stat/counter/leader-health thresholds)
combined with AND semantics — build one with `RequiresFlag`, `RequiresCounterAtLeast`, or the
`CardConditions` constructor directly for anything more specific.

## Conditional choice availability

Pass `availability:` (a `CardConditions`) to `Choice(...)` when a side should not be confirmable at
all under some condition — not currently used by the authored chapters, but supported end-to-end:
`ConditionEvaluator.IsChoiceAvailable` → `CardVariantResolver` marks the resolved side unavailable →
`GameSession.ConfirmDecision` refuses it (`SessionErrorCode.ChoiceUnavailable`) → the swipe
controller itself never lets a drag or tap confirm that side (`CardSwipeController.
SetSideAvailability`, wired from `UnityGamePresenter.ShowCard`) and the card shows no preview text
for it. Prefer this over hiding a choice with placeholder text like "[Unavailable]".

## Leader-risk and reign-succession ("Yıkıcı") choices

Both of the specification's recurring "the same choice is safe unless a value is already critical"
patterns share one mechanism, `ConditionalChoiceEffect`, passed via `Choice(...)`'s
`conditionalEffect` parameter:

- **`LeaderRisk(leaderHealthDeltaWhenFalse, deltasWhenFalse)`** — fatal (triggers a reign
  succession) when leader health is already below `LeaderHealthBounds.CriticalThreshold`;
  otherwise applies the given leader-health delta (and, optionally, stat deltas).
- **`Reign(condition, resetStat)`** — an unconditional or otherwise-gated destructive choice: when
  `condition` holds, the run's leader changes and `resetStat` resets to
  `GameConstants.ReignSuccessionResetStatValue` (3) instead of sitting at its boundary.
- **`ReignIfCritical(stat, atOrBelow, deltasWhenSafe, resetStat)`** — the common case of `Reign`:
  destructive only once `stat` is already at or below a threshold; a small, ordinary delta
  (`deltasWhenSafe`) otherwise.
- **`AlwaysLeaderHealth(delta)`** — a plain, unconditional leader-health change with no risk
  attached (most "👑±N" annotations in the specification that are not a Lider Riski/Yıkıcı card).

A succession resets leader health to full and advances `RunState.ReignNumber`, atomically with the
rest of the decision, before `GameOverEvaluator` ever inspects the run — see `ChoiceResolver`'s
`ApplyConditionalEffect`.

## "Variable" (değişken) outcomes

`randomOutcome: new RandomStatOutcome(optionA, optionB, ...)` on a `Choice(...)` picks one of the
given `StatDeltas` through the run's seeded random source (`SeededRandomSource.
ForChoiceResolution` — deterministic per run seed and turn, decorrelated from card-selection's own
draw). Use this for the specification's "değişken X/Y" outcomes; do not invent a fixed number where
the specification deliberately leaves one unspecified.

## Validation

`RoyalDecisions.Domain.ContentValidator` (run automatically by `StoryContentGenerator` before it
writes anything, and directly by `StoryContentLibraryTests`) checks, among other things:

- every forced-next target (base card *and every variant*) resolves to a real card;
- no cycle in the forced-chain graph (an error: a cycle would be genuinely inescapable);
- every `ForcedChainOnly` card is reachable — by a forced-next edge from somewhere, or by being the
  opening card (warning otherwise: dead, unreachable content);
- a `OncePerRun` card with more than one incoming forced-next edge (warning: worth a human glance —
  confirm at most one of those paths can be live in a single run);
- a `ForcedChainOnly` card with no forced-next anywhere (information: an intentional ending has
  exactly this shape, but so does a forgotten forced-next — see "Forced chains" above);
- the usual duplicate-ID, missing-choice, ending-coverage and flag-usage checks shared with the
  placeholder content.

Run `Tools > Royal Decisions > Content Authoring`, drag the generated `StoryContentCatalogue.asset`
into it, and read the validation panel at the bottom — or just run the generator, which refuses to
write anything at all if validation reports an error.

## Wiring the story into the Game scene

`Tools > Royal Decisions > Scene Setup > Use Story Catalogue In Game Scene` repoints
`GameSceneController.catalogue` (in `Game.unity`) at the generated `StoryContentCatalogue.asset`
and saves the scene — the same `SerializedObject`-based edit `ContentAuthoringWindow` already uses
for catalogue references, never hand-edited YAML. Its counterpart, `Use Placeholder Catalogue In
Game Scene`, points it back. Both are idempotent and report what they changed (or that nothing
needed to).

## Adding a whole new chapter

Add a new `StoryChapter7Cards.cs` following the existing five files' shape (a `static partial class
StoryContentLibrary` contributing `internal static List<CardDefinition> CreateChapter7Cards()`),
and add the call to `StoryContentLibrary.CreateCards()`. Move the previous final chapter's closing
card's forced-next from empty to the new chapter's first card — only the specification's true final
card should ever have no forced-next.
