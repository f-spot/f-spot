//
// OpenWithApps.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using FSpot.FileSystem;
using FSpot.Utils;

using Hyena;

namespace FSpot.Avalonia.Services;

/// <summary>
/// An application that can open the selected photos, as listed by GTK's OpenWithMenu.
/// </summary>
public sealed record OpenWithApp (string Id, string Name, string Target);

public interface IOpenWithService
{
	IReadOnlyList<OpenWithApp> ApplicationsFor (IReadOnlyList<string> paths);
	void Open (OpenWithApp app, IReadOnlyList<string> paths);
}

public static class OpenWithApps
{
	// GTK's OpenWithMenu skips F-Spot itself.
	public const string IgnoredApplication = "f-spot";

	public static IOpenWithService Current { get; } = Create ();

	static IOpenWithService Create ()
	{
		if (OperatingSystem.IsMacOS ())
			return new MacOpenWith ();
		if (OperatingSystem.IsLinux () || OperatingSystem.IsFreeBSD ())
			return new XdgOpenWith (XdgOpenWith.DefaultApplicationDirectories ());
		return new NoOpenWith ();
	}

	public static IReadOnlyList<string> MimeTypes (IEnumerable<string> paths)
	{
		var file = new DotNetFile ();
		var types = new List<string> ();
		foreach (string path in paths) {
			string type;
			try {
				type = file.GetMimeType (new SafeUri (path));
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "No mime type for {Path}", path);
				continue;
			}
			if (!string.IsNullOrEmpty (type) && !types.Contains (type))
				types.Add (type);
		}
		return types;
	}
}

sealed class NoOpenWith : IOpenWithService
{
	public IReadOnlyList<OpenWithApp> ApplicationsFor (IReadOnlyList<string> paths) => [];
	public void Open (OpenWithApp app, IReadOnlyList<string> paths) { }
}

/// <summary>
/// freedesktop.org lookup: mimeinfo.cache and mimeapps.list name the .desktop files for a type.
/// </summary>
public sealed class XdgOpenWith : IOpenWithService
{
	readonly IReadOnlyList<string> directories;

	public XdgOpenWith (IReadOnlyList<string> applicationDirectories)
		=> directories = applicationDirectories;

	public static IReadOnlyList<string> DefaultApplicationDirectories ()
	{
		var dirs = new List<string> ();
		string? home = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
		if (string.IsNullOrEmpty (home))
			home = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".local", "share");
		dirs.Add (Path.Combine (home, "applications"));
		string? data = Environment.GetEnvironmentVariable ("XDG_DATA_DIRS");
		if (string.IsNullOrEmpty (data))
			data = "/usr/local/share:/usr/share";
		foreach (string dir in data.Split (':', StringSplitOptions.RemoveEmptyEntries))
			dirs.Add (Path.Combine (dir, "applications"));
		return dirs;
	}

	public IReadOnlyList<OpenWithApp> ApplicationsFor (IReadOnlyList<string> paths)
	{
		var apps = new List<OpenWithApp> ();
		foreach (string type in OpenWithApps.MimeTypes (paths)) {
			foreach (string id in DesktopIdsFor (type)) {
				if (apps.Any (app => app.Id == id))
					continue;
				if (FindDesktopFile (id) is not { } file)
					continue;
				var entry = XdgDesktopEntry.Parse (File.ReadAllText (file));
				if (entry is null || entry.NoDisplay || string.IsNullOrEmpty (entry.Exec))
					continue;
				if (XdgDesktopEntry.Program (entry.Exec) == OpenWithApps.IgnoredApplication)
					continue;
				apps.Add (new OpenWithApp (id, entry.Name, entry.Exec));
			}
		}
		return apps;
	}

	public void Open (OpenWithApp app, IReadOnlyList<string> paths)
	{
		var args = XdgDesktopEntry.ExpandExec (app.Target, paths);
		if (args.Count == 0)
			return;
		var start = new ProcessStartInfo (args[0]) { UseShellExecute = false };
		foreach (string arg in args.Skip (1))
			start.ArgumentList.Add (arg);
		Process.Start (start)?.Dispose ();
	}

	IEnumerable<string> DesktopIdsFor (string type)
	{
		foreach (string dir in directories) {
			foreach (string name in new[] { "mimeapps.list", "mimeinfo.cache" }) {
				string file = Path.Combine (dir, name);
				if (!File.Exists (file))
					continue;
				foreach (string id in XdgDesktopEntry.AssociationsFor (File.ReadAllText (file), type))
					yield return id;
			}
		}
	}

	string? FindDesktopFile (string id)
	{
		foreach (string dir in directories) {
			string file = Path.Combine (dir, id);
			if (File.Exists (file))
				return file;
			// Desktop ids map "-" to subdirectories, e.g. kde4-foo.desktop -> kde4/foo.desktop.
			string nested = Path.Combine (dir, id.Replace ('-', Path.DirectorySeparatorChar));
			if (File.Exists (nested))
				return nested;
		}
		return null;
	}
}

/// <summary>
/// The parts of the Desktop Entry and MIME association specs Open With needs.
/// </summary>
public sealed class XdgDesktopEntry
{
	public string Name { get; private init; } = string.Empty;
	public string Exec { get; private init; } = string.Empty;
	public bool NoDisplay { get; private init; }

	public static XdgDesktopEntry? Parse (string text)
	{
		string? name = null;
		string? exec = null;
		bool hidden = false;
		bool inEntry = false;
		foreach (string raw in text.Split ('\n')) {
			string line = raw.Trim ();
			if (line.StartsWith ('[')) {
				inEntry = line == "[Desktop Entry]";
				continue;
			}
			if (!inEntry || line.Length == 0 || line.StartsWith ('#'))
				continue;
			int eq = line.IndexOf ('=');
			if (eq <= 0)
				continue;
			string key = line[..eq].Trim ();
			string value = line[(eq + 1)..].Trim ();
			switch (key) {
			case "Name":
				name = value;
				break;
			case "Exec":
				exec = value;
				break;
			case "NoDisplay":
			case "Hidden":
				hidden |= value.Equals ("true", StringComparison.OrdinalIgnoreCase);
				break;
			case "Type":
				if (value != "Application")
					return null;
				break;
			}
		}
		if (name is null)
			return null;
		return new XdgDesktopEntry { Name = name, Exec = exec ?? string.Empty, NoDisplay = hidden };
	}

	// Desktop ids for a type from a mimeinfo.cache ([MIME Cache]) or mimeapps.list
	// ([Default Applications] / [Added Associations]) file.
	public static IReadOnlyList<string> AssociationsFor (string text, string mimeType)
	{
		var ids = new List<string> ();
		bool inSection = false;
		foreach (string raw in text.Split ('\n')) {
			string line = raw.Trim ();
			if (line.StartsWith ('[')) {
				inSection = line is "[MIME Cache]" or "[Default Applications]" or "[Added Associations]";
				continue;
			}
			if (!inSection)
				continue;
			int eq = line.IndexOf ('=');
			if (eq <= 0 || line[..eq].Trim () != mimeType)
				continue;
			foreach (string id in line[(eq + 1)..].Split (';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				if (!ids.Contains (id))
					ids.Add (id);
			}
		}
		return ids;
	}

	public static string Program (string exec)
	{
		var args = Tokenize (exec);
		return args.Count == 0 ? string.Empty : Path.GetFileName (args[0]);
	}

	// Expands the Exec field codes: %f/%u take the first file, %F/%U every file, %% is a literal
	// percent and the deprecated or icon codes drop out. Without a file code the files are appended.
	public static IReadOnlyList<string> ExpandExec (string exec, IReadOnlyList<string> paths)
	{
		var result = new List<string> ();
		bool placed = false;
		foreach (string token in Tokenize (exec)) {
			switch (token) {
			case "%f":
				if (paths.Count > 0)
					result.Add (paths[0]);
				placed = true;
				continue;
			case "%u":
				if (paths.Count > 0)
					result.Add (new Uri (Path.GetFullPath (paths[0])).AbsoluteUri);
				placed = true;
				continue;
			case "%F":
				result.AddRange (paths);
				placed = true;
				continue;
			case "%U":
				result.AddRange (paths.Select (path => new Uri (Path.GetFullPath (path)).AbsoluteUri));
				placed = true;
				continue;
			case "%i":
			case "%c":
			case "%k":
			case "%d":
			case "%D":
			case "%n":
			case "%N":
			case "%v":
			case "%m":
				continue;
			}
			result.Add (token.Replace ("%%", "%"));
		}
		if (!placed)
			result.AddRange (paths);
		return result;
	}

	static List<string> Tokenize (string exec)
	{
		var tokens = new List<string> ();
		var current = new StringBuilder ();
		bool quoted = false;
		bool any = false;
		for (int i = 0; i < exec.Length; i++) {
			char c = exec[i];
			if (quoted && c == '\\' && i + 1 < exec.Length) {
				current.Append (exec[++i]);
				continue;
			}
			if (c == '"') {
				quoted = !quoted;
				any = true;
				continue;
			}
			if (!quoted && char.IsWhiteSpace (c)) {
				if (any || current.Length > 0)
					tokens.Add (current.ToString ());
				current.Clear ();
				any = false;
				continue;
			}
			current.Append (c);
		}
		if (any || current.Length > 0)
			tokens.Add (current.ToString ());
		return tokens;
	}
}

/// <summary>
/// macOS Launch Services: the applications that can view each photo, launched with open -a.
/// </summary>
sealed class MacOpenWith : IOpenWithService
{
	const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
	const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
	const uint LSRolesViewer = 0x00000002;
	const uint LSRolesEditor = 0x00000004;
	const int PosixPathStyle = 0;

	public IReadOnlyList<OpenWithApp> ApplicationsFor (IReadOnlyList<string> paths)
	{
		var apps = new List<OpenWithApp> ();
		foreach (string path in paths.Take (16)) {
			try {
				foreach (string bundle in ApplicationsForPath (path)) {
					if (apps.Any (app => app.Id == bundle))
						continue;
					string name = Path.GetFileNameWithoutExtension (bundle);
					if (name.Equals (OpenWithApps.IgnoredApplication, StringComparison.OrdinalIgnoreCase)
							|| name.Equals ("F-Spot", StringComparison.OrdinalIgnoreCase))
						continue;
					apps.Add (new OpenWithApp (bundle, name, bundle));
				}
			} catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) {
				Logger.Log.Debug (ex, "Launch Services is not available");
				return apps;
			}
		}
		apps.Sort ((a, b) => string.Compare (a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
		return apps;
	}

	public void Open (OpenWithApp app, IReadOnlyList<string> paths)
	{
		var start = new ProcessStartInfo ("/usr/bin/open") { UseShellExecute = false };
		start.ArgumentList.Add ("-a");
		start.ArgumentList.Add (app.Target);
		foreach (string path in paths)
			start.ArgumentList.Add (path);
		Process.Start (start)?.Dispose ();
	}

	static List<string> ApplicationsForPath (string path)
	{
		var bundles = new List<string> ();
		IntPtr cfPath = CreateString (path);
		IntPtr url = IntPtr.Zero;
		IntPtr array = IntPtr.Zero;
		try {
			url = CFURLCreateWithFileSystemPath (IntPtr.Zero, cfPath, PosixPathStyle, false);
			if (url == IntPtr.Zero)
				return bundles;
			array = LSCopyApplicationURLsForURL (url, LSRolesViewer | LSRolesEditor);
			if (array == IntPtr.Zero)
				return bundles;
			long count = CFArrayGetCount (array);
			for (long i = 0; i < count; i++) {
				IntPtr appUrl = CFArrayGetValueAtIndex (array, i);
				IntPtr appPath = CFURLCopyFileSystemPath (appUrl, PosixPathStyle);
				try {
					string? bundle = ReadString (appPath);
					if (!string.IsNullOrEmpty (bundle) && !bundles.Contains (bundle))
						bundles.Add (bundle);
				} finally {
					Release (appPath);
				}
			}
		} finally {
			Release (array);
			Release (url);
			Release (cfPath);
		}
		return bundles;
	}

	static IntPtr CreateString (string value)
		=> CFStringCreateWithCharacters (IntPtr.Zero, value, value.Length);

	static string? ReadString (IntPtr cfString)
	{
		if (cfString == IntPtr.Zero)
			return null;
		long length = CFStringGetLength (cfString);
		var buffer = new char[length];
		CFStringGetCharacters (cfString, new CFRange (0, length), buffer);
		return new string (buffer);
	}

	static void Release (IntPtr value)
	{
		if (value != IntPtr.Zero)
			CFRelease (value);
	}

	[StructLayout (LayoutKind.Sequential)]
	readonly struct CFRange
	{
		public CFRange (long location, long length)
		{
			Location = location;
			Length = length;
		}

		public long Location { get; }
		public long Length { get; }
	}

	[DllImport (CoreServices)]
	static extern IntPtr LSCopyApplicationURLsForURL (IntPtr inURL, uint inRoleMask);

	[DllImport (CoreFoundation, CharSet = CharSet.Unicode)]
	static extern IntPtr CFStringCreateWithCharacters (IntPtr alloc, string chars, long numChars);

	[DllImport (CoreFoundation)]
	static extern long CFStringGetLength (IntPtr theString);

	[DllImport (CoreFoundation, CharSet = CharSet.Unicode)]
	static extern void CFStringGetCharacters (IntPtr theString, CFRange range, [Out] char[] buffer);

	[DllImport (CoreFoundation)]
	static extern IntPtr CFURLCreateWithFileSystemPath (IntPtr allocator, IntPtr filePath, int pathStyle, [MarshalAs (UnmanagedType.I1)] bool isDirectory);

	[DllImport (CoreFoundation)]
	static extern IntPtr CFURLCopyFileSystemPath (IntPtr anURL, int pathStyle);

	[DllImport (CoreFoundation)]
	static extern long CFArrayGetCount (IntPtr theArray);

	[DllImport (CoreFoundation)]
	static extern IntPtr CFArrayGetValueAtIndex (IntPtr theArray, long idx);

	[DllImport (CoreFoundation)]
	static extern void CFRelease (IntPtr cf);
}
