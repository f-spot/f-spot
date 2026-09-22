// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Utils;

namespace FSpot
{
	public static class HelpContents
	{
		public const string GnomeHelp = "ghelp:f-spot";
		public const string ProjectSite = "https://f-spot.app";

		public static string Uri (bool linux)
			=> linux ? GnomeHelp : ProjectSite;

		public static string Current
			=> Uri (Utils.Platform.IsLinux);
	}
}
