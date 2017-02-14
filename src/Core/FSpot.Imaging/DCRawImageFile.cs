//
// DCRawImageFile.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2005-2006 Larry Ewing
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

using Hyena;

namespace FSpot.Imaging
{
	class DCRawImageFile : BaseImageFile
	{
		const string dcraw_command = "dcraw";

		public DCRawImageFile (SafeUri uri) : base (uri)
		{
		}

		public override System.IO.Stream PixbufStream ()
		{
			return RawPixbufStream (Uri);
		}

		internal static System.IO.Stream RawPixbufStream (SafeUri location)
		{
			string path = location.LocalPath;
			string [] args = { dcraw_command, "-h", "-w", "-c", "-t", "0", path };

			var proc = new InternalProcess (System.IO.Path.GetDirectoryName (path), args);
			proc.StandardInput.Close ();
			return proc.StandardOutput;
		}
	}
}
