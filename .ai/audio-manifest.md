# Audio manifest — what exists, and where each sound belongs

Companion to `.ai/audio-direction.md`, which holds the contract and the reasoning. This file
is the inventory and the wiring backlog: seventy-one generated `.wav` files in `assets/sfx/`,
what each one is for, and where in the code it is meant to be triggered. The music - a menu
theme and nine chapter sets in `assets/music/` - has its own section at the end.

**2026-11-08: four more, generated and not yet wired; the White Downs.** The first far-shore chapter's
(`stage_14`, `.ai/second-half.md` 4.2): `hint_hunt_horn`, its chapter hint (a hunting horn far off on the downs);
`hunter_draw` and `hunter_loose`, the Unstrung Hunter drawing his great bow and loosing it; `hind_bolt`, Artemis's
silver hind startled in the stalk. Rendered alone (`build.py hint_hunt_horn hunter_draw hunter_loose hind_bolt`),
each on its target; every other file in `assets/sfx/` and `assets/music/` byte-identical (hashed before and after).
They are in `REGISTRY` but not yet in `SfxCatalog`, so the game does not load them until they are wired. **Measured:**
`--confuse` 0 confusable pairs, none of the four among its category's five closest. Across the whole set: the hint's
nearest 0.190 (`hint_quatrain`), the draw's 0.231 (`spell_beam_start`; 0.272 from the player's `spell_bow_draw`), the
loose's 0.139 (`escort_cough`, then `player_hurt` 0.160; 0.441 from `spell_bow_release`), the bolt's 0.121
(`impact_arcane`; 0.146 from `hint_quatrain` in its own category). Three of the drafts collided and were rebuilt, not
nudged (their rows below). hf 0-4%. The chapter's music went in the same day ("The far shore's music", at the end).

**2026-11-08: two more, both wired; the dry tomb's mirrors.** Imhotep's puzzle (`.ai/unlock-puzzles.md` 3.1, "route the
sun"): `mirror_turn`, a bronze disc swung on its spindle as his feet turn it, and `sun_arrive`, the sunlight landing on a
mirror and on the sun-disc over the door. Both share a thin bronze plate's modes (`sfx._BRONZE_PLATE`), so they are one
object's. Filed with the hints, at -15 and -11 dBFS. **Measured:** `--confuse` 0 confusable pairs; across the whole set the
turn's nearest is 0.117 (`tally_payout`) and the arrival's 0.161 (`hint_caravan_bell`). A first turn, the stone's grind
alone, was 0.084 from `hint_lantern_flare`. Rows under "Chapter hints". `tools/audio/build.py` wrote 112 files with them, every
other one byte-identical (113 once Tituba's `lantern_catch`, below, came in).

**2026-11-08: one more, wired; the lanterns' catch.** Tituba's puzzle (`.ai/unlock-puzzles.md` 3.8, "slip past the
lanterns"): `lantern_catch`, played at his feet the moment a lantern-bearer's light falls on him while he carries the
Gaoler's key (`GaolersKeyEvent`) - the lantern's shutter thrown wide (a dry iron clack and a knock), the cold flame flaring
behind the glass (a breath of flame swelling and rising and burning a third of a second at a 13 Hz flicker, a soft whump
under it, a faint glass tone over it), and the key ringing on the flagstones twice. A mistake the player made, a few a
puzzle at most: -9 dBFS, filed with the hints. Three drafts were measured against the whole registry before it shipped:
a tonal B4-E5 swell was 0.081 from `ice_string_4`, a quick bright whoosh 0.070 from `reroll`, a low swell with the clack
quieted 0.064 from `player_death`. As built its nearest in the whole set is 0.126 (`player_death`), in the hints 0.144
(`hint_lantern_flare`, the Swamp's lantern); `--confuse` 0 confusable pairs. hf 5%. Row under "Chapter hints".

**2026-11-07: six more, all wired; the thaw's kantele.** Väinämöinen's puzzle (`.ai/unlock-puzzles.md` 3.6, "sing it
back"): `ice_string_1` to `ice_string_5`, the five strings of ice over the Frozen Waste's lake, low to high, and
`ice_string_wrong`, a string stepped on out of turn. Each note is played where its string lies (`ThawEvent`, through
`PlacedSoundSchedule` while the ice sings a phrase and `SfxPlayer.AtPosition` as he steps on one), with no pitch jitter -
they are notes. **The notes are A3 D4 F4 C5 A5**, the D minor seventh the hint's song lives in, opened over two octaves:
stacked thirds (D4 F4 A4 C5 E5) were tried first, and two of them landed in bands where UI tones that also only fall
already live - A4 measured 0.07-0.08 from `ui_pause` and E5 0.064-0.089 from `ui_open` (whose bell is struck on E5) and
`ui_back`, through four different constructs. Spread, they skip both bands. Each is one voice: a bright doubled course
(the overtones nearly as strong as the note, the two strands beating at 3.4 Hz), a little of ice's glassy 2.76/5.40, a
short pick, and the lake carrying the note away as a quiet falling "pew" a quarter of a second after (sound in a sheet of
ice disperses). Lower strings ring longer and darker. -11 dBFS (a cast's row: the phrase is the puzzle and must carry over
the swarm), the wrong one -12; filed with the hints so `--confuse` holds them against `hint_ice_song`. **Measured:**
`--confuse` 0 confusable pairs (hint category: none of the six among its five closest, which start at 0.139); the five
notes 0.358-0.545 apart, each 0.179 or more from the wrong one; across the whole set the nearest are 0.111 (`ice_string_5` and `tally_tick`), 0.117 (`ice_string_2`, `enemy_hurt_a`),
0.120 (`ice_string_4`, `enemy_shoot`), 0.144 (`ice_string_wrong`, `impact_metal`), 0.147 (`ice_string_3`,
`hint_caravan_bell`) and 0.202 (`ice_string_1`, `enemy_hurt_b`), and 0.33-0.62 from `hint_ice_song`. hf 0-9%
(the top string's, A5, highest). Rows under "Chapter hints". (The count is left to the merge, as the hints' are.)

**2026-11-07: `boss_block`.** One new file, wired on arrival (the enemies table below): a boss's
guard turning a hit, the Hayward's hurdle first (in-flight item 57 - "hitting the shield ... plays
a different sound, so the player knows the damage is cut"). 91 files in `assets/sfx/` now, which is
what `tools/audio/build.py` reports and `SfxCatalog.AllNames` counts.

**2026-11-07: the shield break, deeper.** `player_shield_break` rebuilt (Eric, in-flight item 60: "annoying -
deeper, like cracking glass"). No new file and no wiring change: the crack now leads (a mid-band snap over a low
split falling to 90 Hz), the glass stack dropped from 1200→900 Hz to 500→400 Hz with the 12.1 partial gone, the
debris band came down an octave (1.0-3.2 kHz, was 2.2-5.5), and the lid from 6 to 4.2 kHz. Measured: centroid
3.8 kHz → 1.46 kHz, hf 32% → 8%. Still one stack struck once in 0.65 s at −8 dBFS (`secret_shatter` keeps the
cascade); nearest neighbour `player_death` at 0.184, `--confuse` 0 confusable pairs.

**2026-11-04: one more, wired; the rot on his way.** One for Paracelsus's walk (`.ai/unlock-puzzles.md` 3.4):
`escort_cough`, played when he steps into a rot pool and again every 1.1 s he stays in one (`GibbetCageEvent`) - a
short wet gulp of the bog and the hint's own tired cough (`_cough`), once and hard, near and dry where the hint's is far
across the water. A mistake the player made, a few a walk at most: -12 dBFS, filed with the hints so `--confuse` holds
it against `hint_flask_chime`. Nearest in the whole set 0.129 (`player_hurt` - both are a man hurt), then 0.169
(`impact_wind`); 0 confusable pairs. Row under "Chapter hints". 103 files.
**2026-11-03: one more, wired; the voice below.** One for the Cursed Dungeon's Oubliette (`.ai/endgame-castle.md` 4.4, D7):
`secret_voice_below`, what answers up the shaft once he has stood on its grate five seconds untouched - a woman's
voice, wordless, humming and then two open syllables, water dripping at the bottom. Normal tier, -8 dBFS, played at
the grate. 0 confusable pairs; inside its category 0.294 (`secret_found`), across the whole set 0.158
(`hint_caravan_bell`), every other voice 0.31 or more (`hint_cell_cry`, the same chapter's, not in the nearest
twelve). Row under "Secrets". (The count is left to the merge, as the hints' are.)
**2026-11-03: one more, wired; the dotted door.** One for Ma Liang (`.ai/chapter-wizards.md` 4.8): `hint_brush`, the
Court's second chapter hint - three strokes of a wet brush along a wall and a drop running off the last. Normal tier;
nearest in the whole set 0.131 (`spell_beam_loop`), the nearest hint 0.273 (`hint_quatrain`), so `--confuse` still
reports 0 undeclared pairs. It is also the sound of his passive painting its flask (`PaintedFlask`), once a run, and of
the last dot inking the door in. Row under "Chapter hints".

**2026-11-03: 95 files, all wired; the gibbet's cage.** One for Paracelsus (`.ai/chapter-wizards.md` 4.4):
`hint_flask_chime`, his chapter hint - a glass flask tapped twice, a man coughing twice, a last faint tap. Normal tier;
nearest in the whole set 0.113 (`cast_metal`), above the 0.10 line, so `--confuse` still reports 0 undeclared pairs. The
cage coming down and his fall play `impact_metal` and `impact_poison` through `SfxPlayer.Impact`, no new sound. Row
under "Chapter hints".

**2026-11-03: 96 files, all wired; the seer reading.** One for Nostradamus (`.ai/chapter-wizards.md` 4.10):
`hint_quatrain`, an old man reading four lines of verse - the Court's chapter hint, and in the seek his readings from
the room his voice is in. Normal tier; nearest in the whole set 0.149 (`hint_ice_song`, the other man's voice), so
`--confuse` still reports 0 undeclared pairs. Row under "Chapter hints".
**2026-11-03: one more, wired; the dry tomb.** One for Imhotep (`.ai/chapter-wizards.md` 4.6): `hint_tomb_knock`, his
chapter hint - three slow knocks of stone on stone under hissing sand. Normal tier; nearest in the whole set 0.176
(`spell_orbit`), so `--confuse` still reports 0 undeclared pairs. A beacon catching plays `cast_fire` and going out on
the wind `cast_wind`, both through `SfxPlayer.Cast`; the slab sliding `impact_earth`. No new sound for those. Row under
"Chapter hints". (The count is left to the merge: other branches add hints beside it.)
**2026-11-03: one more, wired; the granary.** One for Medea (`.ai/chapter-wizards.md` 4.2): `hint_granary_voice`,
her chapter hint - a woman's voice, low and level, through plank walls. Normal tier; nearest in the whole set 0.164
(`pickup_health`), then 0.179 (`hint_cell_cry`, Tituba's, the other voice that speaks), so `--confuse` still reports
0 undeclared pairs. The scale's beam dropping and the bar falling are `impact_metal` and `impact_earth` through
`SfxPlayer.Impact`, and Dragon's Teeth's stone landing `impact_light`: no new sound. Row under "Chapter hints".

**2026-11-03: 94 files, all wired; the walking hut.** One for Baba Yaga (`.ai/chapter-wizards.md` 4.1):
`hint_hut_step`, her chapter hint - two heavy falls and a timber creak. Normal tier; nearest in the whole set 0.187
(`spell_explosion`), so `--confuse` still reports 0 undeclared pairs. The hut's own footfalls in the world are
`impact_earth` through `SfxPlayer.Impact`, no new sound. Row under "Chapter hints".

**2026-11-02: 93 files, all wired; the nine mouths.** Three for the Cumaean Sibyl (`.ai/chapter-wizards.md`
4.3): `hint_sibyl_voice` (her chapter hint), `sibyl_voice` and `sibyl_echo` (the call at the nine mouths,
one voice per mouth a beat apart through `PlacedSoundSchedule`). All three Normal tier. They are declared
twins of each other in `analyse.py` - one voice by design, 0.026-0.059 apart - and the nearest OTHER sound
in the whole set is 0.139 (`hint_lantern_flare`), so `--confuse` still reports 0 undeclared pairs. Rows
under "Chapter hints".

**2026-11-02: 90 files, all wired; the defeat and its payout.** Eric: "Defeat should have a
dramatic animation ... It then tallys up the reward with a satisfying payout sound." Three new
sounds, generated, allowlisted in `SfxCatalog` and played: `player_fall` (the wizard reaching the
ground, `DefeatSequence`), `tally_tick` (each step of the results screen's count, pitched up it by
`SfxPlayer.TallyTick`) and `tally_payout` (the count landing). `game_over` moved to the moment the
word DEFEAT lands, and `stage_clear` - never played - is now the victory's. `player_fall` and
`tally_payout` are Critical (thirteen of ninety; the validator's limit is fourteen). `analyse.py
--confuse` reports 0 confusable pairs. `SfxPlayer` reports `90 sounds preloaded`.

**2026-11-01: 85 files, all wired; battle music.** `spell_maw_open`, Elder God Maw breaking up
through the earth (Eric, 0.10.0: "should make a deep, scary crashing sound when it opens"):
generated, allowlisted in `SfxCatalog` and played by `BlackTentacles` 0.20 s before the breach, so its
crash lands on the frame the plates are thrown; for the maw it replaces `spell_summon`. Its row is
under "Spells". `SfxPlayer` reports `85 sounds preloaded`. The same day the music became battle
music from the first second: every playable chapter's pulse rescored and ungated, the hook stated as
a run opens, and a boss layer per chapter ("Music", below).

**2026-10-30: 84 files, all wired.** The secret stings (piece A14 of
`.ai/achievements-and-secrets.md`): `secret_seen`, `secret_found` and `secret_shatter`, generated,
allowlisted in `SfxCatalog` and - since piece A4 the same day - played where the table under
"Secrets" below proposed: `secret_seen` at the site by `SecretDirector`'s seen mark, once per secret
per save; `secret_found` by `SecretDirector.Found` for a secret found again (`event_reward` is a side
event's chest again); `secret_shatter` by the Geode Heart (`GeodeHeart.OnBroke`). `SfxPlayer` reports
`84 sounds preloaded`. Also on 2026-10-30, three castle sets were rendered for the music
(the Last Bridge, the Court, the Throne; "The castle's music" at the end), and the castle's plumbing
(C0) put them in `MusicCatalog` the same day; their chapters are not playable yet. And the generators now run under Python 3 (the container has only 3.13) as well as
2.7, writing the same bytes under either: all 81 earlier effects and the heath's three stems
re-rendered byte-identical under 3.13 (`.ai/audio-direction.md` section 3).

**2026-10-25: 81 files.** `spell_stormsweep` and `spell_sleet_rune` were added and wired for the facing family
(`.ai/spell-roster.md` "The facing family"); `SfxPlayer` reports `81 sounds preloaded`. Each REPLACES
its spell's element cast cue rather than sitting on it (both spells return `CastOutcome.Maintained`):
- `spell_stormsweep` - one Stormsweep pass, every pass of every arc, throttled at 60 ms. An arc of
  storm-light gathers and then discharges: 80 ms of rising air as it leaps out, then a band-limited snap
  gone in 15 ms with a short 90 Hz-gated buzz behind it as it sweeps. -14 dBFS (up to three sweeps a
  second at level 8), hf 13%, centroid 2.2 kHz; nearest neighbour in the whole set `chest_open` at 0.161.
  The first draft borrowed Lightning's palette crackle for its tail and measured 0.025 from
  `cast_lightning` and 0.039 from `impact_lightning` - a cast that sounded like a lightning hit, on the
  spell whose hits are lightning hits - and was rebuilt as the gather-then-discharge construct; a second
  pass took its hf from 17% to 13%.
- `spell_sleet_rune` - one Sleet Rune leaving the hand, every rune, throttled at 45 ms, Swarm tier. A
  bandpass sweeping UP through 900-2400 Hz (the orbit's swipe falls) ending on a two-partial ice chime
  45 ms in - a late attack, which no other spell sound has. -22 dBFS beside `spell_orbit`, hf 13%,
  centroid 2.3 kHz; nearest neighbour `player_dodge` at 0.234.
(Both were first written as `spell_lash` and `spell_knife`, for a build the developer asked to be rethemed
as magic; same constructs, renamed before anything shipped.)

**Before that, 79 files.** `spell_familiar_bite` was added and wired (`Familiar.Bite` and each strike
of the Griffin's swoop; `.ai/spell-roster.md` "Familiar"). Two pitched clacks 24 ms apart - the jaws
meeting, the teeth setting - then a short huff of breath that rises, at -21 dBFS beside `spell_orbit`,
Swarm tier, throttled at 110 ms at the call site, because a pack bites several times a second. No bark:
a voice at that rate is what the loudness table exists to stop. hf 4%, centroid 1.36 kHz; nearest
neighbour in the whole set `hint_lantern_flare` at 0.126. The first draft, a falling breath over a
falling growl, was `spell_beam_end`'s construct (falling noise over a falling saw) and measured its twin
at 0.098; it was rebuilt, not nudged. The familiar's arrival and each new form play `spell_summon`.

**Before that, 78 files.** `spell_starfall` was added and wired (`Starfall.Land`: every star of the
Starfall rain as it lands, hit or miss; `.ai/spell-roster.md` "Starfall"). A falling run of three
glass glints, D7 G6 D6 28 ms apart, at -24 dBFS - under `spell_orbit`, because at level 8 it is the
most frequent spell sound in the game - Swarm tier, throttled at 90 ms at the call site. hf 0%,
centroid 1.45 kHz; nearest neighbour in the whole set `reroll` at 0.178. Two drafts were twins and
were rebuilt rather than nudged: a swish under one glint (`spell_orbit`, 0.056) and two glints
(`player_shield_absorb`, 0.065 - a star landing must not sound like the shield taking a hit).

Also on 2026-10-25, five chapter hints were added and wired (`StageHintDirector`, see
`.ai/side-events.md` "Hints that something is out there"): one cue per side-event chapter, played
a few hundred pixels toward the place a few times a run, under a caption. `SfxPlayer` reports `78
sounds preloaded` with both. Every one is far away by construction - highpassed, lowpassed at 2-3.4 kHz and
put in a room - and none has more than 3% of itself above 4 kHz. Nearest neighbour of each in the
whole set: the cry `level_up` 0.124, the whisper `enemy_melee` 0.219, the lantern `level_up` 0.127,
the ice `player_death` 0.204, the bell `bearing_found` 0.182. Three first drafts collided and were
rebuilt (the table under "Chapter hints").

**2026-10-22: 72 files.** `bearing_found` was added and wired (`WayfinderPickup.Take`, the Arcane
Compass and its cousins; see `.ai/side-events.md` "The wayfinder"); `SfxPlayer` reports `72 sounds
preloaded`. Five decelerating wooden ticks - a needle coming to rest - then a low open fifth that
holds while the arrows come up. At -8 dBFS, once a run, lowpassed throughout (hf 2%, centroid
923 Hz); nearest neighbour in its category is further than 0.17.

**2026-10-20: 71 files.** `pickup_haste` and `event_reward` were added and wired, and `chest_open`
was wired; `SfxPlayer` reported `71 sounds preloaded`. The status paragraph below is the 2026-09-07
record and its counts are of that date.

**Status as of 2026-09-07: all 69 generated, 44 wired.** The infrastructure is in and
verified in Godot — `SfxPlayer` reports `69 sounds preloaded on bus 'SFX'` and
`ContentValidator` finishes with no audio warnings.

Wired: the whole hit loop (`enemy_hurt_*`, `enemy_death_*`, `boss_death`, `enemy_shoot`), the
player pipeline (`player_dodge`, `player_shield_absorb`, `player_shield_break`, `player_hurt`,
`player_heal`, `player_death`), all twelve `cast_*`, `pickup_xp` with its streak ladder,
`pickup_health`, `level_up`, `card_select`, `reroll`, `ui_back` and `game_over`.

All twelve `impact_*` are now wired too, through the `source` parameter on
`Player.DealDamageToEnemy` - the one funnel every damaging spell already used.

**Not yet wired — 25 files.** Every `spell_*` shape, `elite_spawn`, `boss_roar`,
`enemy_melee`, `player_low_health`, `pickup_magnet`, `chest_open`, `spell_evolve`,
`card_appear`, and the rest of `ui_*` and the meta stingers. For those, the Trigger column
below is still a **proposal** rather than a description of behaviour.

Note that `elite_spawn` and `boss_roar` are wired in the tier table but have no trigger site
yet, so their ducking is defined and untested.

**2026-10-31: `boss_roar` has its trigger site** - once per boss arrival, at the climax of its
entrance (`BossEnemy.RoarOnArrival`, timed by `EntranceRoarSeconds`; only the Hollow Choir's first
voice roars). It is once a run (once a boss in a Boss Rush, minutes apart), so it keeps its −4 dBFS
peak. The manifest's other proposal for it, the phase change, is left unwired: two to four phase
changes a fight would play a once-a-run sound once a minute, and the loudness table would want a
quieter variant from `tools/audio/sfx.py` first.

Lengths and peak levels are generated. Do not retype them:

```
python tools/audio/build.py --manifest
```

## How it is wired

`scripts/SfxPlayer.cs` is an autoload and the only place a sound effect is played from. It
preloads every stream in `_Ready`, owns a fixed pool of 24 positional and 8 non-positional
voices, and holds the throttles. Nothing allocates per hit and no call site sets a volume,
because `tools/audio/build.py` has already normalised every file to its mix target.

`scripts/SfxCatalog.cs` names every file. It is the same allowlist discipline
`ContentValidator.SpellResourcePaths` follows: `RegressionChecks.ValidateAudio` walks it at
startup, so a missing or renamed file is a console warning rather than silence mid-fight. The
24 element sounds are derived from the `Element` enum rather than listed, so the two cannot
drift.

`default_bus_layout.tres` defines **Master, Music and SFX**. Before it existed the project had
only Godot's default Master bus, so `ResolveMusicBusName()` fell through to `"Master"` and the
Music slider was a second Master slider. `scripts/AudioSettings.cs` now routes every slider and
the mute toggle through the save file, so volumes survive a relaunch.

### How impacts are wired

It turned out not to need twenty edits. Every damaging spell already funnels through
`Player.DealDamageToEnemy`, which now takes an optional `source` SpellData used for nothing but
choosing the impact sound. A spell opts in by passing it:

```csharp
player?.DealDamageToEnemy(enemy, damage, source: SpellData);
```

`SfxPlayer.Impact` throttles per element at 55 ms, so an AoE landing on forty enemies makes one
sound. `Enemy.TakeDamage` would have been a smaller edit still and is the wrong place: the
enemy does not know what hit it, so all twelve elements would sound identical.

Two call sites are deliberately left out. `ScorchingRayBeam` damages continuously, so an impact
per tick would be a drone - it wants `spell_beam_*` instead. `OrbitingBlade` has `spell_orbit`
for the same reason.

## Elements — casts

One per canonical element. A spell picks its file from its own `ElementWeights`; where a
spell carries two elements, use the heavier one, and break a tie toward the more distinctive
voice rather than alphabetically.

**The persistent emitters cue themselves.** Solar Flare and Toxic Spore Burst (`ElementalPulse`)
and Cinderbreath (`Flamethrower`) are created once and then kept current, so the fire loop in
`Player` returns `CastOutcome.Maintained` for them - including on the creation frame - and never
plays their cast. Instead `ElementalPulse.TriggerPulse` plays the cast cue on every pulse, hit
or not, and `Flamethrower` plays it each time a burn lights (once per burn, not per puff). The
first pulse and the first burn come from their `_Ready`, which is why creation is not `Fired`:
that would play the first one twice.

| File | Length | Peak | Trigger |
|---|---|---|---|
| `cast_fire.wav` | 0.60 s | −11.0 dBFS | Fireball, MoltenShard, MeteorSwarm, ScorchingRay, SolarFlare |
| `cast_ice.wav` | 0.52 s | −11.0 dBFS | FrostShard, GlacialSpike, ConeOfCold |
| `cast_arcane.wav` | 0.70 s | −11.0 dBFS | ArcaneExplosion, MagicMissile, SpiritualWeapon |
| `cast_darkness.wav` | 0.78 s | −11.0 dBFS | ShadowBolt, VoidLance, BlackTentacles |
| `cast_light.wav` | 0.93 s | −11.0 dBFS | SolarFlare's Light half, any future Light spell |
| `cast_grass.wav` | 0.46 s | −11.0 dBFS | ThornVine, GuardianVines |
| `cast_earth.wav` | 0.58 s | −11.0 dBFS | ObsidianSpike, GroundSpike, StoneBulwark |
| `cast_wind.wav` | 0.80 s | −11.0 dBFS | GaleBlade, CycloneSlash |
| `cast_lightning.wav` | 0.42 s | −11.0 dBFS | ChainLightning, StormguardAura |
| `cast_poison.wav` | 0.68 s | −11.0 dBFS | ToxicSporeBurst, VenomCloak |
| `cast_metal.wav` | 1.03 s | −11.0 dBFS | HuntersDraw, AegisWard |
| `cast_water.wav` | 0.54 s | −11.0 dBFS | TidalBarrier, any future Water spell |

`Player`'s existing `spellFireTimers` loop is the single place these should be triggered
from — one call site, not one per spell scene.

## Elements — impacts

Played where a projectile or AoE actually damages something. **Rate-limit these**: an AoE
landing on forty enemies plays one impact, not forty.

| File | Length | Peak | Trigger |
|---|---|---|---|
| `impact_fire.wav` | 0.33 s | −15.0 dBFS | the damage-dealing call in each Fire spell's scene script |
| `impact_ice.wav` | 0.29 s | −15.0 dBFS | as above, plus `Enemy.ApplySlow` freeze onset |
| `impact_arcane.wav` | 0.39 s | −15.0 dBFS | `ArcaneExplosion.cs`, `MagicMissile.cs` |
| `impact_darkness.wav` | 0.43 s | −15.0 dBFS | `ShadowBolt.cs`, `VoidLance.cs` |
| `impact_light.wav` | 0.51 s | −15.0 dBFS | Light spell hits; also the Light element's heal-on-damage tier |
| `impact_grass.wav` | 0.25 s | −15.0 dBFS | `ThornVine.cs` |
| `impact_earth.wav` | 0.32 s | −15.0 dBFS | `ObsidianSpike.cs`, `GroundSpike.cs` |
| `impact_wind.wav` | 0.44 s | −15.0 dBFS | `GaleBlade.cs`, `CycloneSlash.cs` |
| `impact_lightning.wav` | 0.23 s | −15.0 dBFS | `ChainLightning.cs`, including each chained bounce |
| `impact_poison.wav` | 0.38 s | −15.0 dBFS | Toxic Spore Burst's bursts (`SporeMushroom`, through `DealDamageToEnemy`); **not** per poison tick |
| `impact_metal.wav` | 0.57 s | −15.0 dBFS | `HuntersArrow` landing |
| `impact_water.wav` | 0.30 s | −15.0 dBFS | Water spell hits |

`Enemy.ApplyPoison` ticks every second for its whole duration on every poisoned enemy. It
must **not** play a sound per tick — that is the single worst mix hazard in the game.

## Canticle's notes (2026-11-09)

Twelve `note_<element>` files (`assets/sfx/note_<element>.wav`, one per element voice, A4), for the Sibyl's
Canticle (`.ai/spell-roster.md`). Each charging cast plays its spell's element note pitched to the chord's next
tone in the chapter's key, 8 dB under the burst (`ArcaneExplosion.AddNote`); the burst sounds the gathered notes
together on the next beat (`SoundChord`, six at most). Each note peaks at -26 dBFS, under the loudness table on
purpose: they are only ever heard together, and a chord of three to six sums to -20 to -15 peak, -33 to -29 RMS,
level with a chapter's bed and pulse. `analyse.py --confuse`: 0 confusable pairs after two rebuilds. Not yet heard.

## Spell shapes

Sounds no element table can express, because they are about a spell's *form* rather than its
element.

| File | Length | Peak | Loops | Trigger |
|---|---|---|---|---|
| `spell_explosion.wav` | 0.91 s | −5.0 dBFS | no | `ArcaneExplosion.cs`, `MeteorImpact.cs` — the only files allowed sub-80 Hz energy besides the boss |
| `spell_beam_start.wav` | 0.28 s | −11.0 dBFS | no | `ScorchingRayBeam.cs` ignition |
| `spell_beam_loop.wav` | 0.50 s | −14.0 dBFS | **yes** | the beam sustain — its `.import` already carries `edit/loop_mode=1`; keep it if the file is regenerated |
| `spell_beam_end.wav` | 0.34 s | −12.0 dBFS | no | beam extinguish |
| `spell_orbit.wav` | 0.17 s | −22.0 dBFS | no | `SpiritualWeapon.cs` / `OrbitingBlade.cs` per-tick hit |
| `spell_starfall.wav` | 0.16 s | −24.0 dBFS | no | **wired**, `Starfall.Land`: every star of the rain landing, hit or miss, throttled at 90 ms (`Starfall.SoundGapMs`). Its hits are the Light impact through `DealDamageToEnemy`, as for any spell; the rain has no cast cue (`CastOutcome.Maintained`) because nothing is ever cast |
| `spell_familiar_bite.wav` | 0.13 s | −21.0 dBFS | no | **wired**, `Familiar.Bite` and `Familiar.StrikeAlong`: every strike of every form - a hound's bite, a crow's peck, a cat's swipe (one sound for all three since the forms, 2026-10-28; a peck and a swipe of their own are open) - and every enemy a Griffin's swoop strikes, throttled at 110 ms (`Familiar.SoundGapMs`); also Laelaps catching a bolt in his jaws (`Player.TryCatchBolt`). The damage is the Arcane impact through `DealDamageToEnemy`; the pack has no cast cue (`CastOutcome.Maintained`) |
| `spell_stormsweep.wav` | 0.22 s | −14.0 dBFS | no | **wired**, `Stormsweep.Tick`: every pass of every arc (a Thunderclap arc sweeps twice), throttled at 60 ms (`Stormsweep.SoundGapMs`). It replaces the Lightning cast cue for the spell (`CastOutcome.Maintained`); hits are the Lightning impact through `DealDamageToEnemy`, and a Thunderclap burst plays `cast_lightning` |
| `spell_sleet_rune.wav` | 0.12 s | −22.0 dBFS | no | **wired**, `SleetRunes.Release`: every rune leaving the hand, throttled at 45 ms (`SleetRunes.SoundGapMs`), Swarm tier. It replaces the cast cue for the spell (`CastOutcome.Maintained`); hits are the Ice impact through `DealDamageToEnemy` |
| `spell_maw_open.wav` | 1.85 s | −9.0 dBFS | no | **wired** (2026-11-01): `BlackTentacles._Process`, `MawCrashLeadSeconds` (0.20 s) before the breach (`MawBreach` of `MawOpenSeconds`), so the crash at 0.20 s in the file lands as the plates are thrown; the maw's summon cue, in place of `spell_summon`. The heave (a rumble swelling with two strain cracks), the crack and a thump falling 110 to 46 Hz with short rubble, stone grinding as the maw rises over a sub swelling 40 to 64 Hz, four slabs landing one after another and a bounce, the dust settling. Goes under 80 Hz as `spell_explosion` does (earth breaking, one maw every 3-4 s); centroid 372 Hz, hf 1%. Nearest in the set: `spell_summon` 0.142, `boss_death` 0.151, `boss_roar` 0.184 - the first draft's FM groan was the roar's construct and measured 0.077 from it, so the rise became grinding and the slabs separate blows |
| `spell_summon.wav` | 0.75 s | −9.0 dBFS | no | **wired** (2026-10-23): `BlackTentacles.CastAt` as the rift opens (every face but the Elder God Maw, since 2026-11-01), `ThornVine.Launch` as the vine breaks ground, `SporeMushroom` as a Doomcap breaks ground (the titan only - an ordinary sowing is covered by the cast cue). No slap/lash sound exists; the Darkness/Grass impacts play through `DealDamageToEnemy` as for every hit, and a dedicated wet slap would be the one addition worth generating |
| `spell_bow_draw.wav` | 0.45 s | −15.0 dBFS | no | `BowDrawVisual.cs` wind-up start |
| `spell_bow_release.wav` | 0.30 s | −10.0 dBFS | no | `HuntersArrow` spawn |
| `spell_sagittarius.wav` | 0.60 s | −11.0 dBFS | no | **wired** (2026-11-01), `Sagittarius.Loose`: once a loose, at the wizard, never once an arrow (`.ai/combo-spells.md` section 3), throttled at 120 ms. A bowstring - a dry snap over a string falling 250 to 120 Hz - then a G major chord (G5, B5, D6) rolled upward 22 ms apart in glass partials (a sine and its 2.76), Light's register, all under 4 kHz. Not among the spell category's closest five pairs (`analyse.py --confuse`) |

## The player

Trigger points map onto the damage pipeline in `CLAUDE.md`, and the order matters:

| File | Length | Peak | Trigger |
|---|---|---|---|
| `player_dodge.wav` | 0.26 s | −13.0 dBFS | `Player.cs` dodge branch (`scripts/Player.cs:1387`), where the hit is fully avoided — this replaces `player_hurt`, it does not accompany it |
| `player_shield_absorb.wav` | 0.30 s | −12.0 dBFS | shield pool absorb (`scripts/Player.cs:1410`), when `absorbed > 0` |
| `player_shield_break.wav` | 0.65 s | −8.0 dBFS | the same block, when `shieldPoints` reaches 0 |
| `player_hurt.wav` | 0.34 s | −6.0 dBFS | after mitigation, only when HP actually fell — read the HP delta, not the `DamageTaken` signal argument, which carries the **pre**-mitigation amount |
| `player_heal.wav` | 0.60 s | −11.0 dBFS | any heal, including the Light element tier and the extra-life half-heal |
| `player_low_health.wav` | 0.55 s | −14.0 dBFS | a repeating timer below a HP threshold — designed to loop as a heartbeat, not to fire once |
| `player_death.wav` | 1.60 s | −4.0 dBFS | the `Died` emit, **not** the extra-life branch |
| `player_fall.wav` | 0.66 s | −7.0 dBFS | **wired** (2026-11-02), `DefeatSequence`: the wizard reaching the ground, on the last frame of his death row, about 1.5 s after `player_death`. Two thumps (knees, then body), grit, the staff's knock, dust settling; nothing below 85 Hz. **Critical**, because it lands inside `player_death`'s duck. Nearest neighbour `player_death` at 0.188 (a one-thump first draft measured 0.098 from `player_hurt`) |

## Enemies

| File | Length | Peak | Trigger |
|---|---|---|---|
| `enemy_hurt_a/b/c.wav` | 0.13 s | −19.0 dBFS | `Enemy.TakeDamage` (`scripts/Enemy.cs:374`) — pick at random, randomise pitch, and throttle globally to about one per 60 ms |
| `enemy_death_small.wav` | 0.45 s | −14.0 dBFS | `Enemy.StartDeath` (`scripts/Enemy.cs:415`) for swarmers and runners |
| `enemy_death_heavy.wav` | 0.75 s | −11.0 dBFS | `StartDeath` for bruisers, tanks and shielders |
| `enemy_shoot.wav` | 0.20 s | −14.0 dBFS | `RangedEnemy.cs` firing an `EnemyProjectile` |
| `enemy_melee.wav` | 0.27 s | −12.0 dBFS | a contact hit landing on the player, paired with `player_hurt` |
| `elite_spawn.wav` | 0.90 s | −8.0 dBFS | an elite entering the field |
| `boss_roar.wav` | 1.80 s | −4.0 dBFS | boss spawn and phase change (`BossEnemy.cs`) |
| `boss_block.wav` | 0.16 s | −13.0 dBFS | **wired** (2026-11-07): a boss's guard turning a hit — the Hayward's hurdle (`HaywardBoss.MarkBlockedHit` → `SfxPlayer.BossBlock`, throttled to one per 90 ms). A dry wooden tok (a 640 Hz woody band over a 185→150 Hz box mode, a 6 ms comb for the weave), nothing below 95 Hz, top cut 2.4 kHz: wood where a landed hit is the element, so the cut number has a sound that says why. Not the anvil (steel, a rebound) and not the hull (hollow, double) |
| `boss_death.wav` | 2.43 s | −3.0 dBFS | `BossEnemy.StartDeath` (`scripts/BossEnemy.cs:141`) |
| `hunter_draw.wav` | 1.32 s | −11.0 dBFS | **not wired** (2026-11-08): the Unstrung Hunter (the White Downs' boss) drawing his great bow, at the start of its 1.4 s wind-up, at him. A heavy bow's limbs straining (stick-slip clicks quickening from 22 to 74 a second through two wooden resonances rising 230→330 and 560→760 Hz), the string taking the weight (a low buzz rising 98→147 Hz, never under 85), and a cold thread of his arcane tightening over it (two sines near 1.3-1.76 kHz drifting apart so they beat 2 → 13 times a second), all climbing and stopping at full draw. A swell with no transient - the visual telegraph carries the warning; the loose is the sound to hear. Every few seconds while he lives: two dB under the loose. Nearest in the whole set 0.231 (`spell_beam_start`), 0.272 from the player's `spell_bow_draw` (one 0.45 s band of metallic noise at 620-1300 Hz; this is three times as long, an octave lower, and made of clicks). hf 1% |
| `hunter_loose.wav` | 0.62 s | −9.0 dBFS | **not wired** (2026-11-08): the same bow loosed, at the end of the wind-up, at him. A deep twang - the string at D3 holding its pitch and flapping at 19 Hz, a saw under a fixed 650 Hz lowpass, so it buzzes low - a dry thwack on the grip, the whirr of a great shaft's fletching (a narrow band falling 700→420 Hz, chopped 38 → 24 times a second as it spins), and the arcane thread snapping loose (1.76 kHz falling a fifth). Where `spell_maw_open` sits, between the once-a-second and once-a-minute rows. Three drafts collided and were rebuilt: a saw pluck under a closing lowpass was the grass element's construct (0.066 from `cast_grass`), a falling whoosh over a falling tone was `spell_beam_end`'s (0.076), a round string falling 147→98 Hz under the whirr was a hit's thud (0.091-0.104 from `player_hurt`). As built: nearest 0.139 (`escort_cough`), then 0.160 (`player_hurt`); 0.441 from `spell_bow_release`. Centroid 680 Hz, hf 1% |

`StartDeath` drops rewards and leaves the `"enemies"` group in the same frame, before the
death animation plays. The death sound is not gating anything and may outlast the corpse.

## Pickups and progression

| File | Length | Peak | Trigger |
|---|---|---|---|
| `pickup_xp.wav` | 0.09 s | −23.0 dBFS | `XPOrb.OnBodyEntered` (`scripts/XPOrb.cs:82`) — ramp `PitchScale` upward with the collection streak, reset when it breaks |
| `pickup_health.wav` | 0.42 s | −13.0 dBFS | `PickupBase.OnBodyEntered` (`scripts/PickupBase.cs:78`) for `HealthPickup` |
| `pickup_magnet.wav` | 0.37 s | −13.0 dBFS | **wired**, `GleaningBell.Ring` (2026-10-30): the Gleaning Bell rung, every XP orb on the map called in - the vacuum it was made for, played `Global`, once per pull and never per orb; each orb then lands on its own streak-pitched `pickup_xp`. Also played by `SecretSeed` when the Fenced Bloom's seed is taken |
| `pickup_haste.wav` | 0.38 s | −12.0 dBFS | **wired**, `BuffItem.OnPickedUp` (the speed potion). A double gust and a climbing triangle shimmer; hf 11%, nearest neighbour in the whole set `pickup_magnet` at 0.157. About one kill in a hundred drops a potion, so it sits a dB over `pickup_health` |
| `event_reward.wav` | 1.45 s | −8.0 dBFS | **wired**, `SideEvent.Succeed` when the event pays a chest: a formant breath swelling into one glass tone with vibrato, under the chest rising as a ghost. Not played on a discovery, which keeps `meta_unlock`. hf 3%, nearest neighbour `spell_evolve` at 0.135 |
| `bearing_found.wav` | 1.58 s | −8.0 dBFS | **wired**, `WayfinderPickup.Take`: the chapter's wayfinder taken and every point of interest marked. Five decelerating ticks (a needle settling; `pickup_magnet` accelerates, which is the difference between found and pulled) then an open fifth. hf 2% |
| `level_up.wav` | 0.96 s | −5.0 dBFS | `LevelUpPickup` / the level-up trigger, before `LevelUpMenu` opens |
| `chest_open.wav` | 1.26 s | −6.0 dBFS | **wired** (2026-10-20), `ChestReward.OnPickedUp`, before `ChestItemSelectionMenu` opens. It was allowlisted from the start and never played |
| `spell_evolve.wav` | 1.70 s | −4.0 dBFS | a `SpellEvolutionCatalog` evolution being applied |

## Interface

| File | Length | Peak | Trigger |
|---|---|---|---|
| `card_appear.wav` | 0.18 s | −15.0 dBFS | each card dealt in `LevelUpMenu` / `ChestItemSelectionMenu`, staggered ~70 ms apart |
| `card_select.wav` | 0.55 s | −9.0 dBFS | an option confirmed in either menu |
| `reroll.wav` | 0.23 s | −12.0 dBFS | `LevelUpMenu.OnRerollPressed` (`scripts/LevelUpMenu.cs:51`) |
| `ui_hover.wav` | 0.05 s | −26.0 dBFS | `Control.MouseEntered` on any button — fires constantly, hence the level |
| `ui_click.wav` | 0.11 s | −16.0 dBFS | any forward/confirm button. **Wired** for a purchase in the Arcane Upgrades shop (`MainMenu.Shop.cs`) |
| `ui_back.wav` | 0.12 s | −16.0 dBFS | any back/cancel button, including `ui_cancel` |
| `ui_denied.wav` | 0.24 s | −15.0 dBFS | a locked stage or unaffordable arcane upgrade. **Wired** for the second: A on a shop card that is short of energy or not open yet (`MainMenu.Shop.cs`) |
| `ui_open.wav` | 0.35 s | −14.0 dBFS | a menu or overlay opening |
| `ui_close.wav` | 0.30 s | −15.0 dBFS | a menu or overlay closing |
| `ui_pause.wav` | 0.30 s | −12.0 dBFS | the in-run pause |

## Run and meta stingers

| File | Length | Peak | Trigger |
|---|---|---|---|
| `wave_warning.wav` | 1.30 s | −7.0 dBFS | a wave or boss inbound, from `Node2DGame`'s wave scheduling |
| `stage_clear.wav` | 1.85 s | −4.0 dBFS | **wired** (2026-11-02), `GameOverScreen` appearing on a **victory** - it played `game_over` there until then, a descent with no resolution under the word Victory |
| `game_over.wav` | 2.50 s | −5.0 dBFS | **wired**, the defeat's word landing (`DefeatSequence`, as DEFEAT slams onto its band); `GameOverScreen` appearing only for a defeat that did not come through the sequence |
| `tally_tick.wav` | 0.24 s | −20.0 dBFS | **wired** (2026-11-02), each step of the results screen's Arcane Energy count, `SfxPlayer.TallyTick` - pitched 1.0 to 1.26 up the count (the XP orb's ladder idea), at most one every 45 ms, about fourteen a second. A coin struck twice and left ringing on A5; three click-like drafts measured 0.051-0.069 from `ui_click` and `ui_hover`, this one's nearest neighbour is `card_select` at 0.402 |
| `tally_payout.wav` | 1.29 s | −6.0 dBFS | **wired** (2026-11-02), the count landing on its figure: a purse thump, a spill of the tick's coins, one struck C major chord. **Critical** - it lands inside `game_over`'s duck. Nearest neighbour `stage_clear` at 0.212 |
| `meta_unlock.wav` | 1.70 s | −4.0 dBFS | `SaveData` gaining a character, stage or max-difficulty record — the one sound that means the **save** changed, not the run |

## Chapter hints

Partway through a run, while a chapter's side event still holds a wizard or a spell the save does
not own, a cue comes from its direction (`StageHintDirector`, `StageHintCatalog`). Four a run at
most, so they sit at the quiet end of the once-a-minute band, and the game plays them positionally
300 px toward the source (`SfxPlayer.AtPosition`), which takes another 3.5 dB off and pans them that
way. Tier Normal: they never duck anything. The caption carries the words; these carry the place.

| File | Length | Peak | Trigger |
|---|---|---|---|
| `hint_grove_whisper.wav` | 2.20 s | -9.0 dBFS | **wired**, Enchanted Forest (the grove). Three breathed syllables with gaps between them over a low open-fifth hum and a few leaf ticks. hf 3% |
| `hint_cell_cry.wav` | 2.30 s | -9.0 dBFS | **wired**, Cursed Dungeon (the cell). A woman's call, formant synthesis: one rising "Help!" syllable, then the falling-then-rising tune of "Is someone there?" with no consonants, in a long stone room with a corridor slap. hf 2% |
| `hint_lantern_flare.wav` | 2.30 s | -9.0 dBFS | **wired**, Blighted Swamp (a lantern). A flame catching, burning level for a second at a 13 Hz flicker, guttering out in uneven gaps, under a thin cold E5 with a slow wobble. hf 2% |
| `hint_ice_song.wav` | 2.58 s | -9.0 dBFS | **wired**, Frozen Waste (the ice). Rebuilt 2026-11-01 to be heard as singing (Eric, 0.10.0): a short groan and one crack, then a man singing a six-note line falling through a D minor pentachord (A G F G E D, legato, vowels changing under it, vibrato blooming on the held last note), muffled at 1.6 kHz. The notes are `sfx._ICE_SONG`, which `StageHintCue.IceSongNotes` puts on screen. Nearest sound in the whole set 0.164 (`secret_found`). hf 0% |
| `hint_caravan_bell.wav` | 2.50 s | -10.0 dBFS | **wired**, Scorched Sands (the caravan). Five strikes of a hollow tin bell with a wooden clapper, at a walking beast's uneven stride, nearly level, one dune echo. A dB under the rest: a bell carries. hf 0% |
| `hint_sibyl_voice.wav` | 2.55 s | -10.0 dBFS | **wired**, Sunken Cave (the nine mouths, the Cumaean Sibyl). Her line - an old woman intoning three syllables, F#4 rising to A4, G4, a long trembling E4, with a husk of breath through the same resonances - then the cave giving it back six times a beat (0.24 s) apart, each darker (a 7 ms pipe comb, lowpassed lower each time) and quieter, through the far room. The onsets are `sfx._SIBYL_VOICES` = `StageHintCue.SibylVoices`. A declared twin of the two below (one voice by design); nearest other sound in the whole set 0.143 (`hint_lantern_flare`). hf 1% |
| `sibyl_voice.wav` | 1.25 s | -12.0 dBFS | **wired**, `NineMouthsEvent`: her line out of the true mouth, first in every call, played at that mouth through `PlacedSoundSchedule`. Near and nearly dry - a short stone room, nothing above 3.4 kHz - so the first voice is also the one that sounds like a person. Nearest other sound 0.139 (`hint_lantern_flare`). hf 3% |
| `sibyl_echo.wav` | 1.60 s | -15.0 dBFS | **wired**, `NineMouthsEvent`: the same line out of each of the other eight mouths, a beat apart, and once out of a mouth that answers. From deep in the rock: lowpassed at 1.3 kHz, a 7 ms pipe comb, a smeared start, a long dark tail. Three dB under hers, because hers is the one to hear. hf 1% |
| `hint_paper_wind.wav` | 2.20 s | -10.0 dBFS | **wired**, Mystic Ruins (the Five-Pointed Seal, Abe no Seimei). Five gusts, each carrying a strip of paper snapping in the wind (band noise at 1.4-2 kHz chopped at 24-32 Hz, `sfx._PAPER_GUSTS` = `StageHintCue.PaperGusts`), a few dry crackles of the paper burning under the last three, over one thin whistle of wind through the courtyard that rises and falls with them. Nearest in the whole set 0.125 (`ui_open`); a first draft with no whistle, at 2-3 kHz, was 0.074 from `cast_wind`. hf 6% |
| `hint_anvil.wav` | 2.50 s | -10.0 dBFS | **wired**, The Emberdeep (the great anvil, Wayland the Smith). Four tired blows on an anvil, somewhere below: each the ring of struck steel at a free bar's inharmonic modes (1.24 kHz x 1, 2.76, 5.40, 8.93) over the thud of the hammer landing, and the small rebound tap a smith lets the hammer make 0.11 s after - the gaps lengthening (0.56, 0.60, 0.66 s) and each blow softer (`sfx._ANVIL_STRIKES` = `StageHintCue.AnvilStrikes`, where the sparks come in). Top cut at 3 kHz, a big dark room. Nearest in the whole set 0.180 (`ui_hover`), in the hint category 0.165 is still the closest pair (ice song and caravan bell). hf 2% |
| `hint_flask_chime.wav` | 2.40 s | -10.0 dBFS | **wired**, Blighted Swamp (the gibbet's cage, Paracelsus). Glass ringing across the water and a man coughing, far out on the bog: two taps of a fingernail on a round-bellied flask with a dose in it (a few ms of bright click, then a stiff shell's modes at 1.32 kHz x 1, 2.41, 4.07, 5.93, damped by the liquid so they ring a third of a second, and a low slosh under them), then a cough twice - the burst of the glottis opening through the "kh" of the throat and an open "ah" (noise at 560, 1250 and 2600 Hz), a short voiced bark in a man's register (118 then 112 Hz) and the breath after it - the second weaker, and one faint tap more. The coughs are the loudest thing in it: with the taps leading, it measured 0.098-0.102 from `cast_metal` (a struck shell is a struck shell); lowering the taps until the coughs led moved it to 0.113, and lowering them further made it noise and 0.086 from `hint_lantern_flare`, the Swamp's other hint. The onsets are `sfx._FLASK_TAPS` and `_FLASK_COUGHS` = `StageHintCue.FlaskBeats`, where the vapour comes in. Top cut at 3.2 kHz, a wide soft room over water. Nearest in the whole set 0.113 (`cast_metal`), then 0.126 (`spell_familiar_bite`). hf 1% |
| `escort_cough.wav` | 0.62 s | -12.0 dBFS | **wired**, Blighted Swamp (the gibbet's cage, Paracelsus), not a hint: `GibbetCageEvent.TakeRot`, at his feet, when he steps into a rot pool on his way to the ferry and every 1.1 s he stays in one. The bog's wet gulp, then his cough - `hint_flask_chime`'s cough, near and hard |
| `ice_string_1.wav` - `ice_string_5.wav` | 1.60 s | -11.0 dBFS | **wired**, Frozen Waste (the thaw, Väinämöinen), not hints: `ThawEvent`, the kantele's five strings of ice, low to high (A3, D4, F4, C5, A5), each played at its string as the ice sings a phrase on them (`PlacedSoundSchedule`, 0.6 s apart) and as he steps on it in turn (`SfxPlayer.AtPosition`, no pitch jitter); a phrase sung back strums the strings it used, rising. A plucked doubled course beating at 3.4 Hz, ice's glassy partials, a pick, and the lake's falling chirp at 0.24 s. `Kantele.Notes` names them. hf 0-9% |
| `ice_string_wrong.wav` | 0.50 s | -12.0 dBFS | **wired**, `ThawEvent`: a string stepped on out of turn, at his feet - a knock of ice and the string choked and rattling (G3 and G#3 beating through a 28 Hz buzz), no ring and no note. The phrase is sung again 1.3 s later. hf 2% |
| `mirror_turn.wav` | 0.46 s | -15.0 dBFS | **wired** (2026-11-08), Scorched Sands (the dry tomb's mirrors, Imhotep; `.ai/unlock-puzzles.md` 3.1), not a hint: `TombBeaconsEvent`, at the mirror, whenever his feet swing one to a new facing - at most every 0.14 s however fast he walks round it. The bronze disc's own ring is the sound: a thin plate's modes (470 Hz x 1, 1.59, 2.14, 2.30, 2.65, `sfx._BRONZE_PLATE`) gliding up to 505 Hz over the first tenth of a second as the turn carries it, ringing down in a third; under its start the spindle's stick-slip in the stone, four dry grains of grit at 380-460 Hz, quickening. 120 Hz to 3.2 kHz, a small dry room. Nearest in the whole set 0.117 (`tally_payout`, the defeat screen's), in the hint category 0.178 (`hint_lantern_flare`); the first draft, the grind alone (eight grains round 900 Hz), was 0.084 from `hint_lantern_flare`. hf 1% |
| `sun_arrive.wav` | 1.20 s | -11.0 dBFS | **wired** (2026-11-08), `TombBeaconsEvent`: the sunlight landing on a mirror it had not reached (at that mirror, at most every 0.2 s) and on the sun-disc over the tomb's door (at the tomb, with `impact_earth` for the slab). The light catching, warm and rising, with no strike in it: a breath of air drawn up through a band rising from 700 Hz to 2.2 kHz, then F4 A4 C5 swelling out of it (sine voices doubled 0.4% sharp, so it shimmers) over the mirrors' bronze ring on F4, fading in a second. Not `cast_light` (a struck bell on G5), not `meta_unlock` (stacked fifths swelling two seconds and more), not `bearing_found` (a needle and an open fifth). Lowpassed at 4.6 kHz. Nearest in the whole set 0.161 (`hint_caravan_bell`), then 0.202 (`secret_found`). hf 3% |
| `lantern_catch.wav` | 0.82 s | -9.0 dBFS | **wired**, Cursed Dungeon (the Gaoler's key, Tituba), not a hint: `GaolersKeyEvent.Tick`, at his feet, when a lantern-bearer's light falls on him while he carries the key and it drops. The shutter's clack and knock, the cold flame flaring and burning a moment (rising, where `player_death` falls), the key ringing twice on the stone. Nearest in the whole set 0.126 (`player_death`), in the hints 0.144 (`hint_lantern_flare`). hf 5% |
| `hint_hut_step.wav` | 2.30 s | -10.0 dBFS | **wired**, Enchanted Forest (the walking hut, Baba Yaga). Something heavy walking the deep wood, on two legs: two falls of a great foot 0.64 s apart (a low thump falling from 96 to 54 Hz as the ground takes it, a short dark crunch of earth and old leaves round 560 Hz - what carries on a phone - and a dull knock of the hut's timber a beat after), the second softer, then the hut's logs creaking as its weight settles: stick-slip, a train of clicks whose rate rises and falls, rung through a log wall's two resonances (360 and 930 Hz). The onsets are `sfx._HUT_FALLS` and `_HUT_CREAK` = `StageHintCue.HutBeats`, where the birds lift. Top cut at 2.2 kHz, a short damp forest room, nothing under 50 Hz. Nearest in the whole set 0.187 (`spell_explosion`), then 0.197 (`impact_earth`, which the hut's own footfalls play in the world). hf 0% |
| `hint_hull_knock.wav` | 2.40 s | -10.0 dBFS | **wired**, The Last Bridge (the barge, Morgan le Fay). A moored wooden hull knocking on the bridge's pier far below, three times at the uneven gaps a swell gives (0.92, 0.64 s) - each a dull wooden tok at 700 Hz over a short boomy box resonance at 150 Hz and its next mode at 262 Hz, with the hull's softer second touch 90 ms after - water slapping the pier between them and one creak of the mooring rope as the swell lifts it, one echo off the arch and a short open room, the top cut at 2.2 kHz. The knock onsets are `sfx._HULL_KNOCKS` = `StageHintCue.HullKnocks`. Nearest in the whole set 0.224 (`impact_darkness`), in the hint category 0.327 (`hint_grove_whisper`); a first draft with the second mode at 352 Hz was 0.142 from `impact_water`. hf 0% |
| `hint_quatrain.wav` | 3.30 s | -10.0 dBFS | **wired**, The Court (the seer's voice, Nostradamus). An old man reading aloud in a room of the house: four lines of a quatrain, four syllables each (weak, strong, weak, strong), read as two couplets with a breath between them. Formant synthesis in a man's low register (96-141 Hz), the pitch moving inside every syllable as speech does and falling through each line, the first and third lifting at their ends, the fourth lower, falling furthest, its last syllable held; an old voice's quick tremor (6.4 Hz) and a husk of breath; consonants as breaths, hisses, stops, glides and a hum, so it is read and not sung; each line a little softer than the last. Near, not far: a stone room's air at a fifth of the level, 85 Hz to 3.2 kHz. The line onsets are `sfx._QUATRAIN_LINES` = `StageHintCue.QuatrainLines`, where the marks come in. Plays as the chapter hint (`StageHintCatalog`, site `seers_voice`) and, in the seek, every few seconds from the room his voice is in (`SeersVoiceEvent`, `SfxPlayer.AtPosition` there, so it pans and fades from that room), so it is level and soft-edged on repeat. Nearest in the whole set 0.149 (`hint_ice_song`; a first draft with no breath between the couplets and the room at 0.2 was 0.107 from it and 0.106 from `card_select`), then 0.157 (`player_death`); 0.21 or more from the Sibyl's three and the cell's cry. hf 2% |
| `hint_tomb_knock.wav` | 2.70 s | -10.0 dBFS | **wired**, Scorched Sands (the dry tomb, Imhotep). Far off in the dunes, a slow knocking, stone on stone: three knocks, the gaps lengthening (0.86, 0.98 s) and each softer - "slower now; it has not stopped" - each a dull, dense tok of a stone block's low modes (212 Hz x 1, 2.42, 4.07, gone in a tenth of a second: dead stone, not the anvil's ringing steel), the weight of it in a short low thump, and the grit of two faces grinding (band noise at 700 Hz, chopped at 46 Hz). Under it a steady hiss of running sand that swells after each knock, as if the knock shook some loose (`_sand_hiss`). The onsets are `sfx._TOMB_KNOCKS` = `StageHintCue.TombKnocks`, where the sand lifts. Top cut at 2.6 kHz, the open desert's short dry reflection, nothing under 60 Hz. Nearest in the whole set 0.176 (`spell_orbit`), then 0.177 (`ui_close`); in the hint category 0.143 is still the closest pair (lantern flare and Sibyl). hf 3% |
| `hint_granary_voice.wav` | 2.20 s | -10.0 dBFS | **wired**, Fallow Fields (the Tithe-Reeve's scale, Medea). A woman's voice from the reeve's granary, said, not called: an alto from B-flat 3 down to F-sharp 3, two words, a breath, then the longer line, every phrase falling and almost no vibrato - the cell's cry rises into a question, this does not plead. Six formant syllables (`sfx._GRANARY_SYLLABLES` = `StageHintCue.GranaryVoices`, where her smoke comes in), a breath under the last, and a few soft crackles of herbs smouldering. Through plank walls: top cut at 1.7 kHz, a box resonance at 450 Hz, a short dry timber room. Nearest in the whole set 0.164 (`pickup_health`), then 0.179 (`hint_cell_cry`). hf 0% |
| `hint_brush.wav` | 2.40 s | -10.0 dBFS | **wired**, The Court (the dotted door, Ma Liang). A brush drawing itself along the wall: three strokes of a loaded brush on plaster, the first longer (0.46, 0.36, 0.32 s), each a soft wet press with no click (a brush has no hard part), then the drag - a hush of bristles over a rough surface in a band sliding up from 1.15 to 1.75 kHz as the load runs out, its grain the bristles catching and letting go (an irregular chop at 64-82 Hz), a slow low smear of the wet paint under it - lifted off at the end, not stopped; then a drop running off the last stroke, a short rising chirp as it parts (780 to 1560 Hz) and a soft landing, twice. The onsets are `sfx._BRUSH_STROKES` and `_BRUSH_DRIP` = `StageHintCue.BrushBeats`, where the dabs of the paint cue come in. Indoors, a long room of the house: 110 Hz to 3.4 kHz, a short bright tail. Plays as the chapter hint (`StageHintCatalog`, site `dotted_door`), when the last dot inks the door in (`DottedDoorEvent`), and when Paint What Is Needed draws his flask (`PaintedFlask`). Nearest in the whole set 0.131 (`spell_beam_loop`; a first draft with longer strokes and one drip was 0.111 from it), then 0.182 (`player_death`); 0.27 or more from every other hint |
| `hint_hunt_horn.wav` | 2.50 s | -10.0 dBFS | **not wired** (2026-11-08), The White Downs (the chapter hint; its side event is the stalk of Artemis's silver hind). A hunting horn far off on the downs, blown for a hunt that has lost its quarry: A4 "doubled" - four notes tongued fast - then held and sagging as the breath goes, then a long D4 a fifth under, wavering and falling more than a semitone by its end. A call with no answer. The chapter's own horn (`sfx._hunting_horn`, the score's `hunt_horn`): a straight brass tube, a lip's scoop into each note and a breath of tongue, a lowpass that opens with the note. Back once off the next hill (an echo at 0.5 s), a wide open night, 220 Hz to 2.2 kHz. The onsets are `sfx._HUNT_HORN_CALL`, for `StageHintCue` if it marks them. A dB under the voices, as the caravan's bell is: a horn carries. Nearest in the whole set 0.190 (`hint_quatrain`), 0.452 from `wave_warning` (two low saws swelling). hf 0% |
| `hind_bolt.wav` | 0.75 s | -9.0 dBFS | **not wired** (2026-11-08), The White Downs (the stalk), not a hint: Artemis's silver hind looking up, seeing him move, and bounding away - played at her, a few times a stalk at most, `lantern_catch`'s row. A deer's alarm (a hard blow of air through the nose, noise at 620 Hz and 1.5 kHz cut off dead after a tenth of a second, a rough bark inside it falling 380→250 Hz), then six strikes of hoof on chalk turf in a bound's pairs (`sfx._HIND_HOOVES`): a soft thud and a damp crunch, a click of flint on the first of each pair, each pair softer and duller as she goes. 100 Hz to 3.4 kHz. Nearest in the whole set 0.121 (`impact_arcane`), in the hints 0.146 (`hint_quatrain`); 0.349 from `escort_cough`, the other snort-and-bark in the set. hf 4% |

| First draft | Collided with | Distance | Fix |
|---|---|---|---|
| a continuous whisper over a rustle | `cast_wind` | 0.044 | separate breaths with silences (speech rhythm), and a low hum in the band wind is highpassed out of |
| a whoosh, a thump and a decay | `impact_darkness` | 0.062 | the flame holds level, then gutters: held, not struck |
| one strike and its tail | `ui_pause` | 0.084 | five nearly level strikes - a rhythm, not a decay |
| the cry at G4 | `meta_unlock` | 0.104 | raised to B-flat 4 rising to C-sharp 5, called rather than spoken - out of the A-minor chord's partials |
| the mirror's turn as a grind alone, eight grains round 900 Hz | `hint_lantern_flare` | 0.084 | the disc's bronze ring made the sound, the grit four grains low under its start: a drag that ends in metal, where the flare is a band of noise held |
| the hunt's call on B4 falling to E4 | `hint_caravan_bell` | 0.081 | E4 is the tin bell's 330 Hz. On E5 to B4 it was 0.109 from `hint_paper_wind` (its whistle sits round E5) and on D5 to A4 0.074 from `card_select`; on A4 to D4, with the doubling and the echo at 0.5 s, its nearest is 0.190 |

## Secrets

The secrets of `.ai/stage-secrets.md` and `.ai/achievements-and-secrets.md`: the soft shimmer and
the found sting section 7.7 of the first asked for, and the Geode Heart's shatter (8.4.1 of the
second). Generated 2026-10-30 (`tools/audio/sfx.py`, "secrets"), allowlisted in `SfxCatalog`
(`SecretSeen`, `SecretFound`, `SecretShatter`) and wired by piece A4 the same day, each at the
Trigger below (the first reading of `secret_seen`: once per secret per save, at the site).

They are the near tell in sound. A secret's tell is a warm glint that breathes (stage-secrets
section 2), so the shimmer and the sting are a cloud of soft sine grains - each a Hann swell, the
cloud swelling and going - in A minor's pentatonic from A4 to E6, never on G5 (the light element's
bell). Breathed, never struck: every other reward in the game is a struck bell, and a secret must not
sound like one. Nothing in them opens on `palette.transient`, because nothing here is a spell.

| File | Length | Peak | Tier | Trigger (proposed) |
|---|---|---|---|---|
| `secret_seen.wav` | 2.05 s | -9.0 dBFS | Normal | `SecretDirector`, the moment a secret's near tell is first on screen - the seen mark, recorded into `SaveData.GlimpsedSecretIds` (achievements-and-secrets 4.3, 4.9): **`SfxPlayer.AtPosition(name, site)`**, once per secret per save, with no toast and no text. At the site, not global: the tell is already on screen, so the pan points at nothing he has not seen, and at the ~450 px the mark is taken from Godot's attenuation takes another 5-6 dB off, which is what makes -9 "soft". If stage-secrets 7.7's reading is wanted instead (every approach to a dormant site, every run), it still fits the band: a few a run |
| `secret_found.wav` | 1.74 s | -8.0 dBFS | Normal | `SecretDirector.Found`, **in place of `event_reward`** at both of its sites there (a secret found again this run - another pen blooming - and a find in a later run). `meta_unlock` stays where it is for a find that changes the save (the page mark the first time; a vault's spell): it is the one sound that means the save changed. `event_reward` then goes back to meaning a side event's chest. Global, under the toast. A few a run - every Forest pen blooms - so beside `event_reward` |
| `secret_shatter.wav` | 2.30 s | -4.0 dBFS | **Critical** | the Geode Heart's `Breakable` reaching its last stage (the Sunken Cave's vault, C2), as the ring of frost goes out: `SfxPlayer.AtPosition(name, crystal)`. Once a run at most. Then `SecretDirector.Found` plays its own sting a beat later (`meta_unlock` the first time, with Frost Shard; `secret_found` after), so the shatter carries the breaking and the freeze and leaves the reward to that |
| `secret_voice_below.wav` | 2.70 s | -8.0 dBFS | Normal | **wired**, the Cursed Dungeon's Oubliette (`Oubliette.Answer`): once his feet have been on its grate five seconds without a blow, `SfxPlayer.AtPosition(name, grate)` - only while the one below is still held (Nimue, not named: `UnlockDefinition.RevealSecretId`). Once a run at most. A woman's voice up a long shaft: a closed-mouth hum lifting D4 to F4, then "oh" to G4 and "ah" falling back to D4 and held, the vibrato blooming late; a drop falling into water before the hum, one in the gap, one in the tail; through a 12 ms pipe comb, 120 Hz to 1.5 kHz, a tall stone room. Not called (the cell's cry), not intoned and handed back (the Sibyl), not said (the granary): hummed, once, no echoes. The find waits it out (`Oubliette.VoiceSeconds`, held to the file's length by `ValidateOubliette`), so `meta_unlock` - Critical - never ducks it. Nearest in the whole set 0.158 (`hint_caravan_bell`), then 0.207 (`meta_unlock`). hf 0%, centroid 609 Hz |

What each is:

- **`secret_seen`** - one breath of the glint: twenty-six grains thickening over 0.7 s and thinning
  over the next second, under a faint band of air of the same shape. Not a tremolo (arcane's 17 Hz),
  not detuned triangles (arcane), not a run of glints (`spell_starfall` falls in 60 ms), not a swell
  into a held tone (`event_reward`). Centroid 1.07 kHz, hf 0%.
- **`secret_found`** - the glint, now his: the cloud **gathers** for 0.4 s (its first grain on the
  frame he finds it) and **blooms** into a rolled F major seventh (F3 F4 C5 E5 A5, 18 ms apart), a
  sine and a soft triangle per voice under a lowpass that opens as it sounds, with three grains
  settling over it. Centroid 670 Hz, hf 0%. Warm and on purpose no brighter: spread up to E6 it
  measured 0.112 from `player_heal` and 0.121 from `card_select`, and a Fenced Bloom heals as it is
  found, so this and `player_heal` play together.
- **`secret_shatter`** - two strain ticks 85 ms apart (the hairline cracks giving); the crack, a band
  of noise over a low split falling 190 to 92 Hz (nothing under 80: the sub band is the explosion's
  and the boss's); forty crystal shards going out, dense at the crack and thinning, falling in pitch
  as they fly farther, each two inharmonic partials at 1 : 2.43 - not the ice element's
  1 : 2.76 : 5.40 stack; a cold rush falling 2.2 kHz to 500 Hz (frost spreading - the ice element's
  hiss rises); all in a long cavern. Driven for weight, lowpassed after the drive. Centroid 2.0 kHz,
  hf 6%. It is Critical because it is a vault opening in the thick of the fight the crystal asked
  for, where a Normal sound can lose its voice to the swarm; that makes eleven Critical sounds of
  the fourteen `RegressionChecks` allows.

**The audit.** `analyse.py --confuse` compares inside a category, and these three are a category of
their own (`secret`) - so each was also measured against the **whole** set with the same profile.
Nearest neighbours: `secret_seen` 0.175 `hint_cell_cry`, 0.231 `hint_lantern_flare`;
`secret_found` 0.146 `wave_warning`, 0.152 `card_select`, 0.168 `hint_caravan_bell`, 0.192
`player_heal`; `secret_shatter` 0.155 `cast_wind`, 0.160 `card_appear`, 0.203 `spell_beam_end`. The
other glass breaking in the set, `player_shield_break` - one struck stack in 0.65 s where this is a
cascade over a second and a half - is not among its nearest six. Nothing within 0.14 of anything;
inside their own category the closest pair is `secret_seen` / `secret_found` at 0.415, and the full
`analyse.py --confuse` still reports **0 confusable pairs**.

| First draft | Measured | Fix |
|---|---|---|
| the found bloom voiced close from F3 at full weight | centroid 570 Hz, in the band the music owns | the low F softened (0.45) |
| the found bloom spread up to E6 for brightness | 0.112 from `player_heal`, 0.121 from `card_select` | back to F3-A5; warm, not bright |
| the found cloud starting from a near-silent density | the first 150 ms silent: a sting that answers late | its first grain placed on the frame he finds it |
| the shatter's strain ticks at a third of the crack | 30 dB under the peak, inaudible | raised to about 20 dB under |

## Deliberately absent

Listed so nobody adds them by reflex:

- **Footsteps.** The player is always moving in this genre; footsteps would be the loudest
  thing in the mix by play count and carry no information.
- **A sound per poison or burn tick.** Status damage is continuous and would smear the mix.
  The application is audible; the ticks are not.
- **Per-projectile travel loops.** A dozen missiles in flight is a dozen loops.
- **A distinct hurt sound per enemy class.** Class identity is the silhouette's job
  (`.ai/art-direction.md` §4). Three seeded variants plus pitch randomisation cover it.
- **Recorded voice or foley of any kind.** Out of reach of pure synthesis, and out of scope. The two
  hints that suggest a person (`hint_cell_cry`, `hint_ice_song`) are formant synthesis - a buzz
  through a vowel's resonances, phrased like speech, with no words in it. The caption carries the
  words; a synthesised word would be worse than none.

## Known limitations

- **Nobody has heard these.** They were designed and measured, not auditioned. The audition
  reels at `tools/audio/_preview_<category>.wav` exist for exactly that pass, and the first
  listen will almost certainly move some levels. Levels are one number each in `REGISTRY`.
- **The confusion audit is a proxy.** It measures spectrum and envelope, not melody or
  meaning. `level_up`, `chest_open` and `stage_clear` are separated by their note sequences,
  which the metric cannot see and which a listener certainly can.
- **`ui_denied` is the one sound with a deliberate square-wave buzz.** If it reads as a bug
  rather than a refusal, that is the first candidate for a redesign.

## Music

**Generated, like everything else here** - `tools/audio/music.py`, under the contract in
`.ai/audio-direction.md` section 11. The two stock tracks this section used to list were removed
before the repo went public, for want of a licence (`.ai/asset-licensing.md`); what replaced
them is ours outright. Status as of 2026-10-22: **a menu theme and all nine chapters built and
wired**, each its own mode of the same seven notes, 104-168 to the beat (section 11 has the
per-chapter table: key, metre, tempo, palette). **2026-11-01: battle music from the first second**
(section 11, "Battle music from the first second", has the cause and the change): the eleven
playable chapters' pulses rescored as their battle percussion and up from the first downbeat, the
hook stated as a run opens, and a boss layer each, so the shared boss loop now plays only for the
castle's three.

| File | Length | RMS | Plays | Up when |
|---|---|---|---|---|
| `assets/music/menu_theme.wav` | 26.67 s | -32.0 dBFS | title, every menu | always, as a solo loop; 2 s in, out over 1 s under a run's first downbeat |
| `assets/music/<chapter>_bed.wav` | 24.0-30.0 s | -33.0 dBFS | that chapter's runs | always while the set plays |
| `assets/music/<chapter>_pulse.wav` | same | -34.0 dBFS | that chapter's runs | always while the set plays, from the first downbeat (until 2026-11-01: 28+ enemies alive) |
| `assets/music/<chapter>_lead.wav` | same | -35.0 dBFS | that chapter's runs | the run's first 24 s; an elite, event elite or Warden alive, held 30 s; pace minute 5+ (was 10) |
| `assets/music/<chapter>_boss.wav` | same | -34.0 dBFS | that chapter's boss fight | the boss arrives: in over 1.5 s on the bed and pulse while the lead rests. The eleven playable chapters have one |
| `assets/music/boss_loop.wav` | 41.74 s | -29.5 dBFS | the boss of a set with no boss layer (the castle's three) | crossfaded in over 3 s as the boss arrives |

`<chapter>` is, in `StageCatalog` order: `forest` (25.71 s), `dungeon` (29.09), `cave` (27.00),
`swamp` (24.00), `ruins` (30.00), `frozen` (26.67), `sands` (27.69), `emberdeep` (25.60),
`sketchbook` (25.71). The three stems of a chapter are always the same length to the sample.

All 29 are 22050 Hz mono 16-bit, **33.4 MiB on disk** (the 28 rendered on 2026-10-22 are 31.6 MiB,
33.1 decimal MB; their blobs compress to about 25.5 MiB in git (loose objects went from 1.05 to 26.8 MiB on staging)), imported as QOA (`compress/mode=2`)
with `edit/loop_mode=2` (Forward) - about **6.7 MB in an export**, computed from the format.
Regenerate with `python tools/audio/music.py` (one piece at a time with a name: `music.py cave`);
measure with `music.py --report`, `analyse.py --music` and `mixsim.py --music --policy tiered`.
Audition reels are `music.py --journey`, written to the gitignored `tools/audio/_music_preview/`:
`<chapter>_journey.wav` (bed, then pulse, then lead, then the boss crossfading in),
`menu_journey.wav` (the theme twice, then a run starting) and `all_chapters_sampler.wav`.

### How it is wired

| Moment | Call | What happens |
|---|---|---|
| title screen, any menu | `MusicPlayer.PlayMenuMusic()` from `TitleScreen.StartTitle` and `MainMenu._Ready` | the menu theme as a solo loop, 2 s fade in; whatever else is playing fades out over the same 2 s. Idempotent - a theme already playing is faded back up, never restarted |
| run starts | `Node2DGame.PlayRunMusic` | `MusicCatalog.RunSetForStage` - the chapter's own set, bed, pulse and lead up; `MusicPlayer` starts it on its next frame (the run's first processed frame, after `_Ready`'s load) at bar one and full level, and fades the theme out under it in 1 s |
| every 0.25 s of play | `Node2DGame.UpdateRunMusic` | pulse at 1; the lead for the opening 24 s, an elite's 30 s hold, and pace minute 5 on; `MusicPlayer` fades it in 4 s and out 8 s |
| boss arrives | `Node2DGame.PlayBossMusic` from `TickBossPhaseStart` (and each Boss Rush fight) | a set with a boss layer raises it over the bed and pulse in 1.5 s, rests the lead, and plays on; one without crossfades to the shared `Boss` loop over 3 s and stops once silent. A Boss Rush breather lowers the layer again (or crossfades the set back in over 3 s) |
| boss falls | `CompleteBossVictory` | everything fades over 2.5 s, under `boss_death` |
| run ends, win or lose | `FinishRunAndReward` | the same fade, under `game_over` / `stage_clear` |
| quit to the menu mid-run | `TitleScreen` | `PlayMenuMusic` fades the set out under the theme; from a boss fight, the boss loop fades out on one solo player while the theme fades in on the other |

`MusicCatalog` is the only place a file is named. `RegressionChecks.ValidateMusic` warns at
startup for a missing file, a file that does not loop, a set whose stems differ in length
(which would drift them apart once a loop), a chapter in `StageCatalog` without a row in
`MusicCatalog.PerStage`, and a row for a chapter that does not exist. The Music slider is the bus
volume and `AudioSettings` owns it; nothing in the music path sets a `VolumeDb` against it.

### What the measurements say

All from 2026-10-22, on the 28 rendered files; section 11 of `.ai/audio-direction.md` has the
full tables.

- **Seams:** every loop's wrap is a smaller jump than the loop makes on its own (step 0.00-0.22,
  click energy 0.00-0.57 of the 99th percentile); every bed and pulse is within 2.4 dB of its
  median second at the seam, and the leads, most of which rest before the wrap, read -0.2 to
  -6.2 dB there on purpose.
- **Budget:** 0% of every stem's power under 80 Hz and 0% above 5 kHz, at most 2% in the
  2-5 kHz band, `hf` at most 2%. Beds 53-80% mid band, leads 68-100%, pulses 19-94% low (the
  dungeon's and the Emberdeep's are a big drum and a bass).
- **Under the effects** (`mixsim --music --policy tiered`): a median cast is +14.6 to +15.1 dB
  over a bed alone, +12.2 to +13.2 over bed and pulse, +10.1 over the boss loop and +7.9 to +8.7
  over a full late-game set; the music costs a late cast 1.4-1.8 dB of its headroom over the swarm,
  as the old Forest did. `player_hurt` stays 17-23 dB over it everywhere.
- **Inside each stem:** `music.py --balance` prints every voice's level; no ostinato is more than
  7 dB under its bed, no snare or tek more than 22 dB under its pulse.

**2026-11-01, the battle pass** (22 files: the eleven playable chapters' `_pulse` re-rendered and
their `_boss` added, with `music.py <chapter> --stems pulse,boss`; beds and leads untouched):

- **Seams:** step at most 0.33, click at most 0.40 of each loop's own body, every wrap within 2 dB
  of the median second.
- **Budget:** 0% under 80 Hz and above 5 kHz, at most 1% in 2-5 kHz, hf at most 1%; pulses 49-93%
  low band, boss layers 64-86%.
- **Under the effects:** bed + battle pulse from the first second, a median cast +11.9 to +12.9 dB
  over the music (the old bed + pulse row was +12.2 to +13.2); a chapter's fight, bed + pulse + boss
  layer with the lead resting, -28.5 to -29.2 dBFS, a cast +2.9 to +3.5 dB over swarm and music (the
  shared loop's +3.5), `level_up` at worst +8.1 dB; `player_hurt` +16.3 to +20.5 dB everywhere.
  `.ai/audio-direction.md` section 11 has the table and why the lead rests under the boss layer.
- **Size:** 24.6 MiB of WAV, 19.2 MiB of git objects; 2.6 MB more in an export (2,609,368 bytes,
  the eleven boss layers' imported QOA data as Godot reads it back; the pulses replace theirs at the
  same length). `spell_maw_open` adds 0.16 MB of WAV.
- **Headless** (`_BalanceHarness --immortal --minutes=6` on the Forest, Dungeon, Swamp and Sands;
  `_BossProbe` on the Cave, Ruins and Sketchbook): every run logs its set starting at bar one with
  four stems locked - bed, pulse and lead at 0 dB, the boss layer at -80 - and the pulse never
  leaves; `Node2DGame: run music lead out at 00:24`, back for the first elite (02:15 in the Swamp),
  held from pace minute 5; each boss logs `pulse 1 lead 0 boss 1` on arrival. The headless audio
  driver plays nothing, so this proves the faders, not the sound.
- **Not heard.** Section 11, "Not yet heard", lists what to listen for.

### The castle's music (2026-10-30: rendered, and wired in `MusicCatalog` by C0)

Piece C7 of `.ai/endgame-castle.md`: a set for each of the three new castle chapters, scored in its
own `tools/audio/music_<chapter>.py` (which `music.py` picks up by name) and rendered once into
`assets/music/`. The Cursed Dungeon, which moves into the castle, keeps its own set. Section 11 of
`.ai/audio-direction.md` has their rows in the chapter table and the rule they change (the Court and
the Throne take up his G sharp and B flat). **Nobody has heard them.**

| Files | Chapter | What it is | Length | RMS |
|---|---|---|---|---|
| `bridge_bed/pulse/lead.wav` | The Last Bridge, `stage_11` | a march under wind: A Aeolian, 2/4 at 120; low strings on the march figure under long low gusts (bed), a side drum, bass drum and oom-pah (pulse), horns in octaves then fifes (lead) | 24.00 s | -33 / -34 / -35 |
| `court_bed/pulse/lead.wav` | The Court, `stage_12` | a court dance with no dancers: an A minor minuet in 3/4 at 112; viols and a harpsichord's continuo (bed), a theorbo's bass, pah-pah and a tambour playing the minuet step's hemiola - the dancers' feet, with nobody dancing (pulse), recorder then viol (lead) | 25.71 s | -33 / -34 / -35 |
| `throne_bed/pulse/lead.wav` | The Throne, `stage_13` | the heaviest piece: A minor with B flat and G sharp, 4/4 at 104 in half time, a lament bass to E2; choir, strings, a brass pedal and a slow tread (bed), taiko, snare, square bass and a sixteenth-note motor (pulse), low brass then a chant (lead) | 27.69 s | -33 / -34 / -35 |

All nine are 22050 Hz mono 16-bit, 9.8 MB of WAV (about 2 MB in an export as QOA), each `.import`
written before the first import with `edit/loop_mode=2` (Forward) and `compress/mode=2`.

**Where they are wired, and by whom:**

- **`MusicCatalog`: the castle's plumbing agent (C0). Done** (`wip/castle-plumbing`): three planned
  sets with the Emberdeep's as their stand-in, under these ids - `PlannedChapter("bridge", Emberdeep)`,
  `("court", ...)`, `("throne", ...)` - and rows `[11]`, `[12]`, `[13]` in `PerStage`. A planned set
  switches itself on when its bed is imported, so these play now; `ValidateMusic` checks the three
  stems are the same length (they are, to the sample) and loop, and reports nothing.
- **The Last Bridge and the Court play as every chapter does** (`Node2DGame.PlayRunMusic`, the
  pulse and lead thresholds, the shared boss loop for the Constable and the Seneschal). Nothing more.
- **The Throne does not.** Whoever builds `ThroneRules` (C4) and the dark wizard (C5) between them:
  the set plays from the Muster on and **never hands over to the boss loop** - the whole chapter is
  the fight - so the Throne should not call `PlayBossMusic` (its `MusicSet.Boss` can stay the shared
  loop for `ValidateMusic`; it is simply never asked for). Bed alone under the Muster's level-ups
  (paused; the music runs in real time), the pulse and lead by the usual rule through the procession
  (the pinned pace clock keeps the lead up), all three for his first phase. **His second phase drops
  the pulse: `MusicPlayer.SetLayerTargets(pulse: 0, lead: 1)` when phase 2 begins** - the 8 s
  layer fade-out is the right speed for "the hall goes dark" - held against `Node2DGame.UpdateRunMusic`,
  which re-sets the layers every quarter second and would otherwise bring the pulse straight back;
  **phase 3 restores it** (`SetLayerTargets(1, 1)`, in over 4 s). His fall fades everything as a
  boss's does. No phase-2 file exists or is needed: every bit of the piece's drive is in the pulse,
  so the bed and lead are complete without it. `music.py --journey` writes
  `tools/audio/_music_preview/throne_journey.wav`: bed, pulse in, lead in, pulse out over 8 s, back
  over 4 s, the end - the switch, to listen to before it is built.

**Measured** (the same checks as every chapter; the scratch renders are byte-identical to the
committed files):

- **Seams** (`music.py --report`): step 0.00-0.24 and click 0.00-0.80 of each loop's own body, every
  bed and pulse within 2.3 dB of its median second at the wrap, the leads -3.0 to -3.4 dB there
  because each rests before it. The bridge's pulse is the clickiest seam in the set at 0.80: a side
  drum roll lands on the downbeat across it, which is the music, not a discontinuity (step 0.11).
- **Budget** (`analyse.py --music`): 0% under 80 Hz and above 5 kHz in all nine, at most 1% in the
  2-5 kHz band, hf at most 1%. Beds 51-70% mid band, leads 59-100% mid (the Throne's is 41% low, its
  brass doubled an octave down), pulses 65-85% low.
- **Under the effects** (`mixsim.py --music --policy tiered --sets bridge,court,throne`): a median
  cast +14.8 to +15.2 dB over a bed, +12.0 to +12.3 over bed and pulse, +7.5 to +8.3 over all three;
  `player_hurt` +16.3 to +21.7 dB over the music; the worst critical in the late game is still
  `elite_spawn` (-6.6 to -7.4 dB), the known redraw. The Throne, the heaviest, is the edge of the
  set: a late cast 0.4 dB nearer its music and `player_hurt` about a dB nearer than any road
  chapter's - still 16 dB clear. The Court's worst-tenth cast over bed and pulse (+6.8 dB) is the
  lowest of any chapter's (the road's are +7.4 to +8.7).
- **Inside each stem** (`--balance`): the bridge's gusts 7.5 dB under its bed and its side drum
  6.7 under its pulse; the Court's tambour 6.1 under its pulse; the Throne's tread 7.0 under its
  bed, its motor 6.7 and its snare 13.4 under its pulse.

### The far shore's music: the White Downs (2026-11-08, rendered)

The first far-shore set (`.ai/second-half.md` 4.2, F3), scored in `tools/audio/music_downs.py` and rendered once
into `assets/music/`. `MusicCatalog` already plans it (`PlannedChapter("downs", Heath)`, row `[14]`), so it switches
itself on - in place of the Broken Circle's stand-in - once its bed is imported. **Nobody has heard it.**

| Files | Chapter | What it is | Length | RMS |
|---|---|---|---|---|
| `downs_bed/pulse/lead/boss.wav` | The White Downs, `stage_14` | a hunt at night under no moon: **E hexatonic minor** (E G A B C D - no F, so not the Swamp's Phrygian, and no F sharp, which the seven lack), **12/8 at dotted quarter 140**, wrapping Am to Em (iv - i, the plagal close, no other chapter's). A B-E drone, held strings and the beaters' line walking long-short through the chord, two low horns held in B (bed); taiko on one and three, the hoofbeats on a hand drum (four footfalls a beat, three quick and the fourth on it), a snare on two and four, the bass on the gallop, pizzicato (pulse); **a hunting horn** - a new voice, a natural horn on E that plays only E, B and D, never a third - then three high women's voices in unison on "oo" with the horn far off in each gap (lead); the taiko doubled, brass stabs, a sixteenth motor and the hunting horn doubling on E (boss) | 27.43 s | -33 / -34 / -35 / -34 |

Why that and not a third gallop in 6/8: the Forest already gallops (6/8 at 112, horns in B), and every mode of the
seven notes is some chapter's tonic, so the Downs are made apart by a key with a missing degree, a metre no piece
has, a tempo no piece has, a lead voice no piece has, and a cadence no piece wraps on (the module's docstring has
the reasoning). 4.6 MiB of WAV (four 1.15 MiB stems), about 0.9 MB in an export as QOA; each `.import` written
before the first import with `edit/loop_mode=2` (Forward) and `compress/mode=2`, without a uid, which the importer
fills in.

**Measured** (scratch renders byte-identical to the files):

- **Seams** (`music.py --report`): step 0.00-0.27, click 0.04-0.15 of each loop's own body; every stem within
  1.8 dB of its median second at the wrap (the lead +0.7: the far horn calls across the seam into the hook).
- **Budget** (`analyse.py --music`): 0% under 80 Hz and above 5 kHz, 0% in 2-5 kHz, hf at most 1%. Bed 62% mid,
  lead 100% mid, pulse 83% low, boss layer 64% low.
- **Inside each stem** (`--balance`): the beaters' line level with the strings (-4.6 dB of the bed), the low horns
  -9.4 over the loop (-6.4 in B, where they play); taiko -3.9, bass -6.6, hoofbeats -8.2, pizzicato -10.7, snare
  -16.3 of the pulse; the horn -2.8 and the voices -3.5 of the lead, its two sections within 0.2 dB of each other;
  in the boss layer taiko -3.8 and brass -4.7, the motor -10.3, the hunting horn -11.8 (on alternate bars).
- **Under the effects** (`mixsim.py --music --policy tiered --sets downs`): a median cast +12.2 dB over bed and
  pulse (worst tenth +7.4), +10.9 with the opening lead, +7.4 over the fight's bed, pulse and boss layer (+2.8 over
  swarm and music, `level_up` the worst critical at +8.4), +8.1 over a late-game set; `player_hurt` +17.2 to +18.6.
  Inside the road chapters' ranges on every row.

**Wiring:** none needed beyond the import - the row exists. A boss layer is rendered, so the Unstrung Hunter's fight
raises it over the bed and pulse and rests the lead, as every playable chapter's does.

### Known limitations

- **Nobody has heard it** - the 2026-11-01 battle pass included. As with the effects, it was designed and measured, not auditioned.
  The first listen will move things; the knobs are `STEM_RMS_DB` (loudness), the event lists
  (the music), the velocities `music.py --balance` reports (the mix inside a stem) and the
  instrument functions (the timbre), all in `music.py`. Section 11 lists what to listen for.
- **The boss loop is shared - by the castle's three only, since 2026-11-01.** Every playable chapter
  meets its boss with its own layer, in its own key and tempo. The Last Bridge and the Court still
  hand over to the A minor loop at 138 (the Throne never asks for it); each gets a layer when it is
  playable - `boss_layer()` in its `music_<chapter>.py`, `--stems boss`, about 1.1 MB of history.
- **`AudioStreamSynchronized` is trusted to read its stream volumes live.** It is the engine's
  layered-music stream and is built for exactly this, but the crossfades have only been
  exercised headless, where the dummy audio driver plays nothing. If the pulse or lead never
  arrive by ear, that is the first thing to check.
