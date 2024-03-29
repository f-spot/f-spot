//
// PhotoEventArgs.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Database
{
	public class PhotoEventArgs : DbItemEventArgs<Photo>
	{
		public PhotosChanges Changes { get; private set; }

		public PhotoEventArgs (Photo photo, PhotosChanges changes) : this (new[] { photo }, changes)
		{
		}

		public PhotoEventArgs (Photo[] photos, PhotosChanges changes) : base (photos)
		{
			Changes = changes;
		}
	}
}
