using Godot;
using System;
using System.Linq;
using TheOneHeMissed.scripts;

// D7, THE OUBLIETTE - a secret of the Cursed Dungeon (.ai/endgame-castle.md 4.4; .ai/achievements-and-secrets.md
// 8.5). The cell for someone meant to be forgotten: a shaft under the gaol's floor with a grate over it, at the end
// of a dead end. Its find is where Nimue's card stops saying "Not yet discovered." - someone else is still held
// here - which the Throne opening did alone until this was built (decision 11, its Built note).
//
// SEES. A grate in the floor of a dead end, darkened with the floor it is set in, and through it a cold draught:
// pale motes rising off the bars, never darkened, so they show over the dark and over the walls from the next
// corridor before the grate does. Deep in the middle of the bars, the warm glint that breathes (SecretSite): a
// point of warm light far down, the one sign that what is down there is not his. From one and a half chunks out
// the flagstones are wet - puddles thickening toward it, and nearest it rimed with the draught's cold (its FarTell).
//
// DOES. Untouched (SecretVerb.Untouched). His feet on the grate (GrateReach of its middle) for HoldSeconds without
// being struck: Player.DamageTaken, which a dodged blow never raises (Player.TakeDamage step 2), starts it again,
// and so does stepping off. Stillness is not asked as well - the grate is too small to walk about on, and a dead end
// has one way in, so the five seconds are spent holding that mouth clear. The draught thickens and the glint warms
// as the count runs, and that is the whole of the tell: no bar, no number.
//
// PAYS. A voice below (secret_voice_below, at the grate): a woman's, wordless, up a long shaft with water at the
// bottom - it never names her. When it has finished, the find (SecretDirector.Found): the page mark, the deed, and
// on the save FoundSecretIds, which is what UnlockDefinition.RevealSecretId reads - so her card turns from the next
// screen that draws it, with nothing stored but the find. Nothing else: no chest, no Arcane Energy (the 150 the
// design held in reserve was for a road with no Nimue on it).
//   - Her already revealed (the Throne open) and still held: the same - the voice, "Someone else is still held
//     here" - and the page mark; the card was already turned.
//   - Her freed: no voice. The draught still rises (it is a shaft), and the find says "No one is held here now".
//
// IN A DEAD END. The placer snaps a maze site to a corridor cell's middle; SecretDefinition.RoomAt takes only a cell
// with walls on three sides (DeadEndAt), and the director sweeps the whole mid band for one (SecretPlacement.Mid).
// The grate stands in that cell's middle. It reserves the cell and its mouth (MouthReserve), so no trap is set in
// the one way in and no prop on the grate. Nothing here is solid and nothing is in "enemies": the horde walks over
// the grate as he does.
//
// Progress lives in the run state, never on the node: State.Progress the seconds held, State.Done once the count is
// in, State.Step 1 while the voice is rising and 2 once the find is paid.
//
// Art: tools/art/secret_dungeon.py (the grate, the puddles). The motes and the glint are drawn in code, on the
// world's 2-unit grid, in opaque stops.
public partial class Oubliette : SecretSite
{
	/// <summary>Seconds on the grate without a blow (8.5, D7: five).</summary>
	public const float HoldSeconds = 5f;

	/// <summary>His feet this near the grate's middle are on it: the grate is 72 px across.</summary>
	public const float GrateReach = 44f;

	/// <summary>
	/// How long the voice rises before the find is paid: secret_voice_below's length, so the found sting - the
	/// Critical meta_unlock, which ducks every Normal sound - never lands on top of it.
	/// </summary>
	public const float VoiceSeconds = 2.7f;

	/// <summary>With no voice to wait for (she is free), a beat before the find, so it is not on the same frame.</summary>
	private const float SilentSeconds = 0.6f;

	/// <summary>Kept clear round the grate: the dead-end cell and its doorway (half a maze cell out), so no trap stands in the one way in.</summary>
	public const float MouthReserve = 230f;

	/// <summary>Listens for blows only this near: a grate across the map costs nothing a hit.</summary>
	private const float ListenRadius = 700f;

	private const string GratePath = "res://assets/bonelight/world/dungeon/oubliette-grate.png";

	// The draught: motes at rest, motes with the count full, how high they rise.
	private const int RestMotes = 10;
	private const int FullMotes = 22;
	private const float RiseMin = 70f;
	private const float RiseMax = 120f;

	// Steel's hi, lit and base: the draught's cold, pale and never his arcane blue (bonelight.py "steel").
	private static readonly Color MoteHi = new(0.863f, 0.910f, 0.957f);
	private static readonly Color MoteLit = new(0.722f, 0.784f, 0.863f);
	private static readonly Color MoteBase = new(0.416f, 0.494f, 0.596f);

	private static readonly SecretFarTell Tell = new()
	{
		Textures = new[]
		{
			"res://assets/bonelight/world/dungeon/puddle-1.png",
			"res://assets/bonelight/world/dungeon/puddle-2.png",
		},
		NearTextures = new[]
		{
			"res://assets/bonelight/world/dungeon/rime-1.png",
			"res://assets/bonelight/world/dungeon/rime-2.png",
		},
		// Nothing in the dead end itself: the grate is the thing to see there.
		InnerRadius = MouthReserve + 40f,
		NearRadius = 1024f,
		OuterRadius = 2304f,
		PeakPerChunk = 34,
		NearShare = 0.6f,
		FootFromBottom = 4f,
	};

	public override SecretFarTell FarTell => Tell;

	/// <summary>The seconds held so far, toward HoldSeconds. For the probe.</summary>
	public float Held => State?.Progress ?? 0f;

	/// <summary>True once the count is in this run (the voice rising or the find paid).</summary>
	public bool Answered => State?.Done == true;

	/// <summary>True once the find has been paid this run.</summary>
	public bool Paid => (State?.Step ?? 0) >= 2;

	/// <summary>True if the voice played this run. For the probe.</summary>
	public bool VoicePlayed { get; private set; }

	/// <summary>The dead end's one way out, from the grate (a unit vector), or zero off the maze. For the probe and the shots.</summary>
	public Vector2 Mouth { get; private set; }

	private Sprite2D grate;
	private Node2D glint;
	private Node2D draught;
	private Player listeningTo;
	private float clock;
	private float draughtClock;
	private float pending = -1f;
	private float posed = -1f;

	/// <summary>
	/// The grate on the cell the placer chose - a dead end, which RoomAt asked for - and the cell and its doorway
	/// kept clear (MouthReserve). Off the maze (never, today) it stands where it was put.
	/// </summary>
	public override void LayOut(ExplorePlacer placer)
	{
		placer.Reserve(Position, MouthReserve);
		if (Director?.Game is Node2DGame game && DeadEndAt(game, Position) is Vector2 way)
			Mouth = way;
	}

	/// <summary>
	/// The dead end's open side, if the maze cell centred on <paramref name="cell"/> has walls on exactly three:
	/// asked at the middle of the wall strip half a cell out each way, which is floor where the passage is open
	/// (the lattice: a six-tile corridor block, a two-tile strip east and south, .ai/terrain.md section 6). A hall's
	/// cells never qualify - each has two passages open round its knocked-out post. Asked of the chunk data, which
	/// is made for far ground on demand. Null for anything else.
	/// </summary>
	public static Vector2? DeadEndAt(Node2DGame game, Vector2 cell)
	{
		TerrainStreamer terrain = game?.Terrain;
		if (terrain == null || terrain.IsWallAtWorld(cell))
			return null;
		float half = LevelGenerator.MazeCellTiles * game.ProceduralTilePixels * 0.5f;
		Vector2? open = null;
		foreach (Vector2 dir in new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up })
		{
			if (terrain.IsWallAtWorld(cell + dir * half))
				continue;
			if (open != null)
				return null;
			open = dir;
		}
		return open;
	}

	/// <summary>
	/// The wizard whose card this secret turns (UnlockDefinition.RevealSecretId) - Nimue - or null. Read from the
	/// unlock catalog rather than named here, so the catalog stays the one place that ties the two.
	/// </summary>
	public static UnlockDefinition KeptBelow(string secretId) =>
		UnlockCatalog.All.FirstOrDefault(d => d.Kind == UnlockKind.Character
			&& d.RevealSecretId.Equals(secretId ?? "", StringComparison.OrdinalIgnoreCase));

	/// <summary>True while the one below is still held on this save: the voice answers only then.</summary>
	public bool SomeoneBelow()
	{
		UnlockDefinition kept = KeptBelow(SecretId);
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		return kept != null && !GlobalStatsManager.IsCharacterUnlocked(save, kept.Id);
	}

	public override void _Ready()
	{
		Texture2D texture = ResourceLoader.Exists(GratePath) ? GD.Load<Texture2D>(GratePath) : null;
		if (texture == null)
			GD.PushError("Oubliette: its art is missing; re-run python tools/art/secret_dungeon.py, then import.");
		if (Mouth == Vector2.Zero)
			GD.Print($"Oubliette: not in a dead end at {GlobalPosition}; it stands where it was put.");

		// Set in the floor, darkened with it (under the light pool's room pass, .ai/light-pool.md): the grate is
		// part of the gaol, and what reads in the dark is what rises out of it.
		grate = new Sprite2D
		{
			Name = "Grate",
			Texture = texture,
			ZIndex = DrawLayers.Decal,
			ZAsRelative = false,
		};
		AddChild(grate);
		grate.GlobalPosition = SpellInk.Snap(GlobalPosition);

		// The glint, far down between the bars: on the floor's tell layer, under every body and over the dark.
		glint = new Node2D { Name = "Glint", ZIndex = DrawLayers.GroundTell, ZAsRelative = false, TopLevel = true };
		glint.Draw += DrawGlintBelow;
		AddChild(glint);
		glint.GlobalPosition = Vector2.Zero;

		// The draught, in the air over everything, never darkened.
		draught = new Node2D { Name = "Draught", ZIndex = DrawLayers.Air, ZAsRelative = false, TopLevel = true };
		draught.Draw += DrawDraught;
		AddChild(draught);
		draught.GlobalPosition = Vector2.Zero;

		// Processed through a pause, for one thing only: a run that ends while the voice is rising still pays
		// the find (stage-secrets section 8, "found is found"). Everything else waits out a pause (_Process).
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _ExitTree()
	{
		Listen(null);
		if (glint != null && IsInstanceValid(glint))
			glint.Draw -= DrawGlintBelow;
		if (draught != null && IsInstanceValid(draught))
			draught.Draw -= DrawDraught;
		base._ExitTree();
	}

	private void Listen(Player player)
	{
		if (player == listeningTo)
			return;
		if (listeningTo != null && IsInstanceValid(listeningTo))
			listeningTo.DamageTaken -= OnStruck;
		listeningTo = player;
		if (listeningTo != null)
			listeningTo.DamageTaken += OnStruck;
	}

	// A blow that landed (a dodge raises nothing): the count starts again.
	private void OnStruck(int amount)
	{
		SecretDirector.SiteState state = State;
		if (state == null || state.Done)
			return;
		state.Progress = 0f;
	}

	public override void _Process(double delta)
	{
		if (GetTree().Paused)
		{
			// A level-up or the pause screen holds everything, the find's beat too; a run that has ENDED (the
			// results are paused under) pays it now, sound and all, since nothing after will.
			if (pending >= 0f && Director?.Game?.RunFinished == true)
				PayFind();
			return;
		}
		float dt = (float)delta;
		clock += dt;
		Player player = Player;
		SecretDirector.SiteState state = State;
		bool near = player != null && player.GlobalPosition.DistanceSquaredTo(GlobalPosition) < ListenRadius * ListenRadius;
		Listen(near && state != null && !state.Done ? player : null);

		if (state != null && !state.Done && posed >= 0f)
			state.Progress = posed;
		else if (state != null && !state.Done && near)
		{
			bool on = Active && !player.IsDead && player.FootPosition.DistanceSquaredTo(GlobalPosition) <= GrateReach * GrateReach;
			// Stepping off starts it again too: five seconds on the grate, not five in all.
			state.Progress = on ? state.Progress + dt : 0f;
			if (state.Progress >= HoldSeconds)
				Answer(state);
		}

		if (pending >= 0f)
		{
			pending -= dt;
			if (pending <= 0f)
				PayFind();
		}

		// The draught's own clock, so a faster draught speeds up from where it is rather than jumping.
		draughtClock += dt * (1f + 0.8f * Stir);
		if (player != null && player.GlobalPosition.DistanceSquaredTo(GlobalPosition) < 1600f * 1600f)
		{
			glint?.QueueRedraw();
			draught?.QueueRedraw();
		}
	}

	// The count is in: the voice rises (if anyone is still down there), and the find follows it.
	private void Answer(SecretDirector.SiteState state)
	{
		state.Progress = HoldSeconds;
		state.Done = true;
		state.Step = 1;
		Listen(null);
		if (SomeoneBelow())
		{
			VoicePlayed = true;
			SfxPlayer.AtPosition(SfxCatalog.SecretVoiceBelow, GlobalPosition, 0f);
			pending = VoiceSeconds;
		}
		else
			pending = SilentSeconds;
	}

	private void PayFind()
	{
		pending = -1f;
		SecretDirector.SiteState state = State;
		if (state == null || state.Step >= 2)
			return;
		state.Step = 2;
		Director?.Found(SecretId, VoicePlayed || SomeoneBelow() ? null : NoOneLine);
	}

	/// <summary>The find's line once the one below is free: the voice does not answer, and this says why without naming her.</summary>
	public const string NoOneLine = "No one is held here now";

	/// <summary>
	/// _UiShot's photographs (--oubliette-stage=N): the count held at a quarter of the five seconds a stage - 0 at
	/// rest, 1 to 3 the draught thickening, wherever he stands - and 4 answers it (the voice, then the find; writes
	/// are off). The Geode Heart's --secret-stage, for a site that is not the chapter's vault.
	/// </summary>
	public void PoseForProbe(int stage)
	{
		SecretDirector.SiteState state = State;
		if (state == null || state.Done)
			return;
		if (stage >= 4)
		{
			posed = -1f;
			Answer(state);
			return;
		}
		posed = Mathf.Clamp(stage, 0, 3) * HoldSeconds / 4f;
		state.Progress = posed;
	}

	// 0 at rest, 1 with the count full; held at 1 while the voice rises, then spent.
	private float Stir => State == null ? 0f : State.Done ? (State.Step >= 2 ? 0f : 1f) : Mathf.Clamp(State.Progress / HoldSeconds, 0f, 1f);

	// World units: TopLevel at the origin. The glint breathes until the find is paid, warming as the count runs.
	private void DrawGlintBelow()
	{
		if (Paid)
			return;
		float rising = pending >= 0f && VoicePlayed ? 0.5f + 0.5f * MathF.Sin(clock * 5f) : 0f;
		DrawGlint(glint, SpellInk.Snap(GlobalPosition), clock, Mathf.Min(1f, 0.55f + 0.45f * Stir + 0.2f * rising));
	}

	// The cold draught: motes off the bars, rising and drifting, in steel's opaque stops on the world's grid - every
	// other one a pale square of two art pixels, the rest a short streak one wide and two tall, as a mote in a draught is
	// seen. More of them, and faster, as the count runs; the same few at rest once the find is paid.
	private void DrawDraught()
	{
		float stir = Stir;
		int count = RestMotes + (int)MathF.Round((FullMotes - RestMotes) * stir);
		Vector2 at = SpellInk.Snap(GlobalPosition);
		float grid = PixelInk.Grid;
		for (int i = 0; i < count; i++)
		{
			float period = 2.2f + 1.4f * Hash(i, 1);
			float life = (draughtClock / period + Hash(i, 2)) % 1f;
			// Off a bar, anywhere across the grate's middle.
			float x0 = (Hash(i, 3) - 0.5f) * 56f;
			float y0 = (Hash(i, 4) - 0.5f) * 32f;
			float rise = Mathf.Lerp(RiseMin, RiseMax, Hash(i, 5)) * life;
			float sway = MathF.Sin(life * 5.3f + i * 1.7f) * (4f + 6f * life);
			Vector2 p = at + new Vector2(x0 + sway, y0 - rise);
			int cx = (int)MathF.Round(p.X / grid), cy = (int)MathF.Round(p.Y / grid);
			bool square = i % 2 == 0;
			Color c;
			if (life < 0.1f)
				c = MoteBase;
			else if (life < 0.6f)
				c = square ? MoteHi : MoteLit;
			else if (life < 0.82f)
				c = MoteBase;
			else if (PixelInk.Keeps(cx, cy, (1f - life) / 0.18f))
				c = MoteBase;
			else
				continue;
			draught.DrawRect(new Rect2(cx * grid, cy * grid, grid * (square ? 2f : 1f), grid * 2f), c);
		}
	}

	private static float Hash(int i, int salt)
	{
		uint v = (uint)(i * 2654435761u + (uint)salt * 40503u + 977u);
		v ^= v >> 13;
		v *= 1274126177u;
		v ^= v >> 16;
		return (v % 100003u) / 100003f;
	}
}
