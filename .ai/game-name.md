# The game's name

**Status: the direction is decided, the exact name is not.** Eric, 2026-11-02: *"The name relevant for
wizard survivors should be something like the one he missed."* So the name tells the story's premise -
everyone else was taken, and you were the one he did not get - rather than the genre. Until the exact
words are picked the game ships as *Wizard Survivors*. Pulled on 2026-11-02 from Eric's Claude Docs page "Game name ideas"
(https://claude.ai/artifact/LM9fYy1cs7vbEWRwssepAN), which came from his conversation with Claude Duet
and a voice call. That page also holds names for Creature Tactics; only this game's are copied here.

**Later on 2026-11-02 the name was chosen: The One He Missed** (the rename is its own branch). The figure it
names - the blue-robed wizard on the title, the one he missed - was to stay story only; Eric made him playable
the same day, the fifth starter, named **The Archmage** on the character select (the game keeps the title,
the wizard has his own name). `.ai/protagonist.md`.

## The shortlist

Any of them can be a new title or a subtitle after "Wizard Survivors:". All are drawn from the story
(`.ai/world-and-tone.md`, `.ai/prologue.md`): he strikes the wizards' moot at the Broken Circle and takes
their light; you are the one he missed, your light down to an ember; you free a wizard in each place, then
take your spells back in his castle.

| name | why |
|---|---|
| **The One He Missed** | **the favourite so far.** The line the whole story hangs on; it is already the Court's quatrain ("The one he missed walks every road he made", `.ai/chapter-wizards.md` 4.10) |
| Last Ember | your light down to an ember while he searches the stones |
| Emberward | the ember plus the warded circle he broke (and the app icon is already "the burning ward") |
| Take It Back | the final fight's own words (the Throne's ask is *take it back*, `.ai/endgame-castle.md`) |
| The Broken Circle | where he struck first; also chapter 3's name, so the title would name a chapter |
| Every Wizard Freed | what you earn to reach him (the Throne's gate) |

*Above and Below* is also on the favourites list, "from the voice call". It is Creature Tactics' (its
Undercity and Overcity), not this game's.

## In the same vein

Names "something like the one he missed": each says that he took everyone and missed you. The first is
still the favourite; the others are for comparing it against.

| name | note |
|---|---|
| **The One He Missed** | the original; plain, and already a line in the game (the Court's quatrain) |
| He Missed One | the same thought from his side; shorter, and a little ominous for him |
| One Light Left | the ember: of every wizard's light, yours is the one still burning |
| One Light, Alone | from the quatrain "Out of the west one light, on foot, alone"; the walk across the map |
| The Ember He Missed | joins the favourite to the Last Ember idea |
| The Last Unbound | the others are sealed, chained and jailed; you are the one still free |

Each can stand alone or follow "Wizard Survivors:" while the old name is still known.

## If a name is chosen: what a rename touches

Recorded so the rename is planned, not found out. None of this is done.

- **The save folder moves with `config/name`.** `project.godot`'s `config/name="Wizard Survivors"` is
  also the `user://` folder (`%APPDATA%/Godot/app_userdata/Wizard Survivors/` on Windows), so changing
  it alone would leave every player's save, settings and shots behind. Set
  `application/config/use_custom_user_dir` and `custom_user_dir_name` to the old folder in the same
  commit, or migrate the files, before anything else.
- **Android:** `package/name` in `export_presets.cfg` is the label under the icon and is safe to change;
  `package/unique_name` (`com.ecec.wizardsurvivors`) is the app's identity, and changing it would make
  a second app that cannot update the first. Keep it.
- **Windows:** `application/product_name` and `file_description` in `export_presets.cfg`.
- **Releases and updates:** `tools/release/release.sh`, `install.sh` (which also names the Linux save
  folder) and `UpdateScreen.cs` (its "Updating Wizard Survivors" title). Every installed copy finds the
  next build on GitHub Releases (`.ai/releasing.md`), so renaming the repo or the release assets must
  keep that working.
- **Bug reports:** `ReportBugScreen.cs` sends `game=Wizard Survivors`, which ecec.dev's intake uses for
  the email subject, so change both together.
- **Words:** the title screen art (`tools/art/splash.py`, `splash_wide.py`), `README.md`, the
  ecec.dev pages (the Spell Index), the store text.
