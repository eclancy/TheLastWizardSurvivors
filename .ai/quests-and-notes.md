# Quests and chapter notes

Eric, 2026-10-31:

> "Starting every new map can have a little context as well, such as the players wizard making a small
> note to themselves about the place they are in, what it smells like or feels like, which wizard is in
> this location that they're trying to rescue, etc. User should be able to track specific quests, and
> those default quests should always be to free the next wizard. Tracked quests should show up on their
> screen while playing."
>
> "When you start a level, the tracked quests should show up as big text on the screen, with a spell
> like animation, and then shrink into the corner."
>
> "Overall keep text minimal."

Read `.ai/world-and-tone.md` (the voice) and `.ai/side-events.md`, "Secret wizards" (a locked wizard's
name is his reward) first.

## 1. The rule that governs all of it

**Nothing names a wizard the player has not freed.** A quest that frees someone is titled by his epithet
("Free the accused"); a note says who is held there by the same epithet; once he is free both use his
name. While the next wizard's chapter is not open yet, the quest does not even say who - only where the
road goes next ("Free the next wizard - Defeat the boss of the Fallow Fields"), because an unrevealed
chapter is not on the world map either.

| wizard | epithet | held line (in his chapter's note) | freed line |
|---|---|---|---|
| Tituba | the accused | The accused is in one of these cells. | Tituba's cell stands open. |
| Väinämöinen | the singer under the ice | Someone is singing under the ice. | Väinämöinen sings above the ice now. |
| Laelaps | the hound in the stone | A hound of stone stands mid-leap, still chasing. | Laelaps has run his chase out. |
| John Dee | the scattered scholar | (no chapter) | John Dee has his library back. |

`QuestLog.Prisoners` holds these. The held line goes into whichever chapter the wizard's rescue deed
belongs to (`GlobalStatsManager.RescueChapterOf`), so when the Cursed Dungeon moves into the castle,
Tituba's line moves with it.

**Changed alongside:** `CodexRows.HidesWizard` now hides every locked non-starter's name, not only the
secret ones. The achievements page said "Frees John Dee" on a new save while the character select called
him "???".

## 2. The notes

Two lines per chapter, a third only where someone is held. The wizard's own voice: terse, first person,
what the place smells or feels like. `ChapterNotes` holds them.

| chapter | note |
|---|---|
| Enchanted Forest | Every path bends back toward the middle of the wood. / Wet bark and leaf mould. The trees are listening. |
| The Fallow Fields | Stubble and thistle, and no one left to bring it in. / Chaff in my throat. Only the well still smells clean. |
| The Broken Circle | The stones where we used to meet. He came here first. / Heather, granite, and a wind with nothing in its way. / + Laelaps's line |
| Sunken Cave | Water dripping everywhere, and my light the only light. / Cold, wet stone. It smells like old coins. |
| Cursed Dungeon | Rust, damp straw and old fear. Every door is locked. / + Tituba's line |
| Blighted Swamp | Reeds, fog, and lanterns nobody living lit. / Rot and standing water. My boots never dry. |
| Mystic Ruins | Our old seat of learning. The wards broke from inside. / Dust and cold ash. The stones still hum. |
| Frozen Waste | Too cold to smell anything. The wind cuts through the robe. / + Väinämöinen's line |
| Scorched Sands | Heat off cracked ground, and no water anywhere. / Hot dust and old salt. The dead still walk the road. |
| The Emberdeep | Sulphur, and stone too hot to touch. / The end of the road. His seat is close. |
| The Sketchbook | Squared paper underfoot. It smells of pencil. / He has never been here. |

The castle's three chapters have no note yet. The validator reports the day one flips playable without one.

**When it shows.** On the first run in that chapter (`SaveData.SeenChapterNoteIds`), never in Boss Rush.
It replaces the chapter banner on that run, so there is one piece of writing about the place, not two.
It does not pause the run.

**The card** (`ChapterNoteCard`, CanvasLayer 7) is a torn scrap of paper, drawn in code. The heading is
bold Cinzel. The lines are the engine sans, slanted by a fifth with a `FontVariation`, so they look
handwritten but stay legible (`.ai/level-up-screen.md` 4a). They are written in letter by letter. On a
PC it sits in the lower right. On a phone it sits just under the HUD band, because the lower half
belongs to the thumb. It goes after about ten seconds, or earlier on a click or tap on the paper, or on
A, Enter or Space. **In the Sketchbook it is a slate of floor stone, not paper**: that chapter's ground
is already paper, and the note must not repeat the material it sits on.

**The journal** keeps every note read, plus every chapter the save has played. It is one sheet of paper
on the stone panel, not a card per note, so the material is not repeated.

## 3. Quests

**There is no new progress system.** Every quest is a reading of an `AchievementDefinition` the game
already measures, through the same `Measure` the achievements page uses. The only thing stored is
`SaveData.TrackedQuestIds`.

- **The default is `next_wizard`**, tracked on every save, new or old, until the player untracks it. It
  resolves each time it is read: the first wizard in `QuestLog.WizardChain` who is not free yet. The chain
  is the road wizards in campaign order, then wizards freed anywhere (John Dee's chests). Laelaps is left
  out, as the gates leave him out, and so is a wizard whose chapter is not built yet. Once that wizard is
  freed - including mid-run, by a side event saved on the spot - the quest moves on with no write.
- **Three tracked at most** (`QuestLog.MaxTracked`). A fourth Track press plays `ui_denied` and its button
  says "3 at most". A deed that is already done is pruned from the tracked list.
- **The log** (`JournalPage`, Quests page) has these sections:
  - Tracked.
  - Wizards: the Next row first, then other held wizards whose chapters are revealed, then the freed ones,
    named, with their portraits.
  - This chapter (on the pause screen only).
  - Chapters: road deeds and secrets seen, only in chapters the world map shows.
  - Feats.

  Each row shows the deed's name, one line of what to do, a count where it has one, and Track. It shows
  no reward; the achievements tab does that. A deed is offered only if `AchievementDefinitions.IsListed`
  allows it and its chapter is revealed (`QuestLog.IsOffered`).
- **Where to find it:** the pause screen's new **Journal** tab (Run / Journal / Achievements / Options;
  two by two on a phone), and a **Journal** button in the world map's top bar, which opens `JournalScreen`
  over the map. While it is open the map hears no keys (`JournalScreen.IsOpen`). Both open on the Quests
  and Notes strip; tracking writes the save at once.

## 4. The HUD corner

`QuestTracker` sits in the **lower left** on both canvases. Every other corner and band was taken:

- health, XP and spells in the upper left;
- element tags in the upper right;
- timer, banner, objective line and boss bar at the top middle;
- the achievement plate in the band under the HUD;
- the camera hole on a phone.

Each quest is a Cinzel heading at Body size, then one sans line with the count. There is no plate
behind it; the text is outlined in occlusion black, which reads on the Sketchbook's white paper too.
It ignores the mouse and sits on the HUD's layer, under the touch stick, so a thumb on it still steers.

`Node2DGame.TickAchievementWatch` refreshes it **once a second** with the run folded in
(`AchievementContext.Projected`, the same reading the achievement moment uses). When an entry changes or
completes, it gives one gold breath.

**The edge pointers slide off it.** The boss badge, chests, the event and places all ride a ring set in
from the screen edge, and the lower left is on that ring. `HudKeepOut` holds the tracker's rectangle (and
the note's, while it is up). Each of the four pointers moves a badge that would land in one of those
rectangles along its own side of the ring.

## 5. The run's opening

`QuestIntro` is code-drawn on CanvasLayer 7 (with `SetAssembledMoment`: above the stick, below the
level-up). It is Pausable and takes no input. It lasts **2.1 s**:

1. **Gather (0 to 0.75 s).** Square motes of gold light converge on each tracked quest's title, set big in
   the band above the player's head, and the letters are written in from left to right behind a hotter
   leading edge. A rule draws out under each title, and the step line fades in under it.
2. **Hold (0.75 to 1.3 s).**
3. **Fly (1.3 to 1.95 s).** Each title shrinks and flies on a curve, out to the left and then down, so it
   never crosses the player. It lands on its own entry in the tracker, which fades in as it lands.

Two or three quests are written smaller, so the block stays above the player. Nothing is allocated while
it plays: the mote pool and the letter offsets are made once.

**Sequence at a run's start:** the opening, then (on a first visit) the note, otherwise the chapter banner
after the opening rather than under it.

## 6. Save

No schema bump. Both fields are additive, like `HasSeenPrologue`:

- `TrackedQuestIds` defaults to `[next_wizard]`. A save without the key keeps that default; an empty list
  stays empty, because that is the player's own choice.
- `SeenChapterNoteIds` defaults to empty, so an existing save reads each note once.

The note is marked seen as it is shown and saved then. `SaveGame` does nothing while writes are off.

## 7. Checking it

- `RegressionChecks.ValidateQuestsAndNotes` checks:
  - every playable chapter has a note, of at most 3 lines and 64 characters a line;
  - every rescuable character has an epithet;
  - **no line the log, the HUD, the opening or a note can show carries a locked wizard's name**, with or
    without diacritics, on four sample saves: new, early, mid-campaign and late;
  - the next wizard exists on a new save and is nobody once everyone is free;
  - the two save fields read back correctly.
- `_UiShot` flags:
  - `--track=a,b`
  - `--notes-seen=all|ids`
  - `--notes-unseen`
  - `--beaten=stage ids`
  - `--name-audit`, which reads every Label, Button and RichTextLabel on screen, plus the note's and the
    tracker's words, for a locked name, and prints PASS or FAIL.

```
xvfb-run -a -s "-screen 0 1400x1300x24" "$GODOT_BIN" --path . --resolution 1280x720 --fixed-fps 60 \
  scenes/_UiShot.tscn -- --scene=res://scenes/node_2d_game.tscn --stage=0 --fresh-save --notes-unseen \
  --track=next_wizard,first_blood --frames=20 --burst=8,18 --shot=qn-pc-intro --no-save-writes --name-audit
```

At 60 fps the opening is mid-gather at frame 38, holding at 74, flying at 92 to 110, and landed by 128.
The note is fully written by about frame 330.

## 8. Status

**Done and checked** (2026-11-01, after merging `origin/main-ff86ni`):

- `dotnet build` is clean.
- The validator reports 0 errors and 6 warnings, the baseline. The new check passes.
- Screenshots were looked at on both canvases. PC is 1280x720; phone is
  `540x1212 --portrait --safe-area`.
  - **The opening**:
    - PC: mid-gather, at the hold, in flight and landed.
    - Phone: at the hold and in flight, curving round the player.
  - **Notes**:
    - PC: the Forest, written in fully in the lower right.
    - Phone: the Frozen Waste under the HUD band, with "Someone is singing under the ice."
  - **The HUD**:
    - PC: two tracked quests with the live "0 / 30".
    - Phone: the next wizard has moved on to "Free the singer under the ice" on a save that freed Tituba.
  - **The pause Journal tab**:
    - PC: both pages.
    - Phone: two-by-two tabs, Tracked, Wizards (Next, the scholar 0/30, the hound in the stone).
  - **The world map Journal**:
    - PC: Quests.
    - Phone: Notes.
- **The name audit passes on every one of those shots.** It runs on:
  - a fresh save;
  - a mid-campaign save (Dungeon open, nobody freed);
  - a late save (Tituba freed).

**Not done (pick up here):**

- Pad and touch walks of the journal, on the pause screen and on the map: the inner tabs, Track, Back,
  and the selection returning to the Journal button. These are reasoned from `MenuNavigator`, not driven.
- The Sketchbook's slate note has never been photographed (`--stage=8 --notes-unseen --frames=330`).
- The "3 at most" refusal has never been pressed.
- No sound is wired to the opening (`.ai/audio-direction.md` would decide one).
- The `StageHintCaption`, which floats near the player, does not read `HudKeepOut`.
- The castle chapters need notes when they are built.
- A judgement call for Eric: on a first visit the note replaces the chapter banner, and the HUD corner
  has no backing plate, only outlined text.
