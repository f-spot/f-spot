/*
 * FSpot.Platform.Null.Desktop.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Platform
{
	public class Desktop
	{
		public static void SetBackgroundImage (string path)
		{
			Log.Information ("SetBackgroundImage not implemented in the null platform");
		}
	}
}

