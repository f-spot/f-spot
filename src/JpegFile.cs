#define USE_TIFF
using System;
using System.IO;
using FSpot.Xmp;
using FSpot.Tiff;

namespace FSpot {
	public interface IThumbnailContainer {
		Gdk.Pixbuf GetEmbeddedThumbnail ();
	}

	public class JpegFile : ImageFile, IThumbnailContainer, SemWeb.StatementSource {
		private Exif.ExifData exif_data;
		private XmpFile xmp;
		private JpegHeader header;
		private FSpot.Tiff.Header exif_header;
		
		public JpegFile (Uri uri) : base (uri)
		{
		}
		
		public JpegFile (string path) : base (path) 
		{
		}

		public JpegHeader Header {
			get {
				if (header == null) {
					using (Stream stream = this.Open ()) {
						header = new JpegHeader (stream, true);
					}
				}
				return header;
			}
		}

		public FSpot.Tiff.Header ExifHeader {
			get {
				if (exif_header == null) {
					exif_header = Header.GetExifHeader ();
				}
				return exif_header;
			}
		}

		public void Select (SemWeb.StatementSink sink)
		{
			Header.Select (sink);
		}

		public override Cms.Profile GetProfile ()
		{
			return Header.GetProfile ();
		}

		public override string Description {
			get {
#if USE_TIFF
				try {
					DirectoryEntry e = ExifHeader.Directory.Lookup (TagId.UserComment);
					return e.ValueAsString [0];
				} catch (System.Exception e) {
					Console.WriteLine (e);
					return null;
				}
#else
				Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);
				Exif.ExifEntry entry = exif_content.Lookup (Exif.Tag.UserComment);

				if (entry == null)
					return null;
				
				UserComment comment = new UserComment (entry.Data, entry.ByteOrder == Exif.ByteOrder.Intel);
				return comment.Value;
#endif				
			}
		}

		public void SetDescription (string value)
		{
			Exif.ExifContent exif_content = this.ExifData.GetContents (Exif.Ifd.Exif);			
			Exif.ExifEntry entry = exif_content.GetEntry (Exif.Tag.UserComment);

			UserComment comment = new UserComment (value);
			byte [] data = comment.GetBytes (entry.ByteOrder == Exif.ByteOrder.Intel);
			entry.SetData (data);
		}
		
		public void SetXmp (XmpFile xmp)
		{
			this.xmp = xmp;
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

			if (xmp != null)
				header.SetXmp (xmp);

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
			ExifData.Data = thumb_data;
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
			image_content.GetEntry (Exif.Tag.Orientation).SetData ((ushort)PixbufOrientation.TopLeft);
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
				if (exif_data == null) {
					exif_data = Header.Exif;

					if (exif_data.Handle.Handle == System.IntPtr.Zero)
						exif_data = new Exif.ExifData ();
				}
				System.Console.WriteLine ("loading exif data");
				return exif_data;
			}
			set {
				this.exif_data = value;
			}
		}

		public override PixbufOrientation GetOrientation () 
		{
			PixbufOrientation orientation = PixbufOrientation.TopLeft;
#if USE_TIFF
			try {
				DirectoryEntry e = ExifHeader.Directory.Lookup (TagId.Orientation);
				orientation = (PixbufOrientation)e.ValueAsLong [0];
			} catch {
				System.Console.WriteLine ("error checking orientation");
			}
#else						     
			Exif.ExifEntry e = this.ExifData.GetContents (Exif.Ifd.Zero).Lookup (Exif.Tag.Orientation);
			
			if (e != null) {
				ushort [] value = e.GetDataUShort ();
				orientation = (PixbufOrientation) value [0];
			}
#endif			
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
		
		public void SetDateTimeOriginal (DateTime time)
		{
			Exif.ExifEntry e = ExifData.LookupFirst (Exif.Tag.DateTimeOriginal);
			if (e != null)
				e.SetData (time);
		}

		public override System.DateTime Date {
			get {
				System.DateTime time;
				try {
#if USE_TIFF
					SubdirectoryEntry sub = (SubdirectoryEntry) ExifHeader.Directory.Lookup (TagId.ExifIfdPointer);
					DirectoryEntry e;
					
					if (sub != null) {
						e = sub.Directory [0].Lookup (TagId.DateTimeOriginal);
						
						if (e != null)
							return DirectoryEntry.DateTimeFromString (e.StringValue).ToUniversalTime ();
					}
					
					e = ExifHeader.Directory.Lookup (TagId.DateTime);
					
					if (e != null)
						return DirectoryEntry.DateTimeFromString (e.StringValue).ToUniversalTime ();
					
					return base.Date;
#else
					string time_str = "";				
					time_str = ExifData.LookupFirstValue (Exif.Tag.DateTimeOriginal);
					
					if (time_str == null || time_str == "") 
						time_str = ExifData.LookupFirstValue (Exif.Tag.DateTime);
					
					time = Exif.ExifUtil.DateTimeFromString (time_str).ToUniversalTime (); 
#endif
				} catch (System.Exception e) {
					time = base.Date;
				}
				return time;
			}
		}
		
	}
}
