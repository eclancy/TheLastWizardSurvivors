# The game's name

**2026-11-08: renamed again, to be built.** Eric: the game, the store, the repo, the website and the folders are
to be **The Last Wizard: Survivors** (*"on the game, the name should be 'The Last Wizard: Survivors'"*). It came out
of the store-title discussion of 2026-11-07, which also recorded its risks - an exact-title Play app "The Last
Wizard" (Hosted Games' interactive fiction, a different genre) and a same-genre Play app "Wizard Survivors"
(YouJustPlay, May 2025) whose words it puts side by side, which Google Play's impersonation policy is the thing to
mind - and the safeguards: a USPTO search before the public steps, and art that looks nothing like that app's.
The plan, piece by piece, is `.ai/in-flight.md` item 62; the one trap is that ":" cannot be in a Windows folder name
and `config/name` names the save folder, so `config/name` stays "The One He Missed" as an internal identifier and the
new name is shown everywhere a player sees one (window title, store, labels, art, text). The section below is
the first rename's record, and stays as it was.

**Status (2026-11-02): decided and done.** The game is **The One He Missed**. Eric, 2026-11-02, first the
direction - *"The name relevant for [the game] should be something like the one he missed"* - and then
the word: *"remove all references to wizard survivors from the repo, from the website and from the
game itself and replace it with the new name of the game"*. So the name tells the story's premise -
everyone else was taken, and you were the one he did not get - rather than the genre. (The second
quote is left word for word, old name and all, because it is the decision this page records.)

The rename itself - what changed, what keeps the old name on purpose because changing it would
strand a copy or a save, and how a copy from before the rename updates across it - is in
`.ai/releasing.md`, "The rename". The website moved in its own change (EricClancyEngineeringAndCrafts):
`ecec.dev/the-one-he-missed/`, `/games/the-one-he-missed`, with the old addresses kept for copies
already installed.

Pulled on 2026-11-02 from Eric's Claude Docs page "Game name ideas"
(https://claude.ai/artifact/LM9fYy1cs7vbEWRwssepAN), which came from his conversation with Claude Duet
and a voice call. That page also holds names for Creature Tactics; only this game's are copied here.

**The figure it names** - the blue-robed wizard on the title, the one he missed - was to stay story only; Eric made
him playable the same day, the fifth starter, named **The Archmage** on the character select (the game keeps the
title, the wizard has his own name). `.ai/protagonist.md`.

## The store title

**Decided 2026-11-03: the Google Play listing is titled "The One He Missed: Survivors"** (28 of Play's 30
characters). Eric: *"when we list it, we'll call it that."* Why: Play matches searches mostly on the title, and
"survivors" is the word the genre's players search; the bare name matches nothing they type. "Wizard
Survivors" alone was out - a Steam game and a Play app already use it.

- **Only the store listing carries ": Survivors".** The game's own name stays *The One He Missed* everywhere
  else: the title screen, `config/name`, the website, the release files.
- **The short description leads with "wizard survivors roguelike"** (Play indexes it), so the "wizard"
  searches still match.
- The rest of the launch plan (listing copy, screenshots, trailer, the closed test, the checklist) is Eric's
  Claude Docs page "The One He Missed: store listing and launch plan"
  (https://claude.ai/artifact/91ZJS1xzvQtUiFXXTxZcKU).

## The shortlist it was chosen from

All are drawn from the story (`.ai/world-and-tone.md`, `.ai/prologue.md`): he strikes the wizards'
moot at the Broken Circle and takes their light; you are the one he missed, your light down to an
ember; you free a wizard in each place, then take your spells back in his castle.

| name | why |
|---|---|
| **The One He Missed** | **chosen.** The line the whole story hangs on; it is already the Court's quatrain ("The one he missed walks every road he made", `.ai/chapter-wizards.md` 4.10) |
| Last Ember | your light down to an ember while he searches the stones |
| Emberward | the ember plus the warded circle he broke (and the app icon is already "the burning ward") |
| Take It Back | the final fight's own words (the Throne's ask is *take it back*, `.ai/endgame-castle.md`) |
| The Broken Circle | where he struck first; also chapter 3's name, so the title would name a chapter |
| Every Wizard Freed | what you earn to reach him (the Throne's gate) |

*Above and Below* is also on the favourites list, "from the voice call". It is Creature Tactics' (its
Undercity and Overcity), not this game's.

## In the same vein

Names "something like the one he missed": each says that he took everyone and missed you. They were
for comparing the favourite against.

| name | note |
|---|---|
| **The One He Missed** | the original; plain, and already a line in the game (the Court's quatrain) |
| He Missed One | the same thought from his side; shorter, and a little ominous for him |
| One Light Left | the ember: of every wizard's light, yours is the one still burning |
| One Light, Alone | from the quatrain "Out of the west one light, on foot, alone"; the walk across the map |
| The Ember He Missed | joins the favourite to the Last Ember idea |
| The Last Unbound | the others are sealed, chained and jailed; you are the one still free |

## Clashes the name brought

- **Merlin's deed.** An achievement was already called "The One He Missed" (`AchievementDefinition.cs`,
  `OneHeMissedId`); now that it is the game's name, the deed is renamed on Merlin's branch. Its id is
  a save key and does not change.
- **The title art** spelled the old name in its lettering; it is redrawn by `tools/art/splash.py`,
  `splash_wide.py` and `hero.py` on the title art branch (`.ai/title-screen.md`).
