/*
 * LiveWebGalleryExtension.ResourceRequestHandler.cs
 *
 * Author(s):
 *	Anton Keks  <anton@azib.net>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;

using FSpot;

namespace LiveWebGalleryExtension
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
