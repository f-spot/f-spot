//
// Exif.cs : LibExif wrapper for FSpot
//
// Author:
//   Larry Ewing     (lewing@novell.com)
//   Ravi Pratap     (ravi@ximian.com)
//   Miguel de Icaza (miguel@ximian.com)
//
// (C) 2002, 2004, 2005 Novell, Inc.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Exif {
	public enum ExifTag {
		InteroperabilityIndex		= 0x0001,
		InteroperabilityVersion	        = 0x0002,
		ImageWidth 			= 0x0100,
		ImageLength 			= 0x0101,
		BitsPersample 	         	= 0x0102,
		Compression 			= 0x0103,
		PhotometricInterpretation 	= 0x0106,
		FillOrder 			= 0x010a,
		DocumentName 			= 0x010d,
		ImageDescription 		= 0x010e,
		Make 				= 0x010f,
		Model 				= 0x0110,
		StripOffsets 			= 0x0111,
		Orientation 			= 0x0112,
		SamplesPerPixel 		= 0x0115,
		RowsPerStrip    		= 0x0116,
		StripByteCounts 		= 0x0117,
		XResolution 			= 0x011a,
		YResolution 			= 0x011b,
		PlanarConfiguration 		= 0x011c,
		ResolutionUnit  		= 0x0128,
		TransferFunction 		= 0x012d,
		Software 			= 0x0131,
		DateTime			= 0x0132,
		Artist				= 0x013b,
		WhitePoint			= 0x013e,
		PrimaryChromaticities		= 0x013f,
		TransferRange			= 0x0156,
		JPEGProc			= 0x0200,
		JPEGInterchangeFormat	        = 0x0201,
		JPEGInterchangeFormatLength	= 0x0202,
		YCBCRCoefficients		= 0x0211,
		YCBCRSubSampling		= 0x0212,
		YCBCRPositioning		= 0x0213,
		ReferenceBlackWhite		= 0x0214,
		RelatedImageFileFormat   	= 0x1000,
		RelatedImageWidth		= 0x1001,
		RelatedImageLength		= 0x1002,
		CFARepeatPatternDim		= 0x828d,
		CFAPattern			= 0x828e,
		BatteryLevel			= 0x828f,
		Copyright			= 0x8298,
		ExposureTime			= 0x829a,
		FNumber 			= 0x829d,
		IPTCNAA	        		= 0x83bb,
		IfdPointer      		= 0x8769,
		InterColorProfile		= 0x8773,
		ExposureProgram 		= 0x8822,
		SpectralSensitivity		= 0x8824,
		GPSInfoIfdPointer		= 0x8825,
		ISOSpeedRatings	        	= 0x8827,
		OECF				= 0x8828,
		ExifVersion			= 0x9000,
		DateTimeOriginal		= 0x9003,
		DateTimeDigitized		= 0x9004,
		ComponentsConfiguration	        = 0x9101,
		CompressedBitsPerPixel	        = 0x9102,
		ShutterSpeedValue		= 0x9201,
		ApertureValue			= 0x9202,
		BrightnessValue  		= 0x9203,
		ExposureBiasValue		= 0x9204,
		MaxApertureValue		= 0x9205,
		SubjectDistance 		= 0x9206,
		MeteringMode			= 0x9207,
		LightSource			= 0x9208,
		Flash				= 0x9209,
		FocalLength			= 0x920a,
		SubjectArea			= 0x9214,
		MakerNote			= 0x927c,
		UserComment			= 0x9286,
		SubSecTime			= 0x9290,
		SubSecTimeOriginal		= 0x9291,
		SubSecTimeDigitized		= 0x9292,
		FlashPixVersion 		= 0xa000,
		ColorSpace			= 0xa001,
		PixelXDimension 		= 0xa002,
		PixelYDimension 		= 0xa003,
		RelatedSoundFile		= 0xa004,
		InteroperabilityIfdPointer	= 0xa005,
		FlashEnergy			= 0xa20b,
		SpatialFrequencyResponse	= 0xa20c,
		FocalPlaneXResolution	        = 0xa20e,
		FocalPlaneYResolution	        = 0xa20f,
		FocalPlaneResolutionUnit	= 0xa210,
		SubjectLocation 		= 0xa214,
		ExposureIndex			= 0xa215,
		SensingMethod			= 0xa217,
		FileSource			= 0xa300,
		SceneType			= 0xa301,
		NewCFAPattern		        = 0xa302,
		CustomRendered  		= 0xa401,
		ExposureMode			= 0xa402,
		WhiteBalance			= 0xa403,
		DigitalZoomRatio		= 0xa404,
		FocalLengthIn35mmFilm	        = 0xa405,
		SceneCaptureType		= 0xa406,
		GainControl			= 0xa407,
		Contrast			= 0xa408,
		Saturation			= 0xa409,
		Sharpness			= 0xa40a,
		DeviceSettingDescription	= 0xa40b,
		SubjectDistanceRange		= 0xa40c,
		ImageUniqueId   		= 0xa420
	}
	
	public enum ExifByteOrder {
		Motorola,
		Intel
	}
	
	public enum ExifFormat {
		Byte      = 1,
		Ascii     = 2,
		Short     = 3,
		Long      = 4,
		Rational  = 5,
		Undefined = 7,
		Slong     = 9,
		SRational = 10
	}
	
	public enum ExifIfd {
		Zero = 0,
		One,
		Exif,
		Gps,
		InterOperability,
		Count
	}
	
	internal class ExifUtil {
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_tag_get_name (ExifTag tag);
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_tag_get_title (ExifTag tag);
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_tag_get_description (ExifTag tag);
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_byte_order_get_name (ExifByteOrder order);
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_format_get_name (ExifFormat format);
		
		[DllImport ("libexif.dll")]
		static extern char exif_format_get_size (ExifFormat format);
		
		[DllImport ("libexif.dll")]
		static extern IntPtr exif_ifd_get_name (ExifIfd ifd);
		
		public static string GetTagName (ExifTag tag)
		{
			
			IntPtr raw_ret = exif_tag_get_name (tag);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static string GetTagTitle (ExifTag tag)
		{
			IntPtr raw_ret = exif_tag_get_title (tag);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static string GetTagDescription (ExifTag tag)
		{
			IntPtr raw_ret = exif_tag_get_description (tag);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static string GetByteOrderName (ExifByteOrder order)
		{
			IntPtr raw_ret = exif_byte_order_get_name (order);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static string GetFormatName (ExifFormat format)
		{
			IntPtr raw_ret = exif_format_get_name (format);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static char GetFormatSize (ExifFormat format)
		{
			return exif_format_get_size (format);
		}
		
		public static string GetIfdName (ExifIfd ifd)
		{
			IntPtr raw_ret = exif_ifd_get_name (ifd);
			return Marshal.PtrToStringAnsi (raw_ret);
		}
		
		public static DateTime DateTimeFromString(string dt)
		{
			// Exif DateTime strings are formatted as
			//      "YYYY:MM:DD HH:MM:SS"
			
			string delimiters = " :";
			string[] dt_data = dt.Split ( delimiters.ToCharArray(), 6 );
			DateTime result;
			result = new DateTime (Int32.Parse(dt_data[0]), Int32.Parse(dt_data[1]), Int32.Parse(dt_data[2]),
					       Int32.Parse(dt_data[3]), Int32.Parse(dt_data[4]), Int32.Parse(dt_data[5]));
			
			return result;
		}	
		
	}		

	public abstract class ExifObject : IDisposable {
		protected HandleRef handle;
		
		public HandleRef Handle {
			get {
				return handle;
			}
		}
		
		public ExifObject () {}

		public ExifObject (IntPtr ptr)
		{
			handle = new HandleRef (this, ptr);
		}
		
		protected abstract void Cleanup ();
		
		public void Dispose () {
			Cleanup ();
			System.GC.SuppressFinalize (this);
		}
		
		~ExifObject ()
		{
			Cleanup ();
		}
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _ExifContent {
		IntPtr entries;
		uint count; 
		IntPtr parent;
		
		IntPtr priv;
	}
	
	public class ExifContent : ExifObject {
		System.Collections.ArrayList entries;
		
		internal ExifContent (IntPtr handle) : base (handle)
		{
			exif_content_ref (this.handle);
		}
		
		[DllImport ("libexif.dll")]
		static extern void exif_content_ref (HandleRef handle);
		
		[DllImport ("libexif.dll")]
		static extern void exif_content_unref (HandleRef handle);
		
		protected override void Cleanup ()
		{
			exif_content_unref (handle);
		}
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_content_remove_entry (HandleRef content, HandleRef entry);
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_content_add_entry (HandleRef content, HandleRef entry);
		
		public ExifEntry Lookup (ExifTag tag)
		{
			Assemble ();
			
			foreach (ExifEntry entry in entries) {
				if (entry.Tag == tag) {
					return entry;
				}
			}		
			
			return null;
		}

		public ExifEntry GetEntry (ExifTag tag)
		{
			Assemble ();
			
			ExifEntry entry = Lookup (tag);
			if (entry == null)
				entry = new ExifEntry (this, tag);

			return entry;
		}

		public void Add (ExifEntry entry)
		{
			Assemble ();

			entries.Add (entry);
			exif_content_add_entry (this.handle, entry.Handle);	
		}


		public void Remove (ExifEntry entry)
		{
			Assemble ();
			
			entries.Remove (entry);
			exif_content_remove_entry (this.handle, entry.Handle);
		}
		
		public ExifEntry [] GetEntries () 
		{
			Assemble ();

			return (ExifEntry [])entries.ToArray (typeof (ExifEntry));
		}
		
		[DllImport ("libexif.dll")]
		internal static unsafe extern IntPtr exif_content_foreach_entry (HandleRef content,
										 ExifContentForeachEntryFunc func,
										 IntPtr data);
		
		internal delegate void ExifContentForeachEntryFunc (IntPtr entry_ptr, IntPtr data);
		
	        void AssembleEntry (IntPtr entry, IntPtr data)
		{
			entries.Add (new ExifEntry (entry));
		}
		
		ExifContentForeachEntryFunc func;
		
		public void  Assemble ()
		{
			if (entries == null) {
					entries = new System.Collections.ArrayList ();
					
					func = new ExifContentForeachEntryFunc (AssembleEntry);
					exif_content_foreach_entry (this.Handle, func, IntPtr.Zero);
			}
		}
	}
	
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct _ExifEntry {
		public ExifTag tag;
		public int format;
		public uint components;
		public IntPtr data;
		public uint  size;
		
		public IntPtr parent;
		
		IntPtr priv;
	}
	
	
	public class ExifEntry : ExifObject {
		internal ExifEntry (IntPtr native) : base (native)
		{
			this.handle = new HandleRef (this, native);
			exif_entry_ref (this.handle);
		}

		[DllImport ("libexif.dll")]
		internal static extern IntPtr 
exif_entry_new ();

		[DllImport ("libexif.dll")]
		internal static extern void exif_entry_initialize (HandleRef handle, ExifTag tag);

		public ExifEntry (ExifContent content, ExifTag tag)
		{
			handle = new HandleRef (this, exif_entry_new ());
			content.Add (this);
			this.Reset (tag);
		}
		
		public void Reset (ExifTag tag)
		{
			exif_entry_initialize (handle, tag);

			//FIXME the month string in time fields in libexif ix currently broken so we do our own. 
			if (tag == ExifTag.DateTime
			    || tag == ExifTag.DateTimeOriginal
			    || tag == ExifTag.DateTimeDigitized)
				this.SetData (System.DateTime.Now);

		}


		public void Reset ()
		{
			Reset (Tag);
		}

		protected override void Cleanup ()
		{
			exif_entry_unref (this.handle);
		}
		
		private unsafe _ExifEntry *_handle {
			get {
					return (_ExifEntry *)handle.Handle;
			}
		}
		
		public ExifTag Tag {
			get {
				unsafe {
					return _handle->tag;
				}
			}
		}
		
		public ExifFormat Format {
			get {
				unsafe {
					return (ExifFormat) _handle->format;
				}
			}
		}
		
		public byte [] Data {
			get {
				unsafe {
					byte [] data = new byte [_handle->size]; 
					Marshal.Copy (_handle->data, data, 0, (int)_handle->size);
					return data;
				}
			}
		}
		
		public void SetData (byte [] data, bool check_type)
		{
			unsafe {
				if (data == null || data.Length == 0)
					throw new System.Exception ("Invalid Length");
				
				if (_handle->data != IntPtr.Zero)
					ExifData.free (_handle->data);
				
				_handle->data = ExifData.malloc ((uint)data.Length);
				Marshal.Copy (data, 0, _handle->data, data.Length);

				_handle->size = (uint) data.Length;
				// This needs to be set per type as well but
				// we do it here as well
				_handle->components = (uint) data.Length;
			}
		}
		
		public void SetData (byte []data)
		{
			SetData (data, true);
		}

		public void SetData (ushort [] data)
		{

		} 
		
		public void SetData (short [] data)
		{

		}

		public void SetData (string value)
		{
			int len = System.Text.Encoding.UTF8.GetByteCount (value);
			byte [] tmp = new byte [len + 1];
			System.Text.Encoding.UTF8.GetBytes (value, 0, value.Length, tmp, 0);
			tmp[len] = 0;
			System.Console.WriteLine ("value = {0} len = {1}", value, len);
			SetData (tmp, false);
		}

		public void SetData (System.DateTime time)
		{
			SetData (time.ToString ("yyyy:MM:dd HH:mm:ss"));
		}
		
		public string Description 
		{
			get {
					return ExifUtil.GetTagDescription (Tag);
			}
		}
		
		public string Name
		{
			get {
				return ExifUtil.GetTagName (Tag);
			}
		}
		
		public string Title
		{
			get {
				return ExifUtil.GetTagTitle (Tag);
			}
		}
		
		
		// FIXME this version is only valid in libexif 0.5
		[DllImport ("libexif.dll")]
		internal static extern IntPtr exif_entry_get_value (HandleRef handle);
		
		// FIXME this version is only valid in libexif 0.6
		[DllImport ("libexif.dll")]
		internal static extern IntPtr exif_entry_get_value (HandleRef handle, byte  [] value, int maxlen);
		
		public string Value
		{
			get {
				// FIXME this is a hack to handle libexif 0.5 and 0.6 at the same time
				// The signature of get_value changed between versions
				
				try {
					return Marshal.PtrToStringAnsi (exif_entry_get_value (this.Handle));
				} catch (System.NullReferenceException e) {
					byte [] value = new byte [1024];
					exif_entry_get_value (this.Handle, value, value.Length);
					return System.Text.Encoding.UTF8.GetString (value);
				}
			}
		}
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_entry_ref (HandleRef handle);
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_entry_unref (HandleRef handle);
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct _ExifData {
		IntPtr ifd0;
		IntPtr ifd1;
		IntPtr ifd_exif;
		IntPtr ifd_gps;
		IntPtr ifd_interop;

		internal IntPtr  data;
		internal int     size;
		
		IntPtr priv;
	}
	
	public class ExifData : ExifObject {
		System.Collections.ArrayList ifds;
		
		[DllImport ("libexif.dll")]
		internal static extern IntPtr exif_data_new ();
		
		public ExifData ()
		{
			handle = new HandleRef (this, exif_data_new ());
		}
		
		[DllImport ("libexif.dll")]
		internal static extern IntPtr exif_data_new_from_file (string path);
			
		public ExifData (string filename)
		{
			handle = new HandleRef (this, exif_data_new_from_file (filename));
		}
		
		[DllImport ("libexif.dll")]
		internal static extern IntPtr exif_data_new_from_data (byte [] data, uint size);
		
		public ExifData (byte [] data, uint size)
		{
			handle = new HandleRef (this, exif_data_new_from_data (data, size));
		}

		[DllImport ("libc")] 
		internal static extern void free (IntPtr address);
		
		[DllImport ("libc")] 
		internal static extern IntPtr malloc (uint size);
		
		[DllImport ("libexif.dll")]
		private static extern void exif_data_save_data (HandleRef handle, out IntPtr content, out uint size);
		
		public byte [] Save ()
		{
			Byte [] content = null;
			uint size;
			IntPtr data;
			unsafe {
				exif_data_save_data (handle, out data, out size);
				
				content = new byte [size];
				Marshal.Copy (data, content, 0, (int)size);
				free (data);
			}
				
			System.Console.WriteLine ("Saved {0} bytes", content.Length);
			return content;
		}
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_data_unref (HandleRef data);
		
		[DllImport ("libexif.dll")]
		internal static extern void exif_data_free (HandleRef data);

		protected override void Cleanup ()
		{
			exif_data_unref (handle);
		}
		
		public ExifContent GetContents (ExifIfd ifd)
		{
			Assemble ();

			return (ExifContent) ifds [(int)ifd];
		}

		public ExifContent [] GetContents ()
		{
			Assemble ();

			return (ExifContent []) ifds.ToArray (typeof (ExifContent));
		}
		
		internal delegate void ExifDataForeachContentFunc (IntPtr content, IntPtr data);
		
		[DllImport ("libexif.dll")]
		internal unsafe static extern void exif_data_foreach_content(HandleRef handle, ExifDataForeachContentFunc func, IntPtr data);
		
		unsafe void AssembleIfds (IntPtr content, IntPtr data)
		{
			ifds.Add (new ExifContent (content));
		}
		
		public ExifEntry LookupFirst (ExifTag tag)
		{
			foreach (ExifContent content in ifds) {
				ExifEntry entry = content.Lookup (tag);
				if (entry != null)
					return entry;
			}
			return null;
		}

		public string LookupFirstValue (ExifTag tag)
		{
			ExifEntry entry = LookupFirst (tag);
			if (entry != null) {
				return entry.Value;
			}
			return null;
		}

		public void Assemble ()
		{
			if (ifds == null) {
				ifds = new System.Collections.ArrayList ();

				if (handle.Handle != IntPtr.Zero)
					exif_data_foreach_content (handle, new ExifDataForeachContentFunc (AssembleIfds), IntPtr.Zero);
			}
		}
		
		byte [] empty = new byte [0];
		public byte [] Data {
			get {
				unsafe {
					_ExifData * obj = (_ExifData *) Handle.Handle;
					byte [] result;
					
					if (obj == null || obj->data == (IntPtr) 0)
						result = empty;
					else {
						result = new byte [obj->size];
						Marshal.Copy (obj->data, result, 0, obj->size);
						
					}
					return result;
				}
			}
			set {
				unsafe {
					_ExifData * obj = (_ExifData *) Handle.Handle;
					if (value.Length > 65533)
						throw new System.Exception ("Thumbnail too large");
					
					if (obj->data != IntPtr.Zero)
						free (obj->data);
					
					obj->data = malloc ((uint)value.Length);
					Marshal.Copy (value, 0, obj->data, value.Length);
				}
			}
		}
	}
}
