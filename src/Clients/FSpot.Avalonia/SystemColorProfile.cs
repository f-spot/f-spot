// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using FSpot.Cms;

using Hyena;

namespace FSpot.Avalonia;

// The "System profile" entry FSpot.Gtk's PreferenceDialog offers whenever
// FSpot.ColorManagement.XProfile is non-null: the profile the OS currently
// applies to the display, read the same best-effort way -- cached once,
// degrading to unavailable rather than throwing into a window constructor.
//
// X11 publishes that profile on the root window's _ICC_PROFILE atom; macOS
// hands it out through CoreGraphics/ColorSync instead, so this reads the
// main display's colour space and copies its embedded ICC data.
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

	static Profile? Read ()
	{
		if (!OperatingSystem.IsMacOS ())
			return null;

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
			return new Profile (bytes);
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
}
