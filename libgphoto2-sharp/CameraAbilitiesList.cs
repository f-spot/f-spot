using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{

	public enum CameraDriverStatus
	{
		Production,
		Testing,
		Experimental
	}
	
	public enum CameraOperation
	{
		None		= 0,
		CaptureImage	= 1 << 0,
		CaptureVideo	= 1 << 1,
		CaptureAudio	= 1 << 2,
		CapturePreview	= 1 << 3,
		Config		= 1 << 4
	}
	
	public enum CameraFileOperation
	{
		None		= 0,
		Delete		= 1 << 1,
		Preview		= 1 << 3,
		Raw			= 1 << 4,
		Audio		= 1 << 5,
		Exif		= 1 << 6
	}
	
	public enum CameraFolderOperation
	{
		None			= 0,
		DeleteAll		= 1 << 0,
		PutFile			= 1 << 1,
		MakeDirectory		= 1 << 2,
		RemoveDirectory		= 1 << 3
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraAbilities
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] string model;
		CameraDriverStatus status;
		
		PortType port;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)] int[] speed;
		
		CameraOperation operations;
		CameraFileOperation file_operations;
		CameraFolderOperation folder_operations;
		
		int usb_vendor;
		int usb_product;
		int usb_class;
		int usb_subclass;
		int usb_protocol;
		
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string library;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string id;
		
		int reserved1;
		int reserved2;
		int reserved3;
		int reserved4;
		int reserved5;
		int reserved6;
		int reserved7;
		int reserved8;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _CameraAbilitiesList
	{

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_new (out _CameraAbilitiesList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_free (_CameraAbilitiesList *list);
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_load (_CameraAbilitiesList *list, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_detect (_CameraAbilitiesList *list, _PortInfoList *info_list, _CameraList *l, _Context *context);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_count (_CameraAbilitiesList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_lookup_model (_CameraAbilitiesList *list, [MarshalAs(UnmanagedType.LPTStr)] string model);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_get_abilities (_CameraAbilitiesList *list, int index, out CameraAbilities abilities);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_append (_CameraAbilitiesList *list, CameraAbilities abilities);
	}
	
	public class CameraAbilitiesList : IDisposable
	{
		unsafe _CameraAbilitiesList *obj;
		
		public CameraAbilitiesList()
		{
			unsafe 
			{
				_CameraAbilitiesList.gp_abilities_list_new(out obj);
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~CameraAbilitiesList()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			unsafe
			{
				if (obj != null)
				{
					_CameraAbilitiesList.gp_abilities_list_free(obj);
					obj = null;
				}
			}
		}
		
		public void Load (Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_load(obj, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Detect (PortInfoList info_list, CameraList l, Context context)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_detect(obj, info_list.UnsafePortInfoList, l.UnsafeCameraList, context.UnsafeContext);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public int Count ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_count(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public int LookupModel (string model)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_lookup_model(obj, model);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public CameraAbilities GetAbilities (int index)
		{
			ErrorCode result;
			CameraAbilities abilities = new CameraAbilities();
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_get_abilities(obj, index, out abilities);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return abilities;
		}
		
		public void Append (CameraAbilities abilities)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraAbilitiesList.gp_abilities_list_append(obj, abilities);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
	}
}
