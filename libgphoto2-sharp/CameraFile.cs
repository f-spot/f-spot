using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	public enum CameraFileType
	{
		Preview,
		Normal,
		Raw,
		Audio,
		Exif
	}
	
	public class MimeTypes
	{
		[MarshalAs(UnmanagedType.LPTStr)] public static string AVI = "video/x-msvideo";
		[MarshalAs(UnmanagedType.LPTStr)] public static string BMP = "image/bmp";
		[MarshalAs(UnmanagedType.LPTStr)] public static string CRW = "image/x-canon-raw";
		[MarshalAs(UnmanagedType.LPTStr)] public static string JPEG = "image/jpeg";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PGM = "image/x-portable-graymap";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PNG = "image/png";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PPM = "image-x-portable-pixmap";
		[MarshalAs(UnmanagedType.LPTStr)] public static string QUICKTIME = "video/quicktime";
		[MarshalAs(UnmanagedType.LPTStr)] public static string RAW = "image/x-raw";
		[MarshalAs(UnmanagedType.LPTStr)] public static string TIFF = "image/tiff";
		[MarshalAs(UnmanagedType.LPTStr)] public static string UNKNOWN = "application/octet-stream";
		[MarshalAs(UnmanagedType.LPTStr)] public static string WAV = "audio/wav";
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _CameraFile
	{

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_new (out _CameraFile *file);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_ref (_CameraFile *file);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_unref (_CameraFile *file);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_free (_CameraFile *file);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_append (_CameraFile* file, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, ulong size);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_open (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] string filename);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_save (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] string filename);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_clean (_CameraFile *file);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_get_name (_CameraFile *file, IntPtr name);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_name (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] string name);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_get_type (_CameraFile *file, CameraFileType *type);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_type (_CameraFile *file, CameraFileType type);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_get_mime_type (_CameraFile *file, IntPtr mime_type);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_mime_type (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] string mime_type);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_detect_mime_type (_CameraFile *file);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_adjust_name_for_mime_type (_CameraFile *file);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_convert (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] string mime_type);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_copy (_CameraFile *destination, _CameraFile *source);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_color_table (_CameraFile *file, byte *red_table, int red_size, byte *green_table, int green_size, byte *blue_table, int blue_size);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_header (_CameraFile *file, [MarshalAs(UnmanagedType.LPTStr)] byte[] header);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_width_and_height (_CameraFile *file, int width, int height);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_get_data_and_size (_CameraFile* file, out IntPtr data, out ulong size);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_set_data_and_size (_CameraFile* file, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, ulong size);
	}
	
	public class CameraFile : IDisposable
	{
		unsafe _CameraFile *obj;
		
		public CameraFile()
		{
			unsafe 
			{
				_CameraFile.gp_file_new(out obj);
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~CameraFile()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			unsafe
			{
				if (obj != null)
				{
					_CameraFile.gp_file_unref(obj);
					obj = null;
				}
			}
		}
		
		unsafe internal _CameraFile* UnsafeCameraFile
		{
			get
			{
				return obj;
			}
		}
		
		public void Append (byte[] data)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_append(obj, data, (ulong)data.Length);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Open (string filename)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_open(obj, filename);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Save (string filename)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_save(obj, filename);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Clean (string filename)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_clean(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public string GetName ()
		{
			ErrorCode result;
			string name;
			unsafe
			{
				IntPtr name_addr = IntPtr.Zero;
				IntPtr name_addr_addr = new IntPtr((void*)&name_addr);
				result = _CameraFile.gp_file_get_name(obj, name_addr_addr);
				name = Marshal.PtrToStringAnsi(name_addr);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return name;
		}
		
		public void SetName (string name)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_name(obj, name);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraFileType GetFileType ()
		{
			ErrorCode result;
			CameraFileType type;
			unsafe
			{
				result = _CameraFile.gp_file_get_type(obj, &type);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return type;
		}
		
		public void SetFileType (CameraFileType type)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_type(obj, type);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public string GetMimeType ()
		{
			ErrorCode result;
			string mime;
			unsafe
			{
				IntPtr mime_addr = IntPtr.Zero;
				IntPtr mime_addr_addr = new IntPtr((void*)&mime_addr);
				result = _CameraFile.gp_file_get_mime_type(obj, mime_addr_addr);
				mime = Marshal.PtrToStringAnsi(mime_addr);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return mime;
		}
		
		public void SetMimeType (string mime_type)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_mime_type(obj, mime_type);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void DetectMimeType ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_detect_mime_type(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void AdjustNameForMimeType ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_adjust_name_for_mime_type(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Convert (string mime_type)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_convert(obj, mime_type);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Copy (CameraFile source)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_copy(obj, source.obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void SetHeader (byte[] header)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_header(obj, header);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void SetWidthHeight (int width, int height)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_width_and_height(obj, width, height);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void SetDataAndSize (byte[] data)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFile.gp_file_set_data_and_size(obj, data, (ulong)data.Length);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public byte[] GetDataAndSize ()
		{
			ErrorCode result;
			ulong size;
			byte[] data;
			unsafe
			{
				IntPtr data_addr = IntPtr.Zero;
				result = _CameraFile.gp_file_get_data_and_size(obj, out data_addr, out size);
				data = new byte[size];
				Marshal.Copy(data_addr, data, 0, (int)size);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return data;
		}
	}
}
