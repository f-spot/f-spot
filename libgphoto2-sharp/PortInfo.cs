using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _PortInfo
	{
		internal PortType type;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] internal string name;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] internal string path;

		/* Private */
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] internal string library_filename;
	}
	
	public class PortInfo
	{
		unsafe internal _PortInfo obj;
		
		public PortInfo()
		{
			obj = new _PortInfo();
		}
		
		internal PortInfo(_PortInfo info_obj)
		{
			obj = info_obj;
		}
		
		internal _PortInfo SafePortInfo
		{
			get
			{
				return obj;
			}
			set
			{
				obj = value;
			}
		}
		
		public string Name
		{
			get
			{
				unsafe
				{
					return obj.name;
				}
			}
		}
		
		public string Path
		{
			get
			{
				unsafe
				{
					return obj.path;
				}
			}
		}
	}
}
