using Godot;
using TheOneHeMissed.scripts;

// The Cursed Dungeon's side event: the Gaoler's key (.ai/side-events.md).
//
// "He keeps a wizard in one of these cells." A Warden - the dark wizard's jailer, already the run's
// recurring miniboss - carries the key. Kill it, pick up what it drops, and carry it to the one cell
// on the map that is locked. Three hits while carrying and the key is knocked out of your hands
// where you stand; go back for it.
//
// What it asks that nothing else does: a CARRY. Every other objective in the game is a place or a
// target. This one makes the route home matter, and makes being hit cost something other than HP.
public partial class GaolersKeyEvent : SideEvent
{
	[Export] public int HitsToDrop { get; set; } = 3;
	[Export] public float UnlockSeconds { get; set; } = 1.2f;

	private const float PickupRadius = 30f;
	private const float CellRadius = 46f;

	private enum Step { HuntWarden, KeyOnGround, Carrying }

	private Step step = Step.HuntWarden;
	private Enemy warden;
	private Vector2 wardenLastSeen;
	private Vector2 keyPosition;
	private Vector2 cellPosition;
	private int hitsWhileCarrying;
	private float unlockProgress;
	private bool listening;
	// THE WIN BEAT (in-flight 58): the cell is opened at the cell. The key turns in the lock and
	// nothing moves (the pause before anything does), then the barred gate swings open on its left
	// hinge, the gloom inside lightens and the one sitting there is gone - held open a moment, and
	// only then does the reward column rise, at the exact spot the beat chose.
	private float winBeat = -1f;
	private Vector2 winSpot;
	private const float WinTurnSeconds = 0.35f;
	private const float WinSwingSeconds = 0.9f;
	private const float WinHoldSeconds = 0.45f;

	protected override string Announcement => "A Warden is carrying a cell key.";
	protected override string ChestLine => "The cell stands empty - a chest was left in it";

	protected override string Objective => step switch
	{
		Step.HuntWarden => "Kill the Warden carrying the key",
		Step.KeyOnGround => "Pick up the key",
		_ => $"Carry the key to the locked cell  ({HitsToDrop - hitsWhileCarrying} hits from dropping it)",
	};

	protected override Vector2? PointerTarget => step switch
	{
		Step.HuntWarden => wardenLastSeen,
		Step.KeyOnGround => keyPosition,
		_ => cellPosition,
	};

	protected override string SiteGlyph => "cell";
	protected override float SiteFootprint => 110f;
	protected override float ArrivalRadius => 460f;
	protected override Color TellColor => new Color(0.42f, 0.72f, 0.91f);   // arcane cyan: his mark

	// The locked cell is the site: placed at stage start, out in the maze, and waiting
	// (SideEvent). Arriving at it is what wakes the Warden with the key.
	protected override void OnPlaced()
	{
		cellPosition = MainSite;
	}

	protected override void Begin()
	{
		cellPosition = MainSite;
		// The Warden wakes on the far side of the cell from the player, so the hunt goes past the
		// door he has just found and the carry comes back to it - the route home still matters.
		Vector2 from = Player.GlobalPosition;
		Vector2 beyond = cellPosition - from;
		Vector2 heading = beyond.LengthSquared() > 1f ? beyond.Normalized() : Vector2.Right;
		Vector2 wardenSite = Game.ReachablePoint(cellPosition + heading * WardenBeyondCell);
		if (wardenSite.DistanceTo(from) < 500f || wardenSite.DistanceTo(cellPosition) < 400f)
			wardenSite = Game.FindEventSite(700f, 1000f, new[] { cellPosition }, 500f);

		// The chapter's own jailer (StageBestiary): the Warden, whose dungeon this is.
		warden = Game.SpawnEventEnemy(BestiaryRole.MiniBoss, wardenSite, 9f, true);
		wardenLastSeen = warden != null ? warden.GlobalPosition : wardenSite;
	}

	// How far past the cell the Warden wakes. The old carry crossed about 1500 px; this is about the
	// same from where a player arriving at the cell usually stands.
	private const float WardenBeyondCell = 800f;

	protected override void Tick(float delta)
	{
		// The beat: the lock has turned - the door swings and holds before the reward comes.
		if (winBeat >= 0f)
		{
			winBeat += delta;
			if (winBeat >= WinTurnSeconds + WinSwingSeconds + WinHoldSeconds)
				Succeed(winSpot, exact: true);
			return;
		}

		switch (step)
		{
			case Step.HuntWarden:
				if (!IsGone(warden))
				{
					wardenLastSeen = warden.GlobalPosition;
					break;
				}
				keyPosition = Game.ReachablePoint(wardenLastSeen);
				step = Step.KeyOnGround;
				break;

			case Step.KeyOnGround:
				if (PlayerWithin(keyPosition, PickupRadius))
				{
					step = Step.Carrying;
					hitsWhileCarrying = 0;
					Listen(true);
				}
				break;

			case Step.Carrying:
				if (PlayerWithin(cellPosition, CellRadius))
				{
					unlockProgress += delta / UnlockSeconds;
					if (unlockProgress >= 1f)
					{
						Listen(false);
						winSpot = ChooseRewardSpot(cellPosition);
						winBeat = 0f;
						// The lock's own cue as the key turns; the reward's sound still comes with the column.
						SfxPlayer.AtPosition(SfxCatalog.SecretSeen, cellPosition, 0f);
					}
				}
				else
				{
					unlockProgress = Mathf.Max(0f, unlockProgress - delta);
				}
				break;
		}
	}

	// DamageTaken fires after the dodge roll, so a dodged hit does not shake the key loose.
	private void OnPlayerDamaged(int amount)
	{
		// Once the lock has turned the key is spent: a hit during the beat cannot shake it loose.
		if (step != Step.Carrying || IsResolved || winBeat >= 0f)
			return;
		hitsWhileCarrying++;
		if (hitsWhileCarrying < HitsToDrop)
			return;

		keyPosition = Player.GlobalPosition;
		step = Step.KeyOnGround;
		unlockProgress = 0f;
		Listen(false);
	}

	private void Listen(bool on)
	{
		if (Player == null || on == listening)
			return;
		var callable = new Callable(this, nameof(OnPlayerDamaged));
		if (on)
			Player.Connect(Player.SignalName.DamageTaken, callable);
		else if (Player.IsConnected(Player.SignalName.DamageTaken, callable))
			Player.Disconnect(Player.SignalName.DamageTaken, callable);
		listening = on;
	}

	public override void _ExitTree()
	{
		Listen(false);
		base._ExitTree();
	}

	protected override void OnStoodDown() => Listen(false);

	private static readonly Color Iron = new Color(0.34f, 0.36f, 0.42f);
	private static readonly Color IronLit = new Color(0.62f, 0.64f, 0.70f);
	private static readonly Color Brass = new Color(0.95f, 0.78f, 0.36f);
	private static readonly Color Gloom = new Color(0.03f, 0.03f, 0.05f, 0.92f);

	public override void _Draw()
	{
		if (IsResolved)
			return;

		// The beat's two movements: the key's turn, then the gate's swing (eased, so it lands softly).
		float turn = winBeat < 0f ? 0f : Mathf.Clamp(winBeat / WinTurnSeconds, 0f, 1f);
		float open = winBeat < 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp((winBeat - WinTurnSeconds) / WinSwingSeconds, 0f, 1f));

		// The cell: a dark recess behind bars, with the shape of someone sitting inside. As the door
		// opens the gloom lightens and the sitter is gone - carried out, not kept.
		Vector2 c = ToLocal(cellPosition);
		DrawRect(new Rect2(c - new Vector2(30f, 34f), new Vector2(60f, 60f)), Gloom.Lerp(new Color(0.15f, 0.18f, 0.24f, 0.45f), open));
		if (open < 1f)
		{
			Color sitter = new Color(0.16f, 0.15f, 0.20f, 1f - open);
			DrawCircle(c + new Vector2(0f, -6f), 6f, sitter);
			DrawRect(new Rect2(c + new Vector2(-8f, 0f), new Vector2(16f, 20f)), sitter);
		}
		if (open <= 0f)
		{
			for (int i = 0; i < 6; i++)
			{
				float x = -27f + i * 10.8f;
				DrawLine(c + new Vector2(x, -34f), c + new Vector2(x, 26f), Iron, 3f);
				DrawLine(c + new Vector2(x - 1f, -34f), c + new Vector2(x - 1f, 26f), IronLit, 1f);
			}
		}
		else
		{
			// The gate of six bars and its two rails swings on its bottom-left hinge, out and clear of
			// the doorway, and lies open there through the hold.
			Vector2 hinge = c + new Vector2(-32f, 26f);
			float angle = -1.45f * open;
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);
			Vector2 Swing(Vector2 offset) => hinge + new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
			for (int i = 0; i < 6; i++)
			{
				float x = -27f + i * 10.8f + 32f;
				DrawLine(Swing(new Vector2(x, -60f)), Swing(new Vector2(x, 0f)), Iron, 3f);
			}
			DrawLine(Swing(new Vector2(5f, -60f)), Swing(new Vector2(59f, -60f)), IronLit, 2f);
			DrawLine(Swing(new Vector2(5f, 0f)), Swing(new Vector2(59f, 0f)), IronLit, 2f);
		}
		DrawRect(new Rect2(c - new Vector2(32f, 36f), new Vector2(64f, 64f)), Iron, false, 3f);
		if (step == Step.Carrying && winBeat < 0f)
			DrawProgressArc(cellPosition, CellRadius, unlockProgress, Brass);

		if (winBeat >= 0f && open <= 0f)
		{
			// The key in the lock, turning, before anything moves.
			DrawSetTransform(c + new Vector2(24f, -4f), -1.3f * turn, Vector2.One);
			DrawKey(Vector2.Zero);
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		}
		else if (step == Step.KeyOnGround)
			DrawKey(ToLocal(keyPosition));
		else if (step == Step.Carrying && winBeat < 0f && Player != null)
			// Clear of the hat: the event draws under the actors now (SideEvent), so a key at the
			// crown of his head would be behind it.
			DrawKey(ToLocal(Player.GlobalPosition) + new Vector2(0f, -64f));
	}

	// The far tell while it waits (base). Once the door swings, the cell's own light spills out of
	// the opening: what he kept in the dark is lit on its way out.
	protected override void DrawLight(CanvasItem light)
	{
		base.DrawLight(light);
		if (winBeat < 0f || IsResolved || !NearPlayer(cellPosition))
			return;
		float open = Mathf.Clamp((winBeat - WinTurnSeconds) / WinSwingSeconds, 0f, 1f);
		if (open <= 0f)
			return;
		Vector2 c = ToLocal(cellPosition);
		ExploreArt.DrawGlow(light, c + new Vector2(0f, -4f), 50f + 70f * open, new Color(TellColor, 0.22f * open));
		ExploreArt.DrawShaft(light, c + new Vector2(0f, 26f), 14f, 150f * open, new Color(TellColor, 0.30f * open));
	}

	/// <summary>
	/// _UiShot --win-event: the key already in hand at the cell door, so the real unlock runs - the
	/// fill, the key's turn, the gate's swing - instead of Succeed called straight (the Warden it
	/// would have taken the key from simply never wakes to miss it).
	/// </summary>
	public void PoseCarryForPhoto()
	{
		if (IsResolved || winBeat >= 0f)
			return;
		step = Step.Carrying;
		hitsWhileCarrying = 0;
		Listen(true);
	}

	private void DrawKey(Vector2 at)
	{
		DrawCircle(at + new Vector2(-6f, 0f), 5f, Brass);
		DrawCircle(at + new Vector2(-6f, 0f), 2.2f, Gloom);
		DrawLine(at + new Vector2(-1f, 0f), at + new Vector2(10f, 0f), Brass, 3f);
		DrawLine(at + new Vector2(7f, 0f), at + new Vector2(7f, 4f), Brass, 2f);
		DrawLine(at + new Vector2(10f, 0f), at + new Vector2(10f, 5f), Brass, 2f);
	}
}
