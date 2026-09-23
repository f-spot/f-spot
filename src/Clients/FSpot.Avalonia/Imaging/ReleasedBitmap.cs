//
// ReleasedBitmap.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

namespace FSpot.Avalonia.Imaging;

static class ReleasedBitmap
{
	public static void DisposePrevious (Bitmap? previous, Bitmap? current)
	{
		if (previous is not null && !ReferenceEquals (previous, current))
			previous.Dispose ();
	}
}
