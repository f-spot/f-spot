using System;
using System.IO;
using FSpot.Xmp;
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

		public void SetThumbnail (Gdk.Pixbuf source)
		{
			/*// Then create the thumbnail
			// The DCF spec says thumbnails should be 160x120 always
			Gdk.Pixbuf thumbnail = PixbufUtils.ScaleToAspect (source, 160, 120);
			byte [] thumb_data = PixbufUtils.Save (thumbnail, "jpeg", null, null);
			
			// System.Console.WriteLine ("saving thumbnail");				

			// now update the exif data
			ExifData.Data = thumb_data;*/
            // FIXME: needs to be readded https://bugzilla.gnome.org/show_bug.cgi?id=618769
		}

		public void SetDimensions (int width, int height)
		{
			/* FIXME: disabled, related to metadata copying
             * https://bugzilla.gnome.org/show_bug.cgi?id=618770
             * Exif.ExifEntry e;
			Exif.ExifContent thumb_content;
			
			// update the thumbnail related image fields if they exist.
			thumb_content = this.ExifData.GetContents (Exif.Ifd.One);
			e = thumb_content.Lookup (Exif.Tag.RelatedImageWidth);
			if (e != null)
				e.SetData ((uint)width);

			e = thumb_content.Lookup (Exif.Tag.RelatedImageHeight);
			if (e != null)
				e.SetData ((uint)height);
			
			Exif.ExifContent image_content;
			image_content = this.ExifData.GetContents (Exif.Ifd.Zero);
			image_content.GetEntry (Exif.Tag.Orientation).SetData ((ushort)PixbufOrientation.TopLeft);
			//image_content.GetEntry (Exif.Tag.ImageWidth).SetData ((uint)pixbuf.Width);
			//image_content.GetEntry (Exif.Tag.ImageHeight).SetData ((uint)pixbuf.Height);
			image_content.GetEntry (Exif.Tag.PixelXDimension).SetData ((uint)width);
			image_content.GetEntry (Exif.Tag.PixelYDimension).SetData ((uint)height);*/
		}

		public override void Save (Gdk.Pixbuf pixbuf, System.IO.Stream stream)
		{

			// Console.WriteLine ("starting save");
			// First save the imagedata
			int quality = metadata_file.Properties.PhotoQuality;
			quality = quality == 0 ? 75 : quality;
			byte [] image_data = PixbufUtils.Save (pixbuf, "jpeg", new string [] {"quality" }, new string [] { quality.ToString () });
			System.IO.MemoryStream buffer = new System.IO.MemoryStream ();
			buffer.Write (image_data, 0, image_data.Length);
/*			FIXME: Metadata copying doesn't work yet https://bugzilla.gnome.org/show_bug.cgi?id=618770
			buffer.Position = 0;
			
			// Console.WriteLine ("setting thumbnail");
			SetThumbnail (pixbuf);
			SetDimensions (pixbuf.Width, pixbuf.Height);
			pixbuf.Dispose ();
			
			// Console.WriteLine ("saving metatdata");
			SaveMetaData (buffer, stream);
			// Console.WriteLine ("done");*/
			buffer.Close ();
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
