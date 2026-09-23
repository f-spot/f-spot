// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace FSpot
{
	public static class PhotoTimeAdjust
	{
		public static void Shift (Photo photo, TimeSpan offset, Action<Photo> commit)
		{
			if (photo == null)
				throw new ArgumentNullException (nameof (photo));
			photo.Time = photo.Time + offset;
			commit?.Invoke (photo);
		}

		public static void ShiftEach (IReadOnlyList<Photo> photos, TimeSpan offset, Action<Photo[]> commit)
		{
			if (photos == null)
				throw new ArgumentNullException (nameof (photos));
			foreach (var photo in photos)
				photo.Time = photo.Time + offset;
			commit?.Invoke (photos.ToArray ());
		}

		// Photos before the anchor step backward. The anchor keeps anchorTime.
		// Each step after it moves forward by the interval.
		public static void Space (IReadOnlyList<Photo> photos, int anchorIndex, DateTime anchorTime, TimeSpan step, Action<Photo[]> commit)
		{
			if (photos == null)
				throw new ArgumentNullException (nameof (photos));
			if (photos.Count == 0)
				return;
			if (anchorIndex < 0 || anchorIndex >= photos.Count)
				anchorIndex = 0;
			for (int i = 0; i < photos.Count; i++)
				photos[i].Time = anchorTime + TimeSpan.FromTicks (step.Ticks * (i - anchorIndex));
			commit?.Invoke (photos.ToArray ());
		}
	}

	public sealed class TimeAdjustChoice
	{
		public bool ShiftByOffset { get; set; }
		public double Hours { get; set; }
		public double SpacingMinutes { get; set; }
		public int AnchorIndex { get; set; }
	}
}
