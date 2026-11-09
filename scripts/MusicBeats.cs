using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Every piece of music's key and counted beat, as the score that wrote it says: read from
/// assets/music/beats.json, which tools/audio/music.py writes with every render (and alone with
/// <c>--beats</c>). Nothing in code knows a tempo: a piece re-scored and re-rendered is a beat the game
/// counts, with no second copy here to forget.
/// </summary>
/// <remarks>
/// WHO ASKS. The Canticle (the Cumaean Sibyl's spell, ArcaneExplosion; .ai/character-fit.md section 5,
/// row 19): its burst waits for the next counted beat of the music that is sounding and sounds its chord
/// in that music's key, and The Hundred Mouths' echoes land on the beats after it. Where the beat is
/// (MusicPlayer.TryGetBeat) is read off the playing stream; what a beat IS - its length, where it falls
/// in a bar, the key - is read here.
///
/// RegressionChecks.ValidateMusicBeats holds every set that can play, the shared boss loop and the menu
/// theme to an entry, and every entry's loop length to its rendered file's, so a re-render at a new tempo
/// without the map is reported at startup rather than heard as a burst off the beat.
/// </remarks>
public static class MusicBeats
{
	public const string Manifest = MusicCatalog.Directory + "beats.json";

	/// <summary>One piece's beat and key.</summary>
	public sealed class Piece
	{
		public string Id { get; init; } = string.Empty;
		/// <summary>The file its stems share a clock with (its first stem), for the length check.</summary>
		public string File { get; init; } = string.Empty;
		public double Bpm { get; init; }
		/// <summary>The counted beat: a dotted quarter in 6/8 and 9/8, an eighth in the Swamp's 7/8.</summary>
		public double BeatSeconds { get; init; }
		public int BeatsPerBar { get; init; }
		/// <summary>Where in a bar a beat is counted, in beats from the barline, in order. Every beat but
		/// the Swamp's, which is counted on its three group starts (2+2+3).</summary>
		public double[] Counted { get; init; } = Array.Empty<double>();
		public int Bars { get; init; }
		public double LoopSeconds { get; init; }
		public string Tonic { get; init; } = "A";
		/// <summary>The tonic in the octave from middle C (60..71).</summary>
		public int TonicMidi { get; init; } = 69;
		public string Mode { get; init; } = "aeolian";
		/// <summary>The Canticle's chord on the tonic: semitones above it, in the order its notes gather.</summary>
		public int[] Chord { get; init; } = { 0, 7, 3, 12 };

		public double BarSeconds => BeatSeconds * BeatsPerBar;

		/// <summary>The longest wait from one counted beat to the next (the Swamp's long group).</summary>
		public double LongestGap
		{
			get
			{
				double longest = 0.0;
				for (int i = 0; i < Counted.Length; i++)
				{
					double next = i + 1 < Counted.Length ? Counted[i + 1] : BeatsPerBar;
					longest = Math.Max(longest, (next - Counted[i]) * BeatSeconds);
				}
				return longest;
			}
		}

		/// <summary>
		/// The counted beat at or before <paramref name="position"/> (seconds into the loop): its ordinal in
		/// the loop, how long ago it fell, and how long until the next one.
		/// </summary>
		public void Locate(double position, out int ordinal, out double since, out double toNext)
		{
			double bar = BarSeconds;
			double loop = LoopSeconds > 0.0 ? LoopSeconds : bar * Math.Max(1, Bars);
			double p = position % loop;
			if (p < 0.0)
				p += loop;
			int barIndex = Math.Clamp((int)Math.Floor(p / bar), 0, Math.Max(0, Bars - 1));
			double inBar = p - barIndex * bar;
			int k = 0;
			for (int i = 0; i < Counted.Length; i++)
			{
				if (Counted[i] * BeatSeconds <= inBar + 1e-9)
					k = i;
				else
					break;
			}
			double at = Counted.Length > 0 ? Counted[k] * BeatSeconds : 0.0;
			double next = k + 1 < Counted.Length ? Counted[k + 1] * BeatSeconds : bar;
			ordinal = barIndex * Math.Max(1, Counted.Length) + k;
			since = Math.Max(0.0, inBar - at);
			toNext = Math.Max(0.0, next - inBar);
		}
	}

	private static Dictionary<string, Piece> pieces;
	private static Dictionary<string, Piece> byFile;

	/// <summary>Why the map did not load, or null.</summary>
	public static string LoadProblem { get; private set; }

	/// <summary>Every piece in the map, by id (a MusicSet's id, "boss" for the shared loop, "menu").</summary>
	public static IReadOnlyDictionary<string, Piece> All
	{
		get
		{
			EnsureLoaded();
			return pieces;
		}
	}

	/// <summary>A piece by its id, or null.</summary>
	public static Piece For(string id)
	{
		EnsureLoaded();
		return id != null && pieces.TryGetValue(id, out Piece piece) ? piece : null;
	}

	/// <summary>A piece by one of its files (a solo loop's stream: boss_loop.wav, menu_theme.wav), or null.</summary>
	public static Piece ForFile(string fileName)
	{
		EnsureLoaded();
		return fileName != null && byFile.TryGetValue(fileName, out Piece piece) ? piece : null;
	}

	private static void EnsureLoaded()
	{
		if (pieces != null)
			return;
		pieces = new Dictionary<string, Piece>(StringComparer.OrdinalIgnoreCase);
		byFile = new Dictionary<string, Piece>(StringComparer.OrdinalIgnoreCase);
		if (!FileAccess.FileExists(Manifest))
		{
			LoadProblem = $"{Manifest} is missing - run python tools/audio/music.py --beats";
			GD.PushWarning($"MusicBeats: {LoadProblem}");
			return;
		}
		try
		{
			using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(Manifest));
			foreach (JsonProperty entry in doc.RootElement.GetProperty("pieces").EnumerateObject())
			{
				JsonElement e = entry.Value;
				var counted = new List<double>();
				foreach (JsonElement c in e.GetProperty("counted").EnumerateArray())
					counted.Add(c.GetDouble());
				var chord = new List<int>();
				foreach (JsonElement c in e.GetProperty("chord").EnumerateArray())
					chord.Add(c.GetInt32());
				var piece = new Piece
				{
					Id = entry.Name,
					File = e.GetProperty("file").GetString() ?? string.Empty,
					Bpm = e.GetProperty("bpm").GetDouble(),
					BeatSeconds = e.GetProperty("beat_seconds").GetDouble(),
					BeatsPerBar = e.GetProperty("beats_per_bar").GetInt32(),
					Counted = counted.ToArray(),
					Bars = e.GetProperty("bars").GetInt32(),
					LoopSeconds = e.GetProperty("loop_seconds").GetDouble(),
					Tonic = e.GetProperty("tonic").GetString() ?? "A",
					TonicMidi = e.GetProperty("tonic_midi").GetInt32(),
					Mode = e.GetProperty("mode").GetString() ?? string.Empty,
					Chord = chord.Count > 0 ? chord.ToArray() : new[] { 0 },
				};
				pieces[piece.Id] = piece;
				if (piece.File.Length > 0)
					byFile[piece.File] = piece;
			}
		}
		catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException)
		{
			// Reported, never swallowed: RegressionChecks.ValidateMusicBeats prints this, and with no map the
			// Canticle bursts the moment its chord is full, as it did before it had a beat to wait for.
			LoadProblem = $"{Manifest} is malformed ({ex.Message}) - re-run python tools/audio/music.py --beats";
			GD.PushWarning($"MusicBeats: {LoadProblem}");
			pieces.Clear();
			byFile.Clear();
		}
	}
}
