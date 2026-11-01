# Enemy behaviour — the contract, and the plan for the roster

How an enemy becomes more than a chaser: what the engine already gives you, what has to be built,
which behaviour each enemy in the cast should carry, and the rules any new one has to satisfy.

`gameplay-design.md` describes the five behaviours that exist today. This is the framework under
them and the plan for the rest.

---

## 1. The rule that shapes everything: not every enemy gets a telegraph

The brief was "different enemies should do different things", and every enemy in the cast should
indeed be distinguishable by what it *does*. But a wind-up is not the way to say that for all of
them, and giving every type one would make the game less readable, not more.

**Attention is the budget.** Seventy enemies share one screen. A telegraph works because the player
can pick it out of the crowd and answer it; if a third of the crowd is telegraphing at once, none of
them reads and the tells become texture. The current split is six plain chasers to five telegraphed
types, and that ratio is roughly right. The fix is not to telegraph everything — it is to give the
plain ones **movement character**, which costs the player no attention and still makes a wolf
unmistakably not a skeleton.

So behaviours come in three tiers, and most of the roster belongs in tier 0:

| Tier | What it is | Costs the player | Good for |
|---|---|---|---|
| **0 — Movement character** | Always on, no wind-up, no attack of its own. Circling, skirmishing, planting, drifting. | Nothing. It reads peripherally. | Most of the cast |
| **1 — Telegraphed attack** | Plants, shows a tell, resolves once. | Attention, and a reaction. | A minority |
| **2 — Death behaviour** | Fires when killed. Splitting, bursting, leaving a hazard. | Nothing up front, a surprise after | A handful |

An enemy may hold tier 0 and tier 2 together cheaply. Tier 0 plus tier 1 is the expensive
combination and should be deliberate.

---

## 2. What already exists — do not rebuild any of this

| Piece | What it gives you |
|---|---|
| `Enemy.AdjustSteering(chaseDirection, distanceToPlayer)` | The steering hook. **The returned vector's length is a speed multiplier** — unit to walk, longer to dash, `Vector2.Zero` to hold exactly still (which also suppresses the separation nudge, so a planted enemy cannot drift off its own tell). |
| `MazeNavigation` (the name is historical) | The one shared navigation field, on every procedural stage: routes round maze walls and water, and the `chaseDirection` a subclass receives already has it folded in. Cost is flat in enemy count. Design, cost and the checks are in `.ai/terrain.md` section 9. A behaviour should take `chaseDirection` and never build its own pathing. |
| `AttackTelegraph` | The wind-up clock: cooldown → wind-up → resolve exactly once. Deliberately not a Node — a swarm scene should not pay for a child node per attacker. |
| `GroundSlamAttack` | A marked shape, then everything inside it is hit. The outline drawn and the radius hit are the same number by construction. |
| `Enemy.ReadAttackTell` + `TelegraphIcon` | The icon over the attacker's head through a wind-up: pops in, fills, blinks, flashes on the strike. A class says what it is winding up (glyph and progress) and `Enemy` does the rest. See "Telegraphs are icons" below. |
| `TelegraphOutline` | The ground a shape covers, as an opaque dashed pixel outline, and its strike. Never a translucent fill (0.9.3). |
| `Enemy.StartDeath()` | Virtual. Drops rewards, leaves the `enemies` group and zeroes collision *in the same frame*, then plays `death`. The hook for every tier 2 behaviour. |
| `Enemy.ResetForRespawn()` | Virtual. Called when the recycler moves an abandoned enemy back to the ring. |
| `_Draw()` / `DrawGroundTells` | `QueueRedraw` already runs every physics frame, so a tell costs only the draw. What lies on the floor (an outline) goes in `DrawGroundTells`; what belongs to the body (a caster's aim tick) in `_Draw`. |
| The player's shove | `Enemy.ComputePlayerShove`, read off `Player.ShoveVelocity`. An ordinary enemy in the walking player's lane steps aside as he approaches and is pushed aside once he touches it; the player is slowed per body he is pushing (`Player.CrowdSlowPerEnemy`). Anything with `IsMiniBoss` (`Enemy.BlocksPlayer`) is not pushed and still blocks him, through `Enemy.BlockerCollisionLayer`. See 8, item 10. |
| The `attack` animation | The art contract already reserves **`attack`, 6 frames, non-looping** on every sheet. The slot for every wind-up exists. |
| Swarm pressure | `Node2DGame.SwarmPressure.cs` (`.ai/run-pacing.md` 10.3): while the trickle chokes on the cap, `Enemy.SwarmSpeedScale` speeds stragglers, the recycler brings bodies back ahead of the player, and `Enemy.AbsorbIntoSwarm` folds one body into another (health, `SwarmMass`, XP summed). Only bodies `SpawnEnemy` marked `SwarmEligible` - never an elite, event body, minion or boss - and only the exact `Enemy` type folds. See 8, item 11. |

---

## 3. The gap nobody has closed: `attack` is never played

`Enemy` plays exactly one animation by name — `death`. Every telegraphed enemy in the game draws a
geometric tell and stands in its `moving` loop while doing it. The art shipped (278 frames across
the roster); the code has never asked for it. *(Since written: `PlayAttackAnimation` is called on
every `Beat.Started`, and the geometric tell is now an icon over the head plus, where the ground
matters, a dashed outline - see "Telegraphs are icons", section 8.)*

This is the highest-value single change in this document. It improves all five existing behaviours
at once, it needs no new art, and "archers kneel and wind up" is *mostly just this* — the kneel is
the `attack` animation, and the wind-up clock already exists to drive it.

---

## 4. Phase 0 — plumbing, before any new behaviour

**0.1 Play `attack` on the wind-up.** A small `Enemy.PlayAttackAnimation()` driven from
`Beat.Started`, returning to `moving` on resolve or animation finish. It must fall back silently
when a sheet has no `attack`, exactly as `StartDeath` already does for `death` — otherwise adding a
behaviour to an enemy whose art is not redrawn yet breaks it.

**0.2 `EnemyCatalog`.** One authority for type → scene, spawn share, first-spawn time, health
multiplier, elite eligibility, behaviour tier. Today that is twenty `[Export]` fields plus a
thirty-line switch in `Node2DGame.SelectEnemyForCurrentStage`, and adding an enemy means editing
five places that can disagree. This is the same shape as the four disagreeing stage tables
`StageCatalog` replaced, and the same fix.

**0.3 `LevelTilePainter.TryFindPlaceableNear(worldPos, radius)`.** Rejects wall, water, ice, lava
and pit. Blink and Split both need to put something somewhere legal, and the only placement check
today is `Node2DGame.IsWallPosition`, which is maze-stages-only and private. The campaign plan
already called for this helper for its discovery sites; it is the same helper.

---

## 5. Phase 1 — tier 0 behaviours, no new primitives and no new art

Every one of these is an `AdjustSteering` override and nothing else.

| Behaviour | Who | What it does | Why it reads |
|---|---|---|---|
| **Circler** ("wolf") | FastEnemy | Closes to a ring, then orbits at a per-enemy direction, tightening as it goes. Commits inward when the player stops, or when a packmate is already in contact. | A pack that wheels around you rather than queueing behind you |
| **Skirmisher** | SlowEnemy | Advances, touches, then backs off instead of grinding. Hit-and-run with no dash. | Its damage comes in pulses you can count |
| **Bulwark** | TankEnemy | Closes fast, then plants near the player and stops. Becomes terrain. | You route around it instead of outrunning it |
| **Drifter** | *(unassigned)* | Ignores separation entirely and passes through the pack. | Nothing else moves through a crowd; deeply unsettling |
| **Bouncer** | *(new)* | Hops in committed arcs, each aimed with a wide spread, pausing between. Caroms off whatever it hits. | You can read the hop it is in and not the one after |

**Circler carries a measured hazard.** Lateral motion costs closing speed, and at the current pace
an enemy has very little to spare — an ungated encircling offset was measured to cut a straight-line
chase by ten times. `Enemy.ComputeApproachDirection` already has the guard for this: the `slack`
term, which spends lateral movement only when the player is not pulling directly away. **Circler
must reuse that gate, not reinvent it.**

### The Bouncer, and why "erratic" has to mean something specific

An enemy that moves erratically is the easiest thing in this document to get wrong, in two ways that
pull against each other.

**Erratic must not mean random.** Noise layered on top of a chase is the bad version: it reads as
drunk rather than dangerous, the player cannot plan around it because there is nothing to plan
around, and it is barely distinguishable from the path wander every enemy already carries. The good
version is **committed and unpredictable**: the Bouncer picks a hop, commits to it visibly, and the
player can read *that* hop completely — it is the one after that they cannot call. Determinism
inside each beat is what makes the unpredictability between beats feel fair rather than cheap.

**It must not be slower for it.** This is the measured trap. Movement spent sideways is ground never
recovered, and an erratic walker simply never arrives — the same failure that cut a straight-line
chase by ten times when encircling was left ungated. The Bouncer escapes it the way the Lunger does:
**it moves in bursts faster than it walks**, so it can afford to spend some of that burst on a wide
aim spread and still close. Hop speed is the knob that decides whether this enemy is a threat or a
decoration.

Two things worth deciding up front:

- **Keep the hop closer to a run than to a dash.** A burst fast enough to feel like an attack has to
  telegraph, and then the Bouncer stops being a tier 0 enemy and starts costing the player
  attention. Fast enough to close, slow enough to stay furniture.
- **Let it carom off the crowd** - but that is no longer free. Ordinary enemies stopped colliding
  with each other on 2026-10-28 (8, item 12): a hopping body now passes through the pack, eased
  aside by the separation push, and only the blockers (elites, minibosses, bosses) are still solid.
  A Bouncer that should glance off a skeleton and change course - still the cheapest readability
  this enemy could get - has to decide that itself, from its neighbours (`EnemyRegistry.ForEachNear`),
  rather than from the physics.

**It also exposes a real gap in the framework.** A Bouncer wants to be *still* between hops but
still *shoved* by the crowd it landed in. `AdjustSteering` returning `Vector2.Zero` means hold
exactly still, and it deliberately suppresses the separation nudge as well — that is what stops a
planted wind-up sliding off its own tell. There is currently no way to say "stop steering but stay
pushable". Adding one is a small change to the steering branch in `Enemy._PhysicsProcess`, and the
Bouncer is the first enemy that needs it. Build it with the Bouncer, not before.

(The player's shove is not that primitive, though it lives next to it. A planted body is still
pushed by the *player* when he walks into it - see 8, item 10 - but not by the crowd around it.)

---

## 6. Phase 2 — one new primitive each

**Blink** (the teleporting mage). A shared `BlinkMove` primitive on the same footing as
`GroundSlamAttack`: vanish tell → gone → arrival tell → appear.

- Placed through `TryFindPlaceableNear`, never inside the player (`MinPlayerSeparation`).
- **Both ends are telegraphed.** A blink with no arrival tell is indistinguishable from a spawn,
  and a spawn appearing next to the player is the thing the screen-relative spawn ring exists to
  prevent.
- Never mid wind-up. Teleporting out of your own tell is the same lie as a slam whose ring moves.
- A flag on `RangedEnemy` rather than a subclass, for the reason `Rooted` is a flag: "it blinks" is
  a tuning choice on a scene, not a new behaviour, and one class is easier to keep honest than two.

**Split** (the blob). A `StartDeath` override, and the rules matter more than the code:

- A **generation counter that stops at 2**. Unbounded splitting is an arena-filling bug.
- Children spawn at reduced health and scale, and are placed via `TryFindPlaceableNear`.
- **Children pay no XP.** The Summoner is capped on lifetime summons precisely because a spawner
  becomes an XP fountain; split is the same failure with a different trigger.
- Children count against `MaxEnemies` like anything else — a split at the cap spawns fewer, or none.

---

## 7. The roster, and where each enemy lands

| Enemy | Tier now | Proposed | Phase |
|---|---|---|---|
| `enemy` (skeleton) | — | stays plain, deliberately: the baseline everything else is read against | — |
| `FastEnemy` | — | **Circler** | 1 |
| `SlowEnemy` | — | **Skirmisher** | 1 |
| `TankEnemy` | — | **Bulwark** | 1 |
| `BooEnemy` | Wail + Phase (built) | **The Sketchbook's second miniboss**, scheduled twice a run. Elim's drawing (see 7a and 7b) | 2 |
| `WardenEnemy` | — | miniboss; **Bulwark + a telegraphed slam** (tier 0 + 1, deliberate) | 2 |
| `HexerEnemy` | Ranged | **+ Blink** between shots | 2 |
| `SkullSentry` | Ranged, rooted | **+ kneel** — the `attack` animation, 0.1 | 0 |
| `LungerEnemy` | Lunger | unchanged; gains the `attack` animation free | 0 |
| `SlammerEnemy` | Slammer | unchanged; gains the `attack` animation free | 0 |
| `ExploderEnemy` | Exploder | unchanged | — |
| `SummonerEnemy` | Summoner | unchanged | — |
| *(new)* Bouncer | — | **Bouncer** — erratic hops, tier 0 | 1 |
| *(new)* Blob | — | **Split** | 2 |

One enemy stays plain on purpose. A swarm needs a baseline for the others to be read against, and
the skeleton is it.

### 7a. Who spawns where

**Every chapter's cast is its roster now: `.ai/bestiary.md`** (`scripts/StageBestiary.<Chapter>.cs`),
which names a creature of that chapter's own for every role above - the table bodies, the six
behaviour roles and the miniboss - and stands the cast below in for each one until it is drawn. The
Bonelight roster in the table above is the Cursed Dungeon's; until 2026-10-24 it was every chapter's.

The campaign chapters draw from the Bonelight cast in the table above
(`Node2DGame.SelectEnemyForCurrentStage`, through the roster). **The Sketchbook (stage 8) draws only from the kids'
cast**, and none of it spawns anywhere else: Smiler, One-Eye, Zombie, Vampire, the Message, Blobby
(its charger), Pushy Dude (its slammer), Rockem (its heavy and its miniboss, in place of the
Warden), Wizard Dude (its boss), the Dog (a companion, not an enemy) - and **the Boo**.

The Boo is Elim's drawing, one of Eric's kids, though its sheets live in `assets/enemies/` and
predate the chapter. Until 2026-10-20 it opened every chapter (2.5% of spawns for the first five
pace minutes); for a few hours after that it did the same in the Sketchbook only. What leaving the
campaign did to its opening XP is in `.ai/run-pacing.md`, section 7, finding 2.

**It is no longer in any spawn table.** Eric, 2026-10-20: "we can keep the animation frames and
actually should tune up the drawing a bit to fit in with the rest of the art. update the art, and
also make it bigger and scarier and make it a mini boss." So it is the one kids' drawing that is
redrawn and has derived frames - by request, credited to Elim in `tools/art/sprite_boo.py`, and
built from his file every time the generator runs. Every other kids' sheet is still untouched.

### 7b. The Sketchbook's ghost (`BooEnemy`)

A miniboss on the 96×96 cell at ×2 (~128 px on screen, about 2.3× a skeleton), sent on the pace
clock rather than rolled: `Node2DGame.TickGhostMiniBoss`, at **pace 4:00 and 9:30**. Rockem, the
chapter's recurring miniboss, arrives at about 5:15, 8:00, 10:45 and 13:30, so the two interleave
into a set-piece every 75-90 seconds from minute four. **They never share the screen**: a ghost due
while the scheduled Rockem (or the last ghost) is alive waits for it to die, and Rockem's tick now
waits for the ghost the same way. Both are held **by reference**, not found by `EnemyType`: Rockem
is also 7% of the Sketchbook's ordinary table, and the first version, which asked "is any Rockem
alive", never sent the ghost at all. The table's Rockems are **ordinary bodies**: `SpawnEnemy`
clears `IsMiniBoss` on everything the table rolls (2026-10-27). Before that every one carried its
scene's miniboss flag into the swarm - unshovable, so a Sketchbook crowd pinned the player, and
50 XP a kill - and Rockem's own tick, which refuses to stack a second miniboss of its type, counted
the table's too and so was held back for most of a run. It counts minibosses only now. It calls the music lead (`musicLeadCallers`) like every elite, wears the elite
ring, and logs its arrival (`Node2DGame: the Sketchbook's ghost arrives ...`).

| | |
|---|---|
| Health | 260 at spawn, × (1 + 0.22 × pace minutes) × the preset's elite power - the Warden's curve. About 490 at 4:00 and 800 at 9:30 on normal |
| XP | 50, from `IsMiniBoss` - the same as the Warden and Rockem. Kept rather than raised: it is two kills a run now, not 2.5% of the opening |
| Body | `IsMiniBoss`: a blocker on layer 5 (the player slides off it, never shoves it), never recycled. Radius 42, speed 95, contact 2, knockback resistance 0.6, slow floor 0.35 |

**The wail** (tier 1, stand-off). Within 1.3× its radius it plants and a cyan ring grows round it
for **1.0 s** (`GroundSlamAttack`, ring, radius 150, 4 damage, every 4.6 s). The six-frame `attack`
strip plays at 6 fps, so its scream peaks as the ring lands; change `WailWindUpSeconds` and the
strip's speed together. The ring is the colour of its inner light, so the warning reads as the
ghost's own.

**The phase** (tier 1, anti-kite). Every 9 s (first after 5), if the player is 260-900 px away, it
goes untargetable (`Enemy.SetTargetable(false)`) and shows at 28% opacity while it crosses
(`BurrowMove`, 1.4 s) to **where the player stood when it began**, marking that spot with a
`TelegraphedGroundHit` (radius 100, 4 damage, 1.1 s). The mark lands 0.3 s before the ghost
re-forms on it - the bosses' rule that a burrow outlasts its tell, checked by
`RegressionChecks.ValidateGhost` along with every wind-up being non-zero. So the two attacks chain:
a player kiting at range is made to move, and then has the ghost beside him with the wail coming.

`ResetForRespawn` rebuilds both and ends a crossing, as the contract asks, though an elite is never
recycled. `_UiShot --ghost` / `--ghost-wail` (with `--stage=8`) photograph it beside the player and
mid wind-up.

### 7c. The Blighted Swamp's lantern-tender (`LanternTenderEnemy`)

The lantern event's last fight (`.ai/side-events.md`, "The lantern-tender", which has the numbers),
and the second miniboss on the 96 cell. Not a roster slot: `BogLanternsEvent` calls it. Tier 1 four
times over, which is deliberate for a set-piece the player walked out to: **fire strikes** lobbed onto
rings along his walk (change course), **spilt oil** in a wedge that stays alight (step sideways, and
not back - the section 10 "hazard behind" gap, first filled here), **the dark**, an inverted ring
where inside is safe (come to it - the anti-kite move), and a once-only **rekindle** of a snuffed
lantern that wards it until the player stands in that lantern's light again. One wind-up at a time,
and the specials wait for a thrown volley to land, so the budget of section 1 is spent one tell at a
time. Built from `AttackTelegraph` and `GroundSlamAttack` (the oil's cone, every strike's ring); the
two effects that outlive the swing, `LanternFireStrike` and `BurningOil`, are parented to the arena as
`TelegraphedGroundHit` is. `RegressionChecks.ValidateLanternTender`; `_UiShot --tender[=move]`;
`_ExploreProbe --stage=3 --slow-bleed`.

---

## 8. The contract for any new behaviour

Every line below is load-bearing, and most were learned by getting them wrong.

1. **If it can damage, it telegraphs.** An attack without a wind-up is indistinguishable from
   damage arriving at random.
2. **If it holds an `AttackTelegraph`, it overrides `ResetForRespawn` and rebuilds it.** The
   recycler moves abandoned enemies back to the spawn ring; one that arrives with a banked wind-up
   attacks before the player has seen it.
3. **`Vector2.Zero` from `AdjustSteering` means hold exactly still** — it also suppresses the
   separation nudge, which is what stops a planted enemy sliding out from under its own tell.
4. **The returned vector's length is a speed multiplier.** Return a unit vector to walk at `Speed`.
5. **Drawing is local space; hitting is global.** The outline drawn and the radius hit must be the
   same number by construction, not by two numbers that agree today. (`TelegraphOutline` draws round
   the canvas's own origin in world units, through the inverse of the body's scale, so an elite's
   ring is drawn at the radius it hits.)
5a. **It telegraphs with an icon, never a translucent build-up** (0.9.3). Override
   `Enemy.ReadAttackTell` - glyph and wind-up progress, read only - and the icon over its head
   follows. If the ground matters, draw the shape with `GroundSlamAttack.Draw` (or
   `TelegraphOutline`) in `DrawGroundTells`: an opaque dashed outline, full size from frame one.
   `RegressionChecks.ValidateTelegraphIcons` reports an enemy class that holds an `AttackTelegraph`
   or `GroundSlamAttack` and does not override `ReadAttackTell`.
6. **A `death` animation, or the node frees instantly.** That is the documented fallback, not a bug.
7. **Anything that spawns enemies is capped** — live *and* lifetime — and its offspring pay no XP.
8. **Add a `RegressionChecks` assertion.** `dotnet build` validates none of this.
   `ValidateAttackPatternEnemies` already checks that no wind-up is zero; extend it rather than
   starting a second checker.
9. **Lateral movement costs closing speed.** Anything that circles, strafes or sidesteps must gate
   that on slack, or it stops being able to reach the player at all.
10. **The player shoves ordinary enemies; he slides off the rest.** His body does not mask layer 2,
    so an ordinary enemy never stops him: `Enemy.ComputePlayerShove` moves it aside through its own
    `MoveAndSlide`, which masks walls and water, so a shove can never put a body in a lake. A body
    with `IsMiniBoss` (`BlocksPlayer`) also sits on `Enemy.BlockerCollisionLayer` (5), the one enemy
    layer his body masks, and is never pushed. **A yield is something an enemy does and a shove is
    something done to it:** a body that is not choosing where it goes - planted mid wind-up, stunned,
    frozen - does not step aside, but is still pushed once he touches it. That is safe for every
    telegraph because each draws in its owner's local space and resolves at the owner's position on
    the frame it lands, so the shape the player sees is still the shape that hits. A new attack that
    caches a *world* position at wind-up start breaks that, and must not.

    Measured with the walk bot, which marches one bearing straight through whatever spawns
    (`_BalanceHarness -- --minutes=3 --character=1 --timescale=2 --immortal --walk=30`, the
    report's `crowd:` line), two seeds each, same build but for the shove:

    | | steps with an enemy touching ahead | speed while touching | pinned (< 25% speed) | damage taken |
    |---|---|---|---|---|
    | body-blocked (before) | 37% / 49% | 0.69 / 0.71 | 4.0% / 3.7% of those steps | 410 / 579 |
    | shove | 17% / 22% | 0.91 / 0.87 | 0.1% / 0.2% | 199 / 270 |

    The yield is what halves the contact share: most bodies are out of his lane before he
    arrives. The orb-collecting immortal bot (no `--walk`) reached level 10 in three minutes both
    ways. `--check-feet` on the Swamp found no enemy centre inside water collision in 216 745
    enemy-frames. `_UiShot --shove` photographs a block of twelve parting round him.
11. **The swarm may speed, move and fold an ordinary body, so a behaviour must survive all three.**
    Under swarm pressure (`.ai/run-pacing.md` 10.3) every `SwarmEligible` body walks at
    `EffectiveSpeed`, which carries `SwarmSpeedScale` - a subclass that moves by anything other than
    the base chase should multiply by it too or it will lag the pack it belongs to. Recycling under
    pressure is more frequent, which is item 2 again. Folding is limited to the exact `Enemy` type on
    purpose: a subclass draws its tell in local space against its own numbers and a fold that changed
    its health or contact damage under it would break item 5's promise. A new subclass is therefore
    never folded unless someone decides it should be; if it should, it must say what a fold means for
    its attack. A body leaves the swarm through `SetTargetable(false)`, the one authority on group
    membership, so whatever tracks live enemies sees a fold the way it sees a burrow.
12. **Ordinary enemies are soft to each other; blockers are not** (2026-10-28). Eric: "enemies
    shouldn't get stuck on each other, they appear to be having a hard time chasing player because
    of it". `Enemy.ConfigureEntityCollision` masked layer 2 on every enemy, so every body was a
    wall to every other and a crowd closing on the player queued behind its own front rank - the
    kiting bot's photograph had him pinned on a lake shore with the swarm in one single-file column
    behind him. Separation could not prevent it: it only *bent the heading* (a 0.35 blend), so a
    body still walked straight into the one ahead and stopped against it.

    **Now:** an ordinary enemy masks layers 1 (the player's body, decor), 4 (the ground) and 5 (the
    blockers), and not 2. Separation is also a velocity: `EnemySpacingStrength` (95 to **285**) is
    the push in px/s from a neighbour standing exactly on the body, falling to nothing at
    `EnemySpacingRadius`, added after the lean. A blocker (`BlocksPlayer`: elites, minibosses,
    bosses) is still solid to everything - every enemy masks layer 5, so the swarm slides round it
    instead of standing inside its sprite, which matters because separation knows only the body's
    own radius and not its neighbour's. A blocker no longer masks layer 2, so it walks through its own
    crowd and the engine's depenetration moves the ordinary bodies off it (the mechanism the player's
    shove rides on). A blocker gets no push (a boss shoved about by its swarm stops reading as a
    boss), and neither does a dash (steering longer than unit), which goes where its tell said. A
    planted body still holds exactly still: the push sits in the steering branch, after the
    `Vector2.Zero` hold, so its neighbours step off it instead. Enemies also move in **floating**
    mode now: no enemy scene set `motion_mode`, so every body was grounded with up at the top of the
    screen - a body to its south was a floor that zeroed its vertical velocity and snapped it down
    onto it, one to its north a ceiling, and a side contact after a floor stopped it dead
    (`floor_block_on_wall`). The runtime is the authority; the `.tscn` files still say
    `collision_mask = 3`.

    **Measured, not guessed**, with five numbers `_MazeProbe` gained for it, all independent of how
    bodies keep apart. *Speed* is the distance a plain chaser more than 160 px from the player covered
    in a quarter second over the distance its own `Speed` covers. *Jammed* is the share of those under
    0.4 of it, and in brackets how many had another body in their last slide. *Closing* is the
    distance to the player shed over its own travel. *Stacked* is the share of bodies whose centre is
    within 16 px of another's (the readability cost). *Contact* is how many touch the player's
    HurtBox (what contact damage counts). Full swarm (`--stress-swarm`), at the game's own step
    (`--fixed-fps 60`, `--timescale=1`), 180 s, one run each. The bots that show a crowd are a player
    circling at half his speed (about the swarm's pace) and one standing still: at full speed a
    roaming player outruns the whole cast and the horde only trails him in a thin stream (6-7% jammed
    either way).

        "$GODOT_BIN" --headless --path . --fixed-fps 60 scenes/_MazeProbe.tscn -- --timescale=1 --stage=0 --seconds=180 --bot=circle --bot-speed=0.5 --stress-swarm --no-save-writes

    First, which part of the physics was it - Forest, circling, on the v0.8.1 base:

    | | speed | jammed (on a body) | closing | stacked | contact |
    |---|---|---|---|---|---|
    | grounded, solid to each other (as shipped) | 0.58 | 39.9% (35.4%) | 0.37 | 0.0% | 1.37 |
    | floating, solid | 0.59 | 39.1% (37.7%) | 0.40 | 0.0% | 1.65 |
    | floating, solid, `wall_min_slide_angle` 0 | 0.62 | 34.7% (34.0%) | 0.37 | 0.0% | 1.54 |
    | soft, separation as a lean only | 0.96 | 2.6% (0) | 0.68 | **21.6%** | **4.47** |
    | soft, push 190 | 0.89 | 5.1% (0) | 0.57 | 0.6% | 3.01 |
    | soft, push 285 (shipped) | 0.88 | 5.7% (0) | 0.57 | 0.4% | 2.75 |

    The motion mode was not the jam - the masks were. A soft crowd with only the old lean collapsed
    into stacks and three times as many bodies touched the player; the push holds it apart. 285
    rather than 190 for a little more room round the player at the same flow.

    Then the shipped change against `main-ff86ni` itself (the "before" build is that branch's
    `Enemy.cs` with the probe):

    | | Forest, circling: before / after | Forest, standing: before / after | Maze, roaming at 0.5: before / after |
    |---|---|---|---|
    | speed | 0.59 / **0.88** | 0.39 / **0.87** | 0.55 / **0.82** |
    | jammed (on a body) | 40.2% (35.5%) / **6.2% (0)** | 61.8% (58.1%) / **9.3% (0)** | 39.8% (34.8%) / **8.8% (0)** |
    | closing | 0.39 / 0.52 | 0.36 / 0.80 | 0.14 / 0.25 |
    | stalled | 40.9% / 38.3% | 64.7% / 28.9% | 52.4% / 47.4% |
    | close (8 walking tiles) | 59.5% / 53.2% | 72.5% / 68.5% | 24.7% / 37.0% |
    | stacked | 0.0% / 0.4% | 0.0% / 0.5% | 0.0% / 0.5% |
    | contact | 1.67 / 2.38 | 2.92 / **4.94** | 0.77 / 1.73 |

    **What it costs.** More of the swarm reaches the player, so more of it touches him: about 1.4x
    when he keeps moving, 1.7x when he stands in a full swarm. That is the change, not a side
    effect - but it is a difficulty lever. The survivor bot, which dodges, could not tell the two
    builds apart (`_BalanceHarness --minutes=6 --character=1 --survivor`, Forest, fixed 60 fps):
    mortal, it died at 4.4 / 2.2 / 2.5 minutes before and 2.5 / 2.9 / 3.6 after (seeds 11-13, mean
    3.0 both); immortal, it took 1236 / 315 damage in six minutes before and 626 / 462 after (seeds
    11, 12). The harness's spawns are random whatever the seed, and that spread is its noise. If
    standing in a crowd turns out to hurt more than it should in play, the knob is
    `EnemySpacingStrength`, not the masks. On the maze the share of *stalled* bodies with a wall in
    their last slide (`_MazeProbe`'s "pinned") rose from 0.2% to 2.5%: a body packed in a corridor
    near the player now touches the wall rather than a neighbour, while stalled overall fell. Worth
    watching, not a wedge.

    **Cheaper, too.** With no enemy-to-enemy contacts the physics has far less to resolve.
    `--profile=10`, Forest, full swarm, standing player, one Godot running: the enemy section fell
    from 1.8-2.4 ms a frame to 0.75-0.79, its `MoveAndSlide` from 10-14 µs a call to about 3, and
    physics from 2.8-3.5 ms a frame to 1.7-1.9.

    **Still needs eyes:** whether the soft crowd reads as a crowd in motion rather than as bodies
    passing through each other - the still photographs (`_UiShot --stress-swarm --still-for=0
    --terrain-seed=4242 --frames=1200`) show a ring about a body's width deep with sprites overlapping
    at the edges and no stacks; whether a boss walking through its swarm looks like the swarm parting
    or like bodies popping off it; and whether standing in a full swarm on the phone now hurts too
    much.

13. **A body that meets something slides, skirts it, or never walks into it** (2026-11-01). Eric,
    playing 0.10.0: "All enemies tend to get stuck on terrain. Find an efficient way to have them
    path better." The flow field (`MazeNavigation`, `.ai/terrain.md` section 9) already routed round
    water, walls and fences, so the first job was finding what it did not cover. `_MazeProbe` gained
    a **stuck** count for it: a body that asked to walk (`Enemy.IntendedSpeed`, at least 0.4 of its
    speed) for two seconds and covered under 0.15 of that walk, 160-900 px from the player at both
    ends - out of the crowd pressed on him, inside the ground he can see. Its last slide says what
    held it (the ground, a prop's body, another body, the crowd, or nothing), and a body stuck three
    windows running is **trapped**: the one that stands against a tree for the rest of the fight.
    `--stuck-log` prints each one with its contacts. Three causes, in the order they were found:

    - **Floating mode stops a body dead on a square hit.** `CharacterBody2D.WallMinSlideAngle`
      defaults to 15 degrees: meet a surface within that of head-on and the move is zeroed, not slid.
      The chase's whole wander is inside it (`PathNoiseStrength` 0.24 bends a heading 13.5 degrees at
      most), so a body walking at the player through a trunk, or into a wall's face, stood still -
      "moved 0 px" in the log, window after window. It is 0 now (`ConfigureEntityCollision`): the
      first slide keeps the step's full length along the surface, so any lean at all carries it
      round. Nothing in the game reads a stop at a wall, so no charge or dash changed.
    - **Wedges between props.** What was left on the Forest was all one shape: a body in the notch
      between two rocks 29 px apart, in a channel between a trunk and a rock, between a trunk and a
      shore. Neither is in the grid, and a lone prop should not be - a field round every trunk would
      bend the whole open-ground chase. So only clusters are: once a chunk is furnished, every
      blocking prop within a tile (`PropClusters.Gap`, 48 px - a wolf is 44 across) of another, or of
      water, a wall or a fence, is stamped into the chunk's flags as `TerrainStreamer.FlagProp` with
      the cells between them (`Node2DGame.StampPropClusters`), and the field routes round the cluster
      as round a pond. Read from the bodies furnishing actually made, so nothing else has to agree.
    - **Square into something the grid cannot see.** A body that asked to walk and covered under
      0.35 of it while touching something turns along the surface for 0.45 s (`Enemy.Skirt`, the
      geometry in `SkirtTurn`): on the side it already leans to, keeping that hand while it keeps
      meeting things, and turning along the new surface on the same hand if it is held again mid-turn -
      a wall-follower, which walks out of a corner instead of back into it; held three times running,
      it turns round. Folded into the chase direction before `AdjustSteering`, like the flow, so a
      planted wind-up still returns zero and holds exactly still, and a dash still goes where its tell
      said. Not within 120 px of the player, where touching something is arriving. A free body pays
      one subtraction and a compare against a position it already read; only a held body asks the
      engine what it touched (`IsOnWall`, `GetWallNormal`, neither allocates).

    Flyers are left as they were: a ghost still passes through decor, and its straight-line test
    crosses a cell closed only by a prop cluster, so a cluster never sends it round; a body that clears
    its layer-4 mask still ignores the field. A burrower under the floor still moves by `BurrowMove`.
    The Familiar's walk reads the same grid (`HasClearWalk`, `IsBlockedAt`), so it now goes round a
    cluster too.

    **Measured.** `_MazeProbe`, roaming bot at the swarm's pace, full swarm, 180 s at
    `--fixed-fps 60 --timescale=2`, two terrain seeds each (20261014, 4242), `--no-skirt` for the
    before (it turns all three off in the same build):

        "$GODOT_BIN" --headless --path . --fixed-fps 60 scenes/_MazeProbe.tscn -- --stage=0 --seed=4242 --seconds=180 --timescale=2 --bot=roam --bot-speed=0.6 --stress-swarm --stuck-log [--no-skirt] --no-save-writes

    | stuck, % of body-windows (seed 20261014 / 4242) | before | after | held by ground or a prop, before / after | trapped 6 s+, before / after |
    |---|---|---|---|---|
    | Forest (open) | 0.56 / 0.86 | **0.07 / 0.10** | 0.46 / 0.75 to 0.00 / 0.02 | 1 + 2 to **0** |
    | Cursed Dungeon (maze) | 1.08 / 1.04 | 0.85 / 1.05 | 0.08 / 0.55 to 0.19 / 0.24 | 1 + 4 to **0** |
    | Sunken Cave (caverns) | 1.59 / 3.51 | 1.30 / 1.45 | 0.60 / 0.92 to 0.36 / 0.34 | 5 + 6 to **3** |
    | Mystic Ruins (broken walls) | 1.33 / 1.39 | 0.85 / 0.62 | 0.29 / 0.55 to 0.08 / 0.15 | 1 + 3 to **0** |
    | The Court (authored) | 0.80 / 1.10 | 0.75 / 1.21 | 0.11 / 0.25 to 0.13 / 0.17 | 0 + 1 to **0** |

    24 trapped bodies in ten runs before, 3 after. What is left on the walled chapters is mostly the
    **crowd**: a body with nothing in its last slide and two others within 60 px, queued in a passage
    behind the bodies already round the player - the soft crowd's push holding it, not the ground (it
    was 0.2-2.1% before and 0.3-0.7% after, and moves with the run). The Dungeon's and the Court's
    remaining "ground" holds are the same queue with a wall at its side. The chase did not change
    character: on the Forest a plain chaser still covers 0.97-0.98 of its own speed beyond 160 px, and
    the share of the horde following the field rather than the straight chase went from 5-10% to
    4-16% (a cluster now on its line; ghosts, which cross clusters, are not in it). The final build
    re-run on the Forest: 0.06 / 0.12% stuck, none trapped. Run to run the roaming bot meets different
    ground, so read the trapped column and the two seeds together, not one cell.

    **The Hollow Choir** steers by its own `AdjustSteering`: each voice walks straight at its
    standing place and ignores the chase direction, so the flow and the skirt never reach it - only the
    slide does. `_BossProbe` now reports **held** for its rework to measure against: a boss body that
    asked to walk, covered under 0.15 of it in two seconds and is touching something, split by what.
    `--no-skirt` against this build (the third after is the final build):

    | held body-windows | before | after |
    |---|---|---|
    | `--stage=2` | 22 of 39, 21 of 38 (55%) | 17 of 41, 17 of 34, 6 of 33 (37%) |
    | `--stage=2 --focus` | 81 of 149, 54 of 140 (47%) | 46 of 132, 39 of 112, 59 of 147 (37%) |

    Nearly all of it is a voice pressed against the cave's rock (ground 12-69 of each count, another
    body most of the rest). The slide takes about a third off; the rest is the voice aiming through
    rock at a point beyond it, which only its own steering can fix - route to the place along the field
    (`MazeNavigation.FlowDirectionAt` while `HasClearWalk` to the place is false, as the chase does) or
    choose places the field can reach. Both probes still pass: the plain fight falls cleanly with both
    phases, and `--focus` runs out of clock with the revive refusing to end it, as it should. A body
    standing inside rock or water: 0-190 of about 1,300-5,300 body-frames a run before, 0-16 after -
    the echo placement's noise, not this. Every other chapter's
    boss (0, 1, 3-10) is held in 0 or 1 of its walking windows, and falls (the Hayward runs out of
    clock, as before).

    **Done (2026-11-01, "The Hollow Choir, played" below):** a voice now routes to where it is going
    along the field (`WalkToward`: the flow while `HasClearWalk` to the goal is false, the chase's
    heading while the skirt has it turning), keeps a place off the rock within its own third of the
    circle (`OpenGroundNear`), and dips under when it is held two windows running. Held: `--stage=2`
    2 of 40 and 6 of 40 (10%), `--stage=2 --focus` 13 of 85 and 0 of 90 (7%) - from 37%. Nothing in
    rock or water in any run.

    **What it costs:** see `.ai/performance.md` section 7.

    **Still needs eyes:** whether a body turning along a trunk reads as walking round it on the
    phone; whether the Forest's routed bodies bend their approach visibly round clusters; and the
    Cave's passages, where the queue behind the front rank is the next thing a player will call stuck.

### Telegraphs are icons (0.9.3)

Eric, playing 0.9.2: *"the attacks on enemies that are telegraphed through transparent buildups
animations are weird. like the jump attacks. remove them. if you need to telegraph an attack, do so
with icon animation."* Every wind-up had been a translucent shape growing on the floor. Now a tell
has two parts, and only the first is always there (`.ai/art-direction.md` section 3 has the art
rules):

- **The icon - the clock.** `Enemy` makes an icon node (`DrawLayers.AttackIcon`, over every body and
  bolt) once in `_Ready`, only for a class that overrides `ReadAttackTell`, and ticks a
  `TelegraphIcon` from its physics step: it pops in on the wind-up's first frame, fills from the
  bottom as `progress` runs to 1, blinks in the last third (`TelegraphIcon.PulseLit`), and flashes on
  the falling edge - the strike. It reads the clocks and never touches them, so every wind-up time,
  `AttackTelegraph`'s clock and `ResetForRespawn` are exactly as they were (a recycled body's icon is
  cleared in `Enemy.ResetForRespawn`), and it never shows on a corpse.
- **The outline - the place, when the place matters.** `GroundSlamAttack.Draw` is an opaque dashed
  outline at full reach from the first frame (`TelegraphOutline`), blinking white with the icon, and a
  solid white outline dissolving on the strike. `Contains` is untouched and still the only authority on
  the hit.

| Attacker | Icon | On the ground |
|---|---|---|
| Lunger | claws | nothing - the translucent streak and brace ring are gone; the dash is aimed at the player when the wind-up ends, so the claw is the whole read |
| Slammer, Rime Guard | slam | dashed ring / dashed wedge |
| Exploder | starburst | dashed ring |
| Summoner | skull | nothing - the ring and spawn motes are gone; a summon hurts nobody |
| Caster (`RangedEnemy`) | reticle | an opaque aim tick per bolt in the second half (the translucent mote is gone) |
| Boo | starburst (wail) | dashed ring; its phase mark is a placed hit |
| Lantern-tender | reticle (strikes), slam (oil), `!` (the dark) | the oil's dashed wedge; the dark's safe edge as a dashed ring; each strike's ring under the falling flame |
| Bosses | claws for a lane (Gaoler, Deep Warden's charge), `!` for a sweeping beam (Archivist, Deep Warden's sweep), reticle for the Kid Wizard's fan, slam otherwise | the dashed ring, wedge or lane |
| `TelegraphedGroundHit` | the slam glyph in the middle of the spot | dashed ring (ice, embers, stone-falls, a burrower surfacing) |

`_UiShot --tells` stands a lunger, a slammer, an exploder, a summoner and a caster round the player
mid wind-up (`--fx-lead=34`; add `--burst=9,7 --fx-lead=4` to see an icon pop, fill, blink and flash),
and `--tells-boss=res://scenes/GaolerBoss.tscn` does one boss; both canvases
(`--resolution 540x1212 -- --portrait --safe-area` for the phone).

**Still needs eyes in motion:** whether a fast wind-up's blink (the lunger's 0.7 s: about a quarter of a
second of blinking) reads as urgency or as flicker; whether a packed swarm with three or four icons up
at once is still readable; whether the Archivist's always-on beam, whose `!` refills every 1.45 s,
becomes noise; and the Ninth Stone's close, which lays nine icons at once.

---

## 9. Sequencing

**Phase 0 first**, and 0.1 before anything else: playing the `attack` animation improves five
existing enemies for one small change, and it is the difference between "the archer kneels" being a
behaviour task and a one-line wiring task.

Then **Phase 1** — four behaviours, no new primitives, no new art, and it is the phase that
actually answers the brief for the bulk of the cast.

Then **Phase 2**, which is where the new art and the new primitives live.

**What is cheap and what is not.** Phases 0.1 and 1 need no art at all. Blink needs a vanish and an
arrival effect. Split needs a smaller blob — possibly just the same sheet at reduced scale, which
`art-direction.md` would rather it were not, so check there before assuming.

---

## 10. Taking from the wider genre — patterns, not creatures

Other games in this genre are a **mechanics and density reference**, on the same footing
`art-direction.md` and the pinned art rule put Vampire Survivors: what transfers is the vocabulary
of behaviours, never a creature, a name or a silhouette. Their bosses are theirs. A telegraph that
travels instead of planting belongs to nobody.

That is not a limitation dressed up as principle. The premise in `world-and-tone.md` gives this game
a cast nothing else has: these are not fantasy beasts wandering a dungeon, they are **an occupying
force garrisoning a place where a wizard is held**. Jailers, wardens, the apparatus of an
occupation. An archetype borrowed as a *behaviour* and rebuilt as a jailer is a better enemy than
the same archetype copied whole, because it means something in a game about breaking people out.

### What the genre does that we do not

Five patterns, each of which lands on something already half-built here.

**1. Mass, not knockback resistance.** In that design knockback is a function of how hard the hit
lands against the body's mass, so a heavy swing shifts a big enemy a little and a light one across
the room. We have `Enemy.KnockbackResistance`, a flat 0-to-1 scalar that can say "a boss does not
move" but cannot say "a boss moves *a little* when you hit it properly". Replacing it with mass is a
small change with an unusually wide effect: every enemy gains physical weight, and a heavy-hitting
build starts to *feel* different from a fast one rather than only killing at a different rate.

**2. Attacks that do not require planting.** This is the important one, and it is exactly what the
mounted-attacker idea exposes. **Every telegraphed enemy we have stops to attack** - `AdjustSteering`
returns `Vector2.Zero`, `AttackTelegraph` assumes a stationary wind-up, and `GroundSlamAttack` draws
its ring at a fixed point. An enemy that attacks *while moving* is structurally new: a telegraph
that travels with its owner, and a hit region resolved along a path rather than at a point. Nothing
in the current framework can express it, and it is the single largest gap in section 2's table.

**3. Charges that end in something.** A charge that resolves into a shockwave matters even when it
misses, because the miss still moves you. Our Lunger dashes and is left winded, and the dash
resolves into nothing at all. This costs almost nothing: `GroundSlamAttack` already exists, and
firing it at the end of the dash is two existing primitives combined.

**4. Multi-phase bosses.** `BossEnemy` runs one pattern on a loop. Phase changes at health
thresholds - the pattern shifts, adds arrive, a radial nova goes out - are the standard vocabulary,
and we own every piece needed to build them: `GroundSlamAttack` for the slam, `SummonerEnemy`'s
capped minion logic for the adds, `RangedEnemy`'s volley fan for the nova.

**5. Enemies that leave hazard behind.** We have `StageHazard` and `SpikeTrap` as *level* furniture,
and a cast that never interacts with them. An enemy that lays hazard as it moves bridges two systems
that already exist separately, and it is the one behaviour that makes the player unable to stand
where the fight has already been.

### Where this lands hardest: the bosses we do not have

`BossCatalog` has **one entry**. Elderbark guards the Enchanted Forest, and the other seven chapters
end on a timer with nothing to fight - which `gameplay-design.md` already calls the free
non-victory. A genre where every stage terminates in a distinct, multi-phase Lord is pointing
directly at the largest content hole in this game, and the vocabulary above is what fills it: a
moving attacker, a slam, a nova, capped adds, a phase change.

That is also the honest reason to take the inspiration seriously. It is not that our enemies need to
be more like theirs. It is that seven chapters currently end in nothing.

### Archetypes worth building, in our fiction

| Archetype | Tier | New machinery | Cost |
|---|---|---|---|
| **Rider** — a mounted jailer that circles and strikes without stopping | 1 | **Travelling telegraph** (new) | High, and it unlocks every other moving attacker |
| **Ooze** — slow, splits on death, leaves a corrosive trail | 0 + 2 | Split (Phase 2) + hazard trail | Medium; both halves are planned already |
| **Shield-bearer** — walks in front of the pack and blocks projectiles for what follows | 0 | Projectile blocking | Medium; the first enemy that punishes firing blindly into a crowd |
| **Lantern-bearer** — buffs nearby enemies; killing it weakens the group | 0 | Aura | Low; same design job as the Summoner, a target worth choosing |
| **Burrower** — submerges, untargetable, surfaces near you | 1 | Reuses **Blink** | Low once Blink exists |
| **Charger** — the Lunger, with a shockwave on the dash | 1 | None | Very low; two existing primitives |

**Shield-bearer is the one I would argue for hardest** after the Rider. Every enemy in this game
dies to fire poured in its general direction; nothing yet asks the player to care *which* body is in
front. It is also the cleanest possible partner for a horde that now arrives in formations - a wall
with a shield at the front is a genuinely different problem from a wall without one.

### The seven Lords, read closely

Researched rather than remembered, because the interesting part of a boss is never its stat block.
Mechanics reference only - none of this is a licence to copy art, names or fiction.

| Lord | Stage | What it actually does |
|---|---|---|
| **Wyrm Queen** | Ember Grounds | Stationary at the centre of the arena. Cycles between summoning explosive orbs and a **rotating breath weapon** that sweeps the ring. **Burrows, becomes untargetable, repositions, resurfaces to ambush.** Fights alongside a normal swarm, on a clock. |
| **Lord of Pain** | Haunted Caverns | Two stages: mounted, then **dismounts on foot** when the first health bar empties. Charges are telegraphed by circles that appear in front of him before he moves. |
| **Wraith Horseman** | Forgotten Viaduct | High mobility. Lance charges, **arrow rain marked by circles on the ground**, a spear attack marked by a **spear-shaped** ground decal, and summoned guards that **only attack in the direction they face**. |
| **Frost Knight** | Frozen Depths | Area denial. Fights *with* the arena: falling icicles and freezing winds that slow, plus elite guards. |
| **Lord of Regret** | Chambers of Dissonance | Spectral arrow rain over three marked zones, plus bombs. |

Their elites are worth a line too, because several are things we have already planned: a **slime that
splits when damaged and regenerates**, **twin gargoyles that must be killed close together**, a
**hydra whose difficulty changes as heads come off**, a **lich that summons archers**, and a
**flamelord that leaves a burning trail**.

### The Wyrm Queen is the one to learn from

Three separate ideas, and the third is the one worth stealing.

**1. A stationary boss with a rotating sweep.** It cannot chase, so the threat is entirely
positional: the beam turns at a fixed rate and the player orbits to stay off it. That is a boss
fight made of movement rather than of reaction time, which suits a game where the player never aims.

**2. Orbs alternating with the sweep.** The two attacks want opposite things - the sweep wants you
circling, the orbs want you not standing still - so cycling them stops the fight settling into one
groove.

**3. It burrows, and is untargetable while under.** This is the important one.

A stationary boss in a bullet heaven has one failure mode: a built player parks at max range and
melts it, and the only lever the designer has left is more health, which makes the fight longer
rather than harder. The burrow solves that without inflating anything. It **paces the fight with
time the player cannot spend damaging**, forces them to break position and re-acquire, and converts
a DPS check into an anticipation problem. It also gives the boss a reason to reposition that reads
as intent rather than as pathfinding.

**We already have the primitive.** The Burrower archetype in the table above is this, and it reuses
Blink. What we do not have is the *invulnerable* half: nothing in `Enemy` can currently be
untargetable while remaining alive, because `StartDeath` is the only thing that leaves the
`"enemies"` group. A boss that submerges needs to leave the group and come back, and every targeting
path reads that group - so the mechanic is nearly free, and the one thing to be careful of is that
`Player.OnEnemyDiedAt` and the AoE sweeps must not treat "gone from the group" as "dead".

### The ten chapter bosses — all built

**The `#` column below is the order they were built in, not the campaign order since
`.ai/campaign.md`** (2026-10-25): the Cursed Dungeon is now chapter 5, after two new early chapters
whose bosses, the Hayward and the Ninth Stone, were built last (rows 9 and 10). That doc's table is
the order. (The Sketchbook's Wizard Dude is the kids' and is not in this table.)

Every chapter has a boss and every chapter is enterable. Each one is a different *kind* of
problem rather than a different element, and each is built from the shared primitives below
rather than from its own machinery.

| # | Chapter | Boss | The question it asks | Script |
|---|---|---|---|---|
| 1 | Enchanted Forest | **Elderbark, the Treant** | Can you sustain damage rather than spike it? | `BossEnemy` (bare) |
| 2 | Cursed Dungeon | **The Gaoler** | Can you dodge sideways instead of backwards — twice, at two ranges? | `GaolerBoss` |
| 3 | Sunken Cave | **The Hollow Choir** | Can you spread damage, when your spells choose their own targets? | `HollowChoirBoss` |
| 4 | Blighted Swamp | **Mother Rot** | Do you hit the boss or the thing healing it? | `MotherRotBoss` |
| 5 | Mystic Ruins | **The Archivist** | Can you keep orbiting, and re-acquire after it moves? | `ArchivistBoss` |
| 6 | Frozen Waste | **The Still Warden** | Can you hold ground, after six chapters of being taught to kite? | `StillWardenBoss` |
| 7 | Scorched Sands | **The Long Coil** | How much damage can you land in four seconds? | `LongCoilBoss` |
| 8 | The Emberdeep | **The Warden of the Deep** | All four of the above, in sequence, with no break. | `DeepWardenBoss` |
| 9 | The Fallow Fields | **The Hayward** | Can you go round it? Its hurdle cuts what lands from in front to x0.2 (`Enemy.AdjustIncomingDamage`). | `HaywardBoss` |
| 10 | The Broken Circle | **The Ninth Stone** | Can you find the way out of a closing ring, read from the ground in under a second? | `NinthStoneBoss` |
| 11 | The Last Bridge (castle, not yet playable) | **The Constable** | Can you advance under fire? He holds the gate from the wall, untargetable, and comes down into the yard only to a wizard who has reached the gate's foot (`.ai/endgame-castle.md` 3.6). | `BridgeConstableBoss` |

Two of them deliberately take opposite halves of the same idea. The Archivist is the **stationary
sweep** with burrowing as punctuation; the Long Coil is the **burrow** as the whole fight. Reusing
one idea twice is fine as long as each time it is the spine of a different encounter.

**The Hollow Choir is the only fight the loadout cannot play for the player.** Every spell in this
game picks its own target, so a fight that requires choosing one is a fight the build cannot
answer on its own. That is the whole reason it exists, and it is why three linked bodies were
worth the primitive.

**The Warden of the Deep is a recap and not a new idea**, on purpose. A final boss that introduces
a mechanic gets read for the first time at the moment the player has the most to lose, which is
where a campaign usually chooses between unfair and trivial. A recap asks whether the first
chapter's lesson survived six chapters of not needing it.

### The primitives they are built from

All five of the things the sketches were blocked on now exist, and all five are shared rather than
per-boss.

| Primitive | Where it lives | What it is |
|---|---|---|
| **Untargetable-but-alive** | `Enemy.SetTargetable(bool)` | Leaves and rejoins the `"enemies"` group and zeroes collision — the same teardown `StartDeath` does, minus the dying. Every targeting path and every AoE sweep in this game reads that group, so one method makes a boss invisible to all of them with no change to any. |
| **A travelling telegraph** | `BossEnemy.PlantsDuringWindUp` | `GroundSlamAttack` always drew on the caster and resolved at the caster's position, so a boss that simply does not plant carries its warning with it. The primitive turned out to be one bool. |
| **Telegraph shapes** | `TelegraphShape` + `GroundSlamAttack.Contains` | Ring, Cone and Line. One method decides all three, and the damage and the dashed outline (`TelegraphOutline`) are drawn from the same numbers, so a shape cannot be drawn one size and hit at another. |
| **Phase changes** | `BossEnemy.BuildPhaseThresholds` / `OnPhaseEntered` | Health fractions, crossed in order and only ever forward. The default is the single enrage threshold, so a boss that overrides nothing behaves exactly as Elderbark always has. |
| **Linked health** | `HollowChoirBoss` | Deliberately *not* a framework: no manager node, no shared pool. Each voice holds its siblings and the check is three pointer comparisons on the frame one falls. |

Two more came out of building them:

- **`BurrowMove`** — one dive: go under, travel, come back up. Three bosses burrow, which is two
  more than it takes for the travel maths to be written three times and disagree three ways. It
  does *not* make its owner untargetable; that stays with `SetTargetable`, so there is only ever
  one authority on who is in the group.
- **`TelegraphedGroundHit`** — a warning placed on the ground away from whoever caused it. A bare
  `Node2D` with no scene, because the whole appearance is the warning shape and a `.tscn` would
  only be a place for the numbers to disagree with the call site. Deliberately not an `Area2D`:
  the hit is measured, once, against the same shape the player was shown.

**Why a burrow is worth having at all.** A stationary or slow boss in a bullet heaven has one
failure mode — a built player parks at maximum range and melts it, and the only lever left is more
health, which makes the fight longer rather than harder. Going untargetable paces the fight with
time the player *cannot spend damaging*, forces them to break position and re-acquire, and turns a
damage check into an anticipation problem.

### How a boss is verified

Three layers, because none of them covers the others:

1. **`dotnet build`** sees the scripts and nothing else — not the scene, not the exported tuning,
   not the telegraph.
2. **`RegressionChecks.ValidateBossPatterns`** builds every boss scene and checks the tuning
   invariants: no zero wind-up, no zero-damage attack, a charge tell strictly between 0 and 1, a
   cone half-angle under 90 degrees, a burrow longer than the tell it places. Every one of those
   failures looks like nothing while it is happening.
3. **`scenes/_BossProbe.tscn`** drives the real fight headless. It shortens the survival clock and
   then *bleeds* the boss on a fixed timer rather than waiting for a loadout to kill it, which
   walks every boss through every phase threshold in a known time.

```
"$GODOT_BIN" --headless --path . scenes/_BossProbe.tscn -- --stage=4
"$GODOT_BIN" --headless --path . scenes/_BossProbe.tscn -- --stage=2 --focus
```

`--focus` drains only the first body. It exists for the Hollow Choir: draining all three at one
rate kills them on the same frame, so the revive never fires and the probe reports a fight it did
not actually test. With it, a voice goes down, comes back, and the fight correctly refuses to end.

The probe answers four questions and no others — did the boss arrive, did it reach its phases, did
the burrowers submerge and return, did it die cleanly. "Cleanly" includes the fall below: the probe
waits for the body to be freed and prints how long the fall played, and calls it an error if a
fallen body is still in the tree eight seconds later. **Whether a fight is fun, fair or readable
still needs eyes**; the probe renders nothing.

### How a boss falls

"Bosses should have cool death animations that relate to what they are" (2026-10-28). Every boss
used to end on the same six-frame `death` strip, the body squashed into the floor and darkened. Now
each falls as what it is, for 1.5 to 3 seconds, about the length of `boss_death` (2.4 s), which
plays under it.

**Presentation only, after the fact.** `Enemy.StartDeath` still drops the rewards, leaves the group
and zeroes collision on the killing frame, and `BossDefeated` still fires on that frame, so the
victory flow (the chest and the portal, `Node2DGame`) and a Boss Rush's next arrival hear it exactly
when they did. A body that `StagesOwnDeath` then takes the corpse instead of playing the strip, and
frees it when its fall ends. Nothing in a fall can touch the player. The `death` strips stay on the
sheets as the contract's fallback.

**The stage and the play.** `scripts/BossDeath.cs` is the stage, shared: it cuts the frame the boss
fell on into **pieces** of its own sheet (moved, turned, squashed, sunk into the ground, dissolved
from the top), throws **debris** from one generated atlas (`tools/art/boss_deaths.py`), raises
**puffs**, and gives three layers to draw in: the ground at absolute z -15 (stains, frost, the
crater - under every actor), the air, and additive light. The play is each boss's own, written in
its class through `OnDeathBegin` / `TickDeath` / `DrawDeath*`. Bare `BossEnemy` is Elderbark, so its
fall is the default.

| Boss | Its fall |
|---|---|
| Elderbark | The mark runs down the trunk as a seam of cold light; the trunk splits and the halves topple apart, each on its own foot; dead leaves shed from the crown, the bones it had grown over drop out of the ribcage, bark and dust where the halves land |
| The Gaoler | The ring of keys at its hand flares and bursts; the keys to every cell skitter across the floor and lie winking; chains come off the mount; its legs fold and the rider is pitched forward over its neck |
| The Hollow Choir | All three die on one frame; they go QUIET in turn (`fallOrder`, 0.45 s apart), the last one struck first. Each sings a last note - the hood flares, arcs of song - then the empty robe folds onto the floor, leaving wet stone |
| Mother Rot | The sacs on her back burst one after another, smallest first; she slumps flat; a dark stain bubbles out under her, wider than she was, and soaks away |
| The Archivist | Every line of writing flares; the tablets drop and break; the slab unbinds from the top down along a seam of light, nothing above it but pages lifting, turning over and settling |
| The Still Warden | White cracks race out of the eye through the body (`BossDeath.CrackPath` stops at the silhouette); it shatters into ice and plate; the rime it laid round itself draws back in and is gone |
| The Long Coil | It thrashes; the body goes under a segment at a time from the tail, each into a spurt of sand; the head sinks straight down into a sand funnel that closes over it |
| The Warden of the Deep | Built, not taken, so it fails like a furnace: the heart flares, fire runs along every seam, it bursts, the pauldrons are thrown off, and it slumps into a molten crater (`CraterGround`) as the last of his fire goes out. The longest fall: the finale |
| The Hayward | The hurdle topples away from him flat onto the stubble, bursting straw; his hat spins off behind him, the crook drops, and he folds to his knees |
| The Ninth Stone | The mark burns down its split and then leaves, a thread of light streaming up and away; what is left is granite, and it falls whole, away from the wizard, with a thud of dust, chips and heather |
| The Constable | The gate given up: the mark gutters in the slit, the halberd goes over off his open hand and rings on the setts, he goes down on his knees, the ring of the gate's keys drops from his belt into a heap (not thrown - the Gaoler's burst and skitter), his head bows and the plate goes dark |
| The Seneschal | **His staff is broken**, as a steward broke his white staff over his lord's grave: it snaps where his hand holds it in a burst of white splinters, the top half spins away and the foot tips across his front; his cap goes off behind him; he sinks to his knees and the gown pools; the chain of office slides off and its gilt links skitter; the mark goes out |
| Wizard Dude | **The kids' drawing is never cut, turned or redrawn.** His one frame stays untouched and only FADES, inside a puff of his own spell's colour with sparks rising - an effect of ours round their pixels, nothing in their style |

**Rules a new fall keeps.** Draw on the 2 px art grid (`BossDeath.Disc`, `Line`, `Arc`), never an
anti-aliased curve. Debris turns only in quarter turns. **Never a ring round the body** - a ring
round a boss is a telegraph in this game, so a stain, a frost patch or a sinkhole is filled and
uneven, never outlined. Allocate when the fall begins, never per frame. A crack follows the body
(`CrackPath` reads the frame's own pixels). `RegressionChecks.ValidateBossPatterns` holds each fall
to 1.2 - 3.5 s and checks the debris atlas covers every cell `BossDeath.Cell` names.

**Photographing one:** `_UiShot --stage=N --rush-steps=2 --boss-death --frames=55 --burst=14,12`
stands the boss in front of the camera (`--boss-death=X,Y` moves it), holds the wizard still, and
fells every body of it two frames before the shutter. Under `xvfb-run` with `--fixed-fps 60`.

### Boss entrances and longer fights (2026-10-31)

> "Whenever a boss spawns, all of the players spells should stop firing, and you should see an
> animation of the boss spawning, such as bursting up from the ground, growing from a seed, whatever
> fits its theme. The boss should also have much more health so the fight lasts longer, some even
> having phases, etc. To make the boss fight last a few minutes more on average and feel a lot more
> tense." (Eric; and "overall keep text minimal" - no new words on screen.)

**The arrival is the fall run forwards.** `BossDeath` plays it in its entrance mode
(`ConfigureEntrance`), dispatching to `BossEnemy.OnEntranceBegin` / `TickEntrance` / `DrawEntrance*`,
from the boss's own sheet, with the same debris atlas, puffs and three layers; the body rides the
boss's `DrawLayers.Foremost` and the air and the light sit on `ForemostAir`, over it.
`BossEnemy._Ready` starts it on every spawn path (chapter, Boss Rush, tools).
For `EntranceSeconds` (2.5 - 4 s, `ValidateBossPatterns`) the boss is untargetable and its physics is
off - no step, wind-up, phase or summon - and its tells are hidden; then `FinishEntrance` hands the
body back and the fight starts. `boss_roar` plays once, at the climax (`EntranceRoarSeconds`). The
boss bar fills with the arrival.

**The field holds while any body arrives** (`BossEnemy.EnteringCount`, `Node2DGame.BossEntrance.cs`):
the player's spells are silent with their timers **held, not reset** (`Player.HeldForBossEntrance`,
read inside the one `spellFireTimers` loop); every ordinary body is **held still and takes no damage**
(`Enemy.SetHeld`: process off); and the player cannot be hurt **or walk** (`MovePlayer` drops keys, pad
and touch stick alike while `HeldForBossEntrance` is set; pause and menus are untouched). Chosen over
pushing the swarm back: holding is the only answer that gives nobody a free hit either way, and the
fight then starts exactly where the field stood.

**It is shot as a cinematic** (2026-11-01: "Show black bars like movie trailers, use zoom, screenshake,
and dramatic cinematic flare"). Letterbox bars (11% of the canvas height each, 9% on the tall phone
canvas, plus the phone's safe inset; canvas layer 4, over the HUD and under the touch pad and every menu)
slide in over the HUD's edges and out when the hold lets go - the boss bar fills under them. The camera
leans toward the middle of every arriving body (`BossEnemy.Arriving`: a procession's pair is one shot)
through its `Position` and pushes in through its `Zoom`, at most x1.28 and never past what keeps each body
(150/160 px round it, for its first tell) and the wizard inside the picture between the bars - pulling
out instead, to x0.88 at most, when the bodies are spread wider than that (the Choir, a pair). Spawn rings
read `Node2DGame.GameplayZoomOf`, so the push-in never pulls the swarm on screen. The screen shakes
(`ScreenShake`, the camera's `Offset`) on the beats each boss marks in its own timeline - the roar is the
climax (`EntranceClimaxWeight`, 0.3 the kids' wizard to 1.3 the Warden of the Deep), and landings and
bursts call `BossDeath.Impact(weight)`, in falls too. On the climax the bars jolt in, the push-in kicks,
and their inner edges catch the boss's light (`EntranceFlareElement`): a line stepping down the element's
ramp, core to edge, and a Bayer-dithered spill into the picture, gone in 0.34 s. There is no screen-shake or reduced-motion option yet; `ScreenShake` is where one would be read.

| Boss | Its arrival |
|---|---|
| Elderbark | A seed of the mark's cold light falls into the ground; roots crack out; the tree grows up out of the soil, overshoots and settles; the eyes open |
| The Gaoler | A grate in the floor; chains pay out into the pit; the bars swing open; it rides up out of the oubliette rearing and lands among its keys |
| The Hollow Choir | Each voice where it will stand, a beat apart: a pool of dark, motes winding in, the robe forming as the dark thins, the hood flaring into song. Only the first roars |
| Mother Rot | A bog stain spreads and bubbles; she heaves up out of it, wet, and settles with a slap of muck |
| The Archivist | Pages blow in and fold down; the slab binds upward along a seam of light; every line flares |
| The Still Warden | Frost spreads; a pillar of ice grows with a dark shape in it; it cracks and bursts |
| The Long Coil | A ridge races in under the sand; the ground funnels; the head bursts up and comes down; the body surfaces behind it a segment at a time |
| The Warden of the Deep | The floor melts into a pool; the armour rises out of it cold and black; the heart catches and fire runs along every seam. The longest, as its fall is |
| The Hayward | A dust devil of straw spins up; he takes shape in it and drives his hurdle down with a thud |
| The Ninth Stone | A thread of his light comes down into the heath; the ground cracks; the stone heaves up and the mark runs down its split |
| The Constable | On the wall-walk over the gate, where he fights from: his cold fire runs along the battlements merlon by merlon from the towers in; the halberd's head comes over the parapet, then the sallet with the mark lit, then the whole of him, climbing the stair in heaves; he grounds the halberd with a jolt that shakes grit and dust off the merlons |
| The Seneschal | A pair of panelled oak doors stands where there was none and swings open on the dark; he steps out of it, the light finding him as he comes; the doors shut behind him with a thud of dust; he lifts his staff of office and brings it down on the floor, and the mark opens. It is how he leaves, three times, and how he is found |
| Wizard Dude | **The kids' frame is never cut**: sparks of his colour wind in, a puff, and his own sprite pops up from small with one bounce (scale, height, alpha only) |

**Every boss has at least two phases** (`ValidateBossPatterns` holds two or more thresholds, each
inside 0-1 and below the last). Thresholds are read off `BossHealthFraction`, so the Choir's three
voices change together.

| Boss | Phases (threshold: what changes) |
|---|---|
| Elderbark | 0.66 roots: a line of marked roots races from the tree through the player, each a beat after the last - sidestep it. 0.34 the enrage, roots quicker |
| The Gaoler | unchanged: 0.5 dismount (the cone at half the distance), 0.34 enrage |
| The Hollow Choir | Now each voice **holds its own third of the circle** round the player at 230 px and steps in to sing in turn - they no longer converge and die to one blast. 0.7 (one voice down is enough) the round: the places turn, and each voice in turn sings a note at where the player stands - since 2026-11-01 a launched one (`ChoirNote`, "The Hollow Choir, played" below), not a mark on the floor. 0.33 the last verse: quicker notes, a faster turn, revived voices stronger and sung back in three quarters of the time |
| Mother Rot | unchanged thresholds; her regeneration is now **0.35% of her health a second** while a spawn lives (it was a flat 26, nothing at 80,000) |
| The Archivist | 0.66 a second beam on its back, half a turn round. 0.34 enrage: both turn faster, it dives more often |
| The Still Warden | 0.66 the cold closes in: heavier volleys, one more guard, sooner. 0.34 whiteout: enrage, and a marked ring of ice falls round it outside its claim every 6.5 s - be at its feet |
| The Long Coil | 0.66 it surfaces throwing a lane of sand spurts toward the player. 0.34 enrage (longer window, quicker dive) |
| The Warden of the Deep | unchanged: charge, sweep, burrow, cataclysm |
| The Hayward | 0.5 the swing (unchanged). 0.28 temper: turns 92 deg/s, swings every other attack, quicker |
| The Ninth Stone | 0.6 the double close (was 0.5). 0.28 the circle tightens: closes every 6.2 s and stray marked stones fall on the player between them |
| The Constable | 0.66 and 0.33: each sends him back up the wall at once (he finishes the swing he is in) and thickens the volleys - files of bolts 2, 3, 4 a volley, rings of pitch 1, 2, 3, the interval x1, x0.85, x0.72, and in the last both at every volley - so the gate is taken three times. Every return to the wall pours pitch over the gate's whole foot first |
| The Seneschal | Opens with the staff (a ring) and **the rank** (5 household guards called down a marked lane at the player, a wall that walks at 380 px/s: step out of it sideways), and in the Great Hall the chandeliers. **0.75, 0.5 and 0.25 are flights**: no hit carries him past one; he steps through a servants' door, untargetable and taking nothing, travels through the walls (one `BurrowMove`) to a room one or two away that he has not held, and the room he left fills with his household at its doors (8, 11, 14). He waits there, a shade, until the player reaches the room. Found after 1: ranks every 6.4 s. After 2: every 5.4 s, seven abreast. After 3: the enrage |
| Wizard Dude | 0.66 every fan is cast twice down the same wedge. 0.34 enrage, wider fan |

**Health, second** (`BossCatalog`; the value before is on each line): Elderbark 9,500, the Hayward
10,000, the Ninth Stone 46,000, the Choir 3 x 26,000, the Gaoler 150,000, Mother Rot 170,000, the
Archivist 140,000, the Still Warden 400,000, the Long Coil 300,000, the Warden of the Deep 500,000,
Wizard Dude 9,000; and the castle's two, the Constable 400,000 (11,800, the castle doc's first guess)
and the Seneschal 440,000 (13,000, its guess), each measured below under its own protocol.

**The Boss Rush scales every boss to a third** (`BossRushCatalog.FightHealthShare` = 0.35, the finale
included, on top of the arrival curve). Ten fights of three to five minutes would be forty minutes of
bosses in a mode built as a run a little longer than a chapter; each rush fight keeps its whole shape
(thresholds are fractions) at about the length a chapter's fight had before.

**Measured** (`_BalanceHarness --survivor --immortal --policy=ordinary --stop-overrun=60
--minutes=21`, `--fixed-fps 30 --timescale=1`, Stormcaller, seed 1234; fight = boss down minus
15:00, the arrival included). "Was (doc)" is `.ai/difficulty-curve.md` section 10, on older code;
"base" is the same protocol on this branch's base commit (09aad67), the same code but for this work.
**One run a cell, and the cells swing hard with the build the bot happens to reach the boss with**
(L15, level at 15:00): the same chapter and seed reached the Choir at L32 on one run and L40 on
another, and a fight's length moves four- to eightfold across that range. Read the table as a
direction, and the health as set between the strong and the weak runs.

| # | chapter | save | base: L15, fight | after: L15, fight (health) | after the merge of main: L15, fight (health) | was (doc) | health now |
|---|---|---|---|---|---|---|---|
| 1 | Forest | fresh | 29, 0.3 | 29, **2.9** (9,500) | | 1.2 | 9,500 |
| 2 | Fields | fresh | | 18, over 6: half left at the cap (12,000; a sieged, weak build) | | 1.8 | 10,000 |
| 3 | Circle | farmed | | 34, **2.4** (38,000) | | 1.6 | 46,000 |
| 4 | Cave (Choir) | farmed | 40, 0.7 | 32, **6.0** (3 x 26,000) | 39, **1.0** (3 x 18,000) | 0.1 | 3 x 26,000 |
| 5 | Dungeon (Gaoler) | farmed | | 37, **3.7** (150,000) | | 1.3 | 150,000 |
| 6 | Swamp (Mother Rot) | farmed | | 39, **3.0** (170,000) | | 0.6 | 170,000 |
| 7 | Ruins (Archivist) | farmed | 48, 1.1 | 28, **5.2** (160,000) | | 2.1 | 140,000 |
| 8 | Frozen (Still Warden) | farmed | 41, 0.4 | 37, **1.9** (190,000) | 45, **0.7** (300,000) | 2.4 | 400,000 |
| 9 | Sands (Long Coil) | farmed | | 36, **4.2** (300,000) | | 0.9 | 300,000 |
| 10 | Emberdeep (Warden of the Deep) | farmed | | 43, **3.1** (420,000) | 33, over 6: 56% left at the cap (600,000) | 1.1-2.2 | 500,000 |

**The Boss Rush** (`--boss-rush --farmed --stage=7`, after the merge): all ten fell, each fight 9 s
(Elderbark, at the curve's floor) to 1.7 min (the Still Warden), 9.5 minutes of fights in a 24.5
minute run.

**The Constable, measured** (2026-11-01, `wip/boss-constable`). No harness bot can walk to a point and
hold it: `--walk=315` reaches the castle wall and paces it corner to corner, crossing the gate's foot
in under the 1.5 s hold, and `--survivor` stays down the yard. So the yard fight was measured with one
experimental, uncommitted change - the hold's radius widened to 1,200 px in the scene, so a bot
anywhere at the gate end brings him down - and the wall's cost from `_BossProbe --approach --drain=150
--give-up=900` (a wizard who holds the gate: each 12 s visit costs about 4.5 s of pour, hold and
leaps, so he is in reach about two thirds of the fight; a player crossing the yard after each pour
spends longer). Farmed Stormcaller, `--fixed-fps 30 --timescale=1`, `--policy=ordinary --immortal`,
stage 11; L15 is the level at 15:00, DPS is damage a second while he is in reach.

| bot, seed | L15 | health | what happened | DPS in reach |
|---|---|---|---|---|
| `--survivor`, 1234 | 46 | 400,000 | felled 41 s after his arrival, three visits | ~20,000 |
| `--survivor`, 77 | 36 | 1,500,000 | 30% off in 5.9 min (cap) | ~1,900 |
| `--survivor`, 1234 (again) | 22 | 1,500,000 | never came within 1,200 px of the gate | - |
| `--walk=315`, 77 | 34 | 400,000 | 62% off in 5.3 min (cap), fighting from a corner of the wall | ~1,100 |
| `--walk=315`, 1234 | 25 | 400,000 | 20% off in 5.7 min (cap) | ~330 |
| Warden of the Deep, `--survivor`, 1234, Emberdeep | 34 | 500,000 | felled in 5.9 min | (1,400 over the fight) |

The same seed reached L46 and L22 on two bridge runs (`BridgeRules` spawns from an unseeded RNG), so
read the rows as a spread. **400,000** puts the median bridge build (L36, ~1,900 a second) at about
five minutes - the Warden of the Deep's length for the same class of build today - with a strong one
done in under a minute and a weak one not done at all. The bridge's survivor reached L46 and L36 where
the Emberdeep's reached 34: the travel chapter pays more XP than the road's last, which is the bridge's
to look at (C1), not the boss's.

`_BossProbe` on every chapter (0-10, the Choir with and without `--focus`): every boss arrives, holds
the field for its arrival (spells held, the player unhurt, both released the frame after), reaches
every phase and falls cleanly. The Hayward's probe runs out of clock before the drain kills him -
his hurdle cuts the probe's own drain to x0.2 from in front, as it did before.

**Status (2026-11-01), for whoever picks this up:**

- Done: the arrivals (all eleven, photographed at 1280x720 and 540x1212 portrait, before and after
  the merge of `main-ff86ni`), the field hold, the phases, the health above, the Boss Rush share
  (measured once), a same-code baseline for four chapters, the validators (0 errors, 6 warnings, as
  at base).
- Unfinished: "health now" for the Fields, the Circle, the Ruins, the Frozen Waste and the Emberdeep
  is set from the runs above and **not re-measured at that value**. The spread by build is the real
  open question: the Still Warden (a target that never moves, so every spell lands) and the Choir are
  the most build-sensitive. If a strong build still melts the Still Warden, a phase that spends time
  (an untargetable shell, as the burrowers have) is the lever, not more health.
- Needs a human eye: every arrival in play (the photographs were taken with the boss stood in front
  of the wizard, with no crowd); whether a 2.5-4 s hold reads as a beat or a wait; the Choir's new
  spacing (voices hold places 230 px round the player, step in to sing in turn); Mother Rot's
  percentage regeneration; the Still Warden's whiteout ring.

**The Seneschal (C5, 2026-11-01).** The same protocol on the Court (`--stage=12 --farmed`, Arcane
6,800), three wizards at the seeds above (Stormcaller 1234, Frostweaver 77, Pyromancer 1234), run in
batches of two or three. Fight = boss down minus 15:00, the arrival and the three pursuits included;
pursuit = the three findings summed (the bot walks the maze route to him, as a player who knows the
house). The **hop** (a lost player is come to by a door, 12 s) was added after batch c, where two runs
in three stalled: the boss could not route to the bot and stood at full health for six minutes.

| batch | health | Stormcaller: L15, fight (pursuit) | Frostweaver | Pyromancer |
|---|---|---|---|---|
| a | 520,000 | 40, **5.9** (40 s) | 42, **4.1** (32 s; the chest set's execute takes the last quarter) | |
| b | 460,000 | 39, over 6: 35% left | 41, over 6: 54% left | 42, **2.0** (25 s) |
| c | 400,000 | 45, **1.3** | stalled 5.7 min, then 62% left | stalled: 100% left |
| d | 400,000, the hop | 44, **5.1** (32 s) | 48, **1.6** (20 s) | 43, **2.8** (27 s) |
| e | **440,000**, the hop | 40, over 6: fled the third time at 20:56 | 48, **2.8** (27 s) | 42, **2.7** (23 s) |

Read it as the other chapters' table is read: the build the bot reaches the boss with decides the cell
(L15 39-41 runs out of clock at any of these values; 45-48 is under two minutes at 400,000). 440,000
puts a decent farmed build (L42-48) at about three minutes and the slower Stormcaller builds at five
or past six, which is the Warden of the Deep's spread a step on. **Not anchored to the Constable**:
his number did not exist when this was set; the rule was "after him on the curve", and since the
Seneschal's fight carries half a minute of pursuit and his household per flight, his health may
sit at or under the Constable's and his fight still come out the longer. The bot never steps out of
the rank's lane (it reached him in most calls) - it is immortal here, and a person will do better.

`_BossProbe --stage=12` (it walks to his room, and keeps its wizard whole for this fight): he arrives
and holds the field 2.3-2.9 s, flees at each threshold to a room 740-3,600 px off, is found 10-53 s
later, and falls in 2.4-2.9 s, the body freed. Six runs in a row clean; before them the probe's
wizard twice died to the household it walked through and twice stuck on a route that hugged a jamb,
which keeping it whole, the route's wall clearance and a sidestep fixed.

### The Hollow Choir, played (2026-11-01)

Eric, on 0.10.0: "pathing can get stuck"; "Their ranged attacks don't appear to have an animation
coming from them. Instead of a circle and arrow, show some sort of attack that they launch"; "If one of
the hollow choir gets too far from the player, they should dip into the ground and reappear. The time
between them respawning unless they're killed at the same time should be larger and they should visibly
walk over to their falling comrade and revive each other. So it's obvious to the player that they need
to stop them from reviving each other. CC like freezing them should pause the revival mechanic."

- **The walk** goes round the cave, not into it (section 8 item 13 has the numbers: held 37% to 7-10%).
- **The note is sung and sent.** The round's notes were `TelegraphedGroundHit`s - a dashed ring and the
  arrow glyph at his feet with nothing joining them to a voice. Now the voice plants for `NoteTellSeconds`
  (0.55 s) and plays `sing` (its six mouths light from the hem up, a rising scale, the cowl flares, the
  note swelling at the mouths), and a `ChoirNote` leaves the mouths for where he stood: a crescent of
  three arcs of the song, 26 px across the hit, 230 px/s, so a sidestep clears it. It is an
  `EnemyProjectile`, so every guard against a bolt takes it and cave stone stops it. It leaves the
  mouths, not the cowl: the cowl stands 44 px above the body's floor position, inside the rock against a
  cave wall, and the first notes died there on their first frame.
- **A fallen voice lies where it fell** (`fall`, then `downed`: a heap of wet cloth with the mark still
  lit in it), out of reach, and **is walked to and sung back**: the nearest standing voice not already
  singing (`ChooseReviver`) walks - or dips - to its side, stands `ReviveReach` (96 px) off and plays
  `chant`, every mouth lit, and arcs of song cross to the heap while the clock round the heap fills. The
  song runs only while the singer is in reach and not held (`Enemy.IsHeldByCrowdControl`): a freeze,
  root, stagger or stopped time on the singer pauses it, the arcs stop, its `chant` freezes - and under
  the bosses' rule (below) no hold lasts. `ReviveSeconds` is 7 s of song (it was 6 s down, on a clock)
  plus the walk; 0.75 of it in the last verse. Measured (`--focus`): down 7.7-9.9 s, every one raised;
  with the singer frozen on every frame (`--focus --cc=freeze --drain=16`) down 14.2 s, the song paused
  2.0 s, and staggered 16.4 s, paused 2.4 s - and raised all the same.
  With two down the one left raises them in turn, and the first raised goes to the second. A voice
  struck down mid wind-up lands nothing (its ring and icon go with it). Killing the singer is the other
  answer; the song so far is kept for the next one.
- **The dip.** Further than `DipDistance` (560 px) from where it is going - its place, or a fallen
  voice's side - or held two 1.5 s windows running, a voice sinks into a pool of black water (`dip`),
  goes under (`SetTargetable(false)`, one `BurrowMove`, 1.2 s, a dark swell and ripples on the floor
  where it travels and where it will rise) and comes up there (`dip` backwards) - led, when it is going
  to its place, by most of the wizard's own walk over the 1.7 s it is under (`LeadDip`, 0.6 of it, at
  most 300 px; led the whole way it came up ahead of a wizard who then turned, and dipped again): the
  first photographs of a kiting wizard had all three surfacing in a line behind him. 2.2 s out of reach;
  4 s before it may dip again. Not mid wind-up, mid note, or while singing one back.
- **Art** (`tools/art/sprite_bosses.py`, `choir_extra_anims`; `python tools/art/sprite_bosses.py --choir`
  writes these five strips and the `.tres` alone, and `build.py` appends them as it builds the sheet):
  `sing` 6, `chant` 4, `fall` 6, `downed` 4, `dip` 6, derived from the one pose as the contract's four
  are. The song is WATER (the cave's, and `ChoirColor`'s family); the arcane mark stays the body's one
  emissive pair. The Choir plays these on its own clocks, frame by frame, and `Enemy.HoldsOwnPose`
  keeps a flinch from cutting a song short or waking a heap.
- **Photographed:** `_UiShot --stage=2 --rush-steps=1 --rush-after=175 --choir=note|dip|revive,200
  --frames=612 --burst=12,8` (`--choir` stages each a fixed time after the Choir lands; the revive wants
  `--burst=16,40`). Still needs eyes in play: whether the note's crescent reads at speed in a swarm,
  whether 7 s of song is the right length, and whether players find the singer to stop.

### Bosses and crowd control (2026-11-01)

"Bosses should also be immune to CC that would make it impossible for them to ever get to the player.
They should always have a way to put the player in danger." (Eric, 0.10.0.) Every way a spell stops an
enemy arrives through `Enemy.ApplySlow` (and at 0, a freeze, a root, Event Horizon's stopped time),
`ApplyShock` (a stagger) or `ApplyKnockback` (a blast's throw, and every pull, drag and wall: Gravewell,
Hollow Star, the Elder God Maw, Vortex Reaper, Spring Tide, Iron Palisade, Broken Ward), so the rule is
one place in `Enemy` ("Bosses and crowd control"), on for every `BossEnemy` (`CapsCrowdControl`), and
no spell changed:

| kind | on a boss before | on a boss now |
|---|---|---|
| slow | the scene's floor, 0.3-0.45 | the floor is at least 0.5 (`BossSlowFloor`) |
| freeze, root, time stop | the scene's floor (a slow) | **stops it** - the ice, the frozen look, `IsHeldByCrowdControl` - for 0.75 s at most, then nothing holds it for 2.5x as long: the rest lands as the floor slow |
| stagger | stopped for as long as it was renewed | the same hold, capped; while it shrugs one off a stagger only shakes it |
| knockback, pull, drag, wall | REPLACED its step for 0.45 s at a time: renewed every tick, it held it; at resistance 1 (the Archivist, Still Warden, Ninth Stone) it stood dead still | rides on top of its own walk, scaled by its resistance, capped at 0.4 of that walk (`BossShoveShare`): it leans into a well, is set back by a blast, shoulders through a wall, and comes on |
| fear, the throw | immune | immune |

Held at most 2 seconds in 7, never 0.75 s on end; the worst everything-at-once can do is about a fifth
of its walk, straight at him. Elites and minibosses are not bosses here. Measured with `_BossProbe
--cc=all` (the wizard stands still; every body is frozen, staggered, slowed to 0.05 and pulled away from
him at 900 px/s on every frame, past any spell; `--no-cc-cap` is the before in the same build), within
150 px of him inside 45 s / attacks begun:

| boss | no crowd control | all of it, before | all of it, now |
|---|---|---|---|
| Elderbark | 5.7 s / 8 | never (360 px) / 0 | 21.9 s / 8 |
| the Gaoler | 13.9 s / 4 | never / 6 | 11.9 s / 6 |
| the Hollow Choir | 0.7 s / 14 | never (245 px) / 0 | at once / 9 |
| Mother Rot | 5.8 s / 8 | never / 8 | 31.6 s / 8 |
| the Warden of the Deep | 2.2 s / 6 | never / 8 | 8.6 s / 5 |
| the Hayward | 1.7 s / 10 | never / 0 | 17.5 s / 8 |
| the Ninth Stone | 3.2 s / 10 | never / 0 | 9.4 s / 9 |

The Archivist (dives round him; 25 attacks all three ways), the Still Warden and Wizard Dude (neither
walks in at a wizard who stands off), the Long Coil (burrows to him either way) and the Constable (on
his wall) do not walk in as the test asks, and were unchanged by it. The Seneschal arrives 1,700 px off
in his own rooms and came no nearer than where he arrived (380 px) in 45 s under all of it, before and
now - his walk through the doors is long, his danger is his household, and the test's wizard does not
go to him as that fight asks. Elderbark's and Mother Rot's 20-30 s is the worst case
stacked.

Per kind, on Elderbark (each on every frame; within 150 px is a poor test here, because a wizard who
stands still gathers the swarm's elites round him and a boss, a blocker, cannot walk through them - the
held windows say "a body" - so read the attacks): freeze, before 7 attacks / now 8, held 0% / 30%;
stagger 0 / 8, held 100% / 33%; pull away 0 / 8 (before, 304 px off the whole 45 s); slow 8. With none,
8. The Gaoler's own probe (the bot kiting, no `--cc`) is held against a body in 40-48% of its walking
windows with `--no-cc-cap` and 52-80% now: a boss pushed by the bot's spells is now still asking to walk,
which the old knockback branch hid, and it falls in the same time.

### What not to take

- **Their stage length.** Thirty minutes to a Lord is a structure built around their meta and their
  build depth. Ours runs fifteen (`Node2DGame.TimerVictorySeconds`), and lengthening it is a
  decision about pacing and unlock economy, not an enemy decision.
- **Enemy block chance.** A static defensive roll on enemies adds a second damage layer to a game
  whose damage pipeline is already ten ordered steps deep (`Player.TakeDamage`), and it makes
  outcomes less legible rather than more.
- **Anything recognisable.** Not a name, not a silhouette, not a specific creature. The behaviours
  above are generic to the genre and would be arrived at independently by anyone solving the same
  problems; that is the test for whether something is safe to take.
