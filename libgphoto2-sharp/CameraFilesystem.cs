using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	public enum CameraFilePermissions
	{
		None = 0,
		Read = 1 << 0,
		Delete = 1 << 1,
		All = 0xFF
	}

	public enum CameraFileStatus
	{
		NotDownloaded,
		Downloaded
	}
	
	public enum CameraFileInfoFields
	{
		None		= 0,
		Type		= 1 << 0,
		Name		= 1 << 1,
		Size		= 1 << 2,
		Width		= 1 << 3,
		Height		= 1 << 4,
		Permissions	= 1 << 5,
		Status		= 1 << 6,
		MTime		= 1 << 7,
		All		= 0xFF
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraFileInfoAudio
	{
		CameraFileInfoFields fields;
		CameraFileStatus status;
		ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] char[] type;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraFileInfoPreview
	{
		CameraFileInfoFields fields;
		CameraFileStatus status;
		ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] char[] type;
		
		uint width, height;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraFileInfoFile
	{
		CameraFileInfoFields fields;
		CameraFileStatus status;
		ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] char[] type;
		
		uint width, height;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] char[] name;
		CameraFilePermissions permissions;
		long time;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFileInfo
	{
		CameraFileInfoPreview preview;
		CameraFileInfoFile file;
		CameraFileInfoAudio audio;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _CameraFilesystem
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_new (out _CameraFilesystem *fs);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_free (_CameraFilesystem *fs);
		
		internal delegate ErrorCode _CameraFilesystemGetFileFunc (_CameraFilesystem *fs, char *folder, char *filename, CameraFileType type, _CameraFile *file, void *data, _Context *context);
		
		internal delegate ErrorCode _CameraFilesystemDeleteFileFunc (_CameraFilesystem *fs, char *folder, char *filename, void *data, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_file_funcs (_CameraFilesystem *fs, _CameraFilesystemGetFileFunc get_file_func, _CameraFilesystemDeleteFileFunc del_file_func, void *data);
		
		internal delegate ErrorCode _CameraFilesystemGetInfoFunc (_CameraFilesystem *fs, char *folder, char *filename, CameraFileInfo *info, void *data, _Context *context);

		internal delegate ErrorCode _CameraFilesystemSetInfoFunc (_CameraFilesystem *fs, char *folder, char *filename, CameraFileInfo info, void *data, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_info_funcs (_CameraFilesystem *fs, _CameraFilesystemGetInfoFunc get_info_func, _CameraFilesystemSetInfoFunc set_info_func, void *data);

		internal delegate ErrorCode _CameraFilesystemPutFileFunc (_CameraFilesystem *fs, char *folder, _CameraFile *file, void *data, _Context *context);

		internal delegate ErrorCode _CameraFilesystemDeleteAllFunc (_CameraFilesystem *fs, char *folder, void *data, _Context *context);

		internal delegate ErrorCode _CameraFilesystemDirFunc (_CameraFilesystem *fs, char *folder, char *name, void *data, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_folder_funcs (_CameraFilesystem *fs, _CameraFilesystemPutFileFunc put_file_func, _CameraFilesystemDeleteAllFunc delete_all_func, _CameraFilesystemDirFunc make_dir_func, _CameraFilesystemDirFunc remove_dir_func, void *data);

		internal delegate ErrorCode _CameraFilesystemListFunc (_CameraFilesystem *fs, char *folder, _CameraList *list, void *data, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_list_funcs (_CameraFilesystem *fs, _CameraFilesystemListFunc file_list_func, _CameraFilesystemListFunc folder_list_func, void *data);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_list_files (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraList *list, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_list_folders (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraList *list, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_get_file (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, CameraFileType type, _CameraFile *file, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_put_file (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraFile *file, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_delete_file (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_delete_all (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_make_dir (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_remove_dir (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_get_info (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, out CameraFileInfo info, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_info (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, CameraFileInfo info, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_info_noop (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, CameraFileInfo info, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_number (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_name (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, int filenumber, IntPtr filename, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_get_folder (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string filename, IntPtr folder, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_count (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_reset (_CameraFilesystem *fs);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_append (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string filename, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_set_file_noop (_CameraFilesystem *fs, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraFile *file, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_filesystem_dump (_CameraFilesystem *fs);
	}
	
	public class CameraFilesystem : IDisposable
	{
		unsafe _CameraFilesystem *obj;
		bool need_dispose;
		
		public CameraFilesystem()
		{
			unsafe 
			{
				_CameraFilesystem.gp_filesystem_new(out obj);
			}
			need_dispose = true;
		}
		
		unsafe internal CameraFilesystem(_CameraFilesystem *fs)
		{
			obj = fs;
			need_dispose = false;
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~CameraFilesystem()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (need_dispose)
			{
				unsafe
				{
					if (obj != null)
					{
						_CameraFilesystem.gp_filesystem_free(obj);
						obj = null;
					}
				}
			}
		}
		
		public CameraList ListFiles (string folder, Context context)
		{
			ErrorCode result;
			CameraList list = new CameraList();
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_list_files(obj, folder, list.UnsafeCameraList, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return list;
		}
		
		public CameraList ListFolders (string folder, Context context)
		{
			ErrorCode result;
			CameraList list = new CameraList();
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_list_folders(obj, folder, list.UnsafeCameraList, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return list;
		}
		
		public CameraFile GetFile (string folder, string filename, CameraFileType type, Context context)
		{
			ErrorCode result;
			CameraFile file = new CameraFile();
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_get_file(obj, folder, filename, type, file.UnsafeCameraFile, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return file;
		}
		
		public void PutFile (string folder, CameraFile file, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_put_file(obj, folder, file.UnsafeCameraFile, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void DeleteFile (string folder, string filename, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_delete_file(obj, folder, filename, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void DeleteAll (string folder, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_delete_all(obj, folder, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void MakeDirectory (string folder, string name, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_make_dir(obj, folder, name, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void RemoveDirectory (string folder, string name, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_remove_dir(obj, folder, name, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraFileInfo GetInfo (string folder, string filename, Context context)
		{
			ErrorCode result;
			CameraFileInfo fileinfo = new CameraFileInfo();
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_get_info (obj, folder, filename, out fileinfo, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return fileinfo;
		}
		
		public void SetInfo (string folder, string filename, CameraFileInfo fileinfo, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_set_info(obj, folder, filename, fileinfo, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public int GetNumber (string folder, string filename, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_number(obj, folder, filename, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public string GetName (string folder, int number, Context context)
		{
			ErrorCode result;
			string name;
			unsafe
			{
			
				IntPtr name_addr = IntPtr.Zero;
				IntPtr name_addr_addr = new IntPtr((void*)&name_addr);
				result = _CameraFilesystem.gp_filesystem_name(obj, folder, number, name_addr_addr, context.UnsafeContext);
				name = Marshal.PtrToStringAnsi(name_addr);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return name;
		}
		
		public int Count (string folder, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_count(obj, folder, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public void Reset ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraFilesystem.gp_filesystem_reset(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
	}
}
