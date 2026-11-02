# ADR: Story resets - a release that changes the story restarts everyone's progress, and says so first

**Date:** 2026-11-02 · **Status:** accepted, built (`wip/progress-reset`); the epoch ships at 1, so no
release has reset anyone yet

## The request

Eric: *"We should reset progress for people when we push an update that affects the progress of the
story. We should also warn them when they click download that their progress will be reset, as this
game is in active development."*

## Context

Until now a save has never been allowed to lose anything. `CLAUDE.md` says it plainly: any change that
narrows what a save implicitly grants must add a migration step, or it silently confiscates content
from existing players. Twelve schema steps (`SaveData.Migrate`, 8 to 20) exist to honour that, and
several of them are elaborate - schema 19 re-derives every unlock from achievements so that nobody
loses a spell they already had.

That rule is right for a change to *how* progress is stored. It is the wrong tool when the story
itself is rebuilt: chapters reordered, wizards rescued in a different place, a gate moved. A save
earned under the old story then describes a campaign that no longer exists, and a migration that
tries to map it onto the new one produces states nobody designed or tested (a wizard owned before
the chapter that frees him, a gate open with its boss unbeaten). While the game is in active
development that will keep happening, and each time the honest answer is to start the story again.

## Decision

**A story epoch.** `SaveData.CurrentProgressEpoch` is a hand-set constant, and every save carries
the `ProgressEpoch` it was earned in. When a save from an earlier epoch loads
(`SaveManager.LoadFrom`):

1. The save file is copied, byte for byte, to `user://savegame.before-epoch-<n>.json` (`n` the
   epoch being entered; `-2`, `-3` if that name is taken). **No backup, no reset** - if the copy
   cannot be written, the save loads as it is and the next launch tries again. A reset may take
   progress out of play; it must never take it out of existence.
2. The save is replaced by a brand-new one that keeps only the settings: `SaveData.SettingsKeys`,
   which is everything the Options screen sets (volumes, mute, damage numbers, the tips switch, the
   playtest toolkit and the difficulty preset). Every other key comes back as a new save's - Arcane
   Energy, every unlock, chapter, achievement, shop and found level, secret, quest, seen flag and
   the prologue. A field added later is reset unless it is listed as a setting, which is the safe
   way round.
3. `ProgressResetNoticePending` is set, and the first title screen says, once: *The story has been
   rebuilt. Your progress starts again from the beginning. Your old save is kept.* It is cleared
   when the player presses Continue, so a player who quits before reading it reads it next time.

A save with no `ProgressEpoch` key predates epochs, so it reads as the first epoch
(`SaveData.FirstProgressEpoch`, 1): never reset while the constant is 1, reset by the first bump.
A save from a *newer* epoch (a copy run back on an older build) is left alone, like a newer schema.
With writes off (every `_` tool scene, `--no-save-writes`) the reset happens in memory only.

**Every release carries the epoch.** `release.sh` reads the constant and `package.py` writes it into
`version.json` as `"progressEpoch"`. That is how a copy knows, before it downloads anything, that
the new version would reset it:

- **Windows and Linux** (`UpdateScreen`) ask: *"This update restarts the story: your progress will
  be reset. Your current save is kept as a backup."* - **Update** or **Not now**. Not now plays the
  installed version and asks again at the next launch. Pad, mouse and touch all answer it.
- **Android** (`UpdateBanner`) puts the same line under the strip's offer; Download still opens the
  APK.
- With no epoch change, both behave exactly as before. A release from before epochs has no field,
  which reads as 0 and never warns.

`release.sh` compares the constant with the last tag's, refuses one that goes down, prints a loud
STORY RESET banner when it rises (before the export, while Ctrl+C is free, and again at the end),
and puts a line saying so at the head of the release notes.

## How it relates to the migration rule

It replaces the migration rule **only for a release that deliberately restarts the story**, and only
through the epoch. Everything else still needs a schema step: a renamed field, a moved reward, a
retired spell - any change to how progress is *stored* - is migrated, so that a player who is not
being reset loses nothing. The two compose: a reset save is created at the current schema, so it
never migrates, and a save that is not reset migrates exactly as before. Bumping the epoch is a
release decision Eric makes, never a shortcut to avoid writing a migration.

## Consequences

- One constant decides whether a release resets everyone. It ships at 1; raising it is a deliberate
  act, recorded in `.ai/releasing.md`, "Releasing a story reset".
- Only copies built with this code can ask. One from 0.10.0 or before updates straight into a reset
  (and still gets the backup and the notice, which run in the new version), so the first reset
  should follow at least one ordinary release that carries `progressEpoch`.
- Old saves are kept, not deleted. Nothing in the game reads them back; a player who wants the old
  story would have to run an old build against a restored file.
- An Android player who installs the new APK from the download page or the browser, not the strip,
  sees no warning in the game - the download page has to say it too (it can read `progressEpoch`
  from `version.json`; that page lives in the ecec.dev repository).
- `SaveManager.ResetProgress` (Options, Reset Progress) is unchanged: it still wipes everything,
  settings included, and writes no backup, because the player asked for exactly that.
- `RegressionChecks.ValidateStoryReset` holds the rules: key by key against a new save in memory,
  every named kind of progress gone, the not-reset cases, and - in debug builds, against a scratch
  file - the real load path writing the backup and the new epoch.
