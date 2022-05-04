//
// ImageBrush.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using Cairo;

namespace Hyena.Gui.Canvas
{
	public class ImageBrush : Brush
	{
		ImageSurface surface;
		//private bool surface_owner;

		public ImageBrush ()
		{
		}

		public ImageBrush (string path) : this (new Gdk.Pixbuf (path), true)
		{
		}

		public ImageBrush (Gdk.Pixbuf pixbuf, bool disposePixbuf)
			: this (new PixbufImageSurface (pixbuf, disposePixbuf), true)
		{
		}

		public ImageBrush (ImageSurface surface, bool disposeSurface)
		{
			this.surface = surface;
			//this.surface_owner = disposeSurface;
		}

		protected ImageSurface Surface {
			get { return surface; }
			set { surface = value; }
		}

		public override bool IsValid {
			get { return surface != null; }
		}

		public override void Apply (Cairo.Context cr)
		{
			if (surface != null) {
				cr.SetSource (surface);
			}
		}

		public override double Width {
			get { return surface == null ? 0 : surface.Width; }
		}

		public override double Height {
			get { return surface == null ? 0 : surface.Height; }
		}
	}
}
