using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TheOneHeMissed.scripts;
using HttpClient = System.Net.Http.HttpClient;

/// <summary>
/// The first thing a downloaded copy of the game shows: checks ecec.dev for a newer version and,
/// if there is one, installs it and restarts - no questions. Ported from DrawFight, where it was
/// built and tested end to end first.
///
/// How a release is laid out is in .ai/releasing.md. In short: ecec.dev/the-last-wizard-survivors/version.json
/// redirects to the newest GitHub Release's version.json, which names, under "builds", a small patch
/// (the .pck and the game's own DLL - everything a normal change touches) and the full download
/// (needed only when the Godot engine itself changed) for Windows and for Linux. GitHub serves the
/// files, so downloads cost nothing.
///
/// The running game cannot overwrite itself on Windows - its DLL and .pck are open - so the new
/// files are unpacked to user://update/ and a small script waits for the game to close, copies
/// them over, and starts it again. Linux does the same with a shell script.
///
/// The files are named for the program that is running, not for the release (<see cref="ProgramName"/>).
/// A copy installed before the game was renamed runs as the program of the old name, and its shortcuts,
/// Start menu entry and app-menu entry all point at that file, so it keeps it: the new .pck is installed
/// under its name. How such a copy got the renamed game in the first place - the "bridge" a release
/// carries for builds from before the rename, which read the top-level "patch", "full" and "linux"
/// fields this build ignores - is in .ai/releasing.md, "The rename".
///
/// It is not a scene of its own. TitleScreen stays the main scene (run/main_scene, which every
/// harness and probe assumes) and lays this over the artwork before anything else on it starts;
/// when the check is done this frees itself and the title carries on exactly as it would have.
/// Every failure, from no internet onwards, just goes to the title. An update is never worth not
/// being able to play.
///
/// The one question it ever asks: a release that restarts the story (its version.json carries a
/// "progressEpoch" above <see cref="SaveData.CurrentProgressEpoch"/>) is not installed until the
/// player says Update, because installing it is what resets their progress. Not now plays this
/// version, and the next launch asks again. See .ai/decisions/2026-11-02-story-resets.md.
/// </summary>
public partial class UpdateScreen : Control
{
	/// <summary>The one address baked into every copy of the game. It is on ecec.dev rather than
	/// GitHub so where releases live can change without stranding old copies. Copies from before the
	/// renames have the game's old addresses baked in instead - /wizard-survivors/ up to 0.11.0,
	/// /the-one-he-missed/ from 0.12.0 to 0.20.0 - and ecec.dev answers them the same way, forever
	/// (.ai/releasing.md, "The second rename").</summary>
	public const string VersionUrl = "https://ecec.dev/the-last-wizard-survivors/version.json";

	/// <summary>Points a build at a test server instead - see .ai/releasing.md.</summary>
	public const string UrlOverrideVariable = "THEONEHEMISSED_UPDATE_URL";

	/// <summary>The User-Agent of every request the game makes: the update checks and the bug report.</summary>
	public static string UserAgent => $"TheOneHeMissed/{CurrentVersion}";

	[Signal] public delegate void FinishedEventHandler();

	public static string CurrentVersion =>
		ProjectSettings.GetSetting("application/config/version", "0.0.0").AsString();

	/// <summary>
	/// The running program's name without its extension - TheOneHeMissed for TheOneHeMissed.exe or
	/// TheOneHeMissed.x86_64. Godot loads the .pck of this name from beside the program, so an update is
	/// installed under it whatever the release calls its files. That is what keeps a copy installed before
	/// the rename (still WizardSurvivors.exe, which every shortcut to it names) updating.
	/// </summary>
	static string ProgramName => Path.GetFileNameWithoutExtension(OS.GetExecutablePath());

	static string EngineVersion
	{
		get
		{
			var info = Engine.GetVersionInfo();
			return $"{info["major"]}.{info["minor"]}.{info["patch"]}";
		}
	}

	/// <summary>
	/// What both update checks say before a release that restarts the story is downloaded: here as
	/// the question, and on Android's strip (UpdateBanner) beside Download.
	/// </summary>
	public const string StoryResetWarning =
		"This update restarts the story: your progress will be reset. Your current save is kept as a backup.";

	/// <summary>
	/// The story epoch a release was built at - version.json's "progressEpoch", written by release.sh
	/// from <see cref="SaveData.CurrentProgressEpoch"/>. 0 for a release from before epochs existed,
	/// which never restarts anything.
	/// </summary>
	public static int ProgressEpochOf(JsonElement release) =>
		release.TryGetProperty("progressEpoch", out JsonElement epoch)
			&& epoch.ValueKind == JsonValueKind.Number && epoch.TryGetInt32(out int value) ? value : 0;

	/// <summary>True when installing this release will reset the player's progress.</summary>
	public static bool RestartsStory(JsonElement release) =>
		ProgressEpochOf(release) > SaveData.CurrentProgressEpoch;

	/// <summary>
	/// Plays the whole check against a made-up release instead of the network, in any build:
	/// <c>--update-screen=9.9.9</c> (no epoch: behaves as an ordinary update) or
	/// <c>--update-screen=9.9.9:99</c> (built at an epoch above this build's, so the question is asked; any
	/// number at or below <see cref="SaveData.CurrentProgressEpoch"/> asks nothing). Nothing is ever
	/// downloaded; Update just carries on to the title. For photographing the question with _UiShot -
	/// see .ai/releasing.md, "Releasing a story reset". Android's strip has --update-banner.
	/// </summary>
	const string TestArgPrefix = "--update-screen=";

	static string TestArg()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith(TestArgPrefix))
				return arg.Substring(TestArgPrefix.Length);
		}
		return null;
	}

	/// <summary>A test argument's version.json: "9.9.9" or "9.9.9:99" (version, then story epoch).</summary>
	public static string TestReleaseJson(string arg)
	{
		string[] parts = arg.Split(':');
		var nothing = new System.Collections.Generic.Dictionary<string, string> { ["patch"] = "", ["full"] = "" };
		var release = new System.Collections.Generic.Dictionary<string, object>
		{
			["version"] = parts[0],
			["engine"] = "test",
			["builds"] = new System.Collections.Generic.Dictionary<string, object> { ["windows"] = nothing, ["linux"] = nothing },
			["android"] = "https://ecec.dev/the-last-wizard-survivors/download",
		};
		if (parts.Length > 1 && int.TryParse(parts[1], out int epoch))
			release["progressEpoch"] = epoch;
		return JsonSerializer.Serialize(release);
	}

	// Once per launch. Everything that returns "to the main menu" reloads TitleScreen, and a
	// second check every time the player quits a run would be a pause for nothing.
	static bool checkedThisLaunch;

	/// <summary>Only an exported Windows or Linux build updates itself, and only once per launch.
	/// Running from the editor or from `godot --path .` never does, and `--no-update` turns it off
	/// for testing a build. Android has UpdateBanner instead.</summary>
	public static bool ShouldCheck()
	{
		if (checkedThisLaunch) return false;
		if (TestArg() != null) return true;
		if (!OS.HasFeature("template") || (!Windows && !Linux)) return false;
		return Array.IndexOf(OS.GetCmdlineUserArgs(), "--no-update") < 0
			&& Array.IndexOf(OS.GetCmdlineArgs(), "--no-update") < 0;
	}

	static bool Windows => OS.GetName() == "Windows";
	static bool Linux => OS.GetName() == "Linux";

	enum Phase { Checking, Asking, Downloading, Installing, Restart, Done }

	// Written by the worker thread, read by the main thread. Everything the screen needs is a
	// plain value, so there is nothing to lock. The one value going the other way - the player's
	// answer to the story-reset question - is a TaskCompletionSource, which is thread-safe.
	volatile Phase phase = Phase.Checking;
	volatile float progress;
	string newVersion = "";
	string applyScript = "";
	TaskCompletionSource<bool> answer;
	Control question;

	Label title;
	Label detail;
	ColorRect barBack;
	ColorRect barFill;
	int frames;
	bool finished;

	// The title screen's own ground (#05070C) and the bone-white and gold BonelightSkin uses for
	// menu type and focus, so this reads as the title screen not having finished arriving yet
	// rather than as a different program.
	static readonly Color Ground = new Color(0.019608f, 0.027451f, 0.047059f);
	static readonly Color Bone = new Color(0.94f, 0.93f, 0.88f);
	static readonly Color Quiet = new Color(0.70f, 0.72f, 0.78f);
	static readonly Color Gold = new Color(0.85f, 0.69f, 0.29f);

	const float BarWidth = 440f;
	const float BarHeight = 18f;

	public override void _Ready()
	{
		checkedThisLaunch = true;

		// AndOffsets: this runs already in the tree, where SetAnchorsPreset alone keeps the current
		// rect - a zero-size one at the corner - so the ground, the labels and the bar were all laid
		// out in nothing (photographed 2026-11-02 for the story-reset question, which made it visible).
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		// Swallows clicks, so nothing on the title underneath can be pressed mid-update.
		MouseFilter = MouseFilterEnum.Stop;

		AddChild(new ColorRect
		{
			Color = Ground,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = MouseFilterEnum.Ignore,
		});

		title = CenteredLabel(-70f, 60f, ResponsiveLayout.TextRole.Title, Bone);
		detail = CenteredLabel(40f, 40f, ResponsiveLayout.TextRole.Body, Quiet);

		barBack = new ColorRect { Color = new Color(Bone, 0.18f), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
		barFill = new ColorRect { Color = Gold, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
		foreach (var bar in new[] { barBack, barFill })
		{
			bar.AnchorLeft = 0.5f;
			bar.AnchorRight = 0.5f;
			bar.AnchorTop = 0.5f;
			bar.AnchorBottom = 0.5f;
			bar.OffsetLeft = -BarWidth / 2f;
			bar.OffsetRight = BarWidth / 2f;
			bar.OffsetTop = 0f;
			bar.OffsetBottom = BarHeight;
			AddChild(bar);
		}

		Task.Run(Work);
	}

	Label CenteredLabel(float top, float height, ResponsiveLayout.TextRole role, Color color)
	{
		var label = new Label
		{
			AnchorLeft = 0f,
			AnchorRight = 1f,
			AnchorTop = 0.5f,
			AnchorBottom = 0.5f,
			OffsetTop = top,
			OffsetBottom = top + height,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		// In the tree first: SetFont asks the viewport whether this is a narrow screen.
		AddChild(label);
		ResponsiveLayout.SetFont(label, role);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	public override void _Process(double delta)
	{
		frames++;
		switch (phase)
		{
			case Phase.Done:
				if (finished) return;
				finished = true;
				EmitSignal(SignalName.Finished);
				QueueFree();
				return;

			case Phase.Restart:
				if (finished) return;
				finished = true;
				// The script waits for this process to end, so it has to be started first.
				if (Linux) OS.CreateProcess("/bin/sh", new[] { applyScript });
				else OS.CreateProcess("cmd.exe", new[] { "/c", applyScript });
				GetTree().Quit();
				return;

			case Phase.Checking:
				// Most checks finish before anyone could read this; it only shows when the
				// internet is slow, and then it says why the game has not started.
				title.Text = frames > 45 ? "Checking for updates..." : "";
				detail.Text = "";
				break;

			case Phase.Asking:
				if (question == null)
					AskAboutStoryReset();
				break;

			case Phase.Downloading:
				// The question is answered; its buttons must not stay pressable over the bar.
				if (question != null)
				{
					question.QueueFree();
					question = null;
				}
				title.Text = $"Updating {DisplayProfile.GameTitle}";
				detail.Text = $"Getting version {newVersion}...  {Mathf.RoundToInt(progress * 100.0f)}%";
				ShowBar(progress);
				break;

			case Phase.Installing:
				title.Text = $"Updating {DisplayProfile.GameTitle}";
				detail.Text = "Nearly done - the game will restart by itself.";
				ShowBar(1f);
				break;
		}
	}

	void ShowBar(float filled)
	{
		barBack.Visible = true;
		barFill.Visible = true;
		barFill.OffsetRight = barFill.OffsetLeft + BarWidth * Mathf.Clamp(filled, 0f, 1f);
	}

	/// <summary>
	/// The release restarts the story: say so and wait for the player. One line, two buttons -
	/// Update (selected, so A or Enter takes it) and Not now (B, Escape, or the button), which
	/// plays this version and asks again at the next launch.
	/// </summary>
	/// <remarks>
	/// On this screen's own ground, in the title's colours, so it reads as the title asking rather
	/// than as a dialog from somewhere else. The title underneath ignores input while this screen is
	/// up (TitleScreen.updating), so neither answer also counts as "press any button".
	/// </remarks>
	void AskAboutStoryReset()
	{
		title.Text = "";
		detail.Text = "";

		var center = new CenterContainer { Name = "StoryResetQuestion", MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);
		question = center;

		var column = new VBoxContainer
		{
			// Wide enough for the line to break into two on a PC; a phone gets its full width less a margin.
			CustomMinimumSize = new Vector2(Mathf.Min(640f, GetViewportRect().Size.X - 48f), 0f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		column.AddThemeConstantOverride("separation", 22);
		center.AddChild(column);

		Label Line(string text, ResponsiveLayout.TextRole role, Color color)
		{
			var label = new Label
			{
				Text = text,
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			column.AddChild(label);
			ResponsiveLayout.SetFont(label, role);
			label.AddThemeColorOverride("font_color", color);
			return label;
		}
		Line($"Version {newVersion} is out", ResponsiveLayout.TextRole.Title, Bone);
		Line(StoryResetWarning, ResponsiveLayout.TextRole.Body, Gold);

		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 16);
		column.AddChild(row);
		Button update = AnswerButton(row, "UpdateNow", "Update", BonelightSkin.Emphasis.Primary, true);
		AnswerButton(row, "NotNow", "Not now", BonelightSkin.Emphasis.Quiet, false);

		MenuBackListener.Attach(this, () => Answer(false), "ui_cancel");
		SafeArea.Apply(this);
		MenuNavigator.Attach(this, update);
	}

	Button AnswerButton(HBoxContainer row, string name, string text, BonelightSkin.Emphasis emphasis, bool yes)
	{
		var button = new Button { Name = name, Text = text, FocusMode = FocusModeEnum.All };
		row.AddChild(button);
		BonelightSkin.StyleButton(button, emphasis);
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Body);
		ResponsiveLayout.EnsureTouchTarget(button, 160f);
		button.Pressed += () => Answer(yes);
		return button;
	}

	void Answer(bool update)
	{
		if (answer == null || answer.Task.IsCompleted)
			return;
		SfxPlayer.Global(update ? SfxCatalog.UiClick : SfxCatalog.UiClose);
		// Nothing more can be pressed: the worker carries on to the download, or to the title.
		if (question != null)
			question.Visible = false;
		answer.TrySetResult(update);
	}

	// --- The worker -----------------------------------------------------------------

	async Task Work()
	{
		try
		{
			phase = await Update() ? Phase.Restart : Phase.Done;
		}
		catch (Exception e)
		{
			GD.Print($"UpdateScreen: no update ({e.GetType().Name}: {e.Message})");
			phase = Phase.Done;
		}
	}

	/// <summary>True when a new version is unpacked and the restart script is written.</summary>
	async Task<bool> Update()
	{
		string test = TestArg();
		string url = OS.GetEnvironment(UrlOverrideVariable);
		if (string.IsNullOrEmpty(url)) url = VersionUrl;
		using JsonDocument doc = JsonDocument.Parse(test != null ? TestReleaseJson(test) : await FetchVersionJson(url));
		JsonElement release = doc.RootElement;
		string latest = release.GetProperty("version").GetString() ?? "";
		if (!IsNewer(latest, CurrentVersion))
		{
			GD.Print($"UpdateScreen: {CurrentVersion} is up to date");
			return false;
		}

		string installDir = Path.GetDirectoryName(OS.GetExecutablePath());
		if (test == null && !CanWriteTo(installDir))
		{
			GD.Print($"UpdateScreen: {latest} is out, but {installDir} is not writable");
			return false;
		}

		// The patch only works on the engine it was built with; anything else takes the full download.
		// This copy's pair is under "builds" - never the top-level "patch", "full" and "linux", which
		// since the rename are the bridge for builds from before it (.ai/releasing.md, "The rename"),
		// a whole game in every download. A release with no build for this platform has nothing to
		// update to.
		bool sameEngine = release.TryGetProperty("engine", out JsonElement engine)
			&& engine.GetString() == EngineVersion;
		string platform = Linux ? "linux" : "windows";
		if (!release.TryGetProperty("builds", out JsonElement builds) || !builds.TryGetProperty(platform, out JsonElement files))
		{
			GD.Print($"UpdateScreen: {latest} has no {platform} build");
			return false;
		}
		url = sameEngine && files.TryGetProperty("patch", out JsonElement patch)
			? patch.GetString()
			: files.GetProperty("full").GetString();

		newVersion = latest;

		// Last, so the question is only ever asked about an update that can actually be installed.
		// Installing this one resets the player's progress (SaveManager keeps the old save aside), so
		// it is the player's to choose; the answer arrives from the main thread.
		if (RestartsStory(release))
		{
			answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			phase = Phase.Asking;
			if (!await answer.Task)
			{
				GD.Print($"UpdateScreen: {latest} restarts the story; the player chose to stay on {CurrentVersion} for now");
				return false;
			}
		}

		if (test != null)
		{
			GD.Print($"UpdateScreen: test release {latest} - nothing to download (--update-screen)");
			return false;
		}
		phase = Phase.Downloading;

		string work = ProjectSettings.GlobalizePath("user://update");
		if (Directory.Exists(work)) Directory.Delete(work, true);
		Directory.CreateDirectory(work);
		bool tarball = url.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);
		string zipPath = Path.Combine(work, tarball ? "update.tar.gz" : "update.zip");
		await Download(url, zipPath);

		phase = Phase.Installing;
		string unpacked = Path.Combine(work, "files");
		Directory.CreateDirectory(unpacked);
		if (tarball)
		{
			// The Linux download is a tarball, so the game binary keeps its permission to run.
			using FileStream file = File.OpenRead(zipPath);
			using var gz = new GZipStream(file, CompressionMode.Decompress);
			TarFile.ExtractToDirectory(gz, unpacked, overwriteFiles: true);
		}
		else
		{
			ZipFile.ExtractToDirectory(zipPath, unpacked);
		}
		File.Delete(zipPath);

		// The full download keeps everything in a folder so it unpacks tidily for a person; the patch
		// does not. Either way, copy from wherever the .pck is, under this program's name.
		string pck = FindPck(unpacked);
		if (pck == null) throw new InvalidDataException("the update has no .pck");
		string from = Path.GetDirectoryName(pck);
		InstallAs(from, Path.GetFileNameWithoutExtension(pck), ProgramName);

		applyScript = Path.Combine(work, Linux ? "apply.sh" : "apply.cmd");
		File.WriteAllText(applyScript, Linux
			? ApplyScriptLinux(from, installDir, OS.GetExecutablePath(), OS.GetProcessId())
			: ApplyScript(from, installDir, OS.GetExecutablePath(), OS.GetProcessId()));
		GD.Print($"UpdateScreen: {CurrentVersion} -> {latest} ready, restarting");
		return true;
	}

	static async Task<string> FetchVersionJson(string url)
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
		http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		return await http.GetStringAsync(url);
	}

	async Task Download(string url, string path)
	{
		// A separate client: the five-second timeout above is for the check, not for a full zip
		// on a slow connection.
		using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
		http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		using HttpResponseMessage response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();
		long total = response.Content.Headers.ContentLength ?? 0;

		progress = 0.0f;
		using Stream source = await response.Content.ReadAsStreamAsync();
		using FileStream target = File.Create(path);
		var buffer = new byte[81920];
		long done = 0;
		int read;
		while ((read = await source.ReadAsync(buffer)) > 0)
		{
			await target.WriteAsync(buffer.AsMemory(0, read));
			done += read;
			if (total > 0) progress = done / (float)total;
		}
	}

	/// <summary>The one .pck in the unpacked download, at its top or one folder down; null if none.</summary>
	static string FindPck(string dir)
	{
		var folders = new System.Collections.Generic.List<string> { dir };
		folders.AddRange(Directory.GetDirectories(dir));
		foreach (string folder in folders)
		{
			string[] pcks = Directory.GetFiles(folder, "*.pck");
			if (pcks.Length > 1) throw new InvalidDataException($"the update has {pcks.Length} .pck files in one folder");
			if (pcks.Length == 1) return pcks[0];
		}
		return null;
	}

	/// <summary>
	/// Renames the release's own files in <paramref name="dir"/> - every one called
	/// <paramref name="releaseName"/>, whatever its extension: the .pck, and in a full download the
	/// program too - to <paramref name="programName"/>, so the copy that follows replaces the files this
	/// install actually runs. Nothing to do when the names already agree, which is every install made since
	/// the rename. The .NET folder is named for the assembly, not the program, and needs nothing.
	/// </summary>
	static void InstallAs(string dir, string releaseName, string programName)
	{
		if (releaseName == programName)
			return;
		foreach (string file in Directory.GetFiles(dir))
		{
			if (Path.GetFileNameWithoutExtension(file) != releaseName)
				continue;
			string renamed = Path.Combine(dir, programName + Path.GetExtension(file));
			File.Move(file, renamed, overwrite: true);
			GD.Print($"UpdateScreen: {Path.GetFileName(file)} installs as {Path.GetFileName(renamed)}");
		}
	}

	static bool CanWriteTo(string dir)
	{
		try
		{
			string probe = Path.Combine(dir, ".update-probe");
			File.WriteAllText(probe, "");
			File.Delete(probe);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>"0.10.0" is newer than "0.9.2": compared number by number, not as text.</summary>
	public static bool IsNewer(string candidate, string current)
	{
		string[] a = candidate.Split('.');
		string[] b = current.Split('.');
		for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
		{
			int x = i < a.Length && int.TryParse(a[i], out int ax) ? ax : 0;
			int y = i < b.Length && int.TryParse(b[i], out int by) ? by : 0;
			if (x != y) return x > y;
		}
		return false;
	}

	/// <summary>
	/// Waits for the game to close, copies the new files over the old, and starts it again.
	/// `ping` is the sleep because `timeout` refuses to run without a console to read from.
	/// robocopy retries a locked file a few times, which covers the moment Windows takes to let
	/// go of a closed process's files.
	/// </summary>
	static string ApplyScript(string from, string to, string exe, int pid) =>
		"@echo off\r\n" +
		":wait\r\n" +
		$"tasklist /FI \"PID eq {pid}\" 2>nul | find \"{pid}\" >nul\r\n" +
		"if not errorlevel 1 (\r\n" +
		"  ping -n 2 127.0.0.1 >nul\r\n" +
		"  goto wait\r\n" +
		")\r\n" +
		$"robocopy \"{from}\" \"{to}\" /E /R:10 /W:1 /NFL /NDL /NJH /NJS /NP >nul\r\n" +
		$"start \"\" \"{exe}\"\r\n" +
		$"rmdir /s /q \"{from}\"\r\n";

	/// <summary>
	/// The same on Linux: wait for the game to close, copy the new files over the old, make sure
	/// the game can still be run, and start it again. Linux would let the files be replaced while
	/// the game runs, but the game would then be half old and half new until it restarted.
	/// </summary>
	static string ApplyScriptLinux(string from, string to, string exe, int pid) =>
		"#!/bin/sh\n" +
		$"while kill -0 {pid} 2>/dev/null; do sleep 0.5; done\n" +
		$"cp -Rf {Quoted(from + "/.")} {Quoted(to + "/")}\n" +
		$"chmod +x {Quoted(exe)}\n" +
		$"rm -rf {Quoted(from)}\n" +
		$"cd {Quoted(to)} && nohup {Quoted(exe)} >/dev/null 2>&1 &\n";

	/// <summary>A path in single quotes for sh, with any quote in it kept.</summary>
	static string Quoted(string path) => "'" + path.Replace("'", "'\\''") + "'";
}
