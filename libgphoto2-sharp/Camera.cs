using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraPrivateLibrary
	{
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraPrivateCore
	{
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraText
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=(32*1024))] string text;
		
		public string Text
		{
			get
			{
				return text;
			}
			set
			{
				text = value;
			}
		}
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraFunctions
	{
		internal delegate ErrorCode _CameraExitFunc (_Camera *camera, _Context *context);

		internal delegate ErrorCode _CameraGetConfigFunc (_Camera *camera, out IntPtr widget, _Context *context);

		internal delegate ErrorCode _CameraSetConfigFunc (_Camera *camera, HandleRef widget, _Context *context);

		internal delegate ErrorCode _CameraCaptureFunc (_Camera *camera, CameraCaptureType type, CameraFilePath *path, _Context *context);

		internal delegate ErrorCode _CameraCapturePreviewFunc (_Camera *camera, _CameraFile *file, _Context *context);
		
		internal delegate ErrorCode _CameraSummaryFunc (_Camera *camera, CameraText *text, _Context *context);
		
		internal delegate ErrorCode _CameraManualFunc (_Camera *camera, CameraText *text, _Context *context);
		
		internal delegate ErrorCode _CameraAboutFunc (_Camera *camera, CameraText *text, _Context *context);
		
		internal delegate ErrorCode _CameraPrePostFunc (_Camera *camera, _Context *context);
                                             
		/* Those will be called before and after each operation */
		_CameraPrePostFunc pre_func;
		_CameraPrePostFunc post_func;

		_CameraExitFunc exit;

		/* Configuration */
		_CameraGetConfigFunc       get_config;
		_CameraSetConfigFunc       set_config;

		/* Capturing */
		_CameraCaptureFunc        capture;
		_CameraCapturePreviewFunc capture_preview;

		/* Textual information */
		_CameraSummaryFunc summary;
		_CameraManualFunc  manual;
		_CameraAboutFunc   about;
		
		/* Reserved space to use in the future without changing the struct size */
		void *reserved1;
		void *reserved2;
		void *reserved3;
		void *reserved4;
		void *reserved5;
		void *reserved6;
		void *reserved7;
		void *reserved8;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFilePath
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string name;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] public string folder;
	}

	public enum CameraCaptureType
	{
		Image,
		Movie,
		Sound
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _Camera
	{
		_Port           *port;
		_CameraFilesystem *fs;
		CameraFunctions  *functions;

		CameraPrivateLibrary  *pl; /* Private data of camera libraries    */
		CameraPrivateCore     *pc; /* Private data of the core of gphoto2 */

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_new (out _Camera *camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_ref (_Camera *camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_unref (_Camera *camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_free (_Camera *camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_abilities (_Camera *camera, CameraAbilities abilities);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_abilities (_Camera *camera, CameraAbilities *abilities);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_port_info (_Camera *camera, _PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_port_info (_Camera *camera, out _PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_port_speed (_Camera *camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_port_speed (_Camera *camera, int speed);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_init (_Camera *camera, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_exit (_Camera *camera, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_capture (_Camera *camera, CameraCaptureType type, out CameraFilePath path, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_capture_preview (_Camera *camera, _CameraFile *file, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_config (_Camera *camera, out IntPtr window, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_config (_Camera *camera, out IntPtr window, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_list_files (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraList *list, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_list_folders (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraList *list, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_put_file (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, _CameraFile *file, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_delete_all (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_make_dir (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_remove_dir (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_get (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, CameraFileType type, _CameraFile *camera_file, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_delete (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_get_info (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, out CameraFileInfo info, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_set_info (_Camera *camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, CameraFileInfo info, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_manual (_Camera *camera, out CameraText manual, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_summary (_Camera *camera, out CameraText summary, _Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_about (_Camera *camera, out CameraText about, _Context *context);

		unsafe internal _CameraFilesystem* GetFS ()
		{
			return fs;
		}
	}
	
	public class Camera : IDisposable
	{
		unsafe _Camera *obj;
		
		public Camera()
		{
			unsafe 
			{
				_Camera.gp_camera_new(out obj);
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~Camera()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			unsafe
			{
				if (obj != null)
				{
					_Camera.gp_camera_unref(obj);
					obj = null;
				}
			}
		}
		
		public void SetAbilities (CameraAbilities abilities)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_set_abilities(obj, abilities);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraAbilities GetAbilities ()
		{
			ErrorCode result;
			CameraAbilities abilities;
			unsafe
			{
				result = _Camera.gp_camera_get_abilities(obj, &abilities);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return abilities;
		}
		
		public void SetPortInfo (PortInfo portinfo)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_set_port_info(obj, portinfo.SafePortInfo);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public PortInfo GetPortInfo ()
		{
			ErrorCode result;
			PortInfo portinfo = new PortInfo();
			unsafe
			{
				result = _Camera.gp_camera_get_port_info(obj, out portinfo.obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return portinfo;
		}
		
		public int GetPortSpeed ()
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_get_port_speed(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public void SetPortSpeed (int speed)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_set_port_speed(obj, speed);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Init (Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_init(obj, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Exit (Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_init(obj, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraFilePath Capture (CameraCaptureType type, Context context)
		{
			ErrorCode result;
			CameraFilePath path = new CameraFilePath();
			unsafe
			{
				result = _Camera.gp_camera_capture(obj, type, out path, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return path;
		}
		
		public CameraFile CapturePreview (Context context)
		{
			ErrorCode result;
			CameraFile file = new CameraFile();
			unsafe
			{
				result = _Camera.gp_camera_capture_preview(obj, file.UnsafeCameraFile, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return file;
		}
		
		public CameraList ListFiles (string folder, Context context)
		{
			ErrorCode result;
			CameraList file_list = new CameraList();
			unsafe
			{
				result = _Camera.gp_camera_folder_list_files(obj, folder, file_list.UnsafeCameraList, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return file_list;
		}
		
		public CameraList ListFolders (string folder, Context context)
		{
			ErrorCode result;
			CameraList file_list = new CameraList();
			unsafe
			{
				result = _Camera.gp_camera_folder_list_folders(obj, folder, file_list.UnsafeCameraList, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return file_list;
		}
		
		public void PutFile (string folder, CameraFile file, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_folder_put_file(obj, folder, file.UnsafeCameraFile, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void DeleteAll (string folder, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_folder_delete_all(obj, folder, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void MakeDirectory (string folder, string name, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_folder_make_dir(obj, folder, name, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void RemoveDirectory (string folder, string name, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_folder_remove_dir(obj, folder, name, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraFile GetFile (string folder, string name, CameraFileType type, Context context)
		{
			ErrorCode result;
			CameraFile file = new CameraFile();
			unsafe
			{
				result = _Camera.gp_camera_file_get(obj, folder, name, type, file.UnsafeCameraFile, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return file;
		}
		
		public void DeleteFile (string folder, string name, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_file_delete(obj, folder, name, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraFileInfo GetFileInfo (string folder, string name, Context context)
		{
			ErrorCode result;
			CameraFileInfo fileinfo;
			unsafe
			{
				result = _Camera.gp_camera_file_get_info(obj, folder, name, out fileinfo, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return fileinfo;
		}
		
		public void SetFileInfo (string folder, string name, CameraFileInfo fileinfo, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _Camera.gp_camera_file_set_info(obj, folder, name, fileinfo, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public CameraText GetManual (Context context)
		{
			ErrorCode result;
			CameraText manual;
			unsafe
			{
				result = _Camera.gp_camera_get_manual(obj, out manual, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return manual;
		}
		
		public CameraText GetSummary (Context context)
		{
			ErrorCode result;
			CameraText summary;
			unsafe
			{
				result = _Camera.gp_camera_get_summary(obj, out summary, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return summary;
		}
		
		public CameraText GetAbout (Context context)
		{
			ErrorCode result;
			CameraText about;
			unsafe
			{
				result = _Camera.gp_camera_get_about(obj, out about, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return about;
		}
		
		public CameraFilesystem GetFS()
		{
			CameraFilesystem fs;
			unsafe
			{
				fs = new CameraFilesystem(obj->GetFS());
			}
			return fs;
		}
	}
}
