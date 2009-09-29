/*
 * CameraFile.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	public enum CameraFileType
	{
		Preview,
		Normal,
		Raw,
		Audio,
		Exif,
		Metadata,
	}
	
	public class MimeTypes
	{
		[MarshalAs(UnmanagedType.LPTStr)] public static string ASF = "audio/x-asf";
		[MarshalAs(UnmanagedType.LPTStr)] public static string AVI = "video/x-msvideo";
		[MarshalAs(UnmanagedType.LPTStr)] public static string BMP = "image/bmp";
		[MarshalAs(UnmanagedType.LPTStr)] public static string CRW = "image/x-canon-raw";
		[MarshalAs(UnmanagedType.LPTStr)] public static string CR2 = "image/x-canon-raw";
		[MarshalAs(UnmanagedType.LPTStr)] public static string EXIF = "application/x-exif";
		[MarshalAs(UnmanagedType.LPTStr)] public static string JPEG = "image/jpeg";
		[MarshalAs(UnmanagedType.LPTStr)] public static string MP3 = "audio/mpeg";
		[MarshalAs(UnmanagedType.LPTStr)] public static string MPEG = "video/mpeg";
		[MarshalAs(UnmanagedType.LPTStr)] public static string OGG = "application/ogg";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PGM = "image/x-portable-graymap";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PNG = "image/png";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PNM = "image-x-portable-anymap";
		[MarshalAs(UnmanagedType.LPTStr)] public static string PPM = "image-x-portable-pixmap";
		[MarshalAs(UnmanagedType.LPTStr)] public static string QUICKTIME = "video/quicktime";
		[MarshalAs(UnmanagedType.LPTStr)] public static string RAW = "image/x-raw";
		[MarshalAs(UnmanagedType.LPTStr)] public static string TIFF = "image/tiff";
		[MarshalAs(UnmanagedType.LPTStr)] public static string UNKNOWN = "application/octet-stream";
		[MarshalAs(UnmanagedType.LPTStr)] public static string WAV = "audio/wav";
		[MarshalAs(UnmanagedType.LPTStr)] public static string WMA = "audio/x-wma";
	}

	public class CameraFile : GPObject 
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_new (out IntPtr file);
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_unref (HandleRef file);

		public CameraFile () : base (gp_file_unref)
		{
			IntPtr native;
			Error.CheckError (gp_file_new (out native));
			this.handle = new HandleRef (this, native);
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_new_from_fd (out IntPtr file, int fd);

		public CameraFile (int fd) : base (gp_file_unref)
		{
			IntPtr native;
			Error.CheckError (gp_file_new_from_fd (out native, fd));
			this.handle = new HandleRef (this, native);
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_save (HandleRef file, string filename);

		[Obsolete ("DO NOT USE")]
		public void Save (string filename)
		{
			Error.CheckError (gp_file_save (this.Handle, filename));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_file_get_data_and_size (HandleRef file, out IntPtr data, out IntPtr size);

		[Obsolete ("DO NOT USE")]
		public byte[] GetDataAndSize ()
		{
			IntPtr size;
			byte[] data;
			IntPtr data_addr;

			Error.CheckError (gp_file_get_data_and_size (this.Handle, out data_addr, out size));

			if(data_addr == IntPtr.Zero || size.ToInt32() == 0)
				return new byte[0];

			data = new byte[size.ToInt32()];
			Marshal.Copy(data_addr, data, 0, (int)size.ToInt32());
			return data;
		}
	}
}
