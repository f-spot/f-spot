// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Hyena;

namespace FSpot.Filters
{
	public class UniqueNameFilter : IFilter
	{
		readonly SafeUri destination;

		public UniqueNameFilter (SafeUri destination)
		{
			this.destination = destination;
		}

		public bool Convert (FilterRequest request)
		{
			int i = 1;
			string path = destination.LocalPath;
			string filename = Path.GetFileName (request.Source.LocalPath);
			string dest = Path.Combine (path, filename);
			while (File.Exists (dest)) {
				string numbered_name = string.Format ("{0}-{1}{2}",
					Path.GetFileNameWithoutExtension (filename),
					i++,
					Path.GetExtension (filename));
				dest = Path.Combine (path, numbered_name);
			}

			File.Copy (request.Current.LocalPath, dest);
			request.Current = new SafeUri (dest);
			return true;
		}
	}
}
