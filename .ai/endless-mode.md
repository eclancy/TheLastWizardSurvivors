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

**Status: being built (section 5, E0 to E6).** Every number in sections 1 and 3 was a first value; the
"Built" notes in section 7 say what was measured and what moved.

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
nothing (its focus stops are Start, Journal, Sketchbook and Back, with left and right taken by the map,
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
| The Sketchbook (`stage_8`) | **yes, records only** | the kids' page can be held all night; nothing there counts toward a Feat (Eric: no achievements there) |
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
- **One-shot by pace, done in the first night**: the Sketchbook's ghost (4:00, 9:30), stage hints (last at
  13:30). Nothing to do.

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
that comes after it. (`HasFinishedCampaign`, which opens the Sketchbook's page, is nearly the same; it
also asks that the Throne be earned on the road. Endless reads the deed's fact, as the far shore does.)

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

- **Its results list one more opened thing**, after the Sketchbook's page (*"The road is finished"*,
  `ApplyBossVictoryUnlocks`): **Endless** - *"Every chapter whose warden you have beaten can be held
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
or harder, never in the Sketchbook. Listed only once endless is open (a new `AchievementVisibility.Endless`,
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
  endless chapter is playable, on the road or the Sketchbook, never the Bridge, Court, Throne or far
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
