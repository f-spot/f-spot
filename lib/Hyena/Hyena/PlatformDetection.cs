using System.Runtime.InteropServices;

namespace Hyena
{
	// Copied from FSpot.Utils (for now)
	public static class PlatformDetection
	{
		public static bool IsWindows
			=> RuntimeInformation.IsOSPlatform (OSPlatform.Windows);
		public static bool IsMac
			=> RuntimeInformation.IsOSPlatform (OSPlatform.OSX);
		public static bool IsLinux
			=> RuntimeInformation.IsOSPlatform (OSPlatform.Linux);
	}
}
