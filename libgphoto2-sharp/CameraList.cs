using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct CameraListEntry
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] string name;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] string value;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _CameraList
	{
		int count;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=1024)] CameraListEntry[] entry;
		int refcount;
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_new (out _CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_ref (_CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_unref (_CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_free (_CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_count (_CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_set_name (_CameraList *list, int index, [MarshalAs(UnmanagedType.LPTStr)] string name);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_set_value (_CameraList *list, int index, [MarshalAs(UnmanagedType.LPTStr)] string value);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_get_name (_CameraList *list, int index, IntPtr name);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_get_value (_CameraList *list, int index, IntPtr value);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_append (_CameraList *list, [MarshalAs(UnmanagedType.LPTStr)] string name, [MarshalAs(UnmanagedType.LPTStr)] string value);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_populate (_CameraList *list, [MarshalAs(UnmanagedType.LPTStr)] string format, int count);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_reset (_CameraList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_sort (_CameraList *list);
	}
	
	public class CameraList : IDisposable
	{
		unsafe _CameraList *obj;
		
		public CameraList()
		{
			unsafe 
			{
				_CameraList.gp_list_new(out obj);
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~CameraList()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			unsafe
			{
				if (obj != null)
				{
					_CameraList.gp_list_unref(obj);
					obj = null;
				}
			}
		}
		
		unsafe internal _CameraList* UnsafeCameraList
		{
			get
			{
				return obj;
			}
		}
		
		public int Count ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_count(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public void SetName (int n, string name)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_set_name(obj, n, name);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void SetValue (int n, string value)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_set_value(obj, n, value);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public string GetName (int index)
		{
			ErrorCode result;
			string name;
			unsafe
			{
				IntPtr name_addr = IntPtr.Zero;
				IntPtr name_addr_addr = new IntPtr((void*)&name_addr);
				result = _CameraList.gp_list_get_name(obj, index, name_addr_addr);
				name = Marshal.PtrToStringAnsi(name_addr);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return name;
		}
		
		public string GetValue (int index)
		{
			ErrorCode result;
			string value;
			unsafe
			{
				IntPtr value_addr = IntPtr.Zero;
				IntPtr value_addr_addr = new IntPtr((void*)&value_addr);
				result = _CameraList.gp_list_get_value(obj, index, value_addr_addr);
				value = Marshal.PtrToStringAnsi(value_addr);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return value;
		}
		
		public void Append (string name, string value)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_append(obj, name, value);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Populate (string format, int count)
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_populate(obj, format, count);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Reset ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_reset(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Sort ()
		{
			ErrorCode result;
			unsafe
			{
				result = _CameraList.gp_list_sort(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public int GetPosition(string name, string value)
		{
			for (int index = 0; index < Count(); index++)
			{
				if (GetName(index) == name && GetValue(index) == value)
					return index;
			}
			
			return -1;
		}
	}
}
