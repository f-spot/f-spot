//
// UniqueNameFilter.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006, 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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
//

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
			//FIXME: make it works for uri (and use it in CDExport)
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
