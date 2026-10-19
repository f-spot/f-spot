// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

using FSpot.Cms;

using Hyena;

namespace FSpot.Avalonia;

// The "System profile" entry FSpot.Gtk's PreferenceDialog offers whenever
// FSpot.ColorManagement.XProfile is non-null: the profile the OS currently
// applies to the display, read the same best-effort way -- cached once,
// degrading to unavailable rather than throwing into a window constructor.
//
// The primary display only. macOS copies the main display's ColorSync profile.
// Windows asks GDI for the screen DC's ICM profile and resolves a bare file
// name against the spool colour directory. Linux reads the X11 root window's
// _ICC_PROFILE atom, which is what GTK reads; a Wayland session with no X11
// profile stays unavailable.
static class SystemColorProfile
{
	// The same sentinel FSpot.Gtk persists for "use whatever the system
	// reports as current" (ColorManagement.cs, PreferenceDialog.cs) -- the
	// two clients share one preferences store.
	public const string Sentinel = "_x_profile_";

	const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
	const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

	[DllImport (CoreGraphics)]
	static extern uint CGMainDisplayID ();

	[DllImport (CoreGraphics)]
	static extern IntPtr CGDisplayCopyColorSpace (uint display);

	[DllImport (CoreGraphics)]
	static extern void CGColorSpaceRelease (IntPtr space);

	[DllImport (CoreGraphics)]
	static extern IntPtr CGColorSpaceCopyICCData (IntPtr space);

	[DllImport (CoreFoundation)]
	static extern long CFDataGetLength (IntPtr data);

	[DllImport (CoreFoundation)]
	static extern IntPtr CFDataGetBytePtr (IntPtr data);

	[DllImport (CoreFoundation)]
	static extern void CFRelease (IntPtr cf);

	static Profile? current;
	static bool attempted;

	public static bool IsAvailable => Current is not null;

	// Cached process-wide, like FSpot.ColorManagement.XProfile, and never
	// disposed -- it is a shared instance, not one a caller can own.
	public static Profile? Current {
		get {
			if (!attempted) {
				attempted = true;
				current = Read ();
			}
			return current;
		}
	}

	// A rooted path is the profile. A bare file name joins the spool colour
	// directory. An empty report is no profile.
	internal static string? Combine (string? directory, string? reported)
	{
		if (string.IsNullOrWhiteSpace (reported))
			return null;
		if (Path.IsPathRooted (reported))
			return reported;
		return Path.Combine (directory ?? string.Empty, reported);
	}

	// Injected bytes, so a test can open a profile without a real display.
	// Empty or rejected bytes are null rather than a throw.
	internal static Profile? TryOpen (byte[]? bytes)
	{
		if (bytes == null || bytes.Length == 0)
			return null;
		try {
			return new Profile (bytes);
		} catch (Exception e) when (e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Ignoring the unreadable system display profile: {e.Message}");
			return null;
		}
	}

	static Profile? Read ()
	{
		if (OperatingSystem.IsWindows ())
			return ReadWindows ();
		if (OperatingSystem.IsMacOS ())
			return ReadMac ();
		if (OperatingSystem.IsLinux ())
			return ReadLinux ();
		return null;
	}

	static Profile? ReadMac ()
	{
		IntPtr space = IntPtr.Zero;
		IntPtr data = IntPtr.Zero;
		try {
			space = CGDisplayCopyColorSpace (CGMainDisplayID ());
			if (space == IntPtr.Zero)
				return null;

			data = CGColorSpaceCopyICCData (space);
			if (data == IntPtr.Zero)
				return null;

			long length = CFDataGetLength (data);
			if (length <= 0 || length > int.MaxValue)
				return null;

			var bytes = new byte[length];
			Marshal.Copy (CFDataGetBytePtr (data), bytes, 0, (int)length);
			return TryOpen (bytes);
		} catch (Exception e) when (e is CmsException || e is DllNotFoundException
				|| e is EntryPointNotFoundException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Ignoring the unreadable system display profile: {e.Message}");
			return null;
		} finally {
			if (data != IntPtr.Zero)
				CFRelease (data);
			if (space != IntPtr.Zero)
				CGColorSpaceRelease (space);
		}
	}

	[DllImport ("user32.dll")]
	static extern IntPtr GetDC (IntPtr hwnd);

	[DllImport ("user32.dll")]
	static extern int ReleaseDC (IntPtr hwnd, IntPtr hdc);

	[DllImport ("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern bool GetICMProfile (IntPtr hdc, ref uint size, StringBuilder? filename);

	static Profile? ReadWindows ()
	{
		IntPtr dc = IntPtr.Zero;
		try {
			dc = GetDC (IntPtr.Zero);
			if (dc == IntPtr.Zero)
				return null;

			uint size = 0;
			GetICMProfile (dc, ref size, null);
			if (size <= 1 || size > 32768)
				return null;

			var name = new StringBuilder ((int)size);
			if (!GetICMProfile (dc, ref size, name))
				return null;

			string directory = ProfileCatalog.Directories (CmsPlatform.Windows, null,
				Environment.GetEnvironmentVariable ("SystemRoot"))[0];
			string? path = Combine (directory, name.ToString ());
			if (string.IsNullOrEmpty (path) || !File.Exists (path))
				return null;
			return TryOpen (File.ReadAllBytes (path));
		} catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException
				|| e is IOException || e is UnauthorizedAccessException
				|| ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Ignoring the unreadable system display profile: {e.Message}");
			return null;
		} finally {
			if (dc != IntPtr.Zero)
				ReleaseDC (IntPtr.Zero, dc);
		}
	}

	const string LibX11 = "libX11.so.6";

	[DllImport (LibX11)]
	static extern IntPtr XOpenDisplay (IntPtr name);

	[DllImport (LibX11)]
	static extern int XCloseDisplay (IntPtr display);

	[DllImport (LibX11)]
	static extern IntPtr XDefaultRootWindow (IntPtr display);

	[DllImport (LibX11)]
	static extern IntPtr XInternAtom (IntPtr display, string name, int onlyIfExists);

	// X11 Bool is an int. long_offset and long_length count 32-bit units.
	[DllImport (LibX11)]
	static extern int XGetWindowProperty (IntPtr display, IntPtr window, IntPtr property,
		IntPtr longOffset, IntPtr longLength, int delete, IntPtr reqType,
		out IntPtr actualType, out int actualFormat, out nuint nitems, out nuint bytesAfter,
		out IntPtr prop);

	[DllImport (LibX11)]
	static extern int XFree (IntPtr data);

	static Profile? ReadLinux ()
	{
		IntPtr display = IntPtr.Zero;
		IntPtr data = IntPtr.Zero;
		try {
			display = XOpenDisplay (IntPtr.Zero);
			if (display == IntPtr.Zero)
				return null;

			IntPtr root = XDefaultRootWindow (display);
			IntPtr atom = XInternAtom (display, "_ICC_PROFILE", 0);
			if (atom == IntPtr.Zero)
				return null;

			// XA_CARDINAL. long_length counts 32-bit units, so this asks for at most 2 MiB.
			int status = XGetWindowProperty (display, root, atom, IntPtr.Zero, new IntPtr (512 * 1024),
				0, (IntPtr)6, out _, out int format, out nuint nitems, out nuint bytesAfter, out data);
			if (status != 0 || data == IntPtr.Zero || nitems == 0 || bytesAfter != 0)
				return null;
			if (format is not (8 or 16 or 32))
				return null;

			ulong nbytes = nitems * ((ulong)format / 8);
			if (nbytes == 0 || nbytes > int.MaxValue)
				return null;
			var bytes = new byte[nbytes];
			Marshal.Copy (data, bytes, 0, (int)nbytes);
			return TryOpen (bytes);
		} catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException
				|| e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Ignoring the unreadable system display profile: {e.Message}");
			return null;
		} finally {
			if (data != IntPtr.Zero)
				XFree (data);
			if (display != IntPtr.Zero)
				XCloseDisplay (display);
		}
	}
}
