using Godot;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TheOneHeMissed.scripts;

// A MainLoop intentionally has no autoloads: neither saves nor validation may warm the catalogs first.
[GlobalClass]
public partial class _UnlockStartupProbe : MainLoop
{
	private bool failed;
	public override void _Initialize()
	{
		try
		{
			bool achievementsFirst = Array.IndexOf(OS.GetCmdlineUserArgs(), "--achievements-first") >= 0;
			RuntimeHelpers.RunClassConstructor((achievementsFirst ? typeof(AchievementDefinitions) : typeof(UnlockCatalog)).TypeHandle);
			if (UnlockCatalog.All.Count == 0 || AchievementDefinitions.All.Count == 0)
				throw new Exception("Empty catalog");
			if (UnlockCatalog.GetCharacter("laelaps").SourceId != AchievementDefinitions.SecretDeedId(SecretCatalog.PinfoldId)
				|| UnlockCatalog.GetSpell(OrbOfAges.SpellId).SourceId != AchievementDefinitions.EventDeedId("the_moot"))
				throw new Exception("Rearranged rewards no longer link to their deeds");
			int spells = UnlockCatalog.GrimoireSpellIds.Count();
			if (spells == 0 || AchievementDefinitions.GetById(AchievementDefinitions.GrimoireRestoredId).Target != spells)
				throw new Exception("Incomplete Grimoire target");
			GD.Print($"UNLOCKPROBE {(achievementsFirst ? "achievements" : "unlocks")} first: PASS ({UnlockCatalog.All.Count} unlocks, {AchievementDefinitions.All.Count} deeds, {spells} Grimoire spells)");
		}
		catch (Exception ex)
		{
			failed = true;
			GD.PushError($"UNLOCKPROBE FAIL: {ex}");
		}
	}

	public override bool _Process(double delta)
	{
		if (failed) System.Environment.Exit(1);
		return true;
	}
}
