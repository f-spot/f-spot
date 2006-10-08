/*
 * Filters/ResizeFilter.cs
 *
 * Author(s)
 *
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using FSpot;
using Gdk;

namespace FSpot.Filters {
	public class ResizeFilter : IFilter
	{
		public ResizeFilter ()
		{
		}

		public ResizeFilter (uint size)
		{
			this.size = size;
		}

		private uint size = 600;

		public uint Size {
			get { return size; }
			set { size = value; }
		}

		public bool Convert (string source, string dest)
		{
			int width;
			int height;

			// FIXME this should copy metadata
			PixbufUtils.GetSize(source,out width,out height);
			if (width < size && height < size)
				return false;
			PixbufUtils.Resize (source, dest, (int)size, true);
			return true;
		}
	}
}

