//
// PhotoLoader.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2004-2005 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Imaging;

namespace FSpot
{
	[Obsolete ("nuke or rename this")]
	public class PhotoLoader
	{
		public PhotoQuery query;

		public Gdk.Pixbuf Load (int index)
		{
			return Load (query, index);
		}

		static public Gdk.Pixbuf Load (IBrowsableCollection collection, int index)
		{
			IPhoto item = collection[index];
			return Load (item);
		}

		static public Gdk.Pixbuf Load (IPhoto item)
		{
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load ();
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf LoadAtMaxSize (IPhoto item, int width, int height)
		{
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load (width, height);
				return pixbuf;
			}
		}

		public PhotoLoader (PhotoQuery query)
		{
			this.query = query;
		}
	}
}
