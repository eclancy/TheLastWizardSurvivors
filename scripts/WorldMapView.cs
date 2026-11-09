using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The drawn world on the world map: the picture, the fog over held places, the stand-point rings,
/// and the selected wizard walking the road, under a camera that leads him. The Sketchbook's page is
/// not on it: that is a sheet of its own (<see cref="WorldMapPage"/>, .ai/world-map.md section 4).
/// </summary>
/// <remarks>
/// WHOLE SCREEN PIXELS (.ai/art-direction.md 6a). Everything here is drawn by this one control at a
/// whole number of SCREEN pixels per art pixel - the nearest whole number to x2 on this window - and
/// every position is snapped to a screen pixel, the rule PixelArtRect follows for one sprite. A
/// Camera2D over a world node would be the engine's way, and it would put the map and the wizard on
/// the canvas's fractional grid on any window that is not 1:1 with it.
///
/// THE WIZARD IS ONE NUMBER: his arc length along the road (.ai/world-map.md, section 3). The
/// selection sets a target arc length and he moves toward it along the road every frame, so he can
/// never teleport, a new target mid-walk turns him where he stands, and his animation is chosen
/// every frame from whether he has arrived - it cannot stick.
///
/// THE CAMERA LEADS ON A LEASH. It eases toward the selected place's focus and is then clamped so the
/// wizard stays inside the view: on a long jump it runs ahead until he reaches the frame's edge and
/// then drags him along. Nothing per frame allocates.
/// </remarks>
public partial class WorldMapView : Control
{
	/// <summary>Art pixels are drawn at this many canvas units on a 1:1 window (x2, the project's one render scale).</summary>
	public const int ArtScale = 2;

	/// <summary>The slowest the wizard runs, in art pixels a second - a brisk run between neighbours.</summary>
	public const float MinSpeed = 150f;
	/// <summary>The fastest, so a jump across the whole world is a run of about two seconds.</summary>
	public const float MaxSpeed = 1100f;
	/// <summary>Speed per art pixel left to go: the further the target, the faster he runs.</summary>
	public const float SpeedPerDistance = 3.0f;
	/// <summary>How quickly the camera closes on its target, per second (exponential).</summary>
	public const float CameraRate = 7f;

	public enum StopLook
	{
		Open,
		Locked,
		Unbuilt,
	}

	public sealed class ViewStop
	{
		public WorldMapStop Layout { get; init; }
		public StopLook Look { get; set; }
		/// <summary>
		/// Shown, but not on the save's road (a chapter an older order opened, the grandfathered Cursed
		/// Dungeon): its place is lit as an island, and the road to it stays dark (BuildMask).
		/// </summary>
		public bool OffRoad { get; init; }
	}

	private WorldMapLayout layout;
	private ViewStop[] stops = Array.Empty<ViewStop>();
	private Texture2D[] shrouds = Array.Empty<Texture2D>();
	private Texture2D world;
	private Texture2D ringOpen;
	private Texture2D ringLocked;
	private Texture2D ringCurrent;
	private Texture2D shadow;
	private Texture2D lockGlyph;

	private Figure figure = new();

	private int selected;
	private float wizardS;
	private float targetS;
	private bool facingLeft;
	private double animClock;
	private Vector2 camera;
	private Vector2 lastWizardPosition;
	private float refuseClock;

	// The transform of the last draw, for hit-testing a tap.
	private Vector2 lastOrigin;
	private float lastUnitsPerArt = ArtScale;

	/// <summary>A stop was tapped or clicked on the map.</summary>
	public event Action<int> StopPressed;

	public int Selected => selected;
	public bool IsWalking => Mathf.Abs(targetS - wizardS) > 0.5f;
	public float WizardSpeed { get; private set; }
	public Vector2 WizardPosition => layout?.PointAt(wizardS) ?? Vector2.Zero;
	public Vector2 Camera => camera;
	/// <summary>Screen pixels per art pixel at the last draw. For the harness.</summary>
	public int ScreenPixelsPerArtPixel { get; private set; }

	public WorldMapView()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		ClipContents = true;
		MouseFilter = MouseFilterEnum.Stop;
	}

	/// <param name="revealFromArc">
	/// Where the lit road ended at the last visit, when a stop has been revealed since; the dark then draws
	/// back from there to the new frontier (<see cref="RevealSeconds"/>). Below zero: no reveal.
	/// </param>
	public void Setup(WorldMapLayout map, IReadOnlyList<ViewStop> viewStops, CharacterData character, int startIndex,
		float revealFromArc = -1f)
	{
		layout = map;
		stops = new ViewStop[viewStops.Count];
		shrouds = new Texture2D[viewStops.Count];
		for (int i = 0; i < viewStops.Count; i++)
		{
			stops[i] = viewStops[i];
			string image = viewStops[i].Layout.ShroudImage;
			shrouds[i] = string.IsNullOrEmpty(image) ? null : Load(WorldMapLayout.Directory + image);
		}
		world = Load(WorldMapLayout.Directory + "world.png");
		ringOpen = Load(WorldMapLayout.Directory + "ring-open.png");
		ringLocked = Load(WorldMapLayout.Directory + "ring-locked.png");
		ringCurrent = Load(WorldMapLayout.Directory + "ring-current.png");
		shadow = Load(WorldMapLayout.Directory + "shadow.png");
		lockGlyph = StatIcons.Load("lock");
		figure = Figure.Of(character);

		selected = Mathf.Clamp(startIndex, 0, Math.Max(0, stops.Length - 1));
		wizardS = targetS = stops.Length > 0 ? stops[selected].Layout.ArcLength : 0f;
		lastWizardPosition = map.PointAt(wizardS);
		camera = stops.Length > 0 ? stops[selected].Layout.Focus : map.Size / 2f;
		BuildRevealMasks(revealFromArc);
		// Placed, not walked: the wizard opens standing at his stop and the camera on it.
		CallDeferred(nameof(SnapCamera));
	}

	internal static Texture2D Load(string path)
	{
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
		{
			GD.PushError($"WorldMapView: '{path}' is missing or not imported.");
			return null;
		}
		return GD.Load<Texture2D>(path);
	}

	/// <summary>
	/// The selected wizard as the map draws him: his idle and moving frames, their speeds, his tint and
	/// which way his sheet faces. Shared with the Sketchbook's page (<see cref="WorldMapPage"/>), where he
	/// stands on the paper.
	/// </summary>
	internal sealed class Figure
	{
		public Texture2D[] Idle = Array.Empty<Texture2D>();
		public Texture2D[] Moving = Array.Empty<Texture2D>();
		public float IdleFps = 6f;
		public float MovingFps = 10f;
		public bool SheetFacesLeft;
		public Color Tint = Colors.White;

		public static Figure Of(CharacterData character)
		{
			var f = new Figure
			{
				Tint = CharacterVisuals.GetCharacterTint(character?.Id),
				SheetFacesLeft = CharacterVisuals.SpriteFacesLeft(character?.Id),
			};
			SpriteFrames frames = character?.Frames;
			if (frames != null && frames.HasAnimation("idle"))
			{
				f.Idle = FramesOf(frames, "idle");
				f.IdleFps = (float)frames.GetAnimationSpeed("idle");
				if (frames.HasAnimation("moving"))
				{
					f.Moving = FramesOf(frames, "moving");
					f.MovingFps = (float)frames.GetAnimationSpeed("moving");
				}
			}
			else if (character != null)
			{
				// No sheet of his own (the Test Wizard, whose sprite is Eric's drawing and is never derived):
				// the idle built from the portrait, played quicker while he walks.
				f.Idle = CharacterVisuals.GetIdleFrames(character.Portrait).ToArray();
				f.IdleFps = CharacterVisuals.IdleFramesPerSecond;
			}
			if (f.Moving.Length == 0)
			{
				f.Moving = f.Idle;
				f.MovingFps = f.IdleFps * 2f;
			}
			return f;
		}

		private static Texture2D[] FramesOf(SpriteFrames frames, string animation)
		{
			int count = frames.GetFrameCount(animation);
			var result = new Texture2D[count];
			for (int i = 0; i < count; i++)
				result[i] = frames.GetFrameTexture(animation, i);
			return result;
		}
	}

	public void SetLook(int index, StopLook look)
	{
		if (index >= 0 && index < stops.Length)
			stops[index].Look = look;
	}

	// --- the light going home ----------------------------------------------------------------------
	// .ai/second-half.md 6.2: once the Throne is taken back, "the tower's dark light is out and warm motes stream
	// east off it over the dark sea". The picture has the tower burning; the generator also draws it unlit, cut
	// down to where the two differ (tower-unlit.png, WorldMapLayout.TowerUnlitRect), and that is laid over the
	// world here - the light going out is a picture, never a tint. The motes are the ending's, the light going
	// home: single art pixels of the light ramp, opaque, on the art grid, streaming along the path the generator
	// wrote (WorldMapLayout.MotePath) from his crown out over the sea and on east over the far shore - over the
	// dark too, past whatever the save has been shown, because that is where the light is going. No allocation per
	// frame: every mote is a few numbers fixed when the map opens.
	private const int MoteCount = 120;
	private const float MoteSpeed = 30f;
	private Texture2D towerUnlit;
	private bool lightGoneHome;
	private float[] motePathArc = Array.Empty<float>();
	private readonly float[] motePhase = new float[MoteCount];
	private readonly float[] moteReach = new float[MoteCount];
	private readonly float[] moteRate = new float[MoteCount];
	private readonly float[] moteSway = new float[MoteCount];
	private Color[] moteColors = Array.Empty<Color>();
	private double moteClock;

	/// <summary>True once the light going home is drawn (a save that has taken back the Throne). For the harness.</summary>
	public bool LightGoneHome => lightGoneHome;

	/// <summary>
	/// After Taken Back: his tower unlit and the light streaming east off it (.ai/second-half.md 6.2). Call after
	/// <see cref="Setup"/>; a layout from before the far shore, with no unlit tower in it, keeps the tower as drawn.
	/// </summary>
	public void ShowLightGoneHome()
	{
		if (layout == null || string.IsNullOrEmpty(layout.TowerUnlitImage) || layout.MotePath.Length < 2)
			return;
		towerUnlit = Load(WorldMapLayout.Directory + layout.TowerUnlitImage);
		Vector2[] path = layout.MotePath;
		motePathArc = new float[path.Length];
		for (int i = 1; i < path.Length; i++)
			motePathArc[i] = motePathArc[i - 1] + path[i].DistanceTo(path[i - 1]);
		// Seeded, so the stream is the same every time the map opens (and in every photograph of it).
		var rng = new Random(6200);
		for (int i = 0; i < MoteCount; i++)
		{
			motePhase[i] = (float)rng.NextDouble();
			// Most go out over the sea and the first cliffs; a few go on to the far end of the shore.
			float reach = (float)rng.NextDouble();
			moteReach[i] = 0.18f + 0.82f * reach * reach;
			moteRate[i] = 0.75f + 0.5f * (float)rng.NextDouble();
			moteSway[i] = ((float)rng.NextDouble() - 0.5f) * 2f;
		}
		ElementColors.Ramp ramp = ElementColors.GetRamp(Element.Light);
		moteColors = new[] { ramp.Core, ramp.Hot, ramp.Mid, ramp.Edge };
		lightGoneHome = true;
	}

	private Vector2 MotePoint(float s, out Vector2 normal)
	{
		Vector2[] path = layout.MotePath;
		int i = 0;
		while (i < path.Length - 2 && motePathArc[i + 1] < s)
			i++;
		float span = Mathf.Max(0.001f, motePathArc[i + 1] - motePathArc[i]);
		Vector2 along = (path[i + 1] - path[i]) / span;
		normal = new Vector2(-along.Y, along.X);
		return path[i] + along * Mathf.Clamp(s - motePathArc[i], 0f, span);
	}

	// The stream, from the crown east: each mote runs out to its own reach and starts again at the crown, swaying off
	// the path a little more the further it has gone, cooling down the ramp as it goes - white-hot off the tower,
	// the gold of the light's edge far out over the shore.
	private void DrawMotes(Vector2 origin, float u, Transform2D toScreen, Transform2D fromScreen)
	{
		if (!lightGoneHome || motePathArc.Length < 2)
			return;
		float length = motePathArc[^1];
		for (int i = 0; i < MoteCount; i++)
		{
			float reach = moteReach[i] * length;
			float s = (float)((moteClock * MoteSpeed * moteRate[i] + motePhase[i] * reach) % reach);
			float t = s / length;
			Vector2 at = MotePoint(s, out Vector2 normal);
			float spread = 2f + t * 18f;
			Vector2 sway = normal * (moteSway[i] * spread + Mathf.Sin((float)moteClock * 0.8f + i * 1.7f) * (1f + t * 3f));
			at = (at + sway).Floor();
			int shade = Mathf.Clamp((int)(t * 4.2f + (i % 3) * 0.6f), 0, 3);
			// Its streak, the way it came: one or two art pixels behind it on the path, the ramp's gold edge - so the
			// stream reads as going, not as a dotted line of stars.
			int streak = t < 0.5f ? 2 : 1;
			for (int k = 1; k <= streak; k++)
			{
				Vector2 behind = (MotePoint(Mathf.Max(0f, s - k * 2f), out _) + sway).Floor();
				if (behind == at)
					continue;
				Vector2 tail = fromScreen * (toScreen * (origin + behind * u)).Round();
				DrawRect(new Rect2(tail, new Vector2(u, u)), moteColors[3]);
			}
			Vector2 topLeft = fromScreen * (toScreen * (origin + at * u)).Round();
			float side = i % 3 == 0 && t < 0.3f ? 2f : 1f;
			DrawRect(new Rect2(topLeft, new Vector2(u * side, u * side)), moteColors[shade]);
		}
	}

	/// <summary>Makes <paramref name="index"/> the selected stop. The wizard walks to it.</summary>
	public void Select(int index)
	{
		if (stops.Length == 0)
			return;
		selected = Mathf.Clamp(index, 0, stops.Length - 1);
		targetS = stops[selected].Layout.ArcLength;
	}

	/// <summary>A refused start: the view gives a short shake, so the press visibly landed.</summary>
	public void Refuse() => refuseClock = 0.3f;

	/// <summary>
	/// Where the wizard is among the stops: 2.5 is halfway from the third to the fourth. What the
	/// compass strip slides by.
	/// </summary>
	public float WizardStopProgress
	{
		get
		{
			if (stops.Length == 0)
				return 0f;
			for (int i = 0; i < stops.Length - 1; i++)
			{
				float a = stops[i].Layout.ArcLength;
				float b = stops[i + 1].Layout.ArcLength;
				if (wizardS <= b || i == stops.Length - 2)
				{
					if (wizardS <= a)
						return i;
					return i + Mathf.Clamp((wizardS - a) / Mathf.Max(0.001f, b - a), 0f, 1f);
				}
			}
			return 0f;
		}
	}

	public override void _Process(double delta)
	{
		if (layout == null)
			return;
		float dt = (float)delta;

		// THE WALK. Speed grows with the distance left, so five quick presses are one run, not five
		// queued walks, and a jump across the world is still a run along the road.
		float remaining = targetS - wizardS;
		if (Mathf.Abs(remaining) > 0.01f)
		{
			WizardSpeed = Mathf.Clamp(Mathf.Abs(remaining) * SpeedPerDistance, MinSpeed, MaxSpeed);
			float step = WizardSpeed * dt;
			wizardS = Mathf.Abs(remaining) <= step ? targetS : wizardS + Mathf.Sign(remaining) * step;
		}
		else
		{
			wizardS = targetS;
			WizardSpeed = 0f;
		}
		Vector2 position = layout.PointAt(wizardS);
		float dx = position.X - lastWizardPosition.X;
		if (Mathf.Abs(dx) > 0.05f)
			facingLeft = dx < 0f;
		lastWizardPosition = position;
		animClock += delta * (IsWalking ? Mathf.Clamp(WizardSpeed / MinSpeed, 1f, 2.5f) : 1f);
		moteClock += delta;

		// THE CAMERA: toward the selected place, inside the drawn world, and never without him.
		if (stops.Length > 0)
		{
			Vector2 target = stops[selected].Layout.Focus;
			float k = 1f - Mathf.Exp(-CameraRate * dt);
			camera = camera.Lerp(target, k);
		}
		camera = Constrain(camera);
		if (refuseClock > 0f)
			refuseClock = Mathf.Max(0f, refuseClock - dt);
		if (revealClock >= 0f)
		{
			revealClock += dt;
			if (revealClock >= RevealDelay + RevealSeconds)
			{
				revealClock = -1f;
				maskBefore = null;
			}
		}

		QueueRedraw();
	}

	/// <summary>Jumps the camera onto its target at once. The opening frame, and the harness.</summary>
	public void SnapCamera()
	{
		if (stops.Length > 0)
			camera = Constrain(stops[selected].Layout.Focus);
		QueueRedraw();
	}

	private Vector2 Constrain(Vector2 at)
	{
		if (layout == null)
			return at;
		Vector2 half = HalfViewInArt();
		// Inside the drawn world.
		float minX = half.X;
		float maxX = layout.RightEdge - half.X;
		at.X = minX > maxX ? layout.RightEdge / 2f : Mathf.Clamp(at.X, minX, maxX);
		float minY = half.Y;
		float maxY = layout.Size.Y - half.Y;
		at.Y = minY > maxY ? layout.Size.Y / 2f : Mathf.Clamp(at.Y, minY, maxY);
		// The leash, last, so it wins: his whole figure stays in the frame with room to spare.
		Vector2 body = layout.PointAt(wizardS) + new Vector2(0f, -20f);
		float ax = Mathf.Max(0f, half.X - 40f);
		float ay = Mathf.Max(0f, half.Y - 44f);
		at.X = Mathf.Clamp(at.X, body.X - ax, body.X + ax);
		at.Y = Mathf.Clamp(at.Y, body.Y - ay, body.Y + ay);
		return at;
	}

	/// <summary>Half the view, in art pixels, at the scale the next draw will use.</summary>
	private Vector2 HalfViewInArt()
	{
		float units = UnitsPerArt(out _, out _);
		return Size / 2f / Mathf.Max(0.001f, units);
	}

	/// <summary>Canvas units per art pixel: the whole number of screen pixels nearest x2, in canvas units.</summary>
	private float UnitsPerArt(out Transform2D toScreen, out int screenPixels)
	{
		toScreen = GetViewport().GetFinalTransform() * GetGlobalTransformWithCanvas();
		float screenPerUnit = toScreen.X.Length();
		if (screenPerUnit <= 0.0001f)
			screenPerUnit = 1f;
		screenPixels = Math.Max(1, (int)Math.Round(ArtScale * screenPerUnit, MidpointRounding.AwayFromZero));
		return screenPixels / screenPerUnit;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), new Color("#05070C"));
		if (layout == null || world == null)
			return;

		float u = UnitsPerArt(out Transform2D toScreen, out int screenPixels);
		ScreenPixelsPerArtPixel = screenPixels;
		Transform2D fromScreen = toScreen.AffineInverse();
		Vector2 shake = refuseClock > 0f ? new Vector2(Mathf.Sin(refuseClock * 60f) * 3f * refuseClock / 0.3f, 0f) : Vector2.Zero;
		Vector2 origin = Size / 2f - (camera + shake) * u;
		origin = fromScreen * (toScreen * origin).Round();
		lastOrigin = origin;
		lastUnitsPerArt = u;

		Vector2 At(Vector2 art) => fromScreen * (toScreen * (origin + art * u)).Round();
		void Blit(Texture2D texture, Vector2 artTopLeft, Color? modulate = null)
		{
			if (texture == null)
				return;
			DrawTextureRect(texture, new Rect2(At(artTopLeft), texture.GetSize() * u), false, modulate ?? Colors.White);
		}

		DrawTextureRect(world, new Rect2(origin, world.GetSize() * u), false);
		// After Taken Back, his tower as it stands now: unlit (ShowLightGoneHome).
		if (lightGoneHome && towerUnlit != null)
			Blit(towerUnlit, layout.TowerUnlitRect.Position);
		// The dark over everything past the revealed road; during a reveal, the old dark fading off it.
		if (mask != null)
			DrawTextureRect(mask, new Rect2(origin, world.GetSize() * u), false);
		if (maskBefore != null && revealClock >= 0f)
		{
			float t = Mathf.Clamp((revealClock - RevealDelay) / RevealSeconds, 0f, 1f);
			DrawTextureRect(maskBefore, new Rect2(origin, world.GetSize() * u), false, new Color(1f, 1f, 1f, 1f - t * t * (3f - 2f * t)));
		}

		// His fog over the places he still holds, and the lock above each. UI icons are saved x2, so
		// a 24-pixel glyph is 12 art pixels.
		for (int i = 0; i < stops.Length; i++)
		{
			if (stops[i].Look == StopLook.Open || shrouds[i] == null)
				continue;
			Vector2 size = shrouds[i].GetSize();
			Vector2 focus = stops[i].Layout.Focus;
			Blit(shrouds[i], focus - size / 2f);
			if (stops[i].Look == StopLook.Unbuilt)
				Blit(shrouds[i], focus - size / 2f + new Vector2(3f, -2f));
			if (lockGlyph != null)
			{
				Vector2 glyph = lockGlyph.GetSize() / 2f;
				DrawTextureRect(lockGlyph, new Rect2(At(focus - glyph / 2f - new Vector2(0f, 6f)), glyph * u), false);
			}
		}

		for (int i = 0; i < stops.Length; i++)
		{
			Texture2D ring = i == selected ? ringCurrent : stops[i].Look == StopLook.Open ? ringOpen : ringLocked;
			if (ring != null)
				Blit(ring, stops[i].Layout.Stand - ring.GetSize() / 2f + new Vector2(0f, 1f));
		}

		// The light going home, over the land and the dark alike, under the wizard.
		DrawMotes(origin, u, toScreen, fromScreen);

		// The wizard: his shadow, then him, feet on the road.
		Vector2 feet = layout.PointAt(wizardS);
		if (shadow != null)
			Blit(shadow, feet - new Vector2(shadow.GetWidth() / 2f, 3f));
		Texture2D[] frames = IsWalking ? figure.Moving : figure.Idle;
		float fps = IsWalking ? figure.MovingFps : figure.IdleFps;
		DrawFigure(this, frames, fps, animClock, figure, facingLeft, feet, origin, u, toScreen, fromScreen);
	}

	/// <summary>
	/// The wizard with his feet at <paramref name="feetArt"/> - row cell-2, the contract's anchor
	/// (.ai/art-direction.md, section 5) - drawn from <paramref name="origin"/> at <paramref name="u"/>
	/// canvas units per art pixel, his top-left snapped to a screen pixel, mirrored when he faces
	/// against his sheet. No allocation: the map draws him every frame.
	/// </summary>
	internal static void DrawFigure(CanvasItem canvas, Texture2D[] frames, float fps, double clock, Figure figure,
		bool facingLeft, Vector2 feetArt, Vector2 origin, float u, Transform2D toScreen, Transform2D fromScreen)
	{
		if (frames.Length == 0)
			return;
		Texture2D sprite = frames[(int)(clock * Math.Max(1f, fps)) % frames.Length];
		if (sprite == null)
			return;
		Vector2 cell = sprite.GetSize();
		Vector2 topLeftArt = feetArt - new Vector2(cell.X / 2f, cell.Y - 2f);
		Vector2 topLeft = fromScreen * (toScreen * (origin + topLeftArt * u)).Round();
		Vector2 drawn = cell * u;
		if (facingLeft != figure.SheetFacesLeft)
		{
			canvas.DrawSetTransform(topLeft + new Vector2(drawn.X, 0f), 0f, new Vector2(-1f, 1f));
			canvas.DrawTextureRect(sprite, new Rect2(Vector2.Zero, drawn), false, figure.Tint);
			canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		}
		else
		{
			canvas.DrawTextureRect(sprite, new Rect2(topLeft, drawn), false, figure.Tint);
		}
	}

	// --- the dark past the revealed road ---------------------------------------------------------
	// Eric, 2026-10-31: "you should only unlock the map as you defeat the previous level." The world
	// picture has every place and the whole road in it, so what is not yet revealed is covered: the
	// screen's own ground colour over whatever lies nearer the unrevealed road and places than the
	// revealed ones (BuildMask). Its edge is dithered on the art's
	// own 2-pixel grain, the generator's edge falloff in reverse. Built once when the map opens - a
	// coarse distance field, then a half-resolution mask - and drawn over the world at its scale.
	private const float RevealDelay = 0.5f;
	private const float RevealSeconds = 1.4f;
	// Art pixels of "how much nearer the unrevealed is": fully lit below Inner, fully dark past Outer,
	// dithered between - a band about thirty pixels wide where the two meet.
	private const float RevealInner = -12f;
	private const float RevealOuter = 28f;
	private const float PlaceRadiusFloor = 64f;
	private static readonly Color Dark = new("#05070C");
	private static readonly float[] Bayer4 =
	{
		0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f,
		12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f,
		3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f,
		15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f,
	};

	private ImageTexture mask;
	private ImageTexture maskBefore;
	private float revealClock = -1f;

	/// <summary>True while the dark is drawing back from a newly revealed place. For the harness.</summary>
	public bool IsRevealing => revealClock >= 0f;

	private void BuildRevealMasks(float revealFromArc)
	{
		mask = null;
		maskBefore = null;
		revealClock = -1f;
		if (layout == null || stops.Length == 0)
			return;
		// The road is lit as far as the last stop on it. A stop off the road (ViewStop.OffRoad) does not
		// carry the frontier with it - that would light every place between, shown or not - but its own
		// place is lit, an island beyond the dark (.ai/endgame-castle.md 8, "an open stop beyond fogged ones").
		float frontier = 0f;
		var islands = new List<WorldMapStop>();
		foreach (ViewStop stop in stops)
		{
			if (stop.OffRoad)
				islands.Add(stop.Layout);
			else
				frontier = Mathf.Max(frontier, stop.Layout.ArcLength);
		}
		mask = BuildMask(frontier, islands);
		if (revealFromArc >= 0f && revealFromArc < frontier - 0.5f)
		{
			maskBefore = BuildMask(revealFromArc, islands);
			revealClock = 0f;
		}
	}

	// The mask for a road lit as far as frontier. The land is split between what is revealed - the road
	// up to the frontier and the places at or before it - and what is not - the road past it and every
	// place past it, shown or not - and a point is dark when it lies nearer the second than the first. So
	// a finished campaign is lit everywhere, a fresh one only round the Forest, and the road always
	// darkens just past the last place shown. An island's place is lit wherever it lies.
	private ImageTexture BuildMask(float frontier, List<WorldMapStop> islands)
	{
		const int cell = 8;
		int gw = (int)Mathf.Ceil(layout.Size.X / cell) + 1;
		int gh = (int)Mathf.Ceil(layout.Size.Y / cell) + 1;
		var litRoad = new List<Vector2>();
		var darkRoad = new List<Vector2>();
		for (int i = 0; i < layout.Route.Length; i += 2)
			(layout.Arc[i] <= frontier + 0.5f ? litRoad : darkRoad).Add(layout.Route[i]);
		var litPlaces = new List<(Vector2 At, float Radius)>();
		var darkPlaces = new List<(Vector2 At, float Radius)>();
		foreach (WorldMapStop stop in layout.Stops)
		{
			float radius = Mathf.Max(PlaceRadiusFloor, Mathf.Max(stop.Shroud.X, stop.Shroud.Y));
			bool lit = stop.ArcLength <= frontier + 0.5f || islands.Exists(island => island.Id == stop.Id);
			(lit ? litPlaces : darkPlaces).Add((stop.Focus, radius));
			// The island's stand point too, so the wizard waiting at its door is never in the dark.
			if (lit && stop.ArcLength > frontier + 0.5f)
				litPlaces.Add((stop.Stand, PlaceRadiusFloor * 0.5f));
		}
		if (darkRoad.Count == 0 && darkPlaces.Count == 0)
			return null;

		// How much nearer the unrevealed is than the revealed, at each coarse node, in art pixels (RoadSearch: the
		// same distances as a search of every sample, at about a third of the cost).
		var litSearch = new RoadSearch(litRoad, litPlaces);
		var darkSearch = new RoadSearch(darkRoad, darkPlaces);
		var field = new float[gw * gh];
		for (int y = 0; y < gh; y++)
		{
			for (int x = 0; x < gw; x++)
				field[y * gw + x] = litSearch.Nearest(x * cell, y * cell) - darkSearch.Nearest(x * cell, y * cell);
		}

		// The mask at half the art's resolution: each texel is two art pixels, the map's grain.
		int mw = (int)layout.Size.X / 2;
		int mh = (int)layout.Size.Y / 2;
		var bytes = new byte[mw * mh * 4];
		byte r8 = (byte)Mathf.RoundToInt(Dark.R * 255f), g8 = (byte)Mathf.RoundToInt(Dark.G * 255f), b8 = (byte)Mathf.RoundToInt(Dark.B * 255f);
		for (int y = 0; y < mh; y++)
		{
			float fy = (y * 2f + 1f) / cell;
			int y0 = Math.Min(gh - 2, (int)fy);
			float ty = fy - y0;
			for (int x = 0; x < mw; x++)
			{
				float fx = (x * 2f + 1f) / cell;
				int x0 = Math.Min(gw - 2, (int)fx);
				float tx = fx - x0;
				float top = Mathf.Lerp(field[y0 * gw + x0], field[y0 * gw + x0 + 1], tx);
				float bottom = Mathf.Lerp(field[(y0 + 1) * gw + x0], field[(y0 + 1) * gw + x0 + 1], tx);
				float t = (Mathf.Lerp(top, bottom, ty) - RevealInner) / (RevealOuter - RevealInner);
				if (t <= 0f || t < Bayer4[(y & 3) * 4 + (x & 3)])
					continue;
				int o = (y * mw + x) * 4;
				bytes[o] = r8;
				bytes[o + 1] = g8;
				bytes[o + 2] = b8;
				bytes[o + 3] = 255;
			}
		}
		return ImageTexture.CreateFromImage(Image.CreateFromData(mw, mh, false, Image.Format.Rgba8, bytes));
	}

	/// <summary>
	/// The distance from a point to the nearest of a road's samples or a place's ring (negative inside it), as the
	/// reveal's field reads it: <c>lit.Nearest(p) - dark.Nearest(p)</c>. The samples are kept in runs of
	/// <see cref="RunLength"/> along the road with the circle that holds each, and a run is skipped whole when its circle
	/// cannot hold a nearer sample - the run nearest the point searched first for a tight bound. The answer is exactly
	/// a search of every sample's; the work is a fraction of it, which matters since the far shore made the road half
	/// again as long and a phone builds the mask as the map opens. Flat arrays and plain arithmetic, so it stays quick
	/// in a debug build too.
	/// </summary>
	private sealed class RoadSearch
	{
		private const int RunLength = 16;
		private readonly float[] sx, sy;
		private readonly float[] runX, runY, runR;
		private readonly int[] runStart, runEnd;
		private readonly float[] placeX, placeY, placeR;

		public RoadSearch(List<Vector2> road, List<(Vector2 At, float Radius)> places)
		{
			sx = new float[road.Count];
			sy = new float[road.Count];
			for (int i = 0; i < road.Count; i++)
			{
				sx[i] = road[i].X;
				sy[i] = road[i].Y;
			}
			int runs = (road.Count + RunLength - 1) / RunLength;
			runX = new float[runs];
			runY = new float[runs];
			runR = new float[runs];
			runStart = new int[runs];
			runEnd = new int[runs];
			for (int r = 0; r < runs; r++)
			{
				int start = r * RunLength;
				int end = Math.Min(road.Count, start + RunLength);
				float cx = 0f, cy = 0f;
				for (int i = start; i < end; i++)
				{
					cx += sx[i];
					cy += sy[i];
				}
				cx /= end - start;
				cy /= end - start;
				float radius = 0f;
				for (int i = start; i < end; i++)
					radius = MathF.Max(radius, MathF.Sqrt((sx[i] - cx) * (sx[i] - cx) + (sy[i] - cy) * (sy[i] - cy)));
				runX[r] = cx;
				runY[r] = cy;
				runR[r] = radius;
				runStart[r] = start;
				runEnd[r] = end;
			}
			placeX = new float[places.Count];
			placeY = new float[places.Count];
			placeR = new float[places.Count];
			for (int i = 0; i < places.Count; i++)
			{
				placeX[i] = places[i].At.X;
				placeY[i] = places[i].At.Y;
				placeR[i] = places[i].Radius;
			}
		}

		public float Nearest(float px, float py)
		{
			float best = 1e6f;
			int runs = runX.Length;
			if (runs > 0)
			{
				int first = 0;
				float firstBound = float.MaxValue;
				for (int r = 0; r < runs; r++)
				{
					float dx = px - runX[r], dy = py - runY[r];
					float bound = MathF.Sqrt(dx * dx + dy * dy) - runR[r];
					if (bound < firstBound)
					{
						firstBound = bound;
						first = r;
					}
				}
				float best2 = Scan(first, px, py, float.MaxValue);
				for (int r = 0; r < runs; r++)
				{
					if (r == first)
						continue;
					float dx = px - runX[r], dy = py - runY[r];
					float bound = MathF.Sqrt(dx * dx + dy * dy) - runR[r];
					if (bound > 0f && bound * bound >= best2)
						continue;
					best2 = Scan(r, px, py, best2);
				}
				best = MathF.Sqrt(best2);
			}
			for (int i = 0; i < placeX.Length; i++)
			{
				float dx = px - placeX[i], dy = py - placeY[i];
				best = MathF.Min(best, MathF.Sqrt(dx * dx + dy * dy) - placeR[i]);
			}
			return best;
		}

		private float Scan(int run, float px, float py, float best2)
		{
			for (int i = runStart[run]; i < runEnd[run]; i++)
			{
				float dx = px - sx[i], dy = py - sy[i];
				float d2 = dx * dx + dy * dy;
				if (d2 < best2)
					best2 = d2;
			}
			return best2;
		}
	}

	/// <summary>The stop under a point of this control, or -1.</summary>
	public int StopAt(Vector2 local)
	{
		if (layout == null || lastUnitsPerArt <= 0f)
			return -1;
		Vector2 art = (local - lastOrigin) / lastUnitsPerArt;
		int best = -1;
		float bestDistance = 56f;
		for (int i = 0; i < stops.Length; i++)
		{
			float d = Mathf.Min(art.DistanceTo(stops[i].Layout.Focus), art.DistanceTo(stops[i].Layout.Stand));
			if (d < bestDistance)
			{
				bestDistance = d;
				best = i;
			}
		}
		return best;
	}

	/// <summary>Where a stop's landmark is on this control, in canvas units. For the harness.</summary>
	public Vector2 LocalOf(int index) =>
		index < 0 || index >= stops.Length ? Vector2.Zero : lastOrigin + stops[index].Layout.Focus * lastUnitsPerArt;

	public override void _GuiInput(InputEvent @event)
	{
		// The mouse button only: a finger arrives as one too (emulate_mouse_from_touch), and
		// listening to both would select twice (.ai/controller-and-focus.md, "Touch").
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
		{
			int hit = StopAt(press.Position);
			if (hit >= 0)
			{
				StopPressed?.Invoke(hit);
				AcceptEvent();
			}
		}
	}
}
