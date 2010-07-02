using System;
using System.IO;
using FSpot.Utils;
using Hyena;
using TagLib;
using TagLib.Image;

namespace FSpot.Imaging {
	public interface IThumbnailContainer {
		Gdk.Pixbuf GetEmbeddedThumbnail ();
	}

	public class JpegFile : ImageFile, IThumbnailContainer {
        public TagLib.Image.File Metadata {
            get { return metadata_file; }
        }

        private TagLib.Image.File metadata_file;
		
		public JpegFile (SafeUri uri) : base (uri)
		{
            metadata_file = TagLib.File.Create (new GIOTagLibFileAbstraction () { Uri = uri }) as TagLib.Image.File;
		}
		
		~JpegFile () {
			metadata_file.Dispose ();
		}

		public override Cms.Profile GetProfile ()
		{
			return null;
		}

		public Gdk.Pixbuf GetEmbeddedThumbnail ()
		{
			/*if (this.ExifData.Data.Length > 0) {
				MemoryStream mem = new MemoryStream (this.ExifData.Data);
				Gdk.Pixbuf thumb = new Gdk.Pixbuf (mem);
				Gdk.Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (thumb, this.Orientation);
				thumb.Dispose ();
				
				mem.Close ();
				return rotated;
			}*/
            // FIXME: No thumbnail support in TagLib# https://bugzilla.gnome.org/show_bug.cgi?id=618769
			return null;
		}
		
		public override ImageOrientation GetOrientation ()
		{
            var orientation = metadata_file.ImageTag.Orientation;
			return orientation;
		}
		
		public void SetOrientation (ImageOrientation orientation)
		{
            metadata_file.ImageTag.Orientation = orientation;
		}
		
		public void SetDateTimeOriginal (DateTime time)
		{
            metadata_file.ImageTag.DateTime = time;
		}

	}
}
