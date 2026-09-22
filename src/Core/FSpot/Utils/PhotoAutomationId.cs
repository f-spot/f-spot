// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Utils
{
	public static class PhotoAutomationId
	{
		public static string For (IPhoto photo)
		{
			if (photo is global::FSpot.Photo catalog && catalog.Id != 0)
				return "photo." + catalog.Id;

			string path = photo?.DefaultVersion?.Uri?.AbsolutePath ?? photo?.Name ?? "unknown";
			return "photo." + path.Replace ('\\', '.').Replace ('/', '.');
		}
	}
}
