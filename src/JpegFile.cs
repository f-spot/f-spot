using System.IO;

namespace FSpot {
	public interface IThumbnailContainer {
		Gdk.Pixbuf GetEmbeddedThumbnail ();
	}

	public class JpegFile : ImageFile, IThumbnailContainer, SemWeb.StatementSource {
		private Exif.ExifData exif_data;
		
		public JpegFile (string path) : base (path) 
		{
#if false // TEST_METADATA
			MetadataStore store = new MetadataStore ();
			Select (store);
			store.Dump ();
#endif
		}

		public void Select (SemWeb.StatementSink sink)
		{
			using (System.IO.FileStream stream = System.IO.File.OpenRead (path)) {
				JpegHeader header = new JpegHeader (stream, true);
				header.Select (sink);
			}
		}

		public override Cms.Profile GetProfile ()
		{
			using (System.IO.FileStream stream = System.IO.File.OpenRead (path)) {
				JpegHeader header = new JpegHeader (stream, true);
				return header.GetProfile ();
			}

		}

		public override string Description {
			get {
				// FIXME this should probably read the raw data because libexif sucks.
				Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);
				Exif.ExifEntry entry = exif_content.Lookup (Exif.Tag.UserComment);

				if (entry == null)
					return null;
				
				return entry.Value;
			}
		}

		public void SetDescription (string value)
		{
			string description = value;
			
			Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);			
			int len = System.Text.Encoding.BigEndianUnicode.GetByteCount (description);
			string heading = "ASCII\0\0\0";
			byte [] data = new byte [len + heading.Length];
			System.Text.Encoding.ASCII.GetBytes (heading, 0, heading.Length, data, 0);
			System.Text.Encoding.ASCII.GetBytes (description, 0, description.Length, data, heading.Length);
			exif_content.GetEntry (Exif.Tag.UserComment).SetData (data);
		}

		private void UpdateMeta ()
		{
			Exif.ExifContent image_content = this.ExifData.GetContents (Exif.Ifd.Zero);
			image_content.GetEntry (Exif.Tag.Software).SetData (FSpot.Defines.PACKAGE + " version " + FSpot.Defines.VERSION);

			// set the write time in the datetime tag
			image_content.GetEntry (Exif.Tag.DateTime).Reset ();
		}

		private void SaveMetaData (System.IO.Stream input, System.IO.Stream output)
		{
			JpegHeader header = new JpegHeader (input);
			UpdateMeta ();
			
			header.SetExif (this.ExifData);
			header.Save (output);
		}
		
		public void SaveMetaData (string path)
		{
			UpdateMeta ();

			string  temp_path = path;
			using (System.IO.FileStream stream = System.IO.File.OpenRead (path)) {
				using (System.IO.Stream output = FSpot.Unix.MakeSafeTemp (ref temp_path)) {
					SaveMetaData (stream, output);
				}
			}
			if (FSpot.Unix.Rename (temp_path, path) < 0) {
				System.IO.File.Delete (temp_path);
				throw new System.Exception (System.String.Format ("Unable to rename {0} to {1}", temp_path, path));
			}
		}
		
		public override void Save (Gdk.Pixbuf pixbuf, System.IO.Stream stream)
		{
			// First save the imagedata
			byte [] image_data = PixbufUtils.Save (pixbuf, "jpeg", null, null);
			System.IO.MemoryStream buffer = new System.IO.MemoryStream ();
			buffer.Write (image_data, 0, image_data.Length);
			buffer.Position = 0;
			
			// Then create the thumbnail
			// The DCF spec says thumbnails should be 160x120 always
			Gdk.Pixbuf thumbnail = PixbufUtils.ScaleToAspect (pixbuf, 160, 120);
			byte [] thumb_data = PixbufUtils.Save (thumbnail, "jpeg", null, null);

			// now update the exif data
			exif_data.Data = thumb_data;
			thumbnail.Dispose ();

			Exif.ExifEntry e;
			Exif.ExifContent thumb_content;
			Exif.ExifContent image_content;
			
			// update the thumbnail related image fields if they exist.
			thumb_content = this.ExifData.GetContents (Exif.Ifd.One);
			e = thumb_content.Lookup (Exif.Tag.RelatedImageWidth);
			if (e != null)
				e.SetData ((uint)pixbuf.Width);

			e = thumb_content.Lookup (Exif.Tag.RelatedImageHeight);
			if (e != null)
				e.SetData ((uint)pixbuf.Height);
			
			image_content = this.ExifData.GetContents (Exif.Ifd.Zero);
			image_content.GetEntry (Exif.Tag.ImageWidth).SetData ((uint)pixbuf.Width);
			image_content.GetEntry (Exif.Tag.ImageHeight).SetData ((uint)pixbuf.Height);

			SaveMetaData (buffer, stream);
			buffer.Close ();
		}
		
		public Gdk.Pixbuf GetEmbeddedThumbnail ()
		{
			if (this.ExifData.Data.Length > 0) {
				MemoryStream mem = new MemoryStream (this.ExifData.Data);
				Gdk.Pixbuf thumb = new Gdk.Pixbuf (mem);
				Gdk.Pixbuf rotated = PixbufUtils.TransformOrientation (thumb, this.Orientation);
				
				if (rotated != thumb)
					thumb.Dispose ();
				
				mem.Close ();
				return rotated;
			}
			return null;
		}
		
		public Exif.ExifData ExifData {
			get {
				if (this.exif_data == null) {
					this.exif_data = new Exif.ExifData (path);

					if (this.exif_data.Handle.Handle == System.IntPtr.Zero)
						this.exif_data = new Exif.ExifData ();
				}
				return this.exif_data;
			}
			set {
				this.exif_data = value;
			}
		}

		public override PixbufOrientation GetOrientation () 
		{
			PixbufOrientation orientation = PixbufOrientation.TopLeft;
			Exif.ExifEntry e = this.ExifData.GetContents (Exif.Ifd.Zero).Lookup (Exif.Tag.Orientation);
			
			if (e != null) {
				ushort [] value = e.GetDataUShort ();
				orientation = (PixbufOrientation) value [0];
			}
			
			if (orientation < PixbufOrientation.TopLeft || orientation > PixbufOrientation.LeftBottom)
				orientation = PixbufOrientation.TopLeft;

			return orientation;
		}
		
		public void SetOrientation (PixbufOrientation orientation)
		{
			Exif.ExifEntry e = this.ExifData.GetContents (Exif.Ifd.Zero).GetEntry (Exif.Tag.Orientation);
			System.Console.WriteLine ("Saving orientation as {0}", orientation);
			e.SetData ((ushort)orientation);
		}
		
		public override System.DateTime Date {
			get {
				System.DateTime time;
				try {
					using (Exif.ExifData ed = new Exif.ExifData (path)) {
						Exif.ExifContent content = ed.GetContents (Exif.Ifd.Exif);
						Exif.ExifEntry entry = content.GetEntry (Exif.Tag.DateTimeOriginal);
						time = Exif.ExifUtil.DateTimeFromString (entry.Value); 
						time = time.ToUniversalTime ();
					}
				} catch (System.Exception e) {
					time = base.Date;
				}
				return time;
			}
		}
		
	}
}
