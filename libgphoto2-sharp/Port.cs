using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	public enum PortType
	{
		None = 0,
		Serial = 1 << 0,
		USB = 1 << 2
	}

	public enum PortSerialParity
	{
		Off = 0,
		Even,
		Odd
	}
	
	public enum Pin
	{
		RTS,
		DTR,
		CTS,
		DSR,
		CD,
		RING
	}
	
	public enum Level
	{
		Low = 0,
		High = 1
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct PortPrivateLibrary
	{
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct PortPrivateCore
	{
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct PortSettingsSerial
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] char[] port;
		int speed;
		int bits;
		PortSerialParity parity;
		int stopbits;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct PortSettingsUSB
	{
		int inep, outep, intep;
		int config;
		int pinterface;
		int altsetting;
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct PortSettings
	{
		[FieldOffset(0)] PortSettingsSerial serial;
		[FieldOffset(0)] PortSettingsUSB usb;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _Port
	{
		PortType type;

		PortSettings settings;
		PortSettings settings_pending;

		int timout;

		PortPrivateLibrary *pl;
		PortPrivateCore *pc;

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_new (out _Port *port);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_free (_Port *port);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_set_info (_Port *port, ref _PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_get_info (_Port *port, out _PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_open (_Port *port);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_close (_Port *port);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_read (_Port *port, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, int size);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_write (_Port *port, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, int size);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_get_settings (_Port *port, out PortSettings settings);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_set_settings (_Port *port, PortSettings settings);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_get_timeout (_Port *port, int *timeout);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_set_timeout (_Port *port, int timeout);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_get_pin (_Port *port, Pin pin, Level *level);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_port_set_pin (_Port *port, Pin pin, Level level);

		[DllImport ("libgphoto2.so")]
		internal static extern char* gp_port_get_error (_Port *port);

		//[DllImport ("libgphoto2.so")]
		//internal static extern int gp_port_set_error (_Port *port, const char *format, ...);
	}

	public class Port : IDisposable
	{
		unsafe _Port *obj;
		
		public Port()
		{
			ErrorCode result;
			unsafe 
			{
				result = _Port.gp_port_new(out obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~Port()
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
					result = _Port.gp_port_free(obj);
					if (Error.IsError(result)) throw Error.ErrorException(result);
					obj = null;
				}
			}
		}
		
		public void SetInfo (PortInfo info)
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_set_info (obj, ref info.Handle);
			}

			if (Error.IsError (result))
				throw Error.ErrorException(result);
		}
		
		public PortInfo GetInfo ()
		{
			PortInfo info = new PortInfo (); 

			unsafe
			{
				Error.CheckError (_Port.gp_port_get_info(obj, out info.Handle));
			}
			return info;
		}
		
		public void Open ()
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_open(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void Close ()
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_close(obj);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public byte[] Read (int size)
		{
			ErrorCode result;
			byte[] data = new byte[size];
			unsafe
			{
				result = _Port.gp_port_read(obj, data, size);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return data;
		}
		
		public void Write (byte[] data)
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_write(obj, data, data.Length);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public void SetSettings (PortSettings settings)
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_set_settings(obj, settings);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
		
		public PortSettings GetSettings ()
		{
			ErrorCode result;
			PortSettings settings;
			unsafe
			{
				result = _Port.gp_port_get_settings(obj, out settings);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return settings;
		}
		
		public int GetTimeout ()
		{
			ErrorCode result;
			int timeout;
			unsafe
			{
				result = _Port.gp_port_get_timeout(obj, &timeout);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
			return timeout;
		}
		
		public void SetTimeout (int timeout)
		{
			ErrorCode result;
			unsafe
			{
				result = _Port.gp_port_set_timeout(obj, timeout);
			}
			if (Error.IsError(result)) throw Error.ErrorException(result);
		}
	}
}
