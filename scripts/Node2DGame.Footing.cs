using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FOOTING CENSUS AND THE PLACEMENT HASH (tools only; .ai/terrain.md section 13).
//
// --footing-census (any scene that runs Node2DGame; _BalanceHarness is the usual one) measures, over every
// chunk furnished in the run, what the stands do to the player's way through: every trunk's nearest gap
// to any other blocking footprint (another trunk, a rock, a themed prop that blocks, a fence rail, a body a
// side event or secret stands, and the water, wall and fence cells), and the POCKETS - ground the player's
// Feet could stand on that no path from outside reaches, found by a flood fill in the player's own terms
// (every layer-4 box grown by the Feet, every layer-1 box by the body 29.5 px above them). A chunk is
// counted once, when it and its eight neighbours are all furnished, so a pair across a border is seen with
// both of its members in place. It reads the bodies the run made, not the plan that made them.
//
// --placement-hash prints one hash of everything the first load placed: each painted chunk's terrain,
// roles and flags, its ground detail as GroundDetail plans it, and every prop under its holder (class,
// texture, position, flip). Two builds that print the same hash on the same --terrain-seed placed the same
// world - which is how the canopy profiles were shown to leave the Forest exactly as it was.
public partial class Node2DGame
{
	private bool footingCensus;
	private readonly HashSet<Vector2I> footingCensused = new();
	private int censusChunks, censusTrunks, censusUnder, censusClosed, censusPockets, censusTrunkPockets;
	private float censusMinGap = float.PositiveInfinity;
	private float censusPocketArea, censusRoomArea;
	private int censusRooms, censusTrunkRooms;
	private readonly int[] censusGapHistogram = new int[6];

	// The resolution of the census's flood fill, world px.
	private const float CensusPixel = 4f;
	// How far past a chunk the flood fill reaches, so a pocket on its border is seen whole.
	private const float CensusMargin = 384f;

	private void ReadFootingToolFlags()
	{
		string[] args = OS.GetCmdlineUserArgs();
		footingCensus = args.Contains("--footing-census");
	}

	/// <summary>Called once the first chunks are furnished: the hash, and the census of what is complete.</summary>
	private void RunFootingToolsAfterInitialLoad()
	{
		if (OS.GetCmdlineUserArgs().Contains("--placement-hash"))
			PrintPlacementHash();
		if (footingCensus)
			CensusCompleteChunks();
	}

	/// <summary>Per frame while --footing-census is on: any chunk whose neighbourhood has become complete.</summary>
	private void TickFootingCensus()
	{
		if (footingCensus && terrainStreamer != null && terrainStreamer.IsSettled)
			CensusCompleteChunks();
	}

	private void CensusCompleteChunks()
	{
		if (terrainStreamer == null)
			return;
		var populated = new HashSet<Vector2I>();
		foreach (TerrainChunk chunk in terrainStreamer.DataChunks)
			if (chunk.Populated)
				populated.Add(chunk.Coord);
		List<(Rect2 Box, int Kind)>? blockers = null;
		foreach (Vector2I coord in populated.OrderBy(c => c.Y).ThenBy(c => c.X))
		{
			if (footingCensused.Contains(coord))
				continue;
			bool complete = true;
			for (int dy = -1; dy <= 1 && complete; dy++)
				for (int dx = -1; dx <= 1 && complete; dx++)
					complete = populated.Contains(coord + new Vector2I(dx, dy));
			if (!complete)
				continue;
			blockers ??= CollectBlockers();
			footingCensused.Add(coord);
			CensusChunk(terrainStreamer.DataFor(coord), blockers);
		}
		if (blockers != null)
			PrintFootingSummary();
	}

	// Kinds of blocking box, for the census: a trunk, another box on the ground's layer (a fence rail, a
	// site's wall), a box on layer 1 (a rock, a themed prop), which the body meets rather than the Feet.
	private const int BlockTrunk = 0, BlockGround = 1, BlockBody = 2;

	private List<(Rect2 Box, int Kind)> CollectBlockers()
	{
		var list = new List<(Rect2, int)>();
		uint groundBit = 1u << (LevelTilePainter.TerrainCollisionLayer - 1);
		var stack = new Stack<Node>();
		stack.Push(this);
		while (stack.Count > 0)
		{
			Node node = stack.Pop();
			foreach (Node child in node.GetChildren())
				stack.Push(child);
			if (node is not StaticBody2D body || (body.CollisionLayer & (1u | groundBit)) == 0)
				continue;
			bool ground = (body.CollisionLayer & groundBit) != 0;
			bool trunk = ground && body.IsInGroup(DecorPropGroup);
			foreach (Node part in body.GetChildren())
			{
				if (part is not CollisionShape2D shape || shape.Disabled || shape.Shape is not RectangleShape2D box)
					continue;
				Vector2 size = box.Size * shape.GlobalScale.Abs();
				list.Add((new Rect2(shape.GlobalPosition - size * 0.5f, size), trunk ? BlockTrunk : ground ? BlockGround : BlockBody));
			}
		}
		return list;
	}

	private void CensusChunk(TerrainChunk chunk, List<(Rect2 Box, int Kind)> blockers)
	{
		TerrainStreamer ground = terrainStreamer!;
		float tile = ProceduralTilePixels;
		Vector2 min = ground.CellOriginWorld(chunk.MinCell);
		var rect = new Rect2(min, new Vector2(TerrainStreamer.ChunkTiles * tile, TerrainStreamer.ChunkTiles * tile));
		Rect2 region = rect.Grow(CensusMargin);
		Vector2 origin = ground.CellOriginWorld(Vector2I.Zero);

		// 1. Every trunk standing in this chunk: its nearest gap to anything else that blocks.
		var near = new List<(Rect2 Box, int Kind)>();
		foreach (var b in blockers)
			if (b.Box.Intersects(region))
				near.Add(b);
		int trunks = 0, under = 0, closed = 0;
		foreach (var t in near)
		{
			if (t.Kind != BlockTrunk || !rect.HasPoint(t.Box.GetCenter()))
				continue;
			trunks++;
			float best = float.PositiveInfinity;
			foreach (var o in near)
			{
				if (o.Box == t.Box && o.Kind == BlockTrunk)
					continue;
				best = Mathf.Min(best, Footing.Gap(t.Box, o.Box));
			}
			Rect2 reach = t.Box.Grow(Footing.Clearance);
			for (int cy = Mathf.FloorToInt((reach.Position.Y - origin.Y) / tile); cy <= Mathf.FloorToInt((reach.End.Y - origin.Y) / tile); cy++)
				for (int cx = Mathf.FloorToInt((reach.Position.X - origin.X) / tile); cx <= Mathf.FloorToInt((reach.End.X - origin.X) / tile); cx++)
					if (ground.IsSolidCell(cx, cy))
						best = Mathf.Min(best, Footing.Gap(t.Box, new Rect2(origin + new Vector2(cx, cy) * tile, new Vector2(tile, tile))));
			if (best < Footing.Clearance) under++;
			if (best < Footing.FeetWidth) closed++;
			censusMinGap = Mathf.Min(censusMinGap, best);
			int bucket = best < 24f ? 0 : best < 48f ? 1 : best < Footing.FeetWidth ? 2 : best < Footing.Clearance ? 3 : best < 120f ? 4 : 5;
			censusGapHistogram[bucket]++;
		}

		// 2. Pockets, twice. In the Feet's terms (every layer-4 box grown by the Feet, every layer-1 box by
		// the body): ground he could stand on that no path from outside reaches. In the crowd's terms
		// (every box grown by half the clearance, all round): a ROOM - open ground whose every way out is
		// narrower than the clearance, the corner a swarm closes behind him.
		Vector2I c0 = ground.CellOfWorld(region.Position), c1 = ground.CellOfWorld(region.End);
		var solidCells = new List<Rect2>();
		for (int cy = c0.Y; cy <= c1.Y; cy++)
			for (int cx = c0.X; cx <= c1.X; cx++)
				if (ground.IsSolidCell(cx, cy))
					solidCells.Add(new Rect2(origin + new Vector2(cx, cy) * tile, new Vector2(tile, tile)));
		float half = Footing.Clearance * 0.5f - 1f;
		(int pockets, int trunkPockets, float area) = CensusPockets(rect, region, near, solidCells,
			b => b.Kind == BlockBody ? Footing.BodyForbids(b.Box) : Footing.FeetForbids(b.Box), Footing.FeetWidth * Footing.FeetDepth);
		(int rooms, int trunkRooms, float roomArea) = CensusPockets(rect, region, near, solidCells,
			b => b.Box.Grow(half), CensusPixel * CensusPixel * 16f);
		censusPocketArea = Mathf.Max(censusPocketArea, area);
		censusRoomArea = Mathf.Max(censusRoomArea, roomArea);
		censusRooms += rooms;
		censusTrunkRooms += trunkRooms;

		censusChunks++;
		censusTrunks += trunks;
		censusUnder += under;
		censusClosed += closed;
		censusPockets += pockets;
		censusTrunkPockets += trunkPockets;
		if (under > 0 || pockets > 0)
			GD.Print($"FOOTING chunk {chunk.Coord}: {trunks} trunks, {under} under {Footing.Clearance:0} px ({closed} under the Feet's {Footing.FeetWidth:0}), "
				+ $"{pockets} pockets ({trunkPockets} with a trunk in their wall)");
	}

	// One flood fill: every box forbids what grow() says, the solid cells as layer-4 boxes; the region's
	// edge is outside. Returns the pockets with a cell in the chunk and at least minArea px2, how many have a
	// trunk in their wall, and the largest.
	private static (int Pockets, int ByTrunk, float Largest) CensusPockets(Rect2 rect, Rect2 region,
		List<(Rect2 Box, int Kind)> boxes, List<Rect2> solidCells, Func<(Rect2 Box, int Kind), Rect2> grow, float minArea)
	{
		int w = Mathf.CeilToInt(region.Size.X / CensusPixel), h = Mathf.CeilToInt(region.Size.Y / CensusPixel);
		var owner = new byte[w * h]; // 0 free, 1 a trunk forbids it, 2 something else does
		void Forbid(Rect2 zone, byte who)
		{
			int x0 = Math.Max(0, Mathf.FloorToInt((zone.Position.X - region.Position.X) / CensusPixel));
			int y0 = Math.Max(0, Mathf.FloorToInt((zone.Position.Y - region.Position.Y) / CensusPixel));
			int x1 = Math.Min(w - 1, Mathf.FloorToInt((zone.End.X - region.Position.X) / CensusPixel));
			int y1 = Math.Min(h - 1, Mathf.FloorToInt((zone.End.Y - region.Position.Y) / CensusPixel));
			for (int y = y0; y <= y1; y++)
				for (int x = x0; x <= x1; x++)
				{
					int i = x + y * w;
					if (owner[i] == 0 || who == 1)
						owner[i] = who;
				}
		}
		foreach (var b in boxes)
			Forbid(grow(b), b.Kind == BlockTrunk ? (byte)1 : (byte)2);
		foreach (Rect2 cell in solidCells)
			Forbid(grow((cell, BlockGround)), 2);

		var reached = new bool[w * h];
		var queue = new Queue<int>();
		void Seed(int i) { if (owner[i] == 0 && !reached[i]) { reached[i] = true; queue.Enqueue(i); } }
		for (int x = 0; x < w; x++) { Seed(x); Seed(x + (h - 1) * w); }
		for (int y = 0; y < h; y++) { Seed(y * w); Seed(w - 1 + y * w); }
		while (queue.Count > 0)
		{
			int i = queue.Dequeue();
			int x = i % w, y = i / w;
			if (x > 0) Seed(i - 1);
			if (x < w - 1) Seed(i + 1);
			if (y > 0) Seed(i - w);
			if (y < h - 1) Seed(i + w);
		}
		int pockets = 0, byTrunk = 0;
		float largest = 0f;
		var fill = new Queue<int>();
		for (int start = 0; start < w * h; start++)
		{
			if (owner[start] != 0 || reached[start])
				continue;
			int cells = 0;
			bool trunkWall = false, inChunk = false;
			reached[start] = true;
			fill.Enqueue(start);
			while (fill.Count > 0)
			{
				int i = fill.Dequeue();
				cells++;
				int x = i % w, y = i / w;
				if (rect.HasPoint(region.Position + new Vector2(x + 0.5f, y + 0.5f) * CensusPixel))
					inChunk = true;
				for (int k = 0; k < 4; k++)
				{
					int n = k == 0 ? (x > 0 ? i - 1 : -1) : k == 1 ? (x < w - 1 ? i + 1 : -1) : k == 2 ? (y > 0 ? i - w : -1) : (y < h - 1 ? i + w : -1);
					if (n < 0) continue;
					if (owner[n] == 1) trunkWall = true;
					if (owner[n] == 0 && !reached[n]) { reached[n] = true; fill.Enqueue(n); }
				}
			}
			float area = cells * CensusPixel * CensusPixel;
			// Smaller than that is a rasterising crumb between two boxes that already touch, not a place.
			if (!inChunk || area < minArea)
				continue;
			pockets++;
			if (trunkWall) byTrunk++;
			largest = Mathf.Max(largest, area);
		}
		return (pockets, byTrunk, largest);
	}

	private void PrintFootingSummary()
	{
		GD.Print($"FOOTING census: {censusChunks} chunks, {censusTrunks} trunks; nearest gap to anything that blocks: "
			+ $"{censusUnder} under {Footing.Clearance:0} px, {censusClosed} under the Feet's {Footing.FeetWidth:0} (min "
			+ $"{(float.IsPositiveInfinity(censusMinGap) ? "-" : censusMinGap.ToString("0.0"))}); "
			+ $"gaps <24 {censusGapHistogram[0]}, 24-48 {censusGapHistogram[1]}, 48-56 {censusGapHistogram[2]}, 56-72 {censusGapHistogram[3]}, "
			+ $"72-120 {censusGapHistogram[4]}, 120+ {censusGapHistogram[5]}; pockets {censusPockets} ({censusTrunkPockets} walled by a trunk, "
			+ $"largest {censusPocketArea:0} px2); narrow-mouthed rooms {censusRooms} ({censusTrunkRooms} walled by a trunk, largest {censusRoomArea:0} px2)");
	}

	private void PrintPlacementHash()
	{
		if (terrainStreamer == null)
			return;
		ulong hash = 0xC0FFEE;
		void Mix(ulong v) => hash = LevelGenerator.Mix(hash ^ v);
		void MixString(string? s)
		{
			if (s == null) { Mix(0x5EED); return; }
			foreach (char ch in s) Mix(ch);
			Mix((ulong)s.Length);
		}

		int chunks = 0, props = 0, details = 0;
		var detail = new string[TerrainStreamer.ChunkTiles * TerrainStreamer.ChunkTiles];
		Func<int, int, bool> reserve = (cx, cy) => IsInExploreReserve(stageOrigin + new Vector2(cx + 0.5f, cy + 0.5f) * ProceduralTilePixels);
		foreach (TerrainChunk chunk in terrainStreamer.DataChunks.Where(c => c.Populated).OrderBy(c => c.Coord.Y).ThenBy(c => c.Coord.X))
		{
			chunks++;
			Mix((ulong)(uint)chunk.Coord.X); Mix((ulong)(uint)chunk.Coord.Y);
			for (int i = 0; i < chunk.Terrain.Length; i++)
			{
				MixString(chunk.Terrain[i]);
				Mix((ulong)chunk.Roles[i]);
				Mix(chunk.Flags[i]);
			}
			GroundDetail.PlanChunk(terrainStreamer, chunk, terrainKind, terrainSeed, LandmarkCells(chunk), detail, reserve);
			foreach (string? set in detail)
			{
				MixString(set);
				if (set != null) details++;
			}
			if (chunk.Props == null || chunk.Props.Count == 0)
				continue;
			var lines = new List<string>();
			foreach (Node node in chunk.Props[0].GetChildren())
			{
				if (node is not Node2D n2)
					continue;
				Sprite2D? sprite = n2 as Sprite2D ?? n2.GetChildren().OfType<Sprite2D>().FirstOrDefault();
				Vector2 at = n2.GlobalPosition - stageOrigin;
				lines.Add($"{n2.GetType().Name}|{sprite?.Texture?.ResourcePath}|{at.X:0.00},{at.Y:0.00}|{sprite?.FlipH}|{sprite?.Scale.X:0.00}");
			}
			lines.Sort(StringComparer.Ordinal);
			foreach (string line in lines)
				MixString(line);
			props += lines.Count;
		}
		GD.Print($"PLACEMENT hash {hash:X16} over {chunks} chunks: {props} props, {details} detail cells (stage {terrainKind}, seed {terrainSeed})");
	}
}
