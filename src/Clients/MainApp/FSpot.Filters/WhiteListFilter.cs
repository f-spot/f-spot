//
// WhiteListFilter.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
// Copyright (C) 2006-2007 Stephane Delcroix
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

using System.Collections.Generic;
using System.IO;

namespace FSpot.Filters
{
	public class WhiteListFilter : IFilter
	{
		readonly List<string> valid_extensions;

		public WhiteListFilter (string [] valid_extensions)
		{
			this.valid_extensions = new List<string> ();
			foreach (string extension in valid_extensions)
				this.valid_extensions.Add (extension.ToLower ());
		}

		public bool Convert (FilterRequest req)
		{
			if (valid_extensions.Contains (Path.GetExtension (req.Current.LocalPath).ToLower ()))
				return false;

			// FIXME:  Should we add the other jpeg extensions?
			if (!valid_extensions.Contains (".jpg") &&
			    !valid_extensions.Contains (".jpeg"))
				throw new System.NotImplementedException ("can only save jpeg :(");

			return (new JpegFilter ()).Convert (req);
		}
	}
}
