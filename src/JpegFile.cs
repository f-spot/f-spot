using System.IO;

namespace FSpot {
	public interface IThumbnailContainer {
		Gdk.Pixbuf GetEmbeddedThumbnail ();
	}

	public class JpegFile : ImageFile, IThumbnailContainer {
		private Exif.ExifData exif_data;
		
		public JpegFile (string path) : base (path) {}

		public string Description {
			get {
				// FIXME this should probably read the raw data because libexif sucks.
				Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);
				Exif.ExifEntry entry = exif_content.Lookup (Exif.Tag.UserComment);
				//System.Console.WriteLine ("entry {0}", entry.ToString ());

				if (entry == null)
					return null;
				
				return entry.Value;
			}
			set {
				string description = value;

				Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);			
				int len = System.Text.Encoding.BigEndianUnicode.GetByteCount (description);
				string heading = "ASCII\0\0\0";
				byte [] data = new byte [len + heading.Length];
				System.Text.Encoding.ASCII.GetBytes (heading, 0, heading.Length, data, 0);
				System.Text.Encoding.ASCII.GetBytes (description, 0, description.Length, data, heading.Length);
				exif_content.GetEntry (Exif.Tag.UserComment).SetData (data);

				//System.Console.WriteLine ("testing {0} {1} {2}", this.Description, data.Length, description);
			}
		}

		public void SaveMetaData (string path)
		{
			Exif.ExifContent image_content = this.ExifData.GetContents (Exif.Ifd.Zero);
			image_content.GetEntry (Exif.Tag.Software).SetData (FSpot.Defines.PACKAGE + " version " + FSpot.Defines.VERSION);
			
			// set the write time in the datetime tag
			image_content.GetEntry (Exif.Tag.DateTime).Reset ();
			
			JpegUtils.SaveExif (path, this.ExifData);
		}

		public Gdk.Pixbuf GetEmbeddedThumbnail ()
		{
			return PixbufUtils.GetThumbnail (this.ExifData);
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
		
		public void Crop (Gdk.Rectangle bounds)
		{
			
		}

		public override PixbufOrientation GetOrientation () {
			PixbufOrientation orientation = PixbufOrientation.TopLeft;
			Exif.ExifEntry e = this.ExifData.GetContents (Exif.Ifd.Zero).Lookup (Exif.Tag.Orientation);
			
			if (e != null) {
				ushort [] value = e.GetDataUShort ();
				orientation = (PixbufOrientation) value [0];
			}
			
			return orientation;
		}
		
		public override System.DateTime Date () {
			System.DateTime time;
			try {
				using (Exif.ExifData ed = new Exif.ExifData (path)) {
					Exif.ExifContent content = ed.GetContents (Exif.Ifd.Exif);
					Exif.ExifEntry entry = content.GetEntry (Exif.Tag.DateTimeOriginal);
					time = Exif.ExifUtil.DateTimeFromString (entry.Value); 
					time = time.ToUniversalTime ();
				}
			} catch (System.Exception e) {
				time = base.Date ();
			}
			return time;
		}
		
	}
}
