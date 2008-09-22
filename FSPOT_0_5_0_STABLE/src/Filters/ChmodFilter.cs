/*
 * Filters/ChmodFilter
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */
using Mono.Unix.Native;

namespace FSpot.Filters {
	public class ChmodFilter : IFilter 
	{	
		
		FilePermissions mode;

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
				System.Uri uri = req.TempUri ();
				System.IO.File.Copy (req.Current.LocalPath, uri.LocalPath, true);
				req.Current = uri;
			}

			Syscall.chmod (req.Current.LocalPath, mode);

			return true;
		}
	}
}
