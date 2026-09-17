// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace FSpot.Avalonia;

// Avalonia.Native throws -6661 from CVDisplayLinkCreateWithActiveCGDisplays
// when macOS has no active display yet (a closed lid, or the panel coming
// back after sleep). Wait briefly for one before that startup path runs.
static partial class MacDisplayLink
{
	const string CoreVideo = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

	public static void WaitForActiveDisplay ()
	{
		if (!OperatingSystem.IsMacOS ())
			return;

		long started = Environment.TickCount64;
		Wait (TryCreate, () => Environment.TickCount64 - started, budgetMs: 5_000, () => Thread.Sleep (200));
	}

	internal static bool Wait (Func<bool> tryCreate, Func<long> elapsedMs, long budgetMs, Action sleep)
	{
		while (true) {
			if (tryCreate ())
				return true;
			if (elapsedMs () >= budgetMs)
				return false;
			sleep ();
		}
	}

	static bool TryCreate ()
	{
		if (CVDisplayLinkCreateWithActiveCGDisplays (out IntPtr displayLink) != 0)
			return false;
		CVDisplayLinkRelease (displayLink);
		return true;
	}

	[LibraryImport (CoreVideo)]
	private static partial int CVDisplayLinkCreateWithActiveCGDisplays (out IntPtr displayLink);

	[LibraryImport (CoreVideo)]
	private static partial void CVDisplayLinkRelease (IntPtr displayLink);
}
