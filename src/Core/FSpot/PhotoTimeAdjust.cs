// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

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
	}
}
