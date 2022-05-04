//
// IPhotoExtensions.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
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

using System;
using System.IO;

namespace FSpot.Core
{
	public static class IPhotoExtensions
	{
		public static int CompareDate (this IPhoto photo1, IPhoto photo2)
		{
			return DateTime.Compare (photo1.Time, photo2.Time);
		}

		public static int CompareName (this IPhoto photo1, IPhoto photo2)
		{
			return string.Compare (photo1.Name, photo2.Name);
		}

		public static int Compare (this IPhoto photo1, IPhoto photo2)
		{
			int result = photo1.CompareDate (photo2);

			if (result == 0)
				result = CompareDefaultVersionUri (photo1, photo2);

			if (result == 0)
				result = photo1.CompareName (photo2);

			return result;
		}

		public static int CompareDefaultVersionUri (this IPhoto photo1, IPhoto photo2)
		{
			var photo1_uri = Path.Combine (photo1.DefaultVersion.BaseUri, photo1.DefaultVersion.Filename);
			var photo2_uri = Path.Combine (photo2.DefaultVersion.BaseUri, photo2.DefaultVersion.Filename);
			return string.Compare (photo1_uri, photo2_uri);
		}
	}
}
