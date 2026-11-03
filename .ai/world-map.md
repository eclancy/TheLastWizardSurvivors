# The world map

Eric, 2026-10-29: *"Let's upgrade how map selection works. I want a world map that has all of the
different maps somewhere on it, in one contiguous drawing, separated by the drawn world. When the
player selects a character, that wizard is then placed on the world map, and they can press left or
right buttons to move between locations on the map, and the character Sprite will run over to catch
up with the camera. When highlighting a specific location, you will still see the preview card with
a small description of the location. It should also have silhouettes in the card to show what
unlocks are available on that map that haven't been found yet, that fill in when they unlock that
thing. Use the mobile game vampire crawlers as a reference if needed"*

And, the same day: *"Sketchbook can be a special final separate level that appears outside the
context of this map, but you should be able to scroll to it."* and *"There should also be a compass
style scroll bar at the top of the screen so you can see what levels you can scroll to."*

This doc is the design. It replaces the stage list (`StageSelection`) in the flow: CharacterSelection,
then the world map, then the run. Read with `.ai/campaign.md` (the order), `.ai/world-and-tone.md`
(why each place is held), `.ai/art-direction.md` (the contract, section 6a above all),
`.ai/controller-and-focus.md` and `.ai/stage-secrets.md`.

---

## 1. The world

**One picture, generated** (`tools/art/world_map.py`, never a hand-edited PNG): an overworld seen from
above, 2112x840 art pixels drawn at x2 (1680 until the castle grew it east, section 18), in the Bonelight palette and under the same key from the upper
left as everything else. Every chapter is a *place* in it - a wood, a field system, a ring of stones,
a cave mouth, a castle on a rock - and what lies between them is drawn land: the road, a river, a scarp, a
mountain range, a coast. Nothing is a node on a blank ground.

**The road runs west to east.** The campaign is one road (`.ai/campaign.md`, "The gate": *being past
a chapter is having been through it*), and Eric's controls are left and right. So the first chapter
is at the west edge, the last at the east, and pressing right walks east: the controls, the road on
screen and the compass at the top all agree about which way is forward. The road winds north and
south between places, so the map is a world rather than a strip, and the camera scrolls on both axes.

| # | Chapter | Where | Why there |
|---|---|---|---|
| 1 | Enchanted Forest (`stage_0`) | the west, a deep wood running off the edge of the world | where the player starts; *"Every path now leads inward"* - the road leaves the wood eastward |
| 2 | The Fallow Fields (`stage_9`) | south of the road, just past the wood's edge: hedged fields, a millpond, the well | *"The farms beyond the wood"* (`campaign.md` 5) - the first place that was someone's livelihood |
| 3 | The Broken Circle (`stage_10`) | north, up on the high heath: heather, granite, eight stones and a gap | the moot was held high and apart; the road climbs to it out of the fields |
| 4 | Sunken Cave (`stage_2`) | below the heath's eastern scarp: the tarns' stream runs down and into a cave mouth | *"His shadow followed the water down into the dark"* - the water is drawn going in |
| 5 | Blighted Swamp (`stage_3`) | south-east, where the river spreads into a delta bog before the sea | the lowest land on the map; cold arcane lantern lights in the reeds (*"Nothing living lights them"*) |
| 6 | Mystic Ruins (`stage_4`) | north again, on a plateau above the bog: broken spires, rubble rings | the wizards' old seat of learning - *"The old wards were broken from the inside"* |
| 7 | Frozen Waste (`stage_5`) | the far north, past the ruins: ice field and wind ridges under the mountains | cold is where the land is highest and furthest from his fire |
| 8 | Scorched Sands (`stage_6`) | over the mountain pass, south-east: a dry basin, dunes, the trade road | the rain shadow of the range and the heat of his forge; *"He hid the water"* |
| 9 | The Emberdeep (`stage_7`) | the far east of the land: black ground, lava, a volcano at the end of the road | *"His forge, at the end of the road."* The gate chapter. Its summit holds the forge's fire, and the Boss Rush (section 5) |
| 10 | The Last Bridge (`stage_11`) | from the headland under the Emberdeep's black cliffs, across the dark sea | *"His seat is across the water."* The road comes down off the summit to the cliffs and walks the bridge's deck; the stop is the bridge foot |
| 11 | Cursed Dungeon (`stage_1`) | at the foot of the castle on its rock, where the bridge lands: the cells cut into the rock under the south wall, one window lit, the sea-gate at the water | *"He keeps a wizard in one of these cells."* Since piece C2 the castle's cells, entered from the bridge's gate (`.ai/endgame-castle.md` 1.3, 8). At chapter 5 it was a keep on the river's crag, *"a grim choke-point"*; the keep came off, the crag stays as hills, and the road crosses the river's bridge unguarded |
| 12 | The Court (`stage_12`) | inside the walls: the great hall's slate roof across the yard, and the west range | *"A court with no one left in it"* - so no window in it is lit |
| 13 | The Throne (`stage_13`) | his tower at the rock's north-east, the tallest thing in the world | *"He is here."* The one place on the map lit by the `darkness` ramp as emission: the throne room's windows at its top, a halo, and motes going up off the spire (`world-and-tone.md`: he is the only source of dark light) |

The Emberdeep's summit is where the **Boss Rush** stands (section 5). Past it the road runs down to the
headland under the black cliffs, over the Last Bridge to his rock, and up through the gate, the yard and
the hall door to the tower. Past the rock the dark sea runs to the world's edge. **The Sketchbook is not
in the world, nor on the road** (section 4).

**Lighting and palette.** Ground from the deep half of each material's ramp, features lit on their
top and left, `occ` for contact and cliff shadow, no colour outside `bonelight.py`. The places' colours
are their chapters' ground colours (forest lichen, field gold-and-flesh deep, heath violet and stone,
swamp linen, snow in bone and steel, sand gold-and-flesh, basalt and fire), so each place reads as the
chapter the player is about to walk into. Emissive light is spent only where the fiction puts it:
his lanterns in the swamp (arcane), the Emberdeep's lava and its summit's fire (fire), his tower (darkness),
the one lit cell under his walls (light: somebody is kept there), and the player.

## 2. Locked places: on the map, visitable, never startable

**Superseded 2026-10-31** (Eric: *"you should only unlock the map as you defeat the previous level"*):
**the map unrolls as the campaign is won.** A campaign chapter is a stop, a compass mark and lit on the
map only once it is revealed (`GlobalStatsManager.IsChapterRevealed`): the first chapter; any chapter
the save has earned (`IsStageEarned` - so nothing a save can enter is ever hidden from it); and a gate
chapter whose previous boss has fallen, shown under its mist and lock with its "Still held: N wizards"
line until the wizards are free. Everything else is covered by the screen's ground colour
(`WorldMapView.BuildMask`): whatever lies nearer the unrevealed road and places than the revealed ones,
with a dithered edge on the art's 2-pixel grain - so a new save sees the Forest's land and the first
yards of road fading into the dark, and a finished campaign is lit everywhere. Unrevealed places are
not on the compass at all, so none is named; "Chapter N of 10" still counts the whole road. The rule is
read from the save's beaten bosses and opened list, so it needs no schema step and closes nothing.
**A place revealed since the last visit this session** (`WorldMap.LastShownStops`) is drawn back out of
the dark over about two seconds when the map opens - the beat of coming back after the boss that opened
it. On a cold start the map simply opens as far as it reaches. `ValidateWorldMap` holds a new save to
the first chapter alone and every enterable chapter to being shown. The rest of this section is the
first build's design, kept for the gate chapter and the unbuilt ones it still describes.

**Every chapter is drawn and every chapter is a stop**, open or not. The stage list hid closed
chapters entirely, and the cost it wrote down was real: the Emberdeep's gate - *every spell recovered
and every wizard freed* - was said nowhere until it opened. A map shows the places anyway, so letting
the cursor visit a closed one costs nothing and answers the question the picture raises: *why can I
not go there?* Its card gives the lock reason (`StageDefinition.LockedHint`, derived from the
campaign order) and what the place holds.

- **Drawn under his shroud.** A locked place carries a bank of dark fog over its landmark (a
  generated sprite, laid at runtime because the lock is the save's, not the art's) and the lock glyph
  above it. The road stays clear: the road is the drawn world, and the place is what he holds.
- **The wizard walks to the door, not in.** Every stop has a *stand point* on the road in front of its
  place. He walks there for a locked place as for an open one; the fog sits behind him.
- **A locked place can never be started.** The card's Start button says why instead (*"Locked -
  defeat Elderbark in the Enchanted Forest"*) and pressing it plays `ui_denied`. It is never
  `Disabled`: a disabled primary hands the selection to Back, and a pad user pressing A on a locked
  place would leave the screen. `WorldMap.TryStart` checks `GlobalStatsManager.IsStageAvailable` again
  at the moment of starting, whatever the button said.
- **Not built yet** (`IsPlayable = false`; none today, both Phase 2 chapters shipped): the same fog,
  thicker, and the card's line is *"Not yet open - this chapter is still being built"*. It is part of
  the drawn world like everything else.
- **Until it is drawn** (2026-10-30, the castle's plumbing, `.ai/endgame-castle.md` 11.4): an unbuilt
  chapter may have no place in `PLACES` yet - the castle's three wait on piece C8. It is then left off
  the screen quietly, "Chapter N of M" counts only the chapters shown, and `ValidateWorldMap` lists the
  missing place on the chapter's campaign to-do line instead of as a fault. A **playable** chapter with
  no place is still a fault, so nothing flips undrawn.

## 3. The route, the wizard and the camera

**The route is one polyline** through every stand point in stop order (section 6), written by the
generator into `assets/bonelight/ui/worldmap/worldmap.json` with each stop's stand point, the point the
camera frames (its *focus*) and its shroud. The C# reads that file; nothing in code knows a coordinate.

**The wizard is a position along the route, in arc length** - one number. The selected stop is a
target arc length. Every frame he moves toward it along the line, so:

- **No teleporting, ever**: he can only move along the road, and a new target mid-walk simply turns
  him round where he stands.
- **Fast presses are free**: five presses of right set the target five stops on, and he runs there in
  one walk. His speed grows with the distance left (a floor of a brisk run, a ceiling so a jump from
  the first chapter to the last takes about two seconds), so a long jump is a run, not a crawl.
- **The animation cannot stick**: `moving` while he is more than half a pixel from the target, `idle`
  otherwise, chosen every frame from that one comparison. Facing is the sign of his horizontal motion,
  through `CharacterVisuals.SpriteFacesLeft` (the Stormcaller is drawn facing left). He is the
  selected character's own sprite (`CharacterData.Frames`, `moving` and `idle`): Laelaps is the dog;
  the Test Wizard, who has no `moving`, walks on his idle at a quicker beat.

**The camera leads and the wizard catches up, on a leash.** The camera eases toward the selected
stop's focus point quickly (it arrives first), but it is clamped every frame so that the wizard stays
inside the view with a margin. On a long jump the camera runs ahead until he reaches the edge of the
frame and then drags him along; it can never leave him. At rest the focus sits in the centre of the
map's own rectangle, not of the screen, so the card never covers the place it describes.

**Pixels.** The map, the wizard, the fog and the markers are drawn by one control (`WorldMapView`) at a
whole number of SCREEN pixels per art pixel - x2 on a 1:1 canvas, the nearest whole number to it on
any other window (`.ai/art-direction.md` 6a, `PixelArtRect`'s rule) - and every position is snapped to
a screen pixel. One pixel size for the world and the wizard on it. No `SubViewport`, no `Camera2D`.

## 4. The Sketchbook: its own page, once the campaign is finished

Eric, 2026-10-29: *"a special final separate level that appears outside the context of this map, but
you should be able to scroll to it."* And on 2026-10-30, after playing it: *"sketchbook level should
not be attached to the map, remove it from the map. Once you beat the whole campaign, you should be
able to navigate to it, but it shows up as its own separate map that doesn't mix in with the flow of
the actual world map."*

The first build put it as the last stop past the world's east edge, reached by a pencil line off the
end of the road, and as a page mark at the end of the compass. That is gone. **The Sketchbook is a
sheet of its own** - not a stop, not on the road, not on the compass:

- **The page**: a loose sheet of squared paper on the dark - a workbook page, generated (paper, faint
  blue ruling, a red margin, two strips of tape, a short pencil road in from its torn edge, a pencil
  lying on it; `world_map.py`'s `page()`, never derived from anything in `assets/kidsart/`). Drawn by
  `WorldMapPage` in the map's place, at the world's whole-pixel rule (x2 on a 1:1 canvas, a step down
  if it would not fit), with the selected wizard standing at the end of the pencil road. The card is
  the same card, for the one stop: *"Beyond the map"*, *"A world drawn in a workbook, loose."*, its
  warden, what it holds, *"Enter the Sketchbook"*. Nothing walks: there is one place on the page.
- **How he gets there**: a **Sketchbook** button with the page glyph (`mark-page.png`) at the right end
  of the top bar, past the compass and apart from it. It is there only once the page is open to the
  save (below), so it never names a place the player cannot reach. Pressing it turns the screen to the
  page; on the page, **Back** reads *"Back to the world"* (*"World"* on a phone) and turns back, to the
  stop he was standing at. B, Escape and Android back do the same; from the world they still leave
  the map. Each turn reloads `WorldMap.tscn` with `WorldMap.OnPage` set, so a sheet is always built
  whole. A run started from the page returns to the page (`OnPage` lasts the session).
- **The pad**: the world's focus stops go round Start, Sketchbook, Back with up and down (up from Start
  is the button above it); the page's are Start and Back. Left and right do nothing on the page.
- **When it is open** (`StageDefinition.AfterCampaign`, read by `GlobalStatsManager.IsStageEarned`; its
  `Gate` stays `Open`, since nothing on the road opens it): **the campaign is finished**
  (`GlobalStatsManager.HasFinishedCampaign` - the boss of `StageCatalog.LastPlayableChapter`, the
  Warden of the Deep today, the Throne's once the castle is built), **or the save has already played
  it** (a best time there: it was open from the first run until now, and nothing a save could enter is
  ever closed - CLAUDE.md's migration rule, met without a schema step), **or it is in the opened list**.
  `GlobalStatsManager.RecordFinishedCampaign` writes it there the moment the campaign is finished - at
  that run's end, where the results screen shows *"The Sketchbook - The road is finished"* as a chapter
  opened, and quietly whenever the map opens on a save that finished it earlier - so it stays open when
  the castle's chapters make the campaign longer. **A chapter appended to the end of the campaign does
  not close the page to anyone who had it.** A save that finished at the Emberdeep and never opens the
  map again before the castle ships is the one gap; it would see the page again on finishing the castle.
- **The kids' drawings are on the page as themselves.** The page is mine; what is drawn on it is theirs,
  shown at runtime from their own imported files (`assets/kidsart/doodle-sun.png`, the Wizard Dude),
  untinted and at the page's whole scale - never redrawn, recoloured, cropped or derived, and never
  baked into a generated PNG (the generator does not open their files at all).
- **Everything about the page is in page space**: `worldmap.json`'s `page` holds its image, `size`,
  the paper's `rect`, the wizard's `stand` and the drawings' `at`, all in the page image's own pixels.
  The world's stops and route no longer mention it.
- **The gate validators** read "reachable before any gate" through `StageCatalog.IsOpenFromStart`, which
  is false for a chapter held until the campaign is finished, so nothing in the Sketchbook can ever be
  counted toward a gate (its secrets pay the page mark only, and its deed Arcane Energy).

## 5. Boss Rush

**A stop at the Emberdeep's summit, over his forge**, between the Emberdeep and the Last Bridge -
*"Every warden of the road, one after another, in the Emberdeep. The last is his."* It is a stop only
once `BossRushCatalog.IsOpen` (since piece C2, every boss on its roster beaten - on a new save after the
castle's dungeon, `.ai/endgame-castle.md` 1.8), as its card was before: until then the road simply passes
the summit on its way down to the cliffs and the bridge. The summit was "the seat" while the map ended
there; the seat is across the water now, and the card says *"The summit, over his forge"*. Its card shows the
record (*"Cleared in 12:40"*, *"Best: 6 of 10 felled"*) in place of rewards, and Start hands the run
`Global.BossRush = true` with the arena's index, exactly as `StageSelection.OnBossRushPressed` did.

## 6. The stops, and their order

**One rule, `WorldMap.RoadOrder`** (`.ai/endgame-castle.md` 8): `[the campaign up to and including its
gate chapter] + [Boss Rush, if open] + [the rest of the campaign] + [outside the campaign]`. The Boss Rush
stands on the gate chapter's summit, so it comes between the road and the castle; put after the whole
campaign, the road would run back from the tower to the summit. Left and right step through that list;
the route visits it in the same order, and `world_map.py`'s `PLACES` are written in it. A stop is a stage
id or `boss_rush`. The Sketchbook is not a stop (section 4).

**A stop off the road.** A chapter a save keeps from an older order - the Cursed Dungeon, grandfathered
by schema 21 (`.ai/endgame-castle.md` 1.5) - is a stop like any other: the wizard walks the road to it and
it can be started. But it does not carry the reveal's frontier with it, which would light every place
between, shown or not: its place is lit as an island (`WorldMapView.ViewStop.OffRoad`), the road to it
stays dark, and the stops before it are revealed by their own rules. It looks odd on an old save short of
the gate, and never on a new one.

`RegressionChecks.ValidateWorldMap` holds the layout file to it: every chapter in `StageCatalog` has a
stop, Boss Rush has one, the stand points' arc lengths rise in `RoadOrder`, every focus point is inside
the picture, a chapter held until the campaign is finished (`AfterCampaign`) has **no** stop on the
road, and the page has its sheet, a stand on the paper and its drawings on the paper. **A new
chapter must be placed on the map** (a line in `world_map.py`'s `PLACES` and a landmark drawn for it),
or the validator says so.

**Adding places.** A chapter joins the way the castle's did (section 18): a `StageCatalog` entry and its
place in `CampaignOrder`, a line in `PLACES` in `RoadOrder`'s order with its stand point, focus and fog
radii, the road's waypoints to it in `WAYPOINTS`, and a landmark function. Nothing in C# names a place.
The page is in its own space and does not move.

## 7. The compass strip

Eric: *"a compass style scroll bar at the top of the screen so you can see what levels you can scroll
to."*

A heading bar across the top of the screen, as a game compass is: a dark stone band with a gold rule
at its foot (the level-up heading band's materials), a fixed gold caret at its centre, and a mark for
**every stop** - each chapter, and Boss Rush when it is a stop. Not the Sketchbook (section 4).

- **It slides with the wizard**, not with the press: the strip's centre is his fractional position
  between stops, so it glides as he walks and stops when he does. The selected stop's mark is lit and
  larger; the caret shows where he is on the way to it.
- **Mark states**: *cleared* (the chapter's warden has fallen: gold, with the done glyph), *open*
  (gold), *locked* (stone, with the lock glyph), *Boss Rush* (the seat's glyph). The page glyph
  (`MarkKind.Page`) is still drawn and is the Sketchbook button's icon; no stop on the road wears it.
  Under each chapter mark, its campaign number. Marks thin out toward the ends of the band.
- **Tapping a mark jumps to it** (my call). On a phone the compass is the quickest way to the
  Boss Rush from the first chapter, and because the wizard's speed grows with the distance (section 3)
  a jump is still a run along the road, never a teleport. It takes no focus: a pad steps with left and
  right, which the strip shows, and a stop on the strip for the stick would be a second selection
  fighting the first.
- **Fit**: anchored to the top edge, so `SafeArea.Apply` brings it down under a phone's camera hole and
  rounded corners; numerals at the Label role (18 wide, 20 narrow), ink measured against the band at
  4.5:1 or better (`.ai/level-up-screen.md` 4a). Back sits at the left end of the same row.

## 8. The card

Shown for the highlighted stop, rebuilt when the selection changes:

1. **Where it is in the road**: *"Chapter 3 of 10"* (campaign position, not stage index), *"Beyond the
   map"* for the Sketchbook, *"The Seat"* for Boss Rush.
2. **The name** (Title role, Cinzel) and **one short line** (Body): once the chapter is open, its
   `CorruptionText` - what he has done to it; while it is held, its `FlavorText` - what the place is.
   "He keeps a wizard in one of these cells" is the reveal of a secret wizard, and the opening is its
   beat (`UnlockCatalog`, Tituba), so a held chapter does not say it. The Sketchbook has only the
   scenery line. Same rule as the old list: two lines, not a wall.
3. **The state line**, glyph-led: the lock reason (lock glyph) if locked; otherwise the warden -
   *"Elderbark has fallen"* (done) or *"Elderbark holds it"* - and the best time survived there.
4. **What it holds** (section 9): a row of slots, each the real icon or sprite of something tied to
   this chapter - a silhouette until it is found, filled once it is. Under the row, *"2 of 5 found"*.
5. **Start**: *"Enter the Fallow Fields"*, or the lock reason, never disabled (section 2).

**Silhouettes** are `CharacterVisuals.BuildSilhouetteFrames` - the character select's locked-wizard
shadow (occlusion fill, a stone rim lit from the key) - applied to every kind of icon, so a hidden
spell, boon, wizard or secret reads as one family. **A secret wizard whose chapter is not open** shows
the sealed portrait instead (the character select's rule: a silhouette naming a prisoner the player
cannot know of is a spoiler). **Secrets are never named until found** (`.ai/stage-secrets.md` 7.6: a
list of named blanks is a checklist): an unfound secret is the generic secret glint's silhouette, an
unfound keepsake the keepsake's. Every slot draws at a whole number of screen pixels (`PixelArtRect`).

**Where it sits.** PC landscape: a column down the right of the screen beside the map, under the
compass. Phone portrait: a panel along the bottom under the map. Both from `ResponsiveLayout.IsNarrow`.
The map's rectangle is what is left, and the camera frames the place inside it.

## 9. What a chapter holds: `StageRewards`

**The one authority** for "what a chapter holds", read by the card and by its validator. It invents
nothing: it derives every entry from the catalogs that already own them.

| Kind | From | Found when |
|---|---|---|
| the warden's reward (spell, boon, or Arcane Energy) | the deed whose `BossId` is the chapter's boss (`BossCatalog.ForStageIndex`), its `SpellUnlockId`/`BoonUnlockId`/`CurrencyReward` | the spell/boon is owned; for Energy, the deed is done |
| what its side events carry out (spell, boon, wizard) | every site in `SideEventDirector.EventsFor(id)` (one or two) -> `UnlockCatalog.ForDiscoverySite` | owned |
| a wizard whose reveal is this chapter | `UnlockDefinition.RevealStageId` (Tituba, Vainamoinen, Laelaps's coursing) | owned |
| its secrets and its keepsake | `SecretCatalog.ForStage` | `SaveData.HasFoundSecret` |

The Sketchbook's deed (*Best Drawing*) was removed on 2026-11-02 with everything else it held (Eric: no
unlocks or achievements there), so its card has no "What he keeps here" and is the one playable chapter
`ValidateStageRewards` lets hold nothing.

**`RegressionChecks.ValidateStageRewards`** - so a new unlock tied to a chapter cannot be missed:

- every `Discovery` entry in `UnlockCatalog` lands on exactly one chapter (its site is some chapter's
  side event), every `RevealStageId` names a real chapter, every deed with a `BossId` resolves to a
  chapter, and every secret's stage exists - and each appears in that chapter's list;
- **the prose net**: any unlock whose hint, or whose deed's description, names a chapter (*"in the
  Broken Circle"*) must be in that chapter's list. That is the check that would have caught the
  Coursing if it had been written without `RevealStageId`: a deed tied to a place in words but not in
  data;
- every chapter has at least one thing on its card, and nothing is listed twice.

## 10. Controls

| | Previous / next stop | Choose | Back | Jump |
|---|---|---|---|---|
| Keyboard | Left / Right (and A / D), OS repeat | Enter, Space | Escape | - |
| Pad | D-pad or left stick (`ui_left`/`ui_right`), held repeats after 0.4 s every 0.18 s | A | B | - |
| Mouse | the arrow plates at the map's sides | the Start button | the Back button | click a place, or a compass mark |
| Touch | the arrow plates | Start | Back, or Android back | tap a place, or a compass mark |

`ui_left`/`ui_right` are heard in `_Input` and consumed there, before Godot's own focus navigation, so
they move the map and never the selection. The screen's only focus stops are Start and Back (up and
down move between them); every focus stop's left and right neighbours are itself, so
`MenuNavigator`'s held-stick repeat cannot walk the selection sideways either. `MenuNavigator.Attach(this,
startButton)` and `SafeArea.Apply(this)` run once the screen is built. A direction held as the screen
opens is ignored until let go (the navigator's rule, honoured by the map's own repeat). Taps are
mouse-button presses (the emulated mouse covers a finger), never both.

## 11. Returning

The map opens with the wizard **standing** (no walk) at the last stop played: `WorldMap.LastStopId`,
set when a run is started from it, survives the run and the results screen for the session; on a cold
start it falls back to the save's most recent run (`RunTelemetryHistory[0].StageId`), then to the
furthest open chapter of the campaign. Back returns to CharacterSelection. A run started from the
Sketchbook's page returns to the page (`WorldMap.OnPage`, section 4), and turning back from it finds him
at the world stop he left.

## 12. Files

| What | Where |
|---|---|
| the generator | `tools/art/world_map.py` - the world, the page, the fog, the markers, the compass glyphs, the secret and keepsake icons, and `worldmap.json` |
| the art | `assets/bonelight/ui/worldmap/*.png` + `.import` |
| the screen | `scenes/WorldMap.tscn`, `scripts/WorldMap.cs` (layout, input, card, the two sheets), `scripts/WorldMapView.cs` (the drawn world, wizard and camera), `scripts/WorldMapPage.cs` (the Sketchbook's page), `scripts/WorldMapCompass.cs`, `scripts/WorldMapLayout.cs` (reads the JSON) |
| the authority | `scripts/StageRewards.cs`; `RegressionChecks.ValidateStageRewards`, `ValidateWorldMap` |
| the flow | `CharacterSelection` opens `WorldMap.tscn`; `StageSelection` stays in the tree, unused, until this has been played |
| the harness | `_UiShot --scene=res://scenes/WorldMap.tscn`, plus `--map-at=<stop>` (where he starts), `--character=N` (who), `--all-unlocked` (everything owned, found and beaten, in memory), `--last-run=<stage>` (a run recorded, for the cold-start return), and the existing `--pad=release,right,...` for presses and `--frames` to catch him mid-walk or at rest; `--map-tap=<stop>` / `--compass-tap=<stop>` put a real screen touch on a place or a mark and report the selection; `--map-start-probe` presses Start and reports the chapter the run that loads is, or that a locked stop loaded nothing; `--map-page` opens on the Sketchbook's page (with `--campaign-complete`; without it the world opens with no Sketchbook button) |

## 13. Sound

Nothing new. A step to a new stop plays `ui_hover` (a frequent sound, already at the frequent level of
`.ai/audio-direction.md`'s loudness table), Start plays `ui_click`, a locked Start `ui_denied`. No
footsteps: the walk lasts a second and would be the loudest-by-count sound on the screen.

## 14. What I took from Vampire Crawlers

poncle's mobile crawler (2026) moves from a village to a world map of regions, each a dungeon, with
branching routes, stage difficulty ratings, and some regions paying a new character instead of a relic,
the relics hidden in glowing objects on the floors. What this map takes from it:

- **The map is a place you travel, and what a region holds is stated before you go.** Their regions
  name the relic or the crawler you get there; this card shows it, as a silhouette until found, which
  is the collection-book pull that makes a player go back to a region.
- **A region that pays a person reads differently from one that pays an item** - here, the rescued
  wizards' slots are their own sprites and the spells are icons.
- **Locked regions stay on the map with their requirement**, rather than being hidden.

What it does not take: the branching (this campaign is one road, `.ai/campaign.md`), the numbered
difficulty (the chapter's place on the road says it), and the hub village (the title screen is the
quiet before the road). I could not reach a source that shows how their map animates, so the walking
and the camera are this doc's own, from Eric's description.

## 15. Decisions left to Eric

1. ~~Is the cursor right to visit locked places?~~ Decided 2026-10-31: no - the classic overworld, the
   road revealed as each boss falls (section 2). The Emberdeep's goal is still said, on its card, once
   the Long Coil has fallen and the gate is the next thing on the road.
2. **Should a locked chapter's card show what it holds?** It does (as silhouettes, a secret wizard
   sealed): it is the reason to open it. Hiding it is one line.
3. ~~The Sketchbook as a page off the edge of the world~~ - decided 2026-10-30: its own page, off the
   road and the compass, behind a button that appears once the campaign is finished (section 4).
4. **A save that played the Sketchbook before it was held keeps it** (section 4) - the CLAUDE.md rule
   against confiscating content. If Eric wants it held from everyone until the campaign is finished,
   drop the best-time clause in `GlobalStatsManager.HasReachedAfterCampaign`.

## 16. As built (2026-10-29), and what still needs eyes

Branch `wip/world-map`. Built as written above, with these departures, each found by photographing it:

- **The fog is a mist, not a bank.** Solid occlusion over a held place read as a pit in the map. The
  shroud never covers more than about three pixels in four, so the place shows through it, held.
- **The Broken Circle's stones are pale granite** (the bone row), not stone: in stone they vanished
  into the heather at x2.
- **His mark is the material arcane cyan** (his lanterns, Elderbark's eyes, the dead on the trade road),
  the colour every enemy's eye wears - not the arcane element's violet.
- **The derived lock hint lower-cases a boss's article mid-sentence** (`StageCatalog.LockedHintFor`:
  "defeat the Hayward"). The map is the first screen to show every locked chapter's hint.
- **The card's rewards heading reads "What he keeps here"**, the fiction's words for section 9.

**Checked by the harness** (`_UiShot`, `xvfb`, both canvases): the map at rest and mid-walk; three quick
presses running as one walk with the camera leashed to him; locked places under the mist with their
lock line, sealed and silhouetted slots; silhouettes empty (fresh save), partly filled (one secret) and
full (`--all-unlocked`); the Sketchbook's page off the edge with the kids' drawings on it; the Boss Rush
at the seat; Start on the Forest, the Dungeon (index 1, campaign position 5) and the Boss Rush loading
exactly that run, and on a locked chapter loading nothing; a real screen touch on a place and on a
compass mark selecting it; a pad's A pressing Start on the first attempt (`_PadProbe`); the cold-start
return to the chapter last played. `ValidateStageRewards`' prose net was shown to fire by untying
Laelaps from the Circle in data, then restoring him.

**Still needs eyes, and a phone:**

- **The art is a first pass.** The generator draws every place and the land between, but it has been
  judged only in photographs. The forest canopy, the plain hill country and the sea are the weakest.
- **On a real phone**: the stick-free controls (step plates, taps on places and marks) at the Pixel 9's
  1.5 pixels per unit, and the legibility of the compass numerals there. Computed, not seen.
- **A held stick's repeat** (0.4 s, then every 0.18 s) wants a controller in a hand.
- **On a large monitor** the map draws at the nearest whole number of screen pixels to x2 (x3 at 1080p
  full screen, x4 at 2736), so a bigger screen shows the same amount of map, larger - by design, but
  worth seeing.

## 17. The Sketchbook's page, as built (2026-10-30)

Branch `wip/pause-toasts-sketchbook`. Section 4 is the design as built. `world.png` is byte-identical:
taking the Sketchbook's stop out of `PLACES` left the road's tail drawn (`build_route` now allows a
waypoint list after the last stop), and only `sketchbook.png` and `worldmap.json` changed.

**Checked by the harness** (`_UiShot`, `xvfb`, `--campaign-complete`): the world on both canvases with
no Sketchbook stop or compass mark and the Sketchbook button at the top bar's right; the page on both
canvases (`--map-page`) with the kids' drawings, the wizard on the pencil road and the card; on the
phone, a pad step up from Start landing on the Sketchbook button. The validator run: no new warning.

**Still needs eyes:** the turn itself (a scene fade, both ways) and a run started from the page coming
back to the page; whether "World" reads as Back on a phone; and whether the button wants a first-time
flourish the run after the campaign is finished (the results screen does say the page opened).

## 18. The castle, drawn (2026-11-02, piece C8)

Branch `wip/dungeon-move`, with C2 (`.ai/endgame-castle.md` section 8 is the design; section 13 there is
the record of both pieces).

- **The world is 2112 wide** (`W`; `LAND_W` = 1680 keeps the land's seeded scatters where they were, so
  no tree west of the cliffs moved). The land ends at the Emberdeep's black cliffs (`east_edge`,
  `black_cliffs`): a headland where the road comes down off the summit, a south-facing face where the
  coast steps in, and the dark sea past it, going black toward his rock.
- **The castle** (`isle`, `last_bridge`, `castle`, `cells`, `halls`, `keep_tower`): the Last Bridge on
  piers from the headland to the gate on the rock, its deck the road; the rock's south face in black
  buttresses; the curtain wall and its towers; the gatehouse with his cold arcane flame either side;
  the cells in the cliff under the south wall, barred windows, one lit, the sea-gate at the water; the
  Court's great hall and west range under slate; and the tower, whose throne room burns with the one dark
  light, a halo and motes going up off the spire.
- **The dark light moved** from the Emberdeep's summit to the tower; the summit's caldera is a lava lake
  now (the forge). **The keep came off the river's crag**: the crag is a ragged knoll of bare rock with a
  tor where the keep stood, and the road passes it on the same points as before to the river's bridge.
- **The stops** are 14: the road's ten and the rush as they were (the Dungeon's old stand point is a plain
  point of the road now, so the road is the same road), then `stage_11`, `stage_1`, `stage_12` and
  `stage_13`. Every card counts 13 chapters (*"Chapter 11 of 13"* on the Dungeon's).
- **Checked:** `ValidateWorldMap` and the rest of the validator (0 errors); `_UiShot` at 1280x720 and
  540x1212 `--portrait --safe-area` at `--map-at=stage_13`, `stage_11` and `stage_2` (at rest, and
  mid-walk over the river with no keep), and an in-memory schema-19 save at `stage_1`, the Dungeon
  grandfathered and lit as an island, the bridge closed.
- **Needs eyes:** the castle has been seen only in photographs and crops. The Bridge, the Court and the
  Throne are not playable yet, so on every save they sit under the unbuilt double mist and the tower's
  light is hidden; it shows once the Throne is built and open. Whether the rock reads as a rock (it is
  smooth for one), whether the bridge's piers read at x2 on a phone, and whether the island-lit Dungeon
  on an old save reads as a place the road reaches or as a hole in the dark.
