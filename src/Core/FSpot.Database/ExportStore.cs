//
// ExportStore.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Models;

namespace FSpot.Database
{
	public class ExportStore : DbStore<Export>
	{
		public const string FlickrExportType = "fspot:Flickr";
		// TODO: This is obsolete and meant to be remove once db reach rev4
		public const string OldFolderExportType = "fspot:Folder";
		public const string FolderExportType = "fspot:FolderUri";
		public const string PicasaExportType = "fspot:Picasa";
		public const string SmugMugExportType = "fspot:SmugMug";
		public const string Gallery2ExportType = "fspot:Gallery2";

		FSpotContext Context { get; }

		public ExportStore (IDb db) : base (db)
		{
			Context = new FSpotContext ();
		}

		public Export Create (uint imageId, uint imageVersionId, string exportType, string exportToken)
		{
			var item = new Export { ImageId = imageId, ImageVersionId = imageVersionId, ExportType = exportType, ExportToken = exportToken };
			Context.Add (item);
			Context.SaveChanges ();

			EmitAdded (item);

			return item;
		}

		public override void Commit (Export item)
		{
			Context.Add (item);
			Context.SaveChanges ();

			EmitChanged (item);
		}

		public override Export Get (Guid id)
		{
			// we never use this
			return Context.Exports.Find (id);
		}

		public List<Export> GetByImageId (uint imageId, uint imageVersionId)
		{
			return Context.Exports.Where (x => x.ImageId == imageId && x.ImageVersionId == imageVersionId).ToList ();
		}

		public override void Remove (Export item)
		{
			Context.Remove (item);
			Context.SaveChanges ();

			EmitRemoved (item);
		}
	}
}
