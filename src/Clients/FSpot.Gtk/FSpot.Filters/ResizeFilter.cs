//
// ResizeFilter.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Gdk;

namespace FSpot.Filters
{
	public class ResizeFilter : IFilter
	{
		public ResizeFilter ()
		{
		}

		public ResizeFilter (uint size)
		{
			Size = size;
		}

		public uint Size { get; set; } = 600;

		public bool Convert (FilterRequest req)
		{
			string source = req.Current.LocalPath;
			var dest_uri = req.TempUri (System.IO.Path.GetExtension (source));

			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (req.Current)) {

				using (Pixbuf pixbuf = img.Load ()) {
					if (pixbuf.Width < Size && pixbuf.Height < Size)
						return false;
				}

				using (Pixbuf pixbuf = img.Load ((int)Size, (int)Size)) {
					FSpot.Utils.PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, pixbuf);
				}
			}

			req.Current = dest_uri;
			return true;
		}
	}
}
