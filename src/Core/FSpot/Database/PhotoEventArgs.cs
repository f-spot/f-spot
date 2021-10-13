// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Models;

namespace FSpot.Database
{
	public class PhotoEventArgs : DbItemEventArgs<Models.Photo>
	{
		public PhotosChanges Changes { get; }

		public PhotoEventArgs (Photo photo, PhotosChanges changes) : this (new[] { photo }, changes)
		{
		}

		public PhotoEventArgs (Photo[] photos, PhotosChanges changes) : base (photos)
		{
			Changes = changes;
		}
	}
}
