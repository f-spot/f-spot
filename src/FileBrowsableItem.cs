/*
 * FileBrowsableItem.cs
 *
 * Author(s):
 *	Larry Ewing  (lewing@novell.com)
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * This is free software. See COPYING for details
 */


using System;
using System.IO;
using System.Collections;
using System.Xml;

using FSpot.Utils;
namespace FSpot {
	public class FileBrowsableItem : IBrowsableItem, IDisposable
	{
		ImageFile img;
		Uri uri;
		bool attempted;

		public FileBrowsableItem (Uri uri)
		{
			this.uri = uri;
		}

		public FileBrowsableItem (string path)
		{
			this.uri = UriUtils.PathToFileUri (path);
		}

		protected ImageFile Image {
			get {
				if (!attempted) {
					img = ImageFile.Create (uri);
					attempted = true;
				}

				return img;
			}
		}

		public Tag [] Tags {
			get {
				return null;
			}
		}

		public DateTime Time {
			get {
				return Image.Date;
			}
		}

		public Uri DefaultVersionUri {
			get {
				return uri;
			}
		}

		public string Description {
			get {
				try {
					if (Image != null)
						return Image.Description;

				} catch (System.Exception e) {
					System.Console.WriteLine (e);
				}

				return null;
			}
		}

		public string Name {
			get {
				return Path.GetFileName (Image.Uri.AbsolutePath);
			}
		}

		public uint Rating {
			get {
				return 0; //FIXME ndMaxxer: correct?
			}
		}

		public void Dispose ()
		{
			img.Dispose ();
			GC.SuppressFinalize (this);
		}
	}
}
