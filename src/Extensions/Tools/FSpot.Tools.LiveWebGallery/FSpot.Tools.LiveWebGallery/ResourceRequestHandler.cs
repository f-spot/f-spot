//
// ResourceRequestHandler.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
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
using System.Reflection;

namespace FSpot.Tools.LiveWebGallery
{	
	public class ResourceRequestHandler : RequestHandler
	{	
		public override void Handle (string requested, Stream stream)
		{
			string resource = requested;
			using (Stream source = Assembly.GetCallingAssembly ().GetManifestResourceStream (resource)) {
				string ext = Path.GetExtension (resource);				
				SendHeadersAndStartContent(stream, "Content-Type: " + MimeTypeForExt (ext),
									   "Last-Modified: Fri, 21 Oct 2005 04:58:08 GMT");	// for caching
				byte[] buf = new byte[10240];
				int read;
				while((read = source.Read(buf, 0, buf.Length)) != 0) {
					stream.Write (buf, 0, read);
				}
			}
		}
	}	
}
