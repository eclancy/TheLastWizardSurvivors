# Merlin, the one he missed

Eric, 2026-11-02: *"the blue robed wizard should be his own character. maybe unlocked when finishing the full
campaign on medium? maybe he can have a starter spell that no one else has (therefore able to try a new
apotheosis no one else can get)"*, and then *"merlin is perfect, as one that uses his magic to protect others."*

**Status: built on `wip/blue-wizard` for Eric's review (section 6).** The deed asks for every chapter of the
campaign as `StageCatalog.Campaign` lists them - no list of its own - so on this branch, where the Court and the
Throne are not yet playable, nobody can earn him; once they are (`main-ff86ni`, 41466ed6) the whole road can be
walked.

Read first: `.ai/world-and-tone.md` (he is "you, the one he missed"), `.ai/title-screen.md` (the figure he is),
`.ai/spell-roster.md` section 2 (the four axes), `.ai/dark-wizard-playable.md` (the other whole-road wizard).

---

## 1. Who he is

The title screen's blue-robed figure (`tools/art/hero.py`, `hero.wizard_hero`), the prologue's hider and the
lessons' figure: the one wizard the dark wizard missed, who hid while the others were taken. Unlockable wizards
are named historic or legendary magic-workers; **Merlin** is the archetype the figure already is, and the old
stories have him living his life backwards, which is where his form comes from (section 3).

The name was reserved for another build until 2026-11-02 - the one wizard the dark wizard KEPT, freed at the
Throne, with Spiritual Weapon's sword. That wizard is **Nimue, the Lady of the Lake** now, keeping the sword, with
Excalibur as her form and the verb **sever** (`.ai/endgame-castle.md` decision 11; not built). Nothing gives
both of them the name.

## 2. The rule: every chapter's boss on Default or harder

"The full campaign on medium" is **every chapter of the campaign with its boss beaten on Default or Hardcore**
(the Options difficulty presets: easy, medium and hard are Casual, Default and Hardcore).

- Each chapter keeps the hardest preset its boss has fallen on: `SaveData.MaxDifficultyCleared`, ranked Casual 0,
  Default 1, Hardcore 2, written by `AchievementManager.ApplyRunAchievements` through
  `GlobalStatsManager.RecordChapterClear`. A Boss Rush records no boss and so no clear. The field was saved and
  loaded since the first save format and never written, so no save holds a stale value in it; no schema step.
- The deed **The One He Missed** (*"Defeat every chapter's boss on Default or harder."*) measures
  `GlobalStatsManager.ChaptersClearedAt(save, Default)` out of every chapter in `StageCatalog.Campaign`, **built
  or not** - so it cannot be earned until the castle is built and beaten. The Sketchbook is outside the campaign
  and never counted (Eric: it has no unlocks). The pause tab's projection folds the running run in.
- A Feat, listed from the first run with its count; he is never secret - his silhouette and the line are on the
  character select from the start. It pays him and his spell.
- **Outside the campaign gate** (`OutsideCampaignGate`, Laelaps's flag): he was never imprisoned, and the gates
  count the imprisoned.
- The Dark Wizard's deed reads the same record at Hardcore (`.ai/dark-wizard-playable.md` section 1), so a
  Hardcore finish frees both and a Default finish frees Merlin alone.

## 3. His kit: magic that protects

### Orb of Ages (Light + Arcane, his alone)

*"A ward of light you stand in."* The title screen is the picture: a wizard in a warded circle, the orb in the
claw of his forked staff.

1. **A cast draws the ward on the ground where he stands** - anchored there, not on him - 140 px across the
   radius at level 1, wider with Area.
2. **The orb leaves the staff and ricochets inside it**, point to point of a seven-pointed star ({7/3}: each stroke
   skips three points), so the strokes cross the whole ring and leave its middle, where he stands, quiet.
   Everything a stroke passes through is struck once a stroke (8 at level 1) and **Weakened** (30% softer for
   2.5 s). The orb flies 760 px/s; a wider ring is a bigger, faster orb, so a star keeps its rhythm and its net.
3. **The ward stands 6 s, and closes the moment he steps out of it.** Then the orb flies home, and only then does
   the cooldown (2.0 s) begin.

| axis | Orb of Ages | nearest in the roster, and the difference |
|---|---|---|
| aim | **the ground he stood on** when he cast it; the star decides the rest | Kindled Ward is placed and drifts off; Mirefoot is behind him; Starfall random ground round him |
| cadence | timer to draw it, **his leaving (or its time) to close it**; the rest starts when the orb is home | nothing else is ended by the player's position |
| demand | **hold the ground you chose**: move freely inside the ring, never out of it | Solar Flare stands still and grows; Iron Palisade is placed between you and them |
| payload | **a ricochet**: a piercing orb tracing a star, struck once a stroke, Weakening | Cyclone Slash's blades orbit him; nothing else bounces |

**How it protects, and what it is not.** It holds the dark off by weakening what comes in, never by healing
(`.ai/difficulty-curve.md`: healing stays scarce) and never by shoving (Iron Palisade's wall, the Geomancer's
Sundering ring). Not **Aegis Ward**, a pool of shield on him that breaks and recharges; not **Kindled Ward**, a
mote that drifts off and burns on its own; not **Seimei**, whose ward is that drifting mote and whose verb is
banish. This is a place he must stay in, cut by a ricochet.

**The ring is drawn, and it is not an area marker** (`.ai/spell-roster.md` 2c): it is the ward's edge, the line he
must not cross - a state, as Taproot's ring at his feet is - and under Burning Rim the rim itself is what hits.
The orb and its strokes are the rest of what hits.

Levels: +2 damage at 2, 5 (with -0.3 s) and +3 at 8 (with -0.4 s); the ring 15% wider at 3 and 6; a second orb
at 6, running the star the other way; the ward a second longer at 4 and 7 (`SpellData_OrbOfAges.tres`).

### Its five cards (`SpellEvolutionCatalog.SetupOrbOfAgesEvolutions`)

| card | milestone | what the ward does |
|---|---|---|
| **Burning Rim** | 4, +Light | the ring itself strikes and Weakens whatever crosses into it (60% of a stroke); +20% damage |
| **Twin Orbs** | 4 | one more orb runs the star; -20% cooldown |
| **Missile Ward** | 4 | enemy bolts inside the ring burn out while the ward stands (blocking); +10% damage |
| **Great Circle** | 8, +Arcane | the ring half as wide again; +60% damage |
| **Walking Ward** | 8 | the ward is drawn round him and walks with him; only its time closes it; 2 s longer, -20% cooldown, +50% damage |

### His passive: Warding Hand

*"Whatever touches you is thrown back."* Every contact hit he takes throws the toucher back about 80 px
(`Player.CharacterWardingHandThrow`, `RepelToucher`; bosses resist it as they resist every shove). Holding the
dark off as a passive - an axis no wizard owns: not armour (the Geomancer's Stoneskin), not a heal, not the
Laelaps bolt catch.

### His Apotheosis: The Once and Future (verb **rewind**)

*"When the orb finishes its star it flies it again backwards. Your ward stands as long as you stay inside it."*
**Rewind** was held for him (`.ai/decisions/2026-10-10-apotheosis.md`, `.ai/chapter-wizards.md` section 1): each
time an orb completes its star it reverses and the ward's hour turns back, so the circle never runs out while he
holds it - protection as a place that does not fall. Light, the one of his two the form deepens; no card on Orb of
Ages trades it away. Under Walking Ward it never closes at all. The usual gate: his spell at max level, ascended
down either branch, never combined, and player level 21.

## 4. Exclusivity: one wizard's alone

`UnlockDefinition.ExclusiveTo` (a character id; Orb of Ages names `merlin`) is the rule, read in three places:

- **The level-up offers**: `Player.GetLevelUpOptions` skips a spell `UnlockCatalog.MayBeOffered` refuses, so no
  other wizard - the Test Wizard included - is ever dealt it, whatever the save owns.
- **The Grimoire Restored** does not count it (`UnlockCatalog.GrimoireSpellIds`): it was never taken from anyone,
  so it is not recovered, and counting it would put the collection behind the whole road on Default.
- **The Codex** lists it like any spell - "not yet found" until owned, and once owned its line reads
  "Attack, Merlin's alone" (`CodexModel`); his form shows on its page as any wizard's does.

`RegressionChecks.ValidateTakerTaken` holds that only Merlin may be offered it, that he opens on it, that the deed
pays it, and that any exclusive spell names a wizard who opens on it.

## 5. Art

`tools/art/player_merlin.py` redraws `hero.wizard_hero` on the 48 cell (not a downscale): the wool robe deep and in
shade, the cone hat with its gold band and swept tip, the scalloped mantle, the mane-spear beard, the red sash, the
cloak swept off-axis, the tall gold forked staff with the Light orb setting the top of the silhouette. Idle (frame
one is the portrait), moving, hurt and death (`MerlinFrames.tres`); 0.85 edges/px. The title art is unchanged.
Icon `orb-of-ages-light.png` (`spell_icons.py`: the orb in the claw inside the ring and its star); the six evolution
icons are `evolution_icons.py`'s, three hand-written (Burning Rim, Missile Ward, Walking Ward).

## 6. Built (2026-11-02, `wip/blue-wizard`)

| piece | where |
|---|---|
| him | `CharacterData_Merlin.tres`, roster index **11** (appended); `CharacterVisuals` (untinted; the orb leaves from the staff's claw, cell (13, 6)) |
| Orb of Ages | `SpellData_OrbOfAges.tres`, `scenes/OrbOfAges.tscn` + `scripts/OrbOfAges.cs`, `Player.FireOrbOfAges` and `RestartSpellCooldown`, `ContentValidator.SpellResourcePaths`, its `EvolutionStatReaders` row |
| Warding Hand | `repel_on_hit`: `Player.RepelToucher`, after both contact-damage sites |
| The Once and Future | `ApotheosisCatalog` (verb **rewind**), `OrbOfAges.OnceAndFutureId` |
| the unlock | `the_one_he_missed` (`AchievementDefinitions`), `UnlockCatalog` (`OutsideCampaignGate`; the spell `ExclusiveTo = "merlin"`); the record in `GlobalStatsManager` (`PresetRank`, `RecordChapterClear`, `HardestClearOf`, `ChaptersClearedAt`, `ClearedEveryChapterAt`) |
| quest log | `QuestLog.Prisoners`: "the one he missed", no held line, freed line "{0} keeps his circle." |

## 7. Open for Eric

1. **The name.** Merlin; the Throne's kept wizard is Nimue now (section 1).
2. **Whether holding the ground reads.** The ward closes when he steps out; on a phone, with the stick, that is
   the demand most likely to feel like a chore. Walking Ward is the way out of it, at level 8.
3. **Every number in section 3**, met by the bot once (section 8).
4. **The Codex names him** on his spell's page once owned ("Merlin's alone"); before that it is "not yet found".

## 8. Measured (2026-11-02)

`_BalanceHarness --all-unlocked --immortal --seed=1234`, the Enchanted Forest, the default bot that spreads its
picks (both wizards ended minute 8 on the same loadout: their own spell at level 2, Solar Flare 3, Glacial Spike 2,
Cone of Cold 2, Mirefoot 2, Aegis Ward 2). Two wizards run side by side, nothing else on the machine:

| wizard | his own spell at minute 8 | its share | damage a minute | HP lost a minute |
|---|---|---|---|---|
| Merlin | Orb of Ages, level 2 | **17.3%** (third) | 7,016 | 263.5 |
| Abe no Seimei | Kindled Ward, level 2 | 24.8% (second) | 8,197 | 298.6 |

A first run on the first numbers (6 a stroke, 2.5 s, a 5 s ward, 620 px/s) gave 11.8% against 25.8%; the numbers
were raised once (section 3), and an earlier run on them gave 19.1%. So Orb of Ages is a little under Kindled Ward
early, and he loses about an eighth less health - the Weaken and Warding Hand at work.

The whole path, `--minutes=15 --seek-apotheosis --immortal`: Orb of Ages ascended down Walking Ward at 7.0 minutes
(player level 17), **The Once and Future was offered and taken at 9.3 minutes** (over the 7.5 floor), and the run
ended at level 30 with no errors. His spell did 39.8% of his damage; Kindled Ward, which the bot also took and
ascended to Pyre at 12.1 minutes, did 56.1%. Before the orb grew with the ring and a second orb came at level 6
(section 3) the same run gave 32.3% to 62.2%. **Late, Orb of Ages trails Kindled Ward**: a number for Eric.

**Shots** (`_UiShot`, `user://`): `merlin-select-unlocked-{pc,phone}` (`--all-unlocked`),
`merlin-select-locked-{pc,phone}` (`--campaign-complete`, his silhouette and line), `merlin-orb-{pc,phone}[-n]`
(`--character=11 --spell-stage --cast-on-stage --burst`), `merlin-form-{pc,phone}[-n]` (`--demo-spell-level=8
--demo-evolutions=...twin orbs, great circle, the once and future`) and `merlin-apotheosis-offer-{pc,phone}`
(`--demo-level=21 --demo-spell-level=8 --force-levelup`: the strip offering The Once and Future).
