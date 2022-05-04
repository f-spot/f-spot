//
// MarginStyle.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace Hyena.Gui.Canvas
{
	public class MarginStyle
	{
		public MarginStyle ()
		{
		}

		public virtual void Apply (CanvasItem item, Cairo.Context cr)
		{
		}

		public static readonly MarginStyle None = new MarginStyle ();
	}
}
