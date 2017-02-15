//
// UriExtensions.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
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

namespace FSpot.Utils
{
	public static class UriExtensions
	{
		public static Uri GetDirectoryUri (this Uri uri)
		{
			UriBuilder builder = new UriBuilder (uri);
			builder.Path =
				string.Format ("{0}/", System.IO.Path.GetDirectoryName (uri.AbsolutePath));
			
			return builder.Uri;
		}
		
		public static string GetFilename (this Uri uri)
		{
			return System.IO.Path.GetFileName (uri.AbsolutePath);
		}
	}
}
