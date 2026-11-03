# Godot Engine Notes

## Engine Version
Godot 4.x (update this file when version changes).

## Why Godot
- Open-source, no licensing issues.
- Excellent 2D performance.
- GDScript enables fast iteration.
- Scene system supports modular design.

## Conventions
- Use autoloads for global systems (GameState, SaveData, RNG).
- Prefer composition via scenes over deep inheritance.
- Use signals for decoupling (player_hit, xp_collected, spell_fired).
- Use Resources for spells, upgrades, and enemy definitions.

## Best Practices
- Keep _process lightweight; use timers and signals.
- Use CharacterBody2D for player and enemies.
- Use NavigationAgent2D for advanced enemy pathing.
- Use AnimationPlayer or AnimatedSprite2D for visuals.
- Use TileMap for arenas.
- Use object pooling for projectiles.

## Draw layers (2026-10-28)
The arena (`Node2DGame`) y-sorts, and y-sort only orders things that share a z. Anything left at z 0
is sorted in among the bodies by where its origin stands, so a blast or a bolt is cut by every body
south of its centre and a ring painted on the floor lies across every body north of it. Eric's rule:
"the hammer itself should be above enemies, but the quake it creates should be underneath their
sprites. Same with meteors - the meteor and explosion should be rendered above enemies, but the fires
should be rendered underneath."

So every world effect picks a side from `scripts/DrawLayers.cs`, with `ZAsRelative = false`:

| Layer | z | What |
|---|---|---|
| `Terrain` | -100 | the painted ground |
| `Decal` | -20 | walkable decoration, scorch marks |
| `RoomDark` | -18 | a dark chapter's dark (`LightPool`'s room pass, `.ai/light-pool.md`): over the ground, the walls and the decals, under everything on them. Its second, gentler pass sits at `Bodies`, placed 4000 px below the screen so the y-sort draws it after every body and before anything at z 1 or more |
| `Ground` | -15 | craters, pools, lanes, the maw's pit (crater fire is -14) |
| `GroundTell` | -1 | warnings and marks painted round a body's feet |
| `Bodies` | 0 | the player (his node; his body sprite is on `Foremost`), enemies, familiars that walk (the dog, the cat), chests, trees: y-sorted |
| `Air` | 6 | projectiles, blasts, swung weapons, thrown sprays, the familiar that flies (the crow, the griffin) |
| `Foremost` | 10 | the wizard's body sprite and a boss's body (the whole `BossEnemy` node, its arrival and its fall), over every spell and effect ("the player and bosses should always be rendered on top of spells and effects so they can always be seen", 2026-11-01). One layer under the y-sorted arena, so the two still sort by where they stand. Their floor marks stay on the floor (a boss's tells and ring, the wizard's stance glow and trail); the Constable drops to `Bodies` on his wall-walk, behind the parapet |
| `ForemostAir` | 11 | over those bodies: a boss's arrival dust and light and status marks (`StatusEffectLayer`'s `OverBosses`), Hunter's Draw's bow, a carried secret seed |
| `AttackIcon` | 12 | the icon over an enemy winding up an attack (`TelegraphIcon`, made by `Enemy` for a class that overrides `ReadAttackTell`), and in the middle of a `TelegraphedGroundHit` |
| `FloatingText` | 20 | damage numbers |

One effect is often both, and is then two nodes: `SpiritualWeapon` (weapon in the air, cracks on the
floor), `MeteorImpact` and `CraterLayer`, `Contagion` (spores up, carriers' marks down). An enemy's
floor tells go in `Enemy.DrawGroundTells`, which paints on a ground node made once in `_Ready`; only
what belongs to the body (a caster's aim tick, the Long Coil's segments) stays in `_Draw`. The icon
over a wind-up is the third node, on `AttackIcon`.

To find what is drawn among the bodies: `_UiShot --layer-census` prints a `LAYER` line per drawn
node class with its effective z and whether a y-sorted parent sorts it.

