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
	
	public class CameraAbilitiesList : Object
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_new (out IntPtr native);

		public CameraAbilitiesList()
		{
			IntPtr native;

			Error.CheckError (gp_abilities_list_new (out native));

			this.handle = new HandleRef (this, native);
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_free (HandleRef list);
		
		protected override void Cleanup ()
		{
			gp_abilities_list_free(this.handle);
		}
		
		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_abilities_list_load (HandleRef list, HandleRef context);

		public void Load (Context context)
		{
			unsafe {
				ErrorCode result = gp_abilities_list_load (this.Handle, context.Handle);
				
				if (Error.IsError (result))
					throw Error.ErrorException(result);
			}
		}
		
		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_abilities_list_detect (HandleRef list, HandleRef info_list, HandleRef l, HandleRef context);

		public void Detect (PortInfoList info_list, CameraList l, Context context)
		{
			Error.CheckError (gp_abilities_list_detect (this.handle, info_list.Handle, 
								    l.Handle, context.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_count (HandleRef list);

		public int Count ()
		{
			ErrorCode result = gp_abilities_list_count (this.handle);

			if (Error.IsError (result)) 
				throw Error.ErrorException (result);

			return (int)result;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_lookup_model (HandleRef list, string model);

		public int LookupModel (string model)
		{
			ErrorCode result = gp_abilities_list_lookup_model(this.handle, model);

			if (Error.IsError (result))
				throw Error.ErrorException (result);
	
			return (int)result;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_get_abilities (HandleRef list, int index, out CameraAbilities abilities);

		public CameraAbilities GetAbilities (int index)
		{
			CameraAbilities abilities = new CameraAbilities ();

			Error.CheckError (gp_abilities_list_get_abilities(this.Handle, index, out abilities));

			return abilities;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_append (HandleRef list, ref CameraAbilities abilities);

		public void Append (CameraAbilities abilities)
		{
			Error.CheckError (gp_abilities_list_append (this.Handle, ref abilities));
		}
	}
}
