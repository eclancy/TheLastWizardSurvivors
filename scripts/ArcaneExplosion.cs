using Godot;
using System;
using System.Linq;

namespace TheOneHeMissed.scripts
{
	public partial class ArcaneExplosion : Area2D
	{
		[Export] public SpellData SpellData { get; set; }
		[Export] public int CurrentLevel { get; set; } = 1;
		// Charge-driven, not timed. Player counts casts of the OTHER five spells and detonates
		// this when the count comes due - see Player.NotifyArcaneCharge. Left as an export so the
		// old behaviour is one checkbox away if the charge version does not survive playtesting.
		[Export] public bool ChargeDriven { get; set; } = true;

		[Export] public float BaseKnockbackRange { get; set; } = 100f;
		[Export] public float BaseKnockbackSpeed { get; set; } = 2.0f;
		public float DamageMultiplier { get; set; } = 1.0f;
		public float AreaMultiplier { get; set; } = 1.0f;
		public float CooldownMultiplier { get; set; } = 1.0f;
		public float DurationMultiplier { get; set; } = 1.0f;

		private Weapon weapon;
		public Weapon Weapon
		{
			get => weapon;
			set
			{
				weapon = value;
				ApplyLegacyWeapon(value);
				UpdateScale();
			}
		}

		private int damage = 5;
		private float range = 100f;
		private float cooldown = 1.5f;
		private float knockbackRange = 100f;
		private float knockbackSpeed = 2.0f;

		public Node2D PlayerRef;
		[Export]
		public float TotalLifetime = 5.0f; // total time before freeing the node; 0 = infinite
		private float lifeElapsed = 0f;
		private float fireTimer = 0f;
		private CollisionShape2D collisionShape;

		// THE BLAST IS PIXEL ART (2026-10-29, "does not look up to our typical visual fidelity"). It was
		// spell_fx.py's 32 px ring saved at x2 and shown at range / 32 - its art pixels 6 to 14 world px
		// wide - over a ring of 64 white GPU particles one world px wide, with every evolution's tint
		// multiplied over both. Now it is one quad through scenes/shaders/arcane_blast.gdshader, drawn
		// from `range`, the distance TriggerExplosion tests: a front of the force that lands on that
		// radius and breaks up there, every art pixel on the world's 2 px grid. The evolutions are painted
		// there rather than tinted, from what each one does (StartBlast).
		private const string BlastShaderPath = "res://scenes/shaders/arcane_blast.gdshader";
		private const float BlastSeconds = 0.4f;
		private static Shader blastShader;
		private static readonly StringName ParamRadius = "radius";
		private static readonly StringName ParamProgress = "progress";
		private static readonly StringName ParamSeed = "seed";
		private static readonly StringName ParamDual = "dual";
		private static readonly StringName ParamStatic = "static_nova";
		private static readonly StringName ParamSupernova = "supernova";
		private static readonly StringName ParamEventHorizon = "event_horizon";
		private static readonly StringName ParamGilded = "gilded";
		// The Canticle's look (arcane_blast.gdshader, `canticle`): rings that pulse out to the reach it hits, a
		// sound going out, where the Sovereign Rite's crowned blast keeps the front of force it was drawn with.
		private static readonly StringName ParamCanticle = "canticle";
		private ShaderMaterial blastMaterial;
		private float blastAge = -1f;
		private float blastRadius;
		private int blastCount;

		public override void _Ready()
		{
			// The blast is in the air, over the bodies it catches. A child of the player at z 0, it was
			// sorted at his feet, under every body south of him (DrawLayers). Its rift is on the ground layer.
			ZAsRelative = false;
			ZIndex = DrawLayers.Air;
			RefreshComputedStats();
			collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			blastShader ??= GD.Load<Shader>(BlastShaderPath);
			blastMaterial = new ShaderMaterial { Shader = blastShader };
			Material = blastMaterial;
			Listen(resonance);

			// Apply initial scaling if weapon is already set
			UpdateScale();

			// The first cast goes off at once - on the music's next beat (RequestBurst) - then the charge takes over.
			RequestBurst();
			fireTimer = 0f;
		}

		private void UpdateScale()
		{
			// The collision circle follows the range as it always did; the blast is drawn from the range
			// itself (_Draw), so there is no sprite to scale.
			float baseRange = 32.0f;
			float scaleFactor = range / baseRange;

			if (collisionShape != null && collisionShape.Shape is CircleShape2D circle)
			{
				// The original circle radius is ~23, scale it proportionally
				circle.Radius = 23.0f * scaleFactor;
			}
		}

		public override void _Process(double delta)
		{
			// follow player if assigned
			if (PlayerRef != null)
			{
				GlobalPosition = PlayerRef.GlobalPosition;
			}

			// update overall lifetime and free when exceeded
			if (TotalLifetime > 0f)
			{
				lifeElapsed += (float)delta;
				if (lifeElapsed >= TotalLifetime)
				{
					QueueFree();
					return;
				}
			}
			// Timed firing only when the charge path is switched off. A spell that both ticks on
			// a clock AND detonates on charge would fire twice as often as either rule describes.
			if (!ChargeDriven)
			{
				fireTimer += (float)delta;
				if (fireTimer >= cooldown)
				{
					TriggerExplosion();
					fireTimer = 0f;
				}
			}

			clock += (float)delta;
			TickPending(delta);
			TickMotes((float)delta);
			TickEchoes((float)delta);
			TickStasis((float)delta);

			if (blastAge < 0f)
				return;
			blastAge += (float)delta;
			if (blastAge >= BlastSeconds)
				blastAge = -1f;
			else
				blastMaterial?.SetShaderParameter(ParamProgress, blastAge / BlastSeconds);
			QueueRedraw();
		}

		// One quad over the blast, sized in world px - what TriggerExplosion measured in - whatever this
		// node's scale; the shader decides every pixel and discards the rest.
		public override void _Draw()
		{
			if (blastAge < 0f)
				return;
			float half = (blastRadius + 4f) / MathF.Max(0.01f, GlobalScale.X);
			DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
		}

		// The blast's look, from what this detonation does: the Canticle's rings of sound going out (2026-11-09),
		// the storm's crackle for Storm Chord (Static Nova's id), a collapse for anything that pulls inward, the
		// element Harmony takes for Harmony, gold for a Legendary. Set once a blast, with the radius it hit. (The
		// shader's second front was Dual Shockwave's look; that card is Harmony now, so it is no longer asked for.)
		private void StartBlast(bool isVortex, Element? mimic)
		{
			if (blastMaterial == null)
				return;
			blastRadius = range;
			blastAge = 0f;
			blastCount++;
			Paint(blastMaterial, blastRadius, blastCount, isVortex, mimic);
			QueueRedraw();
		}

		private void Paint(ShaderMaterial material, float radius, int count, bool isVortex, Element? mimic)
		{
			material.SetShaderParameter(ParamRadius, radius);
			material.SetShaderParameter(ParamProgress, 0f);
			material.SetShaderParameter(ParamSeed, (count * 0.6180339f) % 1f * 97f);
			material.SetShaderParameter(ParamDual, false);
			material.SetShaderParameter(ParamStatic, SpellData?.HasEvolution(StaticNovaId) ?? false);
			material.SetShaderParameter(ParamSupernova, false);
			material.SetShaderParameter(ParamEventHorizon, isVortex);
			material.SetShaderParameter(ParamGilded, SpellData?.IsLegendary ?? false);
			material.SetShaderParameter(ParamCanticle, true);
			material.SetShaderParameter(ParamMimic, mimic.HasValue);
			if (mimic.HasValue)
			{
				ElementColors.Ramp ramp = ElementColors.GetRamp(mimic.Value);
				material.SetShaderParameter(ParamMimicCore, ramp.Core);
				material.SetShaderParameter(ParamMimicHot, ramp.Hot);
				material.SetShaderParameter(ParamMimicMid, ramp.Mid);
				material.SetShaderParameter(ParamMimicEdge, ramp.Edge);
			}
		}

		private static readonly StringName ParamMimic = "mimic";
		private static readonly StringName ParamMimicCore = "m_core";
		private static readonly StringName ParamMimicHot = "m_hot";
		private static readonly StringName ParamMimicMid = "m_mid";
		private static readonly StringName ParamMimicEdge = "m_edge";

		public const string SympatheticBlastId = "arcane_explosion_dual_shockwave";
		private const string StaticNovaId = "arcane_explosion_static_nova";
		public const string ResonanceId = "arcane_explosion_supernova";
		public const string EventHorizonId = "arcane_explosion_event_horizon";

		// THE HUNDRED MOUTHS (the Cumaean Sibyl's Apotheosis, the verb ECHO; .ai/chapter-wizards.md 4.3). The cave
		// at Cumae had a hundred mouths and spoke with a hundred voices: each burst sounds her other spells again,
		// from where she stands, at Player.EchoShare of their strength (Player.Echo.cs). The burst only asks; the
		// player queues the echoes and casts a few a frame, and an echo is never a cast - it charges nothing and
		// is never echoed again - so the form cannot feed itself.
		public const string HundredMouthsId = "arcane_explosion_hundred_mouths";
		private bool hundredMouths;

		// THE CANTICLE (Eric, 2026-11-09; .ai/character-fit.md section 5, row 19): "Maybe Sybil's arcane explosion
		// actually adds to the music of the game." This is the one spell set off by the others, so now they sing it:
		// every cast that charges it adds a NOTE to a chord - in the voice of that spell's element (note_<element>,
		// tools/audio/palette.py), pitched to the next degree of the chord in the key of the music playing
		// (MusicBeats, written by the score) - and when the chord is full the burst does not go off at once. It
		// waits for the next counted beat of that music (MusicPlayer.TryGetBeat) and goes off ON it, sounding the
		// gathered notes together. Never more than a beat late: the wait is bounded by the longest gap between two
		// counted beats. Where no music with a known beat is sounding - a headless run, the music fading out, a
		// piece missing from the map - it bursts the moment the chord is full, as it always did.
		//
		// THE NUMBERS DID NOT MOVE ("pending balance testing"): the same charge (Player.GetArcaneChargeRequired is
		// the chord's size), the same hit, one burst per full chord - two chords filling inside one beat both go
		// off on it. Only when, by at most a beat.
		public const int MaxNotes = 8;
		// Every degree folds into A3..A5 (MIDI 57..80): a pitch scale of 0.5 to 2 on the A4 files, the range the
		// notes were built for (palette.NOTE_CEILING, NOTE_FLOOR).
		private const int NoteFoldBottom = 57;
		private const int NoteFoldTop = 81;
		// A beat counts as "now" this long after it fell: a frame or so. Past it (a level-up paused the run across
		// the beat), the burst waits for the next one rather than going off between two.
		private const double OnBeatTolerance = 0.1;
		private static readonly int[] DefaultChord = { 0, 7, 3, 12, 10, 14, 15, 19 };   // A minor, as the menu's

		private readonly Element[] gathered = new Element[MaxNotes];
		private readonly float[] gatheredAge = new float[MaxNotes];
		private int gatheredCount;
		private int chordSize = MaxNotes;
		// The chord that is full and waiting for its beat, and how many bursts it owes.
		private readonly Element[] chord = new Element[MaxNotes];
		private int chordCount;
		private int pendingBursts;
		private MusicBeats.Piece waitPiece;
		private int waitOrdinal;
		private double waited;
		private double maxWait;

		private static bool? logBeats;
		/// <summary>--canticle-log: one line per full chord and per burst, the burst's time against the beat.</summary>
		private static bool LogBeats => logBeats ??= Array.IndexOf(OS.GetCmdlineUserArgs(), "--canticle-log") >= 0;

		/// <summary>
		/// One cast's note: <paramref name="caster"/> has charged the spell, and its element's voice joins the chord.
		/// Played quietly, at the pitch it will hold in the chord. <paramref name="required"/> is the chord's size
		/// (Player.GetArcaneChargeRequired), which places the motes round her.
		/// </summary>
		public void AddNote(SpellData caster, int required)
		{
			chordSize = Math.Clamp(required, 1, MaxNotes);
			if (gatheredCount >= MaxNotes)
				return;
			Element voice = SfxCatalog.DominantElement(caster);
			gathered[gatheredCount] = voice;
			gatheredAge[gatheredCount] = 0f;
			gatheredCount++;
			SfxPlayer.CanticleNote(voice, GlobalPosition, NotePitch(CurrentKey(), gatheredCount - 1), chord: false);
			EnsureMoteCanvas();
		}

		/// <summary>
		/// The chord is full (or, held alone, her own cooldown has come round): burst on the music's next counted
		/// beat, or at once if no beat is sounding.
		/// </summary>
		public void RequestBurst()
		{
			// The full chord is the one that sounds; the next begins gathering at once. Alone she has no other
			// voices, and the burst sounds her own.
			if (gatheredCount == 0)
			{
				gathered[0] = Element.Arcane;
				gatheredCount = 1;
			}
			Array.Copy(gathered, chord, gatheredCount);
			chordCount = gatheredCount;
			gatheredCount = 0;
			pendingBursts++;
			fireTimer = 0f;
			EnsureMoteCanvas();
			if (pendingBursts > 1)
				return;   // already waiting: this chord goes off on the same beat
			waited = 0.0;
			if (!TryBeat(out MusicPlayer.BeatClock clock))
			{
				if (LogBeats)
					GD.Print($"Canticle: chord of {chordCount} full, no beat sounding - burst at once");
				Burst();
				return;
			}
			waitPiece = clock.Piece;
			waitOrdinal = clock.Ordinal;
			maxWait = clock.Piece.LongestGap + OnBeatTolerance;
			if (LogBeats)
				GD.Print($"Canticle: chord of {chordCount} full at {clock.Position:0.000} s into '{clock.Piece.Id}' "
					+ $"(beat {clock.Ordinal}, {clock.SinceBeat * 1000.0:0} ms after it); next beat due in {clock.ToNextBeat * 1000.0:0} ms");
		}

		private static bool TryBeat(out MusicPlayer.BeatClock clock)
		{
			clock = default;
			return MusicPlayer.Instance != null && MusicPlayer.Instance.TryGetBeat(out clock);
		}

		private void TickPending(double delta)
		{
			if (pendingBursts == 0)
				return;
			if (PlayerRef is Player holder)
			{
				if (holder.IsDead)
				{
					pendingBursts = 0;
					return;
				}
				// A boss walking in holds her spells silent (Player.HeldForBossEntrance): the chord waits it out.
				if (holder.HeldForBossEntrance)
				{
					if (TryBeat(out MusicPlayer.BeatClock held))
					{
						waitPiece = held.Piece;
						waitOrdinal = held.Ordinal;
					}
					waited = 0.0;
					return;
				}
			}
			waited += delta;
			if (!TryBeat(out MusicPlayer.BeatClock clock))
			{
				if (LogBeats)
					GD.Print("Canticle: the beat stopped sounding while the chord waited - burst at once");
				Burst();
				return;
			}
			if (clock.Piece != waitPiece)
			{
				// The music changed under the wait (a boss's loop came in): its own next beat.
				waitPiece = clock.Piece;
				waitOrdinal = clock.Ordinal;
				maxWait = waited + clock.Piece.LongestGap + OnBeatTolerance;
				return;
			}
			if (clock.Ordinal != waitOrdinal)
			{
				if (clock.SinceBeat <= Math.Max(OnBeatTolerance, delta * 1.5))
				{
					if (LogBeats)
						GD.Print($"Canticle: burst on beat {clock.Ordinal} of '{clock.Piece.Id}' at {clock.Position:0.000} s, "
							+ $"{clock.SinceBeat * 1000.0:0} ms after it fell, having waited {waited * 1000.0:0} ms");
					Burst();
					return;
				}
				// A beat went by unseen - the run was paused across it - so wait for the next.
				waitOrdinal = clock.Ordinal;
				maxWait = waited + clock.Piece.LongestGap + OnBeatTolerance;
			}
			if (waited > maxWait)
			{
				if (LogBeats)
					GD.Print($"Canticle: no beat seen in {waited * 1000.0:0} ms - burst late");
				Burst();
			}
		}

		// Every burst the chord owes, on this frame: its notes sounded once, together, and its motes thrown out.
		private void Burst()
		{
			int times = pendingBursts;
			pendingBursts = 0;
			SoundChord();
			ReleaseMotes();
			for (int i = 0; i < times; i++)
				TriggerExplosion();
		}

		private void SoundChord()
		{
			MusicBeats.Piece key = CurrentKey();
			int voices = Math.Min(chordCount, SfxPlayer.CanticleChordVoices);
			for (int i = 0; i < voices; i++)
				SfxPlayer.CanticleNote(chord[i], GlobalPosition, NotePitch(key, i), chord: true);
		}

		/// <summary>The key she sings in: the music's that is sounding, else the chapter's own, else A minor.</summary>
		private static MusicBeats.Piece CurrentKey()
		{
			if (TryBeat(out MusicPlayer.BeatClock clock))
				return clock.Piece;
			return MusicPlayer.KeyForStage(Global.SelectedStageIdx);
		}

		/// <summary>
		/// The pitch scale (1 is the files' A4) of the chord's <paramref name="degree"/>th note in
		/// <paramref name="key"/>: its tone of the chord on the tonic, folded into A3..A5.
		/// </summary>
		public static float NotePitch(MusicBeats.Piece key, int degree)
		{
			int[] tones = key?.Chord ?? DefaultChord;
			if (tones.Length == 0)
				tones = DefaultChord;
			int tonic = key?.TonicMidi ?? 69;
			int d = Math.Max(0, degree);
			int midi = tonic + tones[d % tones.Length] + 12 * (d / tones.Length);
			while (midi >= NoteFoldTop)
				midi -= 12;
			while (midi < NoteFoldBottom)
				midi += 12;
			return MathF.Pow(2f, (midi - 69) / 12f);
		}

		// --- the motes: the chord gathering round her ----------------------------------------------------------
		// Each note is a small quaver in its element's light, standing in a ring round her at chest height - one
		// place for each note the chord needs, filling as it gathers - which ticks white on every beat while the
		// music has one, and flies out to the burst's reach when the chord sounds. One canvas, fixed arrays, on the
		// world's 2 px grid; nothing allocates.
		// Clear of her own silhouette (about forty wide): at 26 the notes stood on her robe and read as part of it.
		private const float MoteRing = 36f;
		private const float MoteHeight = -16f;
		private const float MoteSquash = 0.66f;
		private const float MoteAppear = 0.12f;
		private const float MoteReleaseSeconds = 0.3f;
		private static readonly Color MoteOutline = new(0.020f, 0.027f, 0.047f, 1f);   // OCC #05070C
		private readonly Element[] released = new Element[MaxNotes];
		private int releasedCount;
		private int releasedSlots = 1;
		private float releaseAge = -1f;
		private float moteTurn;
		private float beatFlash;
		private int lastMoteOrdinal = -1;
		private Node2D moteCanvas;
		private bool motesDrawn;

		private void EnsureMoteCanvas()
		{
			if (moteCanvas != null && IsInstanceValid(moteCanvas))
				return;
			moteCanvas = new Node2D { Name = "ChordMotes", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air };
			moteCanvas.Draw += DrawMotes;
			AddChild(moteCanvas);
			moteCanvas.GlobalPosition = Vector2.Zero;
		}

		private void ReleaseMotes()
		{
			Array.Copy(chord, released, chordCount);
			releasedCount = chordCount;
			releasedSlots = Math.Max(chordSize, chordCount);
			releaseAge = 0f;
		}

		private void TickMotes(float delta)
		{
			if (moteCanvas == null || !IsInstanceValid(moteCanvas))
				return;
			bool any = gatheredCount > 0 || pendingBursts > 0 || releaseAge >= 0f;
			if (any)
			{
				moteTurn += delta * 0.6f;
				for (int i = 0; i < gatheredCount; i++)
					gatheredAge[i] += delta;
				if (releaseAge >= 0f)
				{
					releaseAge += delta;
					if (releaseAge >= MoteReleaseSeconds)
						releaseAge = -1f;
				}
				beatFlash = MathF.Max(0f, beatFlash - delta / 0.15f);
				if (TryBeat(out MusicPlayer.BeatClock clock) && clock.Ordinal != lastMoteOrdinal)
				{
					if (lastMoteOrdinal >= 0)
						beatFlash = 1f;
					lastMoteOrdinal = clock.Ordinal;
				}
			}
			if (any || motesDrawn)
				moteCanvas.QueueRedraw();
			motesDrawn = any;
		}

		private Vector2 MotePoint(int index, int slots, float offset, float reach)
		{
			float a = moteTurn + (index + offset) * Mathf.Tau / Math.Max(1, slots) - Mathf.Pi * 0.5f;
			return GlobalPosition + new Vector2(MathF.Cos(a) * reach, MathF.Sin(a) * reach * MoteSquash + MoteHeight);
		}

		private void DrawMotes()
		{
			int slots = Math.Max(chordSize, 1);
			// The full chord, waiting for its beat: white-hot, every note in its place.
			if (pendingBursts > 0)
			{
				for (int i = 0; i < chordCount; i++)
					DrawNoteMote(MotePoint(i, slots, 0f, MoteRing), chord[i], 1f, 1f);
			}
			// The next chord gathering (between its places, if one is still waiting).
			float offset = pendingBursts > 0 ? 0.5f : 0f;
			for (int i = 0; i < gatheredCount; i++)
			{
				float grow = Mathf.Clamp(gatheredAge[i] / MoteAppear, 0f, 1f);
				DrawNoteMote(MotePoint(i, slots, offset, MoteRing * (0.6f + 0.4f * grow)), gathered[i], grow, beatFlash);
			}
			// The chord just sounded, carried out to the reach it hit.
			if (releaseAge >= 0f)
			{
				float t = releaseAge / MoteReleaseSeconds;
				float reach = Mathf.Lerp(MoteRing, MathF.Max(MoteRing, range - 6f), 1f - (1f - t) * (1f - t));
				for (int i = 0; i < releasedCount; i++)
					DrawNoteMote(MotePoint(i, releasedSlots, 0f, reach), released[i], 1f - t, 1f - t);
			}
		}

		// A quaver sixteen world px tall, outlined in the occlusion ink so it reads over the grass and the crowd: a head
		// of three by two cells, a stem of six, a hooked flag. Its identity is the element's MID, as every element
		// effect's is (.ai/art-direction.md section 3), lit from the upper left in its hot; `flash` 1 is white-hot.
		private void DrawNoteMote(Vector2 at, Element element, float alpha, float flash)
		{
			if (alpha <= 0.02f)
				return;
			ElementColors.Ramp ramp = ElementColors.GetRamp(element);
			Color body = new(flash > 0.5f ? ramp.Core : ramp.Mid, alpha);
			Color lit = new(flash > 0.5f ? ramp.Core : ramp.Hot, alpha);
			float g = WhipLimb.Grid;
			// `at` is the glyph's middle; o is its head's top-left cell.
			Vector2 o = new(MathF.Round(at.X / g) * g - g * 2f, MathF.Round(at.Y / g) * g + g * 2f);
			var outline = new Color(MoteOutline, alpha);
			moteCanvas.DrawRect(new Rect2(o.X - g, o.Y - g, g * 5f, g * 4f), outline);
			moteCanvas.DrawRect(new Rect2(o.X + g, o.Y - g * 7f, g * 3f, g * 7f), outline);
			moteCanvas.DrawRect(new Rect2(o.X + g * 3f, o.Y - g * 7f, g * 3f, g * 5f), outline);
			moteCanvas.DrawRect(new Rect2(o.X, o.Y, g * 3f, g * 2f), body);
			moteCanvas.DrawRect(new Rect2(o.X, o.Y, g, g), lit);
			moteCanvas.DrawRect(new Rect2(o.X + g * 2f, o.Y - g * 6f, g, g * 6f), body);
			moteCanvas.DrawRect(new Rect2(o.X + g * 3f, o.Y - g * 6f, g, g * 2f), lit);
			moteCanvas.DrawRect(new Rect2(o.X + g * 4f, o.Y - g * 5f, g, g * 2f), body);
		}

		private void TriggerExplosion()
		{
			// Apply to all enemies within range.
			//
			// The group, not GetTree().CurrentScene.GetChildren(). The old form only saw enemies
			// that were DIRECT children of the scene root, so anything parented under a container
			// - a spawner, a formation node, a test harness - was silently immune. CLAUDE.md is
			// explicit that AoE iterates the group for exactly this reason.
			//
			// Event Horizon (Effect Freeze) froze what it caught and, until 2h, threw it INWARD - a pull,
			// which is Hollow Star's verb, and which turned Dual Shockwave's +70% throw into a 70% harder
			// pull under it. Now the blast stops time: what it catches is frozen where it stands, nothing
			// is thrown either way, and a sphere of stopped time stays where it went off (OpenStasis).
			float freezeSeconds = SpellData?.GetFreezeSecondsAtLevel(CurrentLevel) ?? 0f;
			bool stopsTime = freezeSeconds > 0f;
			bool isVortex = SpellData != null && SpellData.HasEffectFlag(SpellEffect.VortexPull);
			Element? mimic = SympathyElement();
			int hit = mimic.HasValue && ElementPaths.RiderFor(mimic.Value) == ElementRider.None
				? Math.Max(1, Mathf.RoundToInt(damage * SympathyBareShare))
				: damage;
			BlastAt(GlobalPosition, hit, stopsTime ? 0f : 1f, isVortex, freezeSeconds, mimic);

			OpenLingeringRift();
			if (stopsTime)
				OpenStasis(GlobalPosition, range, freezeSeconds);
			StartBlast(isVortex, mimic);
			Resonate(damage, stopsTime, isVortex, freezeSeconds, mimic);
			if (hundredMouths && PlayerRef is Player sibyl)
				sibyl.EchoOtherSpells(SpellData);
		}

		// One detonation's hits at `at`: the damage, the throw (scaled by `throwShare`; inward for a pull),
		// the freeze and Harmony's rider.
		private void BlastAt(Vector2 at, int amount, float throwShare, bool isVortex, float freezeSeconds, Element? mimic)
		{
			ElementRider rider = mimic.HasValue ? ElementPaths.RiderFor(mimic.Value) : ElementRider.None;
			Player player = PlayerRef as Player;
			foreach (Enemy e in EnemyRegistry.Snapshot)
			{
				if (!IsInstanceValid(e))
					continue;
				float dist = at.DistanceTo(e.GlobalPosition);
				if (dist > range)
					continue;
				if (throwShare > 0f)
				{
					float knockback = knockbackRange * (1f - (dist / (2f * range)));
					if (knockback < knockbackRange * 0.5f)
						knockback = knockbackRange * 0.5f;
					Vector2 dir = isVortex ? (at - e.GlobalPosition).Normalized() : (e.GlobalPosition - at).Normalized();
					e.ApplyKnockback(dir * knockback * knockbackSpeed * throwShare);
				}
				player?.DealDamageToEnemy(e, amount, source: SpellData);
				if (freezeSeconds > 0f)
					e.ApplySlow(0f, freezeSeconds);
				if (rider != ElementRider.None)
					ApplySympathy(e, rider);
			}
		}

		// HARMONY (arcane_explosion_dual_shockwave's id; Sympathetic Blast until 2026-11-09, Dual Shockwave before
		// .ai/spell-roster.md 2h). The blast goes off in another spell's element - repainted in its ramp - and lays
		// that element's status on what it catches (ElementPaths.RiderFor: ice and water slow, lightning staggers,
		// poison poisons, darkness exposes, light weakens, metal withers); the five with no status (fire, earth,
		// grass, wind, arcane) make it hit SympathyBareShare harder instead.
		//
		// WHICH SPELL: THE ONE IN THE NEXT SLOT (Eric, 2026-11-09: "'takes the element of the last spell cast'
		// should pick a certain spell slot. we are trying to add more spell slot play, and i want the player to be
		// able to purposefully select the element"). It was the last spell cast - whichever cast happened to fill
		// the charge, which nobody could aim. Now it is the spell in the slot right after the Canticle's
		// (Player.SpellInSlotAfter), so the player chooses it by what they take next; an empty next slot leaves the
		// blast Arcane and plain. The loadout strip marks that slot in the element it will lend (LoadoutHud).
		//
		// For a spell of two elements, the old rule stands: its strongest that is not Arcane - Spiritual Weapon's
		// Light, Magic Missile's Lightning once Twin Volley or Overcharge has branched it - and Arcane only for a
		// pure Arcane one (Magic Missile unbranched, since it went pure on 2026-11-02).
		public const float SympathyBareShare = 1.3f;

		private Element? SympathyElement()
		{
			if (SpellData == null || !SpellData.HasEvolution(SympatheticBlastId))
				return null;
			return HarmonyElementOf((PlayerRef as Player)?.SpellInSlotAfter(SpellData));
		}

		/// <summary>
		/// The element Harmony takes from <paramref name="next"/>, the spell in the slot after the Canticle's: its
		/// strongest that is not Arcane, Arcane for a pure Arcane spell - or null for an empty slot (the blast stays
		/// plain). One rule for the blast and for the strip that marks the slot.
		/// </summary>
		public static Element? HarmonyElementOf(SpellData next)
		{
			if (next == null)
				return null;
			Element best = Element.Arcane;
			int bestWeight = 0;
			foreach (var pair in next.GetElementWeights())
			{
				if (pair.Key == Element.Arcane || pair.Value <= bestWeight)
					continue;
				best = pair.Key;
				bestWeight = pair.Value;
			}
			return best;
		}

		private void ApplySympathy(Enemy e, ElementRider rider)
		{
			switch (rider)
			{
				case ElementRider.Poison:
					ElementPaths.Apply(e, rider, MathF.Max(2f, damage * 0.15f), 3f);
					break;
				case ElementRider.Slow:
					ElementPaths.Apply(e, rider, 0.6f, 1.5f);
					break;
				case ElementRider.Vulnerable:
					ElementPaths.Apply(e, rider, 0.15f, 3f);
					break;
				case ElementRider.Weaken:
					ElementPaths.Apply(e, rider, 0.3f, 3f);
					break;
				case ElementRider.Wither:
					ElementPaths.Apply(e, rider, 0.04f, 0f);
					break;
				case ElementRider.Shock:
					ElementPaths.Apply(e, rider, 0f, 0.3f);
					break;
			}
		}

		// RESONANCE (arcane_explosion_supernova's id, 2h). The card was Supernova: +150% area and +120%
		// damage, the same blast bigger. Now every blast goes off again wherever each of the wizard's other
		// spells last struck (in the last ResonanceMemory seconds, and not on top of the blast round him):
		// the same blast, at ResonanceShare of its damage, drawn there by a copy of its own front. A loadout
		// of six spells lights up the screen where its spells are working; one partner gives one echo.
		public const float ResonanceShare = 1f;
		private const float ResonanceMemory = 2.5f;
		private const int MaxEchoes = 5;

		private struct Strike
		{
			public SpellData Source;
			public Vector2 At;
			public float Time;
		}

		private readonly Strike[] strikes = new Strike[8];
		private float clock;
		private bool resonance;
		private bool listening;
		private readonly Node2D[] echoNodes = new Node2D[MaxEchoes];
		private readonly ShaderMaterial[] echoMaterials = new ShaderMaterial[MaxEchoes];
		private readonly float[] echoAges = new float[MaxEchoes];
		private readonly float[] echoRadii = new float[MaxEchoes];
		private int nextEcho;

		private void Listen(bool on)
		{
			if (on == listening)
				return;
			listening = on;
			if (on)
				Player.EnemyDamagedAt += OnEnemyDamagedAt;
			else
				Player.EnemyDamagedAt -= OnEnemyDamagedAt;
		}

		public override void _ExitTree()
		{
			Listen(false);
		}

		// Every hit the wizard lands: where each of his other spells last struck. A fixed list, one slot a spell.
		private void OnEnemyDamagedAt(Vector2 at, int amount, SpellData source)
		{
			if (source == null || source == SpellData)
				return;
			int free = -1;
			for (int i = 0; i < strikes.Length; i++)
			{
				if (strikes[i].Source == source)
				{
					strikes[i].At = at;
					strikes[i].Time = clock;
					return;
				}
				if (free < 0 && (strikes[i].Source == null || clock - strikes[i].Time > ResonanceMemory))
					free = i;
			}
			if (free >= 0)
				strikes[free] = new Strike { Source = source, At = at, Time = clock };
		}

		private void Resonate(int amount, bool stopsTime, bool isVortex, float freezeSeconds, Element? mimic)
		{
			if (!resonance)
				return;
			int echo = Math.Max(1, Mathf.RoundToInt(amount * ResonanceShare));
			float clear = range * 0.75f;
			int made = 0;
			for (int i = 0; i < strikes.Length && made < MaxEchoes; i++)
			{
				Strike s = strikes[i];
				if (s.Source == null || clock - s.Time > ResonanceMemory || s.At.DistanceTo(GlobalPosition) < clear)
					continue;
				BlastAt(s.At, echo, stopsTime ? 0f : 1f, isVortex, freezeSeconds, mimic);
				if (stopsTime)
					OpenStasis(s.At, range, freezeSeconds);
				ShowEcho(s.At, isVortex, mimic);
				made++;
			}
		}

		// An echo's front: the blast's own shader on a node of its own at the echo, in the air.
		private void ShowEcho(Vector2 at, bool isVortex, Element? mimic)
		{
			int i = nextEcho;
			nextEcho = (nextEcho + 1) % MaxEchoes;
			if (echoNodes[i] == null || !IsInstanceValid(echoNodes[i]))
			{
				echoMaterials[i] = new ShaderMaterial { Shader = blastShader };
				var node = new Node2D { Name = $"Echo{i}", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air, Material = echoMaterials[i] };
				int index = i;
				node.Draw += () => DrawEcho(index);
				AddChild(node);
				echoNodes[i] = node;
			}
			echoNodes[i].GlobalPosition = at;
			echoAges[i] = 0f;
			echoRadii[i] = range;
			blastCount++;
			Paint(echoMaterials[i], range, blastCount, isVortex, mimic);
			echoNodes[i].QueueRedraw();
		}

		private void DrawEcho(int i)
		{
			if (echoAges[i] < 0f || echoAges[i] >= BlastSeconds)
				return;
			float half = echoRadii[i] + 4f;
			echoNodes[i].DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
		}

		private void TickEchoes(float delta)
		{
			for (int i = 0; i < MaxEchoes; i++)
			{
				if (echoNodes[i] == null || echoAges[i] < 0f)
					continue;
				echoAges[i] += delta;
				if (echoAges[i] >= BlastSeconds)
					echoAges[i] = -1f;
				else
					echoMaterials[i].SetShaderParameter(ParamProgress, echoAges[i] / BlastSeconds);
				echoNodes[i].QueueRedraw();
			}
		}

		// FERMATA STOPS TIME (Event Horizon's id and card until 2026-11-09, when the Canticle made it a held note -
		// a fermata over the chord, the mark drawn at the sphere's top). Where the blast goes off, a sphere of stopped time hangs for as long
		// as the freeze: what the blast caught stays frozen in it, and whatever walks in freezes at once, for
		// what is left of it (a boss only slows, Enemy.MinSlowMultiplier). Drawn on the ground under the
		// bodies, as the thing it is - motes of the force hung still in the air over a faint dark floor and a
		// rim of them where time stops - never a ring at the reach with nothing in it (2c). A few at a time
		// in fixed slots, one canvas for all of them.
		private const int MaxStasis = 6;
		private const float StasisTick = 0.1f;

		private struct Stasis
		{
			public Vector2 At;
			public float Radius;
			public float Age;
			public float Life;
			public float Seed;
			public bool Live;
		}

		private readonly Stasis[] stasis = new Stasis[MaxStasis];
		private int nextStasis;
		private float stasisTick;
		private Node2D stasisCanvas;
		private bool stasisDrawn;

		private void OpenStasis(Vector2 at, float radius, float seconds)
		{
			if (stasisCanvas == null || !IsInstanceValid(stasisCanvas))
			{
				stasisCanvas = new Node2D { Name = "Stasis", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Ground };
				stasisCanvas.Draw += DrawStasis;
				AddChild(stasisCanvas);
				stasisCanvas.GlobalPosition = Vector2.Zero;
			}
			stasis[nextStasis] = new Stasis { At = at, Radius = radius, Life = seconds * DurationMultiplier, Seed = (blastCount * 0.381966f) % 1f * 53f, Live = true };
			nextStasis = (nextStasis + 1) % MaxStasis;
		}

		private void TickStasis(float delta)
		{
			if (stasisCanvas == null || !IsInstanceValid(stasisCanvas))
				return;
			bool any = false;
			stasisTick -= delta;
			bool hold = stasisTick <= 0f;
			if (hold)
				stasisTick = StasisTick;
			for (int i = 0; i < MaxStasis; i++)
			{
				if (!stasis[i].Live)
					continue;
				any = true;
				stasis[i].Age += delta;
				float left = stasis[i].Life - stasis[i].Age;
				if (left <= 0f)
				{
					stasis[i].Live = false;
					continue;
				}
				if (!hold)
					continue;
				float r2 = stasis[i].Radius * stasis[i].Radius;
				foreach (Enemy e in EnemyRegistry.Snapshot)
				{
					if (IsInstanceValid(e) && e.GlobalPosition.DistanceSquaredTo(stasis[i].At) <= r2)
						e.ApplySlow(0f, left);
				}
			}
			if (any || stasisDrawn)
				stasisCanvas.QueueRedraw();
			stasisDrawn = any;
		}

		private static readonly Color StasisFloor = new Color(0.078f, 0.063f, 0.122f);   // violet deep
		private static readonly Color StasisMote = new Color(0.910f, 0.753f, 1.000f);    // arcane hot
		private static readonly Color StasisDim = new Color(0.478f, 0.247f, 0.831f);     // arcane edge
		private static readonly Color StasisCore = new Color(1.000f, 0.941f, 1.000f);    // arcane core

		private void DrawStasis()
		{
			for (int i = 0; i < MaxStasis; i++)
			{
				Stasis s = stasis[i];
				if (!s.Live)
					continue;
				float fade = Mathf.Clamp(s.Age / 0.15f, 0f, 1f) * Mathf.Clamp((s.Life - s.Age) / 0.35f, 0f, 1f);
				stasisCanvas.DrawSetTransform(s.At, 0f, new Vector2(1f, CraterGround.Squash));
				stasisCanvas.DrawCircle(Vector2.Zero, s.Radius, new Color(StasisFloor, 0.28f * fade));
				stasisCanvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
				// The rim: motes where time stops, every few degrees, still.
				int rimMotes = Math.Clamp((int)(s.Radius / 7f), 16, 72);
				for (int k = 0; k < rimMotes; k++)
				{
					float a = Mathf.Tau * k / rimMotes + s.Seed;
					Vector2 p = s.At + new Vector2(MathF.Cos(a) * s.Radius, MathF.Sin(a) * s.Radius * CraterGround.Squash);
					Color c = k % 3 == 0 ? StasisMote : StasisDim;
					stasisCanvas.DrawRect(WhipLimb.Cell(p, WhipLimb.Grid), new Color(c, fade));
				}
				// Motes hung in the sphere, where the blast left them: the same place every frame.
				int motes = Math.Clamp((int)(s.Radius * s.Radius / 900f), 10, 60);
				for (int k = 0; k < motes; k++)
				{
					float h1 = ((k * 0.6180339f + s.Seed) % 1f);
					float h2 = ((k * 0.7548777f + s.Seed * 0.31f) % 1f);
					float rr = MathF.Sqrt(h2) * s.Radius * 0.95f;
					float a = h1 * Mathf.Tau;
					Vector2 p = s.At + new Vector2(MathF.Cos(a) * rr, MathF.Sin(a) * rr * CraterGround.Squash);
					Color c = k % 5 == 0 ? StasisCore : StasisMote;
					stasisCanvas.DrawRect(WhipLimb.Cell(p, WhipLimb.Grid), new Color(c, 0.85f * fade));
				}
				DrawFermata(s.At + new Vector2(0f, -s.Radius * CraterGround.Squash - 8f), fade);
			}
		}

		// The fermata over the held note: an arc and a dot standing on the sphere's far rim, in the motes' own
		// light. Thirteen cells on the world's grid - a mark, not a sign.
		private void DrawFermata(Vector2 at, float fade)
		{
			float g = WhipLimb.Grid;
			Vector2 o = new(MathF.Round(at.X / g) * g, MathF.Round(at.Y / g) * g);
			for (int k = 0; k <= 10; k++)
			{
				float a = Mathf.Pi + Mathf.Pi * k / 10f;
				Vector2 p = o + new Vector2(MathF.Cos(a) * 9f, MathF.Sin(a) * 8f);
				stasisCanvas.DrawRect(WhipLimb.Cell(p, g), new Color(k == 5 ? StasisCore : StasisMote, fade));
			}
			stasisCanvas.DrawRect(new Rect2(o.X - g, o.Y - g, g * 2f, g * 2f), new Color(StasisCore, fade));
		}

		// Sustain (Lingering Rift's id; Effect ZoneDuration = seconds): the ground the blast went off on keeps
		// hurting for a while after - since 2026-11-09 the Canticle's note ringing on there, drawn as a standing
		// wave out to the zone's radius (scenes/shaders/arcane_rift.gdshader). It promised "a pulsating hazard
		// zone" and ArcaneExplosion left none until 2026-10-26. The zone is a LingeringZone, Mirefoot's pool: it
		// stays where the blast was while the player walks on, which is what makes it a zone.
		private const float RiftTickShare = 0.25f;
		private const float RiftRadiusShare = 0.8f;

		private void OpenLingeringRift()
		{
			float seconds = SpellData?.GetEffectValueAtLevel(SpellEffect.ZoneDuration, CurrentLevel) ?? 0f;
			if (seconds <= 0f || PlayerRef is not Player player)
				return;
			Node parent = player.GetParent();
			if (parent == null)
				return;

			var rift = new LingeringZone
			{
				Name = "LingeringRift",
				Radius = range * RiftRadiusShare,
				Duration = seconds * DurationMultiplier,
				TickSeconds = 0.5f,
				AppliesSlow = false,
				DamagePerTick = Math.Max(1, Mathf.RoundToInt(damage * RiftTickShare)),
				SpellData = SpellData,
				PlayerRef = player,
			};
			// A tear in the ground with the force wound out of it to the zone's radius, not a translucent
			// disc (scenes/shaders/arcane_rift.gdshader).
			BlastPoolSurface.Attach(rift, BlastPoolSurface.Kind.ArcaneRift);
			parent.AddChild(Player.TagSpell(rift));
			rift.GlobalPosition = GlobalPosition;
		}

		public void SetSpellLevel(int level)
		{
			CurrentLevel = Math.Max(1, level);
			RefreshComputedStats();
			UpdateScale();
		}

		private void RefreshComputedStats()
		{
			if (weapon != null)
			{
				ApplyLegacyWeapon(weapon);
				return;
			}

			damage = Math.Max(1, Mathf.RoundToInt((SpellData?.GetDamageAtLevel(CurrentLevel) ?? 5) * DamageMultiplier));
			// With the level table's Area line (Effect AreaSize, a fraction), read since 2026-10-26.
			range = (SpellData?.GetRangeAtLevel(CurrentLevel) ?? 100f) * AreaMultiplier * (SpellData?.GetAreaMultiplierAtLevel(CurrentLevel) ?? 1f)
				* (1f + (SpellData?.GetEffectValueAtLevel(SpellEffect.AreaSize, CurrentLevel) ?? 0f));
			cooldown = MathF.Max(0.05f, (SpellData?.GetCooldownAtLevel(CurrentLevel) ?? 1.5f) * CooldownMultiplier);
			// Effect Knockback from the level table and KnockbackBonus from an evolution (Dual
			// Shockwave) are the same units; the second was read by nothing until 2026-10-26.
			knockbackRange = MathF.Max(0f, BaseKnockbackRange + (SpellData?.GetEffectValueAtLevel(SpellEffect.Knockback, CurrentLevel) ?? 0f)
				+ (SpellData?.GetKnockbackBonusAtLevel(CurrentLevel) ?? 0f));
			knockbackSpeed = MathF.Max(0f, BaseKnockbackSpeed);
			resonance = SpellData != null && SpellData.HasEvolution(ResonanceId);
			hundredMouths = SpellData != null && SpellData.IsApotheosis
				&& string.Equals(SpellData.SelectedApotheosisId, HundredMouthsId, StringComparison.OrdinalIgnoreCase);
			if (IsInsideTree())
				Listen(resonance);
		}

		private void ApplyLegacyWeapon(Weapon value)
		{
			if (value == null) return;

			damage = value.Damage;
			range = value.Range;
			cooldown = MathF.Max(0.05f, value.Cooldown);
			knockbackRange = value.KnockbackRange;
			knockbackSpeed = value.KnockbackSpeed;
		}
	}
}
