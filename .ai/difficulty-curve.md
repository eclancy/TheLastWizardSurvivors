# The difficulty curve: chapters that get harder, damage that stays taken, Ascensions that are earned

Eric, 2026-10-30 and 31:

> "ascensions shouldn't be easy to unlock at first, and the game shouldn't be easy to beat. the first
> level can be easy, second should be harder, and then the player should reach walls that require
> strategy, farming powerups, etc."
>
> "rerolls/bans/etc should be per run, not per level up. we should rebalance the amounts that are able
> to be acquired based on that knowledge."
>
> "skip should give you 50% of your exp bar when you choose it."
>
> "remove number from health bar. player should have more health, but there should be a lot less
> sources of getting health back. the game is too easy to survive because damage is so impermanent
> right now."

(He also asked for four spell slots, then withdrew it: "let's stick with 6 spells available to
select." The book stays at six, the Blank Folio's seventh as before.)

This doc is the measurement, the target, the levers and what was built. It is the authority on **how
hard each chapter is meant to be, and how much a run lets the player heal**. `.ai/run-pacing.md` stays
the authority on *when* things happen in a run, `.ai/campaign.md` on the order, `.ai/gameplay-design.md`
on the Ascension and the Apotheosis themselves.

---

## 1. How it is measured

### 1.1 The bot, and what it is asked

`_BalanceHarness --survivor --immortal --policy=ordinary --stop-overrun=60`, `--fixed-fps 30
--timescale=1` (`.ai/run-pacing.md` 10.5: the protocol whose numbers survive a shared machine), 19
minutes so the boss fight fits, seed 1234, the Stormcaller (character 3) on every road chapter, the
Frostweaver (character 2, seed 77) on a few for the range.

- `--survivor` dodges, walks to orbs and chests when the way is clear and backs off from what is close:
  the careful player. `--policy=ordinary` takes cards as a good player who does not plan Ascensions
  does, takes every Ascension the strip shows, and opens every chest.
- `--immortal` lets it play the whole run. What it would have needed to live is the **health ledger**
  (`_BalanceHarness.NeedMultiple`, new): real health lost after armour and shields, every heal it was
  offered (the **healing ledger**, below) and its regeneration, revives, in five-second buckets - and
  the least multiple of the wizard's starting health that lives through all of it.

| column | what it is | why it matters |
|---|---|---|
| **over** | the minute swarm pressure reached 0.9 and held it a minute (`--stop-overrun`, which ends the run) | the swarm's verdict that the build fell behind for good. `.ai/run-pacing.md` 10.4: at full pressure every dodger down to a thirtieth of this bot's damage died within minutes. **An overrun is a lost chapter.** |
| **siege** | minutes the field sat at the trickle's cap at least half the time | how long the player was under siege: a chapter cleared after a nine-minute siege is not an easy one |
| **boss** | when the boss fell (the fight starts at 15:00) | the other way a chapter is lost |
| **need** | the health multiple the bot needed | **a ranking, not a verdict**: the bot dodges far worse than a person (10.4 put a careful player at a twentieth of its damage or less), so it compares chapters and saves on one bot, never says what a person survives |
| **L15** | level at 15:00 | the build the boss meets |
| **asc** | Ascensions taken / when the first was offered | Eric's other half |
| **heal, regen, lost** | HP a minute offered by heals, by regeneration, and lost | the healing audit (section 5) |

### 1.2 Two saves: fresh and farmed

- **Fresh** (`--fresh-save`): starters only, nothing bought. It now also records the bosses of the
  chapters before `--stage` as beaten - the kill and the opened chapter, nothing they pay - because no
  save stands in a chapter without them, and the Ascension's teaching (section 6) reads that record.
- **Farmed** (`--farmed`, new): the player who has farmed sensibly up to this chapter. Every chapter
  before it beaten; every Feat done; every Road deed and secret of those chapters found, so their spells
  and boons are in the pool; and Arcane Energy spent on Powers - **300 + 650 for each chapter behind**
  (one clear and two failures of each: a clear pays about 200 and its boss's bonus, a failure at minute
  eight about 130, `Node2DGame.CalculateArcaneReward`), spent on the cheapest next level among damage,
  Vitality, Emberheart, cooldowns, attack speed, area, projectiles, move speed, pickup, Sage Spiral,
  Phoenix Oath and duration, again and again. `--arcane=N` overrides the budget. At the Broken Circle
  that is 1,600 Arcane (damage 3, Vitality 4, Emberheart 3, cooldowns 2, attack speed 2, area 3,
  projectiles 1, move speed 5, pickup 3, Sage Spiral 3, duration 3) and 15 spells and 3 boons from
  deeds; at the Emberdeep 6,150.

  All Feats from the first chapter on is generous - the five Adepts each want four of an element - and
  it makes the farmed column the strong end of "farmed sensibly". The walls are tuned to it, so a
  player who farmed less meets them harder.

### 1.3 What it costs, and what was skipped

A run is 4-10 minutes of wall clock at three in parallel, longer while a field sits at the cap. **Not
measured:** the other four wizards, a second seed on most cells, the planned and default policies, the
castle (not playable), the Sketchbook, the phone. Read every cell as a direction: two runs of one
cell differ by a level or two and a minute of siege.

---

## 2. Before: the curve as it was

On `main-ff86ni` at 6352ebe (the Gleaning Bell and Spiritual Weapon in) with only the harness changes:
six slots, the Ascension at player level 17 with no meta gate, the old healing. Stormcaller, seed 1234;
the Frostweaver rows are seed 77. Times are game minutes; the boss arrives at 15.0.

| # | chapter | fresh: over | siege | boss | L15 | asc (first offered) | farmed: over | siege | boss | L15 | asc (first offered) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Enchanted Forest | - | 0 | 15.2 | 36 | 3 (8.8) | - | 0 | 15.0 | 33 | 2 (11.5) |
| 1 | ... the Frostweaver | **10.3** | 9 | - | 14 | 0 | | | | | |
| 2 | The Fallow Fields | - | 3 | 16.8 | 35 | 2 (11.6) | - | 8 | 17.4 | 27 | 1 (14.1) |
| 3 | The Broken Circle | **11.5** | 9 | - | 14 | 0 | - | 0 | 15.1 | 34 | 3 (6.1) |
| 3 | ... the Frostweaver | **8.6** | 6 | - | 10 | 0 | | | | | |
| 4 | Sunken Cave | - | 0 | 15.4 | 30 | 1 (10.6) | - | 0 | 15.5 | 36 | 2 (5.9) |
| 5 | Cursed Dungeon | - | 7 | 16.6 | 27 | 1 (11.3) | - | 0 | 15.1 | 35 | 3 (8.3) |
| 6 | Blighted Swamp | **10.4** | 8 | - | 14 | 0 | - | 0 | 15.0 | 34 | 3 (6.1) |
| 7 | Mystic Ruins | - | 3 | 15.6 | 33 | 3 (10.8) | - | 0 | 15.1 | 38 | 4 (6.9) |
| 8 | Frozen Waste | **11.8** | 9 | - | 16 | 0 | - | 0 | 15.3 | 36 | 3 (5.4) |
| 9 | Scorched Sands | - | 6 | 15.6 | 29 | 2 (12.8) | - | 0 | 15.3 | 42 | 3 (5.4) |
| 10 | The Emberdeep | **8.0** | 6 | - | 8 | 0 | - | 0 | 15.1 | 37 | 4 (6.4) |
| 10 | ... the Frostweaver | | | | | | - | 0 | 15.1 | 40 | 4 (7.5) |

(A first pass on 92e9063, before the Gleaning Bell, read the same way: fresh saves under a 7-9 minute
siege from the Forest on, the Emberdeep the only overrun, every farmed boss dead in seconds.)

### 2.1 What it says

1. **The curve was noise by chapter and a cliff by save.** A fresh save cleared the Forest, the Fields,
   the Cave, the Dungeon, the Ruins and the Sands and was overrun in the Circle, the Swamp, the Frozen
   Waste and the Emberdeep - with no order to it, and the Frostweaver overrun in the Forest itself.
   Which runs fall behind is decided in minutes 3-5: a build that is not clearing the field by the time
   it fills stops reaching its orbs, stops levelling, and the swarm closes. Eric's chapter 1 was not
   reliably easy; his chapter 3 was a coin toss rather than a wall.
2. **A farmed save trivialised all of it.** No siege anywhere but one Fields run, every boss dead within
   thirty seconds of arriving and most within ten. Boss health was set for a build that no longer
   arrives: measured, a farmed build at 15:00 took the Warden of the Deep's 11,000 in about two seconds.
3. **Ascensions came to everyone**: one to three on a fresh save that kept up, from the first chapter;
   three or four on a farmed one, the first offered at minute 5-8.
4. **Healing outran damage** (section 5): a fresh Forest run was offered 130 HP a minute against 43 lost,
   and Light's share of spell damage healed a farmed Ruins run 2,957 a minute.

---

## 3. The target

| # | chapter | fresh save | farmed save |
|---|---|---|---|
| 1 | Enchanted Forest | **easy**: no overrun, siege 3 minutes or less, boss down inside 1.5 | trivial |
| 2 | The Fallow Fields | **harder**: no overrun, a siege of 4-8 minutes, boss down inside 3 | easy |
| 3 | The Broken Circle | **the first wall**: overrun, or the boss not beaten | passes: no overrun, siege 5 or less, boss inside 3 |
| 4-9 | Cave to the Sands | walls, overrun sooner the further in | passes, and works for it: siege up to 6, pressure under 0.8, boss 1-3 minutes |
| 10 | The Emberdeep | overrun early | the hardest pass: the most siege and pressure of any chapter, the longest boss |

| who | Ascensions |
|---|---|
| a new save, chapters 1 and 2 | **none** - not yet learned (section 6) |
| once learned, an average good run | 1-2 (`.ai/gameplay-design.md`, "How many Ascensions a run gets") |
| a planned run on a farmed save | up to 4 |

**What "passes" means for a person.** A farmed save that keeps the field clear is never overrun, so a
person who dodges as most do lives; a fresh save overrun at minute nine dies whatever he does. Between
the two the human margin decides - with less healing (section 5) a larger part of it - and that is the
playtest (section 11).

---

## 4. The levers

### 4.1 Per chapter: `ChapterDifficultyCatalog` (new, `scripts/ChapterDifficulty.cs`)

One table, one row per chapter in campaign order, every value a multiplier on the game as it was
tuned (1 is unchanged). It is its own file rather than fields on `StageCatalog`: the difficulty is one
concern read in one place (`Node2DGame.ApplyChapterDifficulty`, after the balance preset, so the two
multiply), and the castle chapters' builders edit `StageCatalog` and `StageBestiary.*` in parallel.
Boss health stays where it lives, in `BossCatalog`.

| lever | what it scales | read at |
|---|---|---|
| `EnemyHealth` | ordinary spawns' health, on top of the pace curve and each creature's own multiplier; elites are built from a spawn, so they carry it too | `UpdateSpawnScaling` (`presetSpawnHealthScale`) |
| `EnemyDamage` | every hit an enemy lands - contact, bolts, slams, the boss's attacks - rounded stochastically as Weaken is, so 1.25 makes a quarter of the one-damage touches land for two. Never below 1: under 1 a touch could roll nothing and read as a dodge | `Enemy.ScaleOutgoingDamage` |
| `EliteInterval` | the gap between elites (under 1 is more often) | `GetEliteIntervalSeconds` |
| `ElitePower` | elites' and Wardens' health on top of the above | the elite and Warden spawns |
| boss health | `BossDefinition.Health` | `BossCatalog` |

**Why health is the main lever.** The overrun is the swarm's answer to a build that cannot clear the
field, so the spawns' health is what decides whether a save keeps up - and a wall is exactly a chapter
a fresh save cannot keep up in and a farmed one can. Damage is the second lever: it decides what the
survivors of a siege carry out of it, which section 5 makes permanent. Spawn pressure (the burst
window, the interval) is **not** used: it is the XP ceiling (`.ai/run-pacing.md` 3.3), so raising it
would pay the player for the difficulty it added. The XP curve is not moved for the same reason.

### 4.2 What shipped, and how it got there

| # | chapter | EnemyHealth | EnemyDamage | EliteInterval | ElitePower | boss health (was) |
|---|---|---|---|---|---|---|
| 1 | Enchanted Forest | **0.65** | 1.00 | **0.85** | **0.80** | 3,800 (4,200) |
| 2 | The Fallow Fields | **0.75** | 1.00 | 0.95 | 0.90 | 6,500 (4,500) |
| 3 | The Broken Circle | **1.80** | 1.10 | 0.90 | 1.25 | 20,000 (5,000) |
| 4 | Sunken Cave | 2.60 | 1.10 | 0.90 | 1.30 | 3 x 20,000 (3 x 1,900) |
| 5 | Cursed Dungeon | 2.40 | 1.15 | 0.85 | 1.35 | 70,000 (6,200) |
| 6 | Blighted Swamp | 3.00 | 1.15 | 0.85 | 1.40 | 80,000 (6,800) |
| 7 | Mystic Ruins | 3.00 | 1.20 | 0.80 | 1.45 | 90,000 (7,200) |
| 8 | Frozen Waste | 3.80 | 1.25 | 0.80 | 1.50 | 110,000 (7,800) |
| 9 | Scorched Sands | 4.00 | 1.25 | 0.80 | 1.55 | 130,000 (8,200) |
| 10 | The Emberdeep | 3.80 | **1.35** | **0.75** | **1.65** | **200,000** (11,000) |

Four rounds, each a handful of runs (section 10 has the last of each cell):

1. **The first values** (0.85 / 1.0 / 1.4 ... 2.3 at the Emberdeep, bosses about x3) left every farmed
   save untouched - no siege, the Warden of the Deep's 36,000 gone in seven seconds - while the Forest
   still sieged a fresh save for seven minutes and the Fields overran one.
2. **Lighter early spawns did not help the Forest** (0.65: siege 5-8). A fresh build is swamped at minute
   four by the *number* that arrive, and at that point it kills a body in one hit whatever its health.
   The walls rose to 1.8-3.6 and the bosses to what a farmed build's damage takes a minute or two to
   remove (the Ninth Stone's 20,000 in 1.6 minutes, the Warden of the Deep's 200,000 in 2.0).
3. **The Forest's and the Fields' elites come more often and weaker** (0.85 / 0.80, 0.95 / 0.90). Each
   elite carries a chest, and the relics and 50 XP are what let a starters-only build break out of the
   crowd: the Forest's siege went to none on both seeds. The late walls rose again, to 3.0-4.4.
4. **4.4 overran the farmed Emberdeep at minute 12** - the farmed build sat at the cap from minute 4 and
   never ascended - where 3.6 had left it untouched. The step between is narrow (the build either keeps
   the field or loses it), so the Emberdeep came back to **3.8**, the hardest pass the farmed bot makes.

**The column is not monotonic, on purpose.** The spawn tables underneath are not flat - the Ruins' mean
health multiplier is 1.41, the Sands' 1.09, the Emberdeep's 1.28 (`.ai/campaign.md`) - so a lower number
can be a harder chapter: the Emberdeep at 3.8 is 4.9 in effect, the Sands at 4.0 is 4.4. What
`RegressionChecks.ValidateChapterDifficulty` holds is what does not depend on the table: a row for every
playable road chapter, every multiplier positive, `EnemyDamage` never under 1 and never easing along the
road, and no chapter lighter than the first. The castle chapters carry the Emberdeep's row until their
builders tune them (their bosses, on the Warden of the Deep's scene as a stand-in, keep their own first
guesses of 11,800-18,000, now far under the Warden's 200,000 - the castle's to retune); the Sketchbook,
outside the campaign, plays at 1.

**The Boss Rush** reads `BossCatalog` health (scaled by `BossRushHealthScale`), so its bosses grew with
these numbers - several-fold for the late ones. Not re-tuned here; it is a playtest item.

**2026-10-31: the boss column above is superseded.** Boss fights were lengthened to three to five
minutes - phases first, health second - and the Boss Rush now brings a third of each boss
(`BossRushCatalog.FightHealthShare`). The numbers, the phases and the measured lengths are in
`.ai/enemy-behaviour.md`, "Boss entrances and longer fights".

---

## 5. Healing: damage that stays taken

### 5.1 The audit: every way a run gives health back

Measured with the healing ledger (`GameStats.TrackHealing`, harness only: every `Player.Heal` call
named by its caller, as **offered** - before the bar caps it - plus every regen line integrated,
`Player.RegenBreakdown`), Stormcaller, per minute of play. Lines the Stormcaller never met are worked
from the code at a typical holding.

| source | where | rule | HP a minute, typical | now |
|---|---|---|---|---|
| base regeneration | `Player.BaseHealthRegenPerSecond` | 0.25 a second, every run | **15**, always | **removed** (0) |
| Emberheart (shop `recovery`) | `ApplyArcaneUpgrades` | +0.4 a second a level, 8 levels | **72** farmed at the Circle (3 levels), **168** at the Emberdeep (7) | **+0.05 a level** (3 a minute a level), re-priced 15 + 10 a level and refunded |
| Grass tier | `GetGrassRegenPerSecond` | 1 / 2 / 4 a second at tier 2 / 4 / 6 | 60 / 120 / 240 | **0.1 / 0.2 / 0.4** (6 / 12 / 24) |
| Light tier | `NotifySpellDamageDealt` | 3 / 6 / 10% of every point of spell damage dealt | **402** (a fresh Ruins run), **649** (farmed Cave), **2,957** (farmed Ruins): the bar refilled as fast as it emptied | **0.5 / 1 / 1.5%**, banked, and **at most 1 HP a second** (60) |
| health flask | `HealthPickup`, `DropTable` | 10 HP, a rare drop | **9-19** | **8 HP**, half as often |
| Essence Chalice relic | `OnChestEnemyKilled` | 1 HP a kill, first 50 kills | 50 a run | **first 15 kills** |
| Life Drain set | `OnChestEnemyKilled` | 1 HP a kill, unlimited | **80** in the Forest run that held it | **1 HP every 15th kill** (about 15) |
| Heart of Renewal relic | `chestRegenPerSecond` | 0.8 a second | 48 while held (11 averaged over its run) | **0.15** (9) |
| the Frostweaver's passive (`health_regen_0_5`) | `ApplyCharacterPassiveBonus` | 0.5 a second | 30, every Frostweaver run | **0.15** (9) |
| Tidewater Flask boon (`BoonEffect.Regen`) | `BoonCatalog` | 0.6 a second | 36 | **0.15** (9) |
| opening a chest | `ChestReward`, `Node2DGame` | 4 HP | **5** | **removed** |
| the Reliquary set (`vaultguard`) | `Player` | a 6-point shield and 6 HP per chest | under 5 | heal removed, shield kept |
| Spiritual Weapon's Soul Siphon | `SpiritualWeapon.Siphon` | up to 3 a second | up to 180 | **1 a second** |
| Black Tentacles' wisps | `BlackTentacles.TearWisp` | 1 HP a wisp | spell-bound, small | unchanged |
| Taproot stance | `GetStanceRegenPerSecond` | while rooted | small | unchanged |
| Marginalia | `TakeMarginalia` | 30% of max HP, only into an otherwise empty hand | rare | unchanged: a dead pick's consolation |
| the grove, the first tree, the Court's fowl | side events and secrets | one heal each | once a run at most | unchanged |
| Phoenix Oath revives | `TakeDamage` | half the bar, per revive | - | unchanged |
| ~~Avalon~~, Morgan le Fay's passive until 2026-11-09 (`first_fall_full`, 2026-11-03; now Glamour, a dodge, which heals nothing) | `TakeDamage` step 10, `Player.ExtraLifeHeal` | the first revive of a run heals the whole bar, not half | +25 once a run at 50 max HP, and only with a revive to spend (Phoenix Oath, or the Familiar's Nine Lives) | new: on the ledger as two lines, "an extra life" (every revive's half, never counted before) and "Avalon" (the other half, once) |
| Paint What Is Needed, Ma Liang's passive (`paint_flask`, 2026-11-03) | `Player.MaybePaintFlask` (`TakeDamage` step 8), `PaintedFlask` | once a run, the first blow that leaves him below a quarter of his health and standing: a flask painted at his feet, the ordinary `HealthPickup` | **8 HP once a run** at 50 max HP - one flask, the rarest drop's size - and nothing from a blow that kills outright | new: on the ledger as a flask (`HealthPickup.OnPickedUp`), since that is what heals |
| **the Familiar's Purr** | `Familiar.Visit` | 0.8 HP a second (48 a minute; up to 100 with Grimalkin and Cath Palug) | not met by this bot | **3 HP every 20 s**, the cat running to him with a heart over his head (9; Grimalkin and Cath Palug no longer scale it) |

The Gleaning Bell pays XP, not health.

**Before, a fresh Forest run was offered 130 HP a minute against 43 lost** - three times what it took -
and a farmed Emberdeep run 200 against nothing. That is "damage is so impermanent": a 19-HP wizard was
refilled every ten seconds.

### 5.2 More health, and almost none back

- **`Player.MaxHP` 20 to 50** (every wizard's modifier still applies: the Stormcaller is 48). Vitality
  stays +5 a level - a smaller share of a bigger bar, which is the point.
- **No base regeneration.** What heals is now something the player took: Grass, Light, a relic, a boon,
  Emberheart, a flask - each cut to a fraction (table above), so a build that wants sustain spends picks
  and slots on it, and even then gets a trickle.
- **The health bar shows no number** (`Node2DGame.BuildPlayerHealthHud`): the bar and the shield strip
  only, on the phone and the PC alike, which share the one HUD. Nothing else read the label.

How much damage a chapter deals is now the second half of its difficulty, and `EnemyDamage` (4.1) is
the lever for it.

---

## 6. The Ascension is learned: the Hayward teaches it

> **Superseded by the slow start (2026-11-02, `.ai/slow-start.md` section 10).** The Ascension is still
> learned, and still around chapter 2, but a landmark teaches it now - the Pinfold, the Fields' spell vault,
> or whichever of the three teaching vaults a save finds first - and it is learned through a lesson reel
> after that run (`Teachings`). The Hayward teaches nothing. What follows is the reasoning for a gate at
> all, which stands.

### 6.1 The four ways it could have been made hard at first

| option | what it does | verdict |
|---|---|---|
| **later in the run** (a pace-clock gate) | no Ascension before pace minute N, on every save | **No.** It is not "at first": it moves every Ascension of every save, the expert's too, and lands it on the Apotheosis (level 23, about minute 10) - the "seconds apart" problem the two gates were set to solve (`.ai/gameplay-design.md`, "The Apotheosis"). |
| **stricter element tiers on a fresh save** | 6 tags instead of 4 until something | **No.** What lifts it is meta progress, so it is the next row with a worse explanation: the browser would say "Requires 6 Fire" on one save and 4 on another and never say why - and six of one element on a starters-only pool is out of reach for some wizards, a lock nobody is told about. |
| **a meta unlock over the campaign** | a chapter's boss teaches it | **Chosen.** Legible ("Learned by defeating the Hayward"), uses the campaign as the teacher, costs nothing in a run once learned, and it is literally "at first". |
| **rarer offers** | the strip shows an earned Ascension only some of the time | **No.** It puts the roll back in charge of the timing, which the player-level gate was built to stop, and it is the "artificial roof" the Ascension target forbids. |

### 6.2 The rule

**A new save cannot ascend until it has beaten the Hayward**, the boss of the Fallow Fields (chapter
2). Until then a spell stops at level 7, as it always has before player level 17, and the ascension
browser lists every path with one more requirement - **Learned by defeating the Hayward** - beside the
spell level, the tags and the player level. The Apotheosis needs its spell's Ascension path, so it is
sealed with it.

- **Why the Hayward.** The first two chapters are learned on the base game - level-ups, element tiers,
  the level 4 mutation - and the Forest is where a new player dies first, so Elderbark teaching it would
  be teaching it at once. The Ninth Stone (chapter 3) would make the first wall a wall without the tool
  the walls are built around. The Hayward closes the "clearly harder" chapter and hands over what the
  walls ask the player to plan for: one spell to 7, one element to 4.
- **What reads it.** `GlobalStatsManager.HasLearnedAscension(save)`: the save carries
  `AscensionLearned`, or **the chapter the Hayward opens is earned** (`IsStageEarned` on
  `StageCatalog.ChapterOpenedBy("stage_9")`: the Hayward beaten, the Broken Circle written open, or any
  chapter past it opened). Derived rather than written at the kill, so a save that came past the Fields
  before the Fields existed is already learned, as is every save at the Cave or beyond.
- **Where it is enforced.** `Player.GetAscensionOffers` (no level 8 offer while unlearned) and
  `Player.BuildAscensionPreview` (`AscensionRequirementKind.Learned`). Read once at the start of a run;
  the Hayward's fall ends the run it falls in.
- **The kill says so.** The results screen lists the Ascension as taught the first time, and the
  Hayward's deed reads "Unlocks Thornseed and teaches Ascension".

---

## 7. Rerolls and bans are per run

**Before:** Fate Fracture (`rerolls`) gave +1 reroll **at every level-up**, refilled each time - five at
its cap, about 160 a run, which is no limit at all; the `--seek-ascensions` bot spent 30-45 a run.
Proscription (`bans`) was already per run, +1 a level to 3.

**Now both are a pool for the run** (`Player.RerollsRemaining`, `Player.BansRemaining`), filled when the
run starts and never refilled:

| Power | per level | cap | pool at cap | price (base + per level) | was |
|---|---|---|---|---|---|
| Fate Fracture | **+2 rerolls per run** | 5 | 10 | **40 + 25** | +1 per level-up, 75 + 50 |
| Proscription | +1 ban per run | 3 | 3 | 90 + 70 | unchanged |

Ten rerolls a run makes a reroll a decision, as a ban already was; the price comes down because the
level buys less. Playtest mode adds two rerolls a run (it added one a level-up). Nothing in a run adds
charges. **Only the counts moved**: what a ban does on screen is another agent's change, and the two
meet only in `Player.BansRemaining`. Every Fate Fracture level already bought is re-priced and the
difference paid back (section 9).

---

## 8. Skip pays half a bar

The Skip button now pays **half of the XP bar the player has just started** - 50% of
`CalculateXPForLevel(CurrentLevel)` after the level-up - straight into the bar, not through the XP
multipliers (it is a share of the bar, not XP). If that fills the bar, the next level-up opens at once.
The button says so: **Skip (+50% XP)**.

Skipping is still a loss in picks - one pick for half a level - so it is the answer to a hand where
every card is wrong, never a farm. It moves the player level (and so the level 17 and 23 gates) half a
level sooner per skip, which no measured policy uses: the ordinary bot never skips. `--pick-every=N`
and the harness's dead-level-up skip now gain the XP too, so `.ai/run-pacing.md` 10.4's falling-behind
rows read slightly kinder if re-run.

---

## 9. The save: schema 20

**Schema 20**, one field and one step, `MigrateToLearnedAscensionsAndRepricedPowers`. It was planned as
21, with 20 held for the castle's Dungeon move (C2, not built); since steps run in order, a save
migrated to 21 first would skip a 20 that landed later, so this took 20 and **C2 is now 21**
(`.ai/endgame-castle.md` 1.5, `.ai/in-flight.md`).

1. **`AscensionLearned`**, set for every save with a run on record (`Lifetime.TotalRuns`, or the
   telemetry ring). Every such save had the Ascension in every run it played; taking it away until the
   Hayward falls would be the silent confiscation CLAUDE.md forbids. A save with no run had nothing, and
   starts sealed like a new one.
2. **Re-priced Powers are paid back** (`SaveData.RepricedPowers`, frozen): every level bought of Fate
   Fracture and of Emberheart (section 5) is refunded the difference between what it cost and what it
   costs now. The levels stay; what each pays is the new rule.

Held by `RegressionChecks.ValidateDifficultyMigration` on sample saves: a played save keeps the
Ascension and is paid 425 for Fate Fracture 5 and 75 for Emberheart 3 with its levels kept, a second
migration pays nothing, an unplayed save and a new save start sealed, and a save past the Fields or
with the Hayward beaten is learned with no flag.

---

## 10. After

Same protocol as section 2, on the shipped values (4626b18), Stormcaller seed 1234 unless marked:
`s99` is seed 99, `F` the Frostweaver (seed 77). **need** is now in units of a 48-HP wizard (it was 19),
so a need of x10 is 480 HP. Cells marked (r1) were measured on the first round: the fresh Dungeon at
1.7 (it shipped at 2.4, so it is at least that hard) and the farmed Fields at 1.0 (it shipped at 0.75).

| # | chapter | fresh: over | siege | boss | L15 | need | farmed: over | siege | boss | L15 | need | asc |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Enchanted Forest | - | 0 | 16.2 | 34 | x2.3 | | | | | | |
| 1 | ... s99 / F | - / - | 0 / 4 | 15.6 / 16.1 | 34 / 29 | x7.8 / x10.8 | | | | | | |
| 2 | The Fallow Fields | - | 1 | 16.8 | 32 | x6.9 | - (r1) | 0 | 16.7 | 38 | x2.3 | 0 |
| 2 | ... s99 / F | - / - | 3 / 1 | 18.0 / 17.0 | 31 / 31 | x16.9 / x11.4 | | | | | | |
| 3 | The Broken Circle | **7.8** | 5 | - | 9 | - | - | 0 | 16.6 | 38 | x3.9 | 4 |
| 3 | ... F | | | | | | - | 4 | 15.7 | 38 | x3.4 | 4 |
| 4 | Sunken Cave | **8.9** | 6 | - | 13 | - | - | 0 | 15.1 | 34 | x0.4 | 3 |
| 5 | Cursed Dungeon | **10.8** (r1) | 7 | - | 14 | - | - | 3 | 16.3 | 38 | x1.3 | 4 |
| 6 | Blighted Swamp | | | | | | - | 0 | 15.6 | 36 | x4.0 | 4 |
| 7 | Mystic Ruins | | | | | | - | 0 | 17.1 | 49 | x10.1 | 6 |
| 8 | Frozen Waste | | | | | | - | 2 | 17.4 | 38 | x0.3 | 2 |
| 9 | Scorched Sands | **2.9** | 2 | - | 2 | - | - | 0 | 15.9 | 46 | x0.1 | 5 |
| 10 | The Emberdeep | | | | | | - | 0 | 16.2 | 35 | x1.7 | 3 |
| 10 | ... s99 / F | | | | | | - / - | 0 / 0 | 17.2 / 16.1 | 39 / 44 | x0.7 / x0.1 | 3 / 5 |

Fresh cells left empty were not re-run: each overran or nearly did at the old values (section 2), and
every lever on them only rose. The farmed Forest is the fresh Forest (300 Arcane, nothing behind it).

### 10.1 Against the target

| target | before | after |
|---|---|---|
| chapter 1 easy on a fresh save | cleared by the Stormcaller with no siege, **overrun at 10.3 for the Frostweaver** | cleared on all three runs, siege 0-4, boss down in 0.6-1.2 minutes |
| chapter 2 harder, still a fresh save's | cleared, siege 3 | cleared on all three, siege 1-3 - and harder than the Forest by the boss (1.8-3.0 minutes against 0.6-1.2) and the health it costs (x7-17 against x2-11) |
| walls from chapter 3: a fresh save fails | the Circle, the Swamp, the Frozen Waste and the Emberdeep overran; the Cave, the Dungeon, the Ruins and the Sands were cleared | every wall measured overruns: the Circle at 7.8, the Cave at 8.9, the Dungeon at 10.8, the Sands at 2.9 |
| a farmed save passes | every chapter, no siege, every boss dead in seconds | every chapter, siege 0-4, bosses 0.1-2.4 minutes (the Ninth Stone 1.6, the Warden of the Deep 1.1-2.2) |
| the Emberdeep the hardest | overran a fresh save at 8.0; untouched farmed | 3.8 is one step under the 4.4 that overran the farmed bot; its boss the longest fight |
| Ascensions not easy at first | 1-3 on a fresh save from the Forest on | **none in chapters 1-2 on a new save**, then 2-6 on a farmed one |

### 10.2 Healing, before and after (HP a minute)

| run | heals offered | regeneration | lost |
|---|---|---|---|
| fresh Forest, before / after | 104 / 14-46 | 26 / 3-9 | 43 / 8-41 |
| farmed Cave, before / after | 649 / 5 | 111 / 30 | 2 / 7 |
| farmed Ruins, before / after | 2,957 / 16 | 135 / 32 | 0.1 / 85 |
| farmed Emberdeep, before / after | 18 / 5-10 | 183 / 24-30 | 0.1 / 21 |

The farmed Emberdeep now loses health it does not get back (21 a minute against about 30 offered,
where it was offered 200 against nothing), and a fresh Forest is offered roughly what it loses rather
than three times it.

### 10.3 What it did not reach

- **The farmed walls are mostly passes without a siege.** The farmed bot either keeps the field or loses
  it - the step between is a few tenths of `EnemyHealth` (the Emberdeep: untouched at 3.6, overrun at
  4.4) - so "works for it" shows in the boss fight and the health it costs, not in a long siege. A real
  farmer with less behind him than this generous profile meets the wall nearer the edge.
- **The Hollow Choir still dies in seconds** to a farmed build (3 x 20,000 in six seconds: its voices fall
  together to area damage). Its health was not raised again.
- **Ascensions on a farmed save run 2-6** (the Ruins 6, at level 49). The seal answers "at first"; the count
  once learned is the economy's (`.ai/gameplay-design.md`, "How many Ascensions a run gets") and above its
  "up to 4" with this profile's XP (Sage Spiral, the Gleaning Bell). Not changed here.
- **Noise.** A cell is one or two runs; which side of the crowd a fresh build lands on at minute four is
  close to a coin in the chapters near the edge. Four of about sixty runs crashed in the engine ("Caller
  thread can't call this function in this node (/root)", `propagate_notification`), before and after
  alike, and were re-run or left out.

---

## 11. What needs a human playtest

Everything above is one bot on one machine; these are what only playing answers.

1. **A new save through the first three chapters.** The Forest should be cleared in a try or two, the
   Fields should feel harder (its boss above all), and the Broken Circle should stop the player - and the
   Ascension's absence in the first two should read as something to earn, not a bug. The browser's
   "Learned by defeating the Hayward" line, and the results screen the first time he falls.
2. **Living on 50 HP with almost nothing back.** The bot's need column says the Forest costs a fresh build
   two to eleven of its own bars over a run; a person dodges far better, but no human margin was measured.
   If chapter 1 kills new players through attrition rather than a siege, the first thing to give back is a
   little base regeneration (`Player.BaseHealthRegenPerSecond`), not the old numbers.
3. **The walls after farming.** Fail the Circle on a new save, farm the Forest and the Fields a few times,
   buy Powers, and see whether it opens - the farmed profile here is generous (every Feat), so a real
   farmer meets it harder.
4. **The new boss health, for every wizard.** Only the Stormcaller and the Frostweaver fought them. 20,000
   to 200,000 is one to two minutes of a farmed Stormcaller; a wizard with less single-target damage, or a
   fight whose mechanic hides the boss (the Long Coil, the Archivist), may run long. Read each fight for
   length and readability; `_BossProbe` can drive them headless.
5. **The Boss Rush**, which reads the same health: much longer now.
6. **Skip (+50% XP)** and **ten rerolls a run**: whether a skip is ever worth it, and whether the reroll pool
   feels like a decision.
7. **The phone.** The health bar was photographed without its number on the 540x1212 canvas and at
   1280x720 (`_UiShot`); nothing else on the phone was checked.
8. **A real save's first load** at schema 20: the Ascension kept, and the Fate Fracture and Emberheart
   refunds arriving as Arcane Energy.
