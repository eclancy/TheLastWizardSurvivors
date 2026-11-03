using Godot;
using TheOneHeMissed.scripts;

// The Hollow Choir's note: a sung wave that leaves a voice's cowl and flies at where the wizard stood.
//
// Eric, playing 0.10.0: "Their ranged attacks don't appear to have an animation coming from them.
// Instead of a circle and arrow, show some sort of attack that they launch." The round's notes used to
// be TelegraphedGroundHits - a dashed ring and the arrow glyph on the floor at his feet, landing a beat
// later, with nothing between the voice and the ring to say who sent it. Now the voice SINGS it (the
// `sing` strip: its mouths light from the hem up, the cowl flares) and the note crosses the open ground
// to him as a thing he can watch come and step out of the way of, as the cultist's bolt is.
//
// An EnemyProjectile in everything but its look, so everything that already guards against a bolt -
// the Familiar's hounds, Solar Flare's Daybreak Avatar, Spiritual Weapon's blades, Laelaps's jaws -
// takes a note out of the air too, and cave stone stops it. Created in code (HollowChoirBoss.LaunchNote)
// with its collision circle as a child, so its hit and its picture come from one radius.
//
// THE LOOK is the Choir's own song, the arcs its fall and its arrival lift off the cowl (BossDeath.Arc),
// sent forward: a crescent of three arcs, nearest the brightest, on the 2-unit art grid in the song's
// water ramp - white-hot at the core, ChoirColor at the edge (art-direction 3, "Elements are emissive").
// Its node is never turned (the cells would leave the grid); the arcs are turned round its heading.
public partial class ChoirNote : EnemyProjectile
{
	/// <summary>The Choir's colour, for the outer arc.</summary>
	public Color SongColor { get; set; } = new Color(0.45f, 0.86f, 0.92f);

	private float age;

	/// <summary>Makes a note with its collision circle, ready to be added and launched.</summary>
	public static ChoirNote Make(float radius, float speed, int damage, float lifetime, Color song)
	{
		var note = new ChoirNote
		{
			Radius = radius,
			Speed = speed,
			Damage = damage,
			LifetimeSeconds = lifetime,
			SongColor = song,
		};
		note.AddChild(new CollisionShape2D { Name = "CollisionShape2D", Shape = new CircleShape2D { Radius = radius } });
		return note;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Never turned: EnemyProjectile turns the node along its heading for its tail, and a turned node
		// draws its cells off the art grid. The heading it flies along is its own field, not this.
		Rotation = 0f;
		base._PhysicsProcess(delta);
		age += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
		float facing = Heading.Angle();
		// It swells out of the cowl over its first tenth of a second, and its arcs breathe as it flies.
		float grow = Mathf.Min(1f, age / 0.12f);
		float breathe = 0.5f + 0.5f * Mathf.Sin(age * 18f);
		float r = Radius * (0.55f + 0.45f * grow);
		// Under it, a stroke of occlusion, so the light reads on the pale floors as on the black stone.
		BossDeath.Arc(this, Vector2.Zero, r + 2f, r + 2f, facing - 1.05f, facing + 1.05f, new Color(0.02f, 0.03f, 0.05f, 0.85f), BossDeath.Px * 2f);
		// The leading arc: the note itself, the full width of what it hits.
		BossDeath.Arc(this, Vector2.Zero, r, r, facing - 0.95f, facing + 0.95f, water.Hot, BossDeath.Px * 2f);
		BossDeath.Arc(this, Vector2.Zero, r, r, facing - 0.45f, facing + 0.45f, water.Core);
		// Two smaller behind it, the song still leaving the voice, dimming toward the tail.
		Vector2 back = -Heading;
		BossDeath.Arc(this, back * (r * 0.55f), r * 0.68f, r * 0.68f, facing - 0.85f, facing + 0.85f,
			new Color(water.Mid, 0.75f + 0.25f * breathe));
		BossDeath.Arc(this, back * (r * 1.05f), r * 0.42f, r * 0.42f, facing - 0.75f, facing + 0.75f,
			new Color(SongColor, 0.5f + 0.3f * breathe));
		// The heart of it, where it will strike: one hot cell and its glow.
		BossDeath.Disc(this, Heading * (r * 0.25f), 3f, 3f, new Color(water.Core, 0.9f));
	}
}
