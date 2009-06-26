/*
 * FSpot.Platform.Null.WebProxy.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using FSpot.Utils;

namespace FSpot.Platform
{
	public class WebProxy {
		public static void Init ()
		{
			Log.Information ("No WebProxy in the Null Platform");
		}
	}
}

