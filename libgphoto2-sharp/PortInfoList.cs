using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _PortInfoList
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_new (out _PortInfoList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_free (_PortInfoList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_load (_PortInfoList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_count (_PortInfoList *list);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_get_info (_PortInfoList *list, int n, out _PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_lookup_path (_PortInfoList *list, [MarshalAs(UnmanagedType.LPTStr)] string path);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_lookup_name (_PortInfoList *list, [MarshalAs(UnmanagedType.LPTStr)] string name);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_info_list_append (_PortInfoList *list, _PortInfo info);
	}
	
	public class PortInfoList : IDisposable
	{
		unsafe _PortInfoList *obj;
		
		public PortInfoList()
		{
			ErrorCode result;
			unsafe 
			{
				result = _PortInfoList.gp_port_info_list_new(out obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~PortInfoList()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			ErrorCode result;
			unsafe
			{
				if (obj != null)
				{
					result = _PortInfoList.gp_port_info_list_free(obj);
					if (Error.IsError(result)) throw Error.ErrorException(result);
					obj = null;
				}
			}
		}
		
		unsafe internal _PortInfoList* UnsafePortInfoList
		{
			get
			{
				return obj;
			}
		}
		
		public void Load()
		{
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_load(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public int Count()
		{
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_count(obj);
			}
			
			if (Error.IsError(result)) throw Error.ErrorException(result);
			
			return (int)result;
		}
		
		public PortInfo GetInfo(int n)
		{
			_PortInfo info = new _PortInfo();
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_get_info(obj, n, out info);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			PortInfo class_info = new PortInfo(info);
			return class_info;
		}
		
		public int LookupPath(string path)
		{
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_lookup_path(obj, path);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public int LookupName(string name)
		{
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_lookup_name(obj, name);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
		
		public int Append(PortInfo info)
		{
			ErrorCode result;
			unsafe
			{
				result = _PortInfoList.gp_port_info_list_append(obj, info.SafePortInfo);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return (int)result;
		}
	}
}
