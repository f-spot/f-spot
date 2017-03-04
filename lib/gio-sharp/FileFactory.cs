//
// FileFactory.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// Copyright (c) 2008 Stephane Delcroix
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;

namespace GLib
{
	public class FileFactory
	{
		[DllImport ("libgio-2.0-0.dll")]
		private static extern IntPtr g_file_new_for_uri (string uri);

		public static File NewForUri (string uri)
		{
			return GLib.FileAdapter.GetObject (g_file_new_for_uri (uri), false) as File;
		}

		public static File NewForUri (Uri uri)
		{
			return GLib.FileAdapter.GetObject (g_file_new_for_uri (uri.ToString ()), false) as File;
		}

		[DllImport ("libgio-2.0-0.dll")]
		private static extern IntPtr g_file_new_for_path (string path);

		public static File NewForPath (string path)
		{
			return GLib.FileAdapter.GetObject (g_file_new_for_path (path), false) as File;
		}

		[DllImport ("libgio-2.0-0.dll")]
		private static extern IntPtr g_file_new_for_commandline_arg (string arg);

		public static File NewFromCommandlineArg (string arg)
		{
			return GLib.FileAdapter.GetObject (g_file_new_for_commandline_arg (arg), false) as File;
		}
	}
}
