using System.Runtime.InteropServices;

namespace FSpot.Utils
{
	public static class Platform
	{
		public static bool IsWindows
			=> RuntimeInformation.IsOSPlatform (OSPlatform.Windows);
		public static bool IsMac
			=> RuntimeInformation.IsOSPlatform (OSPlatform.OSX);
		public static bool IsLinux
			=> RuntimeInformation.IsOSPlatform (OSPlatform.Linux);
	}
}
