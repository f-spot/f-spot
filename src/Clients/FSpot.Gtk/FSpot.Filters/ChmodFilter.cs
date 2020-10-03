//
// ChmodFilter.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Mono.Unix.Native;

namespace FSpot.Filters
{
	public class ChmodFilter : IFilter
	{
		readonly FilePermissions mode;

		public ChmodFilter () : this (FilePermissions.S_IRUSR |
						  FilePermissions.S_IWUSR |
						  FilePermissions.S_IRGRP |
						  FilePermissions.S_IROTH)
		{
		}

		public ChmodFilter (FilePermissions mode)
		{
			this.mode = mode;
		}

		public bool Convert (FilterRequest req)
		{
			if (req.Current == req.Source) {
				var uri = req.TempUri ();
				System.IO.File.Copy (req.Current.LocalPath, uri.LocalPath, true);
				req.Current = uri;
			}

			Syscall.chmod (req.Current.LocalPath, mode);

			return true;
		}
	}
}
