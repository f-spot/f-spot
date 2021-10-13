// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

		public Export Create (Guid imageId, long imageVersionId, string exportType, string exportToken)
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

		public List<Export> GetByImageId (Guid imageId, uint imageVersionId)
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
