/*
 * FSpot.UI.Dialog.SizeAttribute.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */
using System;

namespace FSpot.UI.Dialog
{
	[AttributeUsage (AttributeTargets.Class)]
	public class SizeAttribute : Attribute
	{
		string width_key;
		public string WidthKey {
			get { return width_key; }
		}
		string height_key;
		public string HeightKey {
			get { return height_key; }
		}

		public SizeAttribute (string width_key, string height_key)
		{
			this.width_key = width_key;
			this.height_key = height_key;
		}
	}
}
