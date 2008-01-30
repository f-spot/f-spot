/*
 * Filters/UniqueNameFilter.cs
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using FSpot.Utils;

namespace FSpot.Filters {
	public class UniqueNameFilter : IFilter
	{
		Uri destination;

		public UniqueNameFilter (string destination) : this (UriUtils.PathToFileUri (destination))
		{}

		public UniqueNameFilter (Uri destination)
		{
			this.destination = destination;
		}

		public bool Convert (FilterRequest request)
		{
			//FIXME: make it works for uri (and use it in CDExport)
			int i = 1;
			string path = destination.LocalPath;
			string filename = System.IO.Path.GetFileName (request.Source.LocalPath);
			string dest = System.IO.Path.Combine (path, filename);
			while (System.IO.File.Exists (dest)) {
				string numbered_name = String.Format ("{0}-{1}{2}",
						System.IO.Path.GetFileNameWithoutExtension (filename),
						i++,
						System.IO.Path.GetExtension (filename));
				dest = System.IO.Path.Combine (path, numbered_name);
			}
			
			System.IO.File.Copy (request.Current.LocalPath, dest);
			request.Current = UriUtils.PathToFileUri (dest); 
			return true;
		}
	}
}
