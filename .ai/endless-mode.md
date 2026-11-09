# Endless: the night goes on

Eric, 2026-11-08:

> "we want to also unlock endless mode once you beat the original campaign"

**Decided 2026-11-08.** Eric read the summary - unlocked by taking back the Throne (and the chapter's own
warden beaten), chosen in the run when the warden falls (the vigil beside the way home), the warden
returning every quarter hour with the field's strength climbing by the minute, a wall that ends every run
by about 90 minutes, a capped purse (600 a run), three Feats, records per chapter, wizard and difficulty,
only the first night counting for the campaign's deeds, the far shore left out, no cutscene - with all
five questions of section 6 answered as recommended, and said:

> "Build it"

**Status: built 2026-11-09 (section 5, E0 to E6).** Every number in sections 1 and 3 was a first value; section 7
says what was built, where it departs from this design, what was measured and what moved - above all the night's
strength (7.3), which the measurement reshaped.

No endless mode exists today. "Endless" in the code means only that the procedural ground streams
forever (`TerrainStreamer`). "The original campaign" is the road: the 13 chapters of
`StageCatalog.CampaignOrder`, ending with the Throne. The far shore (`FarShoreOrder`) is not part of it.

Read with: `.ai/run-pacing.md` (the pace clock, the XP economy, the swarm that closes in),
`.ai/difficulty-curve.md` (chapter multipliers, the healing budget), `.ai/endgame-castle.md` (the
Throne, Boss Rush), `.ai/slow-start.md` (one locked thing on show), `.ai/second-half.md` (the far shore,
reel E), `.ai/achievements-and-secrets.md`, `.ai/world-map.md` (the card), `.ai/performance.md`.

---

## 0. In one paragraph

After the Throne is taken back, every chapter of the road whose warden the player has beaten can be
**held through the night**. The run is an ordinary run until its warden falls at 15:00. Then, beside the
way home, a ring of his own light opens on the ground: **the vigil**. Step into the portal and the run
ends as it always has. Stand in the vigil and the night goes on: the field comes back, a little stronger
every minute, and the chapter's warden is sent again every quarter hour. Each fall offers the portal and
the vigil again. Nothing new spawns that the phone has not already carried: the enemy cap stays 140,
only health and damage climb, so a night ends the way a lost chapter does, with the swarm closing in.
**The first night is an ordinary run in every way the save can see**; what comes after pays its own
records, three Feats and a capped purse of Arcane Energy, and never a campaign deed more cheaply.

---

## 1. What it is

### 1.1 The precedents

| game | where it is chosen | what a cycle is |
|---|---|---|
| Vampire Survivors, Endless | a toggle on the stage select | at 30:00 the Reaper does not come; the clock goes back to 0:00 and the waves replay. Each cycle: enemy max health +100% of base, spawn frequency and amount +50%, enemy damage +25% |
| Halls of Torment, Agony | a toggle per map, opened by beating that map's Lord | a harder, untimed version of the map, more loot |

Sources: the Vampire Survivors wiki
([Stages](https://vampire-survivors.fandom.com/wiki/Stages)) and
[Prima Games on Agony Mode](https://primagames.com/gaming/halls-of-torment-what-is-agony-mode).
Principles only, as always.

What we take: a cycle with a count the player can say ("I made night four"), health and damage that
climb, no end but a death. What we do not: **more spawns** (our 140 cap is the phone's, and spawn supply
is the XP ceiling, `.ai/run-pacing.md` 3.3, so more spawns would pay XP and cost frames), and **the
clock going back to 0:00** (our opening five minutes would be dead time against a level-40 build, and
every timestamp stored against `timeElapsed`, such as `nextFormationTime`, would break).

### 1.2 Where it is chosen: in the run, at the warden's fall (recommended)

Today a warden's fall drops a chest, opens the way home a little past the body, and holds the field (no
trickle, no elites) until the player steps in (`Node2DGame.CompleteBossVictory`, `OpenVictoryPortal`).
In a run that can go on, that moment gains one thing:

- **The vigil**: a ring of the player's own warm light on the ground, on the far side of the chest from
  the portal. Drawn in code (the Light ramp, as `DrawPortalGlyph` draws the portal's badge), so no PNG.
- **Standing in it for 1.5 seconds keeps the vigil**: the portal closes, the night begins. A fill arc
  round the ring shows the hold, so a player walking across it to the chest does not trigger it.
- **No timer.** The field is held as it is today until he chooses. Dying before he chooses ends the run
  as the victory it already is (`OnPlayerDied`'s rule, unchanged).
- The banner reads *"The way home is open. Or keep the vigil, and the night goes on."* The first time a
  save ever sees it, a second line: *"Stand in your light. He will send his warden again."*

**Why here and not on the map.** The first night of a run that can go on is an ordinary run, by
construction (1.4). A toggle chosen before it would decide nothing the vigil does not decide better and
later: at the fall the player knows whether his build is worth keeping. It also costs the world-map card
nothing (its focus stops are Start, Journal and Back, with left and right taken by the map,
`.ai/world-map.md` 10, and the phone's card has no room for a toggle row), and there is no forgotten
toggle. The card still advertises it and carries the records (3.4). The alternative, a card button that
commits before the run, is question 1.

### 1.3 Which chapters

| chapter | endless? | why |
|---|---|---|
| Enchanted Forest, Fallow Fields, Broken Circle, Sunken Cave, Blighted Swamp, Mystic Ruins, Frozen Waste, Scorched Sands, the Emberdeep | **yes** | open or walled ground that streams forever, and a warden who fights in it |
| Cursed Dungeon (`stage_1`) | **yes** | an endless maze; its rules are only the stair at the start (`DungeonRules`) |
| The Last Bridge (`stage_11`) | no | a band that ends at the gate yard, and a front that collapses on the pace clock |
| The Court (`stage_12`) | no | one fixed building, explored and finished; a second night has nothing left to find |
| The Throne (`stage_13`) | no | not a map: the Muster, the procession, the story's end |
| ~~The Sketchbook (`stage_8`)~~ | removed 2026-11-09 | it was endless for its records only (`EndlessCatalog.SketchbookId`, `IsRecordsOnly`, both gone); schema 28 takes its records out of every save |
| The far shore | **not now** | its own half, still being built, with its own system (question 5) |

**A chapter is endless on a save when** the Throne is taken back (2.1) **and** the chapter's warden is
in the lifetime record (`LifetimeStats.HasDefeatedBoss`). The second half is Boss Rush's rule: nobody
meets a returning warden before he has beaten it where it lives. It is read once, at the run's start,
as every flag is. A chapter's first-ever clear never offers the vigil: that fall is the chapter's own
moment (its deed, the opened chapter).

### 1.4 How a run goes

| run clock | what happens |
|---|---|
| 0:00 - 15:00 | **the first night**: the chapter exactly as it is today. Same pace clock, side event, secrets, elites, Warden, boss |
| 15:00 | the warden; at its fall, the chest, the portal and the vigil |
| the vigil kept | a 6 s breather (Boss Rush's `BossRushBreatherSeconds`), the run's music back, the banner *"Night 2"*, the clock runs again |
| 15:00 - 30:00 | **the second night**: the field at its 15:00 state, its strength climbing every minute (1.5) |
| 30:00 | the warden returns (its entrance, the field held, as every boss arrives); its fall offers the portal and the vigil |
| every 15:00 after | the same: a night, a warden, a choice |

The run clock stops in every fight and while a fall is held, as it does today, so "30:00" is always
fifteen minutes of field. The timer reads *"Night 2 · 21:40"*. The pause screen's Run tab gains one line
in player words: *"The night: enemies +120% health, +30% damage"*.

**The returning warden** is the chapter's own, at half its chapter health times the night's strength at
the moment it arrives (`ReturningWardenShare` 0.5): the second night's warden is 1.1 times the first's,
the third's 1.7, the fourth's 2.3. Target: a fight of one to three minutes for the build that reached it.
A fall pays a chest where it fell.

### 1.5 How it scales

> **Superseded by measurement (E5, section 7.3).** The curve below was the design's first value; as built the
> strength climbs +12% health and +3% damage a minute for the second night and then compounds (x1.09 and x1.04 a
> minute), because the linear hour below never caught a farmed build. The shape of the night - one number,
> health and damage only, the field otherwise the 15:00 field - is as written.

One number grows: **the night's strength**, read from `m`, the minutes of run clock since the first
warden fell. It multiplies on top of the chapter's row (`ChapterDifficulty`) and the preset.

| clock | night | enemy health | enemy damage | warden returns at |
|---|---|---|---|---|
| 0 - 15:00 | 1 | x1 | x1 | 15:00, the chapter's own fight |
| 15 - 30:00 | 2 | x1 to x2.2 | x1 to x1.3 | 1.1 x chapter health |
| 30 - 45:00 | 3 | x2.2 to x3.4 | x1.3 to x1.6 | 1.7 x |
| 45 - 60:00 | 4 | x3.4 to x4.6 | x1.6 to x1.9 | 2.3 x |
| 60 - 75:00 | 5 | x4.6 to x5.8 | x1.9 to x2.2 | 2.9 x |
| 75:00 on | 6+ | **x1.08 a minute** from x5.8 | **x1.04 a minute** from x2.2 | every 15 minutes |

```
health(m) = 1 + 0.08 m                      for m <= 60, then health(60) x 1.08^(m - 60)
damage(m) = 1 + 0.02 m                      for m <= 60, then damage(60) x 1.04^(m - 60)
```

Per night that is +120% health and +30% damage, about Vampire Survivors' +100% and +25% a cycle.

- **What it reaches**: every ordinary spawn's health (`UpdateSpawnScaling`, so elites built from a spawn
  carry it), the Warden's health at its spawn, the returning warden's, and `EnemyDamageScale` (every hit
  an enemy lands passes through `Enemy.ScaleOutgoingDamage`, so Weaken still works on it). Ticked once a
  second, never per frame.
- **What does not move**: the enemy cap (140), the burst window, the spawn interval and batch sizes, the
  elite cap, the Warden's interval, the swarm-pressure knobs. The field is the 15:00 field, made of
  tougher bodies.
- **How a night ends**: a build that stops keeping pace with the strength stops clearing the field, the
  trickle chokes on the cap, and the swarm closes in (`Node2DGame.SwarmPressure.cs`). The difficulty doc's
  rule holds here too: an overrun is a lost night.
- **The wall** (question 3): from an hour past the first fall the strength compounds and doubles about
  every nine minutes. At 90:00 on the clock enemies carry 18 times their 15:00 health; at 105:00, 58 times.
  Every run is over by about an hour and a half on the clock (100-110 minutes of real time, with the
  fights). That bounds the phone's session, the purse (3.2) and the record table, and it makes the record
  a race to the wall rather than a test of patience.

**Targets for the tuning** (E5, the farmed profile of `.ai/difficulty-curve.md` 1.2, `--survivor`):

| bot | target |
|---|---|
| ordinary policy, farmed save, the Forest | overrun in night 2 or 3 |
| ordinary policy, farmed save, the Emberdeep | overrun in night 2 |
| planned policy (`--seek-ascensions`), farmed, its best chapter | fells the fourth warden |
| any bot | never past 90:00 on the clock |
| a returning warden | one to three minutes for the build that met it |

### 1.6 The clock past 15:00

Today the clock stops at the boss and never runs past `TimerVictorySeconds`. In a night it does. Every
reader of the pace clock was checked:

- **Already flat by 15:00, untouched**: the spawn interval (floor 0.18 s from pace 11.4), batch sizes
  (flat from 11), horde speed (from 5), elite interval (30 s from 12), formation shapes, the late tables
  (from 5), the music lead (from 5), swarm pressure's start (2:00).
- **Still growing past 15:00, held at their 15:00 value**: spawn health's per-minute line, the Warden's
  `1 + 0.22 x PaceMinutes`, and the XP multiplier (`XpPaceMultiplier`, x2.5 at 15:00, which is where it
  freezes at the boss today). One line does it: `PaceMinutes` is clamped to 15
  (`ReferenceRunSeconds / 60`). It changes nothing before 15:00, so every measured table in
  `.ai/run-pacing.md` stands.
- **Read as raw `PaceSeconds`, and meant to keep running**: the elite schedule (`ShouldSpawnElite`), the
  Warden's first arrival. Pinning the pace clock instead, as the Throne does, would stall the elites,
  whose next arrival is a pace time that a pinned clock never reaches.
- **One-shot by pace, done in the first night**: stage hints (last at 13:30); the Sketchbook's ghost was the
  other until the chapter was removed (2026-11-09). Nothing to do.

The night's strength is the only thing that grows. That is the point of holding the three: one number
to tune, one number the pause screen can state.

### 1.7 What the player has for so long

- **Levels keep coming, slower.** XP stays at its 15:00 multiplier and every level costs 45 more than the
  last. Modelled from the 10.4 income at 15:00 (`.ai/run-pacing.md`): about a level a minute at level 40,
  one every 80 s by level 60, fewer as the night's strength cuts the kill rate. No level cap: the curve
  is its own brake.
- **Everything a run builds toward fits in a night**: every Ascension, the Apotheosis (level 21 and the
  wizard's own spell ascended), a Conjunction or two (which also frees a page), the Sovereign Rite
  (level 28, three active spells). A night is where the build a 15-minute run only begins gets finished.
- **The book fills** around level 45-55 by the count of picks (six spells at 8 and the boons; not
  measured). From then every level-up is
  **Marginalia**: heal 30%, +2% spell damage. Today the +2% compounds (`damageMultiplier *= 1.02`):
  harmless at the handful a 15-minute run can take, but sixty of them would be x3.3, an exponential that
  could outrun any linear strength. **Recommended: make it add** (+2% of the base each, so sixty are
  +120%). At five the two differ by under half a percent, so it is a change for nights only.
- **Its heal is the one heal a night keeps growing.** At a level a minute it is about 15 HP a minute on
  a 50-HP wizard, near what a farmed Emberdeep is offered today (`.ai/difficulty-curve.md` 10.2). The
  health ledger (E5) says whether it outruns the night's damage; the lever, if it does, is a smaller heal
  in a night, not the ordinary run's card.
- **Chests run dry.** A night at the end-state cadence carries about 25 chests (elites every 30 s, 65%
  carrying, and the Warden every 165 s); the pool is 39 relics. Once the run holds every relic it can
  take, `GetChestItemOptions` pads the hand with owned ones, which `Player.AddChestItem` ignores: a chest
  of dead cards. **Recommended: carriers carry nothing once the pool is dry** (they still pay 50 XP).
- **Rerolls and bans stay per run** (`.ai/difficulty-curve.md` 7): ten rerolls across an hour are a
  decision, as intended.

### 1.8 How it ends

| how | the outcome |
|---|---|
| steps into the portal at a fall | **Victory**, as today, with the endless tile (3.4) |
| dies in a night | the defeat's sequence plays (it is a death); the results' heading reads *"Fell in the third night"*, not *"Defeat"*: the save already has the first night's victory (3.1) |
| quits from the pause screen | *"Run ended"*, as today; the nights and the clock are recorded |

---

## 2. The unlock, and how it is taught

### 2.1 The save fact

**`GlobalStatsManager.HasTakenBackTheThrone(save)`**: the deed *Taken Back* (`throne_taken_back`) is done,
or the dark wizard (`throne_dark_wizard`) is in the lifetime record. It is the same fact that opens the
far shore's reveal and owes reel E, so the game has one meaning for "the campaign is beaten" in everything
that comes after it. (`HasFinishedCampaign`, which opened the Sketchbook's page until that chapter was
removed on 2026-11-09, is nearly the same; it also asks that the Throne be earned on the road. Endless reads
the deed's fact, as the far shore does.)

### 2.2 Before: nothing on show

Nothing of endless is shown before Taken Back: no card line, no Feat row, no vigil. Boss Rush is the
precedent (it is not a stop until it is open, `.ai/world-map.md` 5). This keeps Eric's *"one locked thing
on show at a time"* (`.ai/slow-start.md`, Q8): until the Throne falls, the Throne is that thing. After it,
endless is shown **open**, never sealed, so it does not become a second locked thing beside the far
shore's sealed first stop.

### 2.3 The run that wins the Throne: a results entry, not a reel (recommended)

That run already ends on the most the game ever plays in a row: the ending's row and credits, the freeing
of the one he kept (Nimue), **reel E** (*The Light Goes On*, `Node2DGame.PlayOwedReelsThen`), then any
lesson owed, then the results. A fifth beat would bury the story's end under a menu feature.

- **Its results list one more opened thing** (after the Sketchbook's page, *"The road is finished"*, until
  that chapter's removal on 2026-11-09): **Endless** - *"Every chapter whose warden you have beaten can be held
  through the night."* A new `RunUnlockKind.Mode`, so it wears its own glyph rather than a chapter's.
- **No reel, and no `Teaching`.** The slow start's rule is that a *system used inside a run* is explained
  by a post-run cutscene. Endless is a way a run can go on, and it is explained where it is used: the
  vigil's banner at the next fall says what it is in one line. Boss Rush, the other mode, has no reel
  either.
- **Order beside reel E**: reels first, story before systems, as `PlayOwedReelsThen` already orders them;
  the results, with the endless entry, after all of it. Reel E is untouched.

### 2.4 After: the card, and the first fall

- **The world-map card** of an endless chapter gains one glyph-led line under the warden's state, after
  "Longest stand": *"Longest night: Night 3 · 41:20"* once there is a record, otherwise *"Endless: keep
  the vigil when Elderbark falls"*, marked new until the save keeps its first vigil
  (`SaveData.HasKeptVigil`). No new focus stop, no new button.
- **The first warden's fall** in an endless chapter shows the vigil with its two-line banner (1.2).

### 2.5 A save that won the Throne before this ships

Nothing announces it but the card's line, marked new, and the vigil at the next fall. No migration, no
reel to owe: there is nothing to replay, and a toast on the map would be a second message on a screen
that may be opening with reel E.

---

## 3. Rewards and records

### 3.1 The ledger: the first night is an ordinary run

**The rule.** What the save learns from a run that kept the vigil is exactly what it would have learned
had the player stepped into the portal at the first warden's fall. At that fall the run's result is
taken as a victory (`SnapshotRunResult`, which already builds a victory once `bossVictoryPending`), with
its telemetry, element peaks and Arcane payout, and held as the **campaign ledger**. Everything the save
records about the run reads the ledger, not the whole night:

- `LifetimeStats.Absorb` (kills, chests, level-ups, playtime, best level, best time on the stage, best
  element counts, the hares, every total);
- `AchievementManager.ApplyRunAchievements` (every run-measured deed, `MaxDifficultyCleared`, so the
  Hardcore clears the Dark Wizard's deed counts);
- `ApplyBossVictoryUnlocks`, `RecordRunTelemetry` (the winning streak), the improvement bonus;
- the one-second achievement watch and the pause screen's tab, which already read `SnapshotRunResult()`:
  past the first fall it returns the ledger, so nothing measured completes mid-night.

**Places count; measures do not.** A side event won or a secret found in a night is recorded the moment
it happens, as it is today (`CompleteNow`, `SecretDirector.Found`): it was found, not measured, and a
night is no cheaper road to it than a run. That includes the spell vaults, which the far shore's gate
counts through *The Grimoire Restored*, and which `.ai/second-half.md` question 6 already wants pointed
at after Taken Back. **A Conjunction first made in a night is not recorded** (`RecordCombinationsMet`
skips past the first fall): he may take it in the run, but its Feat waits for a run that makes it in the
first night (question 4). The far sky, the half's reason to make combinations, is not undercut by a mode
that gives unlimited time to make them.

What a long night would otherwise have cheapened, and now cannot:

| deed | measured over | in a night |
|---|---|---|
| Survivor, Untouchable, Archmage Training, Full Grimoire, Elementalist and the Adepts | one run | the first night |
| The Coursing (100 hares as Laelaps, one run) | one run | the first night |
| every Conjunction Feat, the Sovereign Rite's Feat | the first time it is met | the first night |
| The Taker Taken, The Road Restored | each chapter's best clear by preset | the first night's clear, which is one |
| Exterminator, Elite Hunter, the Library at Mortlake, Relic Hunter, Attuned | lifetime totals | the first night's |
| side events, secrets, vaults, keepsakes | found | **counted when found** |

### 3.2 Arcane Energy

| part | pays | why |
|---|---|---|
| the first night | the ordinary payout, exactly: base, minutes, level, the boss's bonus, every multiplier (`CalculateArcaneReward` on the ledger) | it is an ordinary clear |
| each later night | 8 a minute of clock (`ArcanePerMinuteSurvived`, the same multipliers), half of each returning warden's bonus (Boss Rush's `ArcaneBonusShare`), no level term | the level term would pay for levels a night makes cheap |
| **the cap** | the later nights pay **at most 600 a run** (`EndlessCatalog.ArcaneCap`) | about one more clear of a late chapter |
| endless's Feats | their own once-only rewards, outside the cap | deeds pay once |

**Why it cannot be farmed.** A later night pays about 300 for about 17 minutes (15 of field and a one-
to three-minute fight), 15-20 a minute. An ordinary clear pays about 450-750 (base, minutes, level, the
shop's multipliers and the boss's 150-450) for 18-20 minutes, its fight lasting three to five: 25-40 a
minute. So a night always pays less per minute than replaying a chapter, and the cap only trims
outliers. These are worked from `CalculateArcaneReward`'s terms, not measured (E5 measures them). A night won by
the portal or lost to the swarm pays the same: the minutes it lasted and the wardens it felled.

### 3.3 Feats

Arcane Energy only, never an unlock: endless is the far shore's *"achievements welcome, unlocks not
required"*, and a Feat that paid a spell or a wizard would put a gate behind a mode. Counted on Default
or harder, on the road's endless chapters only. Listed only once endless is open (a new `AchievementVisibility.Endless`,
as the far sky's Feats wait for the far sky).

| Feat | asks | pays |
|---|---|---|
| **Into the Night** | keep the vigil, and fell the warden a second time in one run | 150 |
| **The Long Watch** | fell the warden four times in one run (60:00 on the clock) | 400 |
| **Every Vigil Kept** | fell the warden a second time in one run on every endless chapter of the road (ten) | 500 |

Measured from the records (3.4), so each completes at a fall, in the night, as a deed done should.

### 3.4 Records

- **One record per chapter, wizard and preset**: wardens felled, then the run clock (the timer the
  player watched), and the level. A run replaces it when it fells more wardens, or as many in a longer
  clock. Per preset, so the card shows the record for the difficulty he is about to play.
- **Written at every fall** and saved there, not only at the end: an hour-long run killed by the phone
  (a call, a low battery) keeps the nights it held. Everything else is written at the end, as for every
  run (runs are not saved mid-run; that is unchanged).
- **The results** gain an endless headline tile beside the chapter's, built as Boss Rush's is
  (`GameOverScreen.BuildBossRushTile`): *"Night 3 · 41:20"*, caption *"3 wardens felled"*, and *"a new
  record for the Pyromancer here"* when it is one.
- **The card**: this chapter's best for the selected wizard on this preset, and the best of any wizard
  (2.4).
- The "Longest stand" line keeps meaning a run's stand: it reads `BestTimeByStage`, which the ledger
  keeps at the first night's time.

### 3.5 The save

| field | what | default |
|---|---|---|
| `SaveData.EndlessRecords` | list of `EndlessRecord` (`StageId`, `CharacterId`, `PresetId`, `WardensFelled`, `ClockSeconds`, `Level`, `When`), the best per key | empty |
| `SaveData.HasKeptVigil` | the first vigil kept: the card's new mark and the first fall's second banner line | false |

**No schema step.** Both are additive and default to the quiet state, the pattern of `HasSeenLightGoesOn`
and `BossRushBestFelled`; nothing a save holds is narrowed or reinterpreted, and the ledger rule means no
existing record changes meaning. `CurrentSchemaVersion` is 25 at this writing. If a step turns out to be
needed, take the next free number **checked on main at the time** (two branches bumping it at once
collide, CLAUDE.md), and every Godot run on the branch passes `-- --no-save-writes`. The story epoch is
not raised.

---

## 4. What must not break

| what | how it is kept |
|---|---|
| the campaign's deeds and Feats | the ledger rule (3.1): every measure reads the first night |
| a 15-minute run | the first night is an ordinary run. The two shared changes (`PaceMinutes` clamped at 15, Marginalia adding) change nothing before 15:00; E0 proves it with a before/after harness run |
| Hardcore | the preset multiplies with the night's strength. The first night's Hardcore clear counts as one (it is), so *The Taker Taken* is unchanged. Records are per preset |
| the far shore | never endless (`EndlessCatalog` lists road chapters only, and the validator says so). Endless writes nothing its gate reads but vaults found in a night, which its question 6 wants found anyway. The far sky is never named in a night |
| Boss Rush | a run with `Global.BossRush` is never endless. `BossRushCatalog` and its records are untouched; the night's strength is never applied in a rush |
| the Throne and the story | not an endless chapter; endless opens only after it; reel E and the ending's order are untouched |
| the pacing and difficulty tables | nothing before 15:00 moves; endless runs never feed those tables, as the Throne's do not (their own lines, E5) |
| the healing budget | Marginalia's heal measured on the ledger (1.7); no new heal |
| saves | no schema step; tool runs pass `--no-save-writes` |

### 4.1 Performance over a long run

A night adds **no bodies**: the cap stays 140 and the burst window stays, so a frame in night 4 is a
frame at minute 12. `.ai/performance.md` measured that frame: on the PC at the cap 80-115 fps (12.5 ms at
minutes 6.5-8), and on the Pixel 9 a held 60 fps with the scripts using 4.4 ms. What grows with time is
not the swarm but what it leaves:

| grows | today | in a long night | guard |
|---|---|---|---|
| live enemies | 140 cap | the same | none needed |
| XP orbs far behind | folded into one bank past 4,200 px, every 2 s (`SweepFarXpOrbs`) | the same | none needed |
| XP orbs near him | a 15-minute run bounds them | an hour at up to 7 kills a second; a player who circles leaves rings of them, each an `Area2D` with a `_Process` | **past 300 loose orbs, the oldest fold into a bank** in the same 2-second sweep (`XPOrb.Absorb`, already there) |
| chests | about 17 a run | about 25 a night | carriers stop when the pool is dry (1.7) |
| terrain | a streamed window of chunks | the same | none needed |
| position | - | a straight walk of an hour is about half a million pixels out, where a float32 still resolves 1/16 px | measure with `--walk` |
| the night's own work | - | one tick a second | no per-frame work, nothing allocated per hit |
| the phone | 60 fps at the cap | heat and battery over 90 minutes | measured on the Pixel 9 (performance.md section 6), not assumed |

The wall (1.5) is also what bounds the session: no run outlasts about 100-110 minutes of real time.

---

## 5. Build plan

The castle's and the far shore's pattern: plumbing first, then one piece per worktree. **S** a day or
less, **M** a few days, **L** a week or more.

| # | piece | size | needs |
|---|---|---|---|
| E0 | **Plumbing**: the catalog, the save fields, the two shared one-line changes, the validator | S | - |
| E1 | **The night**: the vigil, the nights, the strength, the returning warden, the dry pool, the orb cap, the HUD lines; the harness's `--endless` | L | E0 |
| E2 | **The ledger**: the snapshot at the first fall, the run's end reading it, the Arcane rule | M | E1 |
| E3 | **Results, card, records, the Throne run's entry** | M | E2 |
| E4 | **The three Feats** | S | E2 |
| E5 | **Measurement and tuning**: the strength curve, the warden's share, Marginalia's heal, performance | M (mostly wall clock) | E1-E3 |
| E6 | **Docs**: run-pacing (the clock past 15:00), difficulty-curve (the night), world-map (the card line), CLAUDE.md (one line), this doc's "as built" | S | E5 |

E3 and E4 can run in parallel once E2 lands.

### E0, plumbing (S)

- `scripts/EndlessCatalog.cs` (new): `IsEndlessChapter(stage)`, `IsOpen(save)`, `IsOpenOn(save, stage)`,
  `HealthStrength(m)`, `DamageStrength(m)`, `ReturningWardenShare`, `ArcaneCap`, `WallMinutes`,
  `MaxLooseOrbs`.
- `scripts/SaveData.cs`: `EndlessRecords`, `HasKeptVigil`, their read and write; `EndlessRecord` beside it.
- `scripts/Node2DGame.cs`: `PaceMinutes` clamped to `ReferenceRunSeconds / 60`.
- `scripts/Player.cs`: `TakeMarginalia` adds rather than compounds.
- `scripts/RegressionChecks.Endless.cs` (new) and its line in `RunAll`: **`ValidateEndless`** - every
  endless chapter is playable, on the road (the Sketchbook too, until its removal on 2026-11-09), never the Bridge, Court, Throne or far
  shore, and has a warden with a scene; both strength curves start at 1, never fall, and damage never
  goes under 1; the wall comes after the linear part; the cap is positive.
- **Verify**: `dotnet build`; the validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes
  --validate-content`); one `_BalanceHarness --minutes=15 --fixed-fps 30 --timescale=1` before and after
  on the same seed: the same `lvByMin`.

### E1, the night (L)

- `scripts/Node2DGame.Endless.cs` (new partial): eligibility at the run's start; the vigil at a fall; the
  kept vigil (portal closed, boss flags cleared, the breather, the music, the banner); `m` and the
  strength, ticked once a second into spawn health, the Warden, `EnemyDamageScale`; the next warden at
  `TimerVictorySeconds x night`; the loose-orb fold; the record written at each fall.
- `scripts/VigilRing.cs` (new): the code-drawn ring and its hold, a `Node2D` on a `DrawLayers` floor layer.
- `scripts/Node2DGame.cs`, call sites only: `CompleteBossVictory` (offer the vigil), `TickBossPhaseStart`
  (the next warden's time), `SpawnBoss` (the returning share), `UpdateSpawnScaling` and the Warden's
  health (the strength), `OpenVictoryPortal` (the banner), `UpdateRunTimerHud` (*"Night 2 · 21:40"*),
  `DropCarriedChest` (the dry pool).
- The pause screen's Run tab: the night's line.
- Sounds: the portal's and the event reward's, reused; a sound of its own is the audio manifest's to add.
- `scripts/_BalanceHarness.cs`: `--endless` (keep every vigil), `--leave-at=N`, a `HARNESS NIGHT` line at
  every fall (clock, level, strength, kills a minute, pressure, Marginalia taken, HP a minute offered and
  lost), and `nights` / `endlessClock` in `HARNESS SUMMARY`.
- **Verify**: `dotnet build`; the validator; `_BalanceHarness --endless --immortal --minutes=95` reaches
  the wall and every warden returns; `_UiShot` of the fall (vigil and portal) and of the night's timer at
  1280x720 and at 540x1212 `--portrait --safe-area`. **Whether the vigil reads as a choice needs eyes.**

### E2, the ledger (M)

- `scripts/Node2DGame.Endless.cs`: the ledger taken at the first fall (`SnapshotRunResult`, its element
  peaks, its Arcane breakdown).
- `scripts/Node2DGame.cs`: `FinishRunAndReward` reads the ledger for the save and the live run for the
  results; `SnapshotRunResult` returns the ledger past the first fall; `RecordCombinationsMet` skips past
  it; `CalculateArcaneReward` adds the capped later nights.
- `scripts/RegressionChecks.Endless.cs`: a synthetic night-3 run feeds the save exactly what its first
  night would have; the cap holds; a vault found in a night is still recorded.
- **Verify**: the validator; a `_BalanceHarness --endless --all-unlocked` run's save diff (with writes off,
  printed) against the same seed's portal exit at the first fall.

### E3, results, card, records (M)

- `scripts/RunReport.cs`: the nights, the clock, whether it is a record.
- `scripts/GameOverScreen.cs`: the endless tile; the heading for a death in a night.
- `scripts/WorldMap.cs`: the card line (2.4), only once endless is open on the chapter.
- `scripts/AchievementManager.cs`: `RunUnlockKind.Mode`; `Node2DGame.ApplyBossVictoryUnlocks`: the entry
  on the run that first takes the Throne back.
- `scripts/_UiShot.cs`: a flag to force an endless results screen and a card with a record.
- **Verify**: `_UiShot` of the results (a record, a death in night 3, a portal exit in night 2), the card
  before and after a record, and the Throne run's results list (`--scene=res://scenes/node_2d_game.tscn
  --stage=13 --campaign-complete --unbeaten=stage_13 --force-victory`, as F5 photographed it), each at
  1280x720 and 540x1212 `--portrait --safe-area`; ink contrast by `tools/art/ui_contrast.py`.

### E4, the Feats (S)

- `scripts/AchievementDefinition.cs`: the three Feats and `AchievementVisibility.Endless`; `IsListed`
  reads `EndlessCatalog.IsOpen`.
- **Verify**: the validator (`ValidateAchievements`; `ValidateGateReachability` and `ValidateUnlockCatalog`
  unchanged, since none pays an unlock); `_UiShot` of the achievements page before and after Taken Back.

### E5, measurement and tuning (M)

`--fixed-fps 30 --timescale=1` (`.ai/run-pacing.md` 10.5), one Godot at a time, seed 1234 and a second:

```
"$GODOT_BIN" --fixed-fps 30 --headless --path . scenes/_BalanceHarness.tscn -- --endless --farmed
    --survivor --immortal --policy=ordinary --stop-overrun=60 --stage=<0|4|7> --character=<1..6>
    --minutes=100 --no-save-writes
```

- Against the targets of 1.5: move the strength slopes (0.08, 0.02) first, the warden's share second.
- The health ledger: Marginalia's heal against the night's damage; cut its heal in a night if it outruns.
- Performance: one 75-minute run with `--profile=30`; `nodes=`, `orbs=` and `alloc=` must be flat after
  the first night. Then the Pixel 9, by `.ai/performance.md` section 6.
- Arcane: the purse a minute from these runs against an ordinary clear's (3.2).

**What no harness checks:** whether a night feels like more rather than only slower; whether the vigil
reads as a choice and not a hazard; whether a fourth Elderbark is a fight or a chore; how a phone feels
in the hand at minute 80.

---

## 6. Questions for Eric

All five **decided 2026-11-08, as recommended** ("Build it").

1. **Where the choice is made.** In the run, at the warden's fall: the way home or the vigil
   (recommended) - or a button on the world-map card, chosen before the run, as Vampire Survivors and
   Halls of Torment do? **Decided: in the run, at the fall.**
2. **What a night is.** The warden is sent again every quarter hour, the field growing stronger by the
   minute between (recommended) - or no boss after the first, only the climb - or Vampire Survivors' loop,
   the clock back to 0:00 and the chapter replayed stronger? **Decided: the warden every quarter hour,
   the field climbing between.**
3. **The wall.** Should the night close in for certain from an hour after the first fall, so every run
   ends by about an hour and a half (recommended) - or grow without end? **Decided: the wall.**
4. **What a night may earn.** Places found in a night count, but everything measured over a run - a
   Conjunction first made included - counts only the first night (recommended) - or should a Conjunction
   first made in a night count? **Decided: places count, measures read the first night; a Conjunction
   first made in a night waits for a run that makes it in the first.**
5. **The far shore.** Leave it out of endless until its six seats are built, then decide (recommended) -
   or plan for it now? **Decided: left out.**

---

## 7. As built (2026-11-09)

Built in one branch, E0 to E6, in the order of section 5. Where the build departs from sections 1-4 it says so
here; everything not named below is as written above.

### 7.1 What is where

| piece | files |
|---|---|
| the catalog: chapters, the open rule, the strength, the shares, the cap, the Feats' ids, the records, **the one run-length shim** | `scripts/EndlessCatalog.cs` (`EndlessRecord` beside it) |
| the night: eligibility, the vigil, the ledger, the strength tick, the returning warden, the orb fold, the dry pool, the HUD and pause lines, the purse, the records, the tools' hooks | `scripts/Node2DGame.Endless.cs`; call sites in `Node2DGame.cs`, each marked "Endless" |
| the ring and its hold | `scripts/VigilRing.cs` |
| the results tile, heading and purse line | `scripts/GameOverScreen.Endless.cs`; `RunReport.Endless*`, `ArcaneRewardBreakdown.Night*` |
| the card line | `scripts/WorldMap.Endless.cs` |
| the Feats | `scripts/AchievementDefinitions.Endless.cs`, `AchievementVisibility.Endless` |
| the opened-mode entry | `RunUnlockKind.Mode`, its icon `reward-vigil` (`tools/art/reward_icons.py`, a candle in its ring of light) |
| the save | `SaveData.EndlessRecords`, `SaveData.HasKeptVigil` - additive, **no schema step** |
| the validator | `scripts/RegressionChecks.Endless.cs`, `ValidateEndless` in `RunAll` |
| the tools | `_BalanceHarness --endless --leave-at=N --warden-limit=S` (`_BalanceHarness.Endless.cs`); `_UiShot --endless-open --endless-steps=N --endless-after=F --endless-night=N,CLOCK --endless-leave --vigil-stand=K --endless-record=... --kept-vigil` (`_UiShot.Endless.cs`) |

**The run-length shim.** Every "past the boss" rule - the pace clamp, the returning warden's quarter hour, the
first night's end, the results bar - reads `EndlessCatalog.ChapterVictorySeconds(stageId)`, and nothing else. Since
the far shore's twenty-minute runs merged it reads their accessor, `StageCatalog.VictorySecondsFor`: 900 on every
endless chapter, 1200 on the far shore, which is never endless. `Node2DGame.EndlessVictorySeconds` caches it once a
run; the clamp divides it by `RunPace`, so a longer chapter holds its pace at its own boss.

### 7.2 Departures from the design, each a decision the doc left open

- **The ledger is taken when the first vigil is kept**, not at the fall: it is exactly what stepping into the
  portal at that moment would have recorded, chest and last XP included, which is the rule of 3.1 read literally.
- **A vigil opened under his feet waits for him to step out once** before it counts a hold, so a fall beside him
  never keeps it for him.
- **The ring is added to the ground, not painted** (an additive material) and has a small upright flame at its
  heart: the first photograph, painted, read as a tan stain on the Forest's dirt.
- **The banner is two lines**, never three (a third overran the band's panel): *"The way home is open - or keep
  the vigil. / Stand in your light, and the night goes on."* the first time, *"... or keep the vigil, / and the
  night goes on."* after, and at a later fall *"Elderbark falls · the second night is held / The way home is open -
  or keep the vigil."* Keeping it shows *"Night 2 / He will send Elderbark again at 30:00."*
- **The pause tab's section is two lines**: *"Night 3: enemies +212% health, +53% damage"* and *"2 wardens felled ·
  Elderbark returns at 45:00"* - the clock is already the tile above it.
- **The results tile is the clock over "Night 3 · 2 wardens felled"**, with *"A new record"* under the bar; a death
  in a night is headed *"Fell in the third night"* at the Title size on a PC, wrapping in its column. The whole
  "Night 3 · 41:20" as the headline number, and the record as a long caption, both pushed a PC's band past 1280.
- **Records are written only by a run that kept a vigil** (a plain clear is not an endless run), at every later
  fall (saved there) and at the run's end.
- **The dry pool**: once the run holds every relic it can take, a night's carriers carry nothing *and a returning
  warden's fall sets out no chest* (the design named the carriers only; a warden's chest of dead cards is the same
  problem). Both, and the loose-orb fold, act in a night only, so the first night is untouched. The pool counts
  every relic not held, the Scribe's Penknife included - so the harness's bot, which never takes the knife, never
  dried its pool and opened 218 chests in a 100-minute night (193 from elites, at the 30-second cadence), most of them
  repeats it ignored. A player who takes the knife dries it; one who never does keeps a knife and two dead cards a
  chest. Whether that wants the knife left out of the count is a playtest's.
- **The card's other-wizard line** reads *"Merlin's longest: Night 4 · 52:30"* under this wizard's own; the night
  and its clock are joined by non-breaking spaces.
- **Sound**: the event reward's sting when the vigil is kept (the portal's own plays at the fall); a sound of its
  own is still the audio manifest's to add.
- **`_BalanceHarness --endless --farmed`** stands behind the whole road wherever it plays: endless is open only to
  a save that took the Throne back, so the farmed profile at the Forest is the whole road's (8,750 Arcane, every
  road deed), not the Forest's 300.

### 7.3 Measured, and what moved

**Protocol.** `_BalanceHarness --fixed-fps 30 --headless -- --endless --farmed --survivor --immortal
--policy=ordinary --stage=<0|7> --character=1 --seed=1234 --minutes=100 --timescale=1 --no-save-writes`, two at a
time, on a copy of the tree. **The save is the whole road's**: endless is open only to a save that took the Throne
back, so `--endless --farmed` stands behind every chapter of the road wherever it plays - 35 spells and 8 boons from
deeds, 8,525 of 8,750 Arcane on Powers (Sage Spiral at its cap). `--stop-overrun=60` was given; it fired only once, in the final planned run.
The **E0 check** (`PaceMinutes` clamped, Marginalia adding): a 15-minute `--all-unlocked --immortal` run on seed 1234
before and after read `lvByMin` [3,6,8,11,13,16,18,20,22,24,26,28,30,32,34] and [3,5,8,10,13,16,18,20,22,24,27,29,31,
33,35] - the same within the spawner's unseeded noise; no Marginalia was taken in either.

**Step 1, the design's first values** (+8% / +2% a minute, the wall an hour past the fall at x1.08 / x1.04):

| | Forest (Elderbark) | Emberdeep (the Warden of the Deep) |
|---|---|---|
| first fall | 15:00, level 49 (game 15.2) | 15:00, level 53 (game 19.0) |
| level at 30:00 / 45:00 / 60:00 / 75:00 | 107 / 147 / 178 / 204 | 93 / 129 / 162 / 191 |
| Marginalia by 75:00 | 149 (+298% spell damage) | 136 |
| returning wardens' fights (game minutes) | 1.0, 0.3, 0.2, 0.4, 0.9 | 2.0, 2.5, 2.4, 3.1 |
| where the 100 game minutes ended | **night 7, 1:37:00, x31.6 health**, 6 wardens, swarm pressure 0 | **night 6, 1:25:58, x13.5**, 5 wardens, pressure 0 |

Kills sat at the spawn supply (about 420 a minute) every minute of every night, the wizard lost almost no health
(the field died before it reached him) and swarm pressure never left zero. **Why**: a night pays two to four levels a
minute (the XP multiplier is held at x2.5 and the kill rate is the supply; Sage Spiral and the relics make it x3.46
besides), so the book fills by level 50 and every level after is Marginalia's +2% spell damage - about 5% of the
build a minute, as fast as the linear strength. And the build's area spells kill a capped field with large room to
spare. The design's model (a level a minute at level 40, 1.7) was off by three to four times.

**Step 2, compounding from the third night** (+12% / +3% a minute for the second night, then x1.09 / x1.04):

| | Forest | Emberdeep |
|---|---|---|
| night strength at 30:00 / 45:00 / 60:00 / 75:00 / 90:00 | x2.8 / 10.2 / 37 / 135 / 493 | the same |
| level at those falls | 106 / 146 / 177 / 204 / 229 | 106 / 148 / 182 / - |
| returning wardens (half the chapter health x the strength) | x1.4, 5.1, 18.6, 67.7, 246 - each felled in 0.3-1.3 minutes | x1.4 in 1.6 min, x5.1 in 2.5, **x18.6 in 7.0**, **x67.7 (34 million) never** |
| the end | **night 7, 1:33:17, x653**, kills still at the supply; the field at the cap from night 6 (alive 104-128 of 131, at the cap a third of each minute) | **stalled at 1:15:00** in the fifth warden's fight to the end of the budget |

Two findings: the field's strength must become a wall in fact past an hour (the Forest bot was killing x653 bodies
at the supply - 66,000 health each - at 1:33), and a returning warden cannot ride the field's strength (a single
body meets area damage once; the Emberdeep's fourth was seven minutes and its fifth unwinnable, and a fight the
build cannot finish holds the clock - nothing ends the run but the wizard's death).

**As built (step 3):** three stretches of one curve, and the warden on its square root.

```
m <= 15 (night 2)         health 1 + 0.12 m          damage 1 + 0.03 m
15 < m <= 60 (nights 3-5) x1.09 a minute             x1.04 a minute
m > 60 (the wall)         x1.25 a minute             x1.10 a minute
returning warden          chapter health x sqrt(health strength)
```

| clock | night | health | damage | the warden who ends it |
|---|---|---|---|---|
| 15:00 - 30:00 | 2 | x1 - 2.8 | x1 - 1.45 | x1.7 |
| 30:00 - 45:00 | 3 | x2.8 - 10.2 | x1.45 - 2.6 | x3.2 |
| 45:00 - 60:00 | 4 | x10.2 - 37 | x2.6 - 4.7 | x6.1 |
| 60:00 - 75:00 | 5 | x37 - 135 | x4.7 - 8.5 | x11.6 |
| 75:00 - 90:00 | 6, the wall | x135 - 3,850 | x8.5 - 35 | x62 |

**Measured on the final curve.** The strongest build of the three steps - the Forest, ordinary policy, the
whole-road farmed save, Aedan, seed 1234 - run alone with the profiler (`--profile=30 --warden-limit=240
--minutes=95`, never needed: every warden fell on his own):

| fall | clock | level | Marginalia | warden | fight (game minutes) |
|---|---|---|---|---|---|
| 1 | 15:00 | 50 | 0 | Elderbark, 9,500 | 0.4 |
| 2 | 30:00 | 94 | 39 | x1.7 | 0.5 |
| 3 | 45:00 | 130 | 75 | x3.2 | 0.3 |
| 4 | 1:00:00 | 160 | 105 | x6.1 | 0.6 |
| 5 | 1:15:00 | 188 | 133 | x11.6 | 0.5 |
| 6 | 1:30:00 | 211 | 156 | x62 | 0.6 |

Nights 2 to 5 it killed at the spawn supply (410-430 a minute) with swarm pressure at zero. **The wall closed it at
about 1:28**: at game minute 90 the field sat at the cap, pressure went from 0 to 0.70 in two minutes, the swarm's
swells and trail recycler began, and by 1:32:03 kills had fallen to 122 a minute at pressure 1.00 - an overrun,
which a mortal wizard taking x43 hits does not outlive. So the strongest build measured ends at the wall, as Eric
decided; any weaker one ends sooner.

The **Emberdeep** on the final curve (ordinary, Aedan, seed 1234; this run's bot levelled slower than step 2's, 66 at
30:00 against 106): first fall at 15:00 (level 52, a 4.8-minute fight), then the returning Warden of the Deep at
x1.7 (836,664) in **3.3 minutes**, x3.2 in **3.6**, x6.1 (3 million) in **4.9**, and x11.6 (5.8 million, at 1:15:00)
still standing when the budget ran out 8.4 minutes later - every night held at pressure 0 until then. The
Emberdeep's own first fight is about five minutes (`.ai/difficulty-curve.md`), so nights 2-4 now meet a warden about
as long as the chapter's, against step 2's seven minutes and an unwinnable fifth; the fifth is still long. The
square root is the knob (an exponent of 0.4 would make the fifth x7 rather than x11.6).

The **planned bot** (`--seek-ascensions`) on the Forest, its best chapter, with another wizard (character 3) and
seed (77): level 64 at the first fall, 118 / 151 / 181 / 205 / 224 at the next five; every returning warden felled
(x1.7 to x62) in 0.6-1.7 minutes; **overrun at about 1:33** (swarm pressure full from game minute 98.8, kills down to
172 a minute, the run ended by `--stop-overrun` at 1:34:27, night 7, x10,381 health). It fells the fourth warden, as
the design asked, and the sixth.

**Performance over that run** (frame-weighted, PC, headless, the bot's own cost included; by quarter of its frames):

| part of the run | enemies | frame p50 / p95 ms | phys / proc ms | nodes (max) | loose orbs (max) | alloc KB/frame (max) |
|---|---|---|---|---|---|---|
| first night | 44 | 6.9 / 13.0 | 2.8 / 2.5 | 4,382 (4,966) | 207 (347) | 23.8 (29.5) |
| nights 2-3 | 39 | 7.3 / 13.4 | 2.7 / 2.6 | 4,548 (4,936) | 143 (215) | 25.7 (36.1) |
| nights 4-5 | 105 | 10.6 / 18.3 | 4.5 / 3.0 | 4,757 (5,223) | 125 (209) | 41.5 (56.5) |
| the wall | 130 | 15.4 / 30.7 | 6.1 / 6.0 | 5,462 (6,179) | 88 (137) | 106.6 (133.2) |

Nodes and orbs follow the field, not the clock: flat through nights 2-5 at the first night's level, and up only when
the wall fills the field to the cap (the 300-orb fold was never reached here; it held the step-1 Emberdeep's at 300
while its bot stood at a fight's edge). The walk reached 20,000 px from the start with no sign of float error.
**Allocation is the one number that grew**: about 25 KB a frame through nights 2-5 and 107 at the wall, where every
body takes dozens of hits (take-damage calls went from under one a frame to fifteen, floating numbers from 9 to
324 alive). The endless code allocates once a second (the timer's text) and once every two seconds (the orb fold's
group read); what grew is the hit path under a field of 400,000-health bodies, which no ordinary run reaches.
`.ai/performance.md`'s 5 KB a frame at the cap was a weaker build; this farmed one already makes 24 in the first
night. Worth a look there, not a fault of the night. **The phone was not touched.**

**Against the targets of 1.5**, honestly:

| target | measured |
|---|---|
| ordinary, farmed, the Forest: overrun in night 2 or 3 | **missed**: the whole-road farmed ordinary build holds every night to the wall (overrun about 1:28). Nights 2-3 as the usual end would need a second night many times steeper, which would make the wall the first thing a weaker Throne-beater meets |
| ordinary, farmed, the Emberdeep: overrun in night 2 | **missed**: it held night 4 (step 2 and final) and was stopped by its fifth warden at 1:15 |
| planned, farmed, its best chapter: fells the fourth warden | **met**: six, overrun about 1:33 |
| any bot never past 90:00 | **about**: the two strongest measured close at 1:28 and 1:33 - the wall Eric chose, "by about an hour and a half"; an earlier wall start (`WallMinutes`) is the knob for a hard 90 |
| a returning warden: one to three minutes | Forest 0.3-1.7 minutes every night; Emberdeep 3.3, 3.6, 4.9, then the fifth over 8.4 (its own first fight is about 5) |

**The levers left, Eric's.** What keeps a farmed build alive is Marginalia's damage and the night's XP, not the
strength: two to four levels a minute, each +2%. Cutting Marginalia's damage in a night (its heal was the design's
named lever, 1.7), or letting the night's XP multiplier fall, would let the strength curve be gentler and make the
nights a slower climb rather than a wall at an hour. Neither is in the design, so neither was done.

**Kept as built** (Eric, 2026-11-09, shown the two misses above: "Keep as built"). The two missed targets stand
as the measured behaviour, not as faults to chase; the levers stay where they are until play says otherwise.

**The purse** (3.2), from these runs' minutes (the harness ends a run without paying it): every later night pays 8 a
minute and half each returning warden's bonus, so a Forest run to the wall pays its 600 cap by about 1:15 (75 minutes
x 8 already 600), about 7 Arcane a minute of night against 25-40 a minute for an ordinary clear. The cap binds
every long run; a run that leaves by the portal at the second fall pays 195 on top of its first night (photographed:
15 minutes, one warden). Endless is never a farm.

**Performance** (4.1): nodes across a 100-minute Forest night held at 3,800-5,200 and loose orbs under 180 (the 300
fold never needed in the Forest; it held the Emberdeep's at 300 while its survivor bot stood at a fight's edge); the
walk reached 20,000 px from the start with no sign of float error. The profiled run is above.

### 7.4 What needs a person's eyes

- Whether the vigil reads as a choice and not a hazard, beside the portal, at a real fall (photographed: it does
  read as a ring of his light with a flame, distinct from the arcane door; the motion was not seen).
- Whether a night feels like more rather than only slower, and whether a fourth Elderbark is a fight or a chore.
- How a phone feels at minute 80 (heat, battery): not measured, no phone was touched.
- The banner's words, the card's line and the results' heading are first wordings.
