/*
 * Port.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{

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
	public unsafe struct PortSettingsSerial
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public char[] port;
		public int speed;
		public int bits;
		public PortSerialParity parity;
		public int stopbits;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct PortSettingsUSB
	{
		public int inep, outep, intep;
		public int config;
		public int pinterface;
		public int altsetting;
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct PortSettings
	{
		[FieldOffset(0)] public PortSettingsSerial serial;
		[FieldOffset(0)] public PortSettingsUSB usb;
	}

	public class Port : GPObject
	{
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_new (out IntPtr port);
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_free (HandleRef port);

		public Port() : base (gp_port_free)
		{
			IntPtr native;
			Error.CheckError (gp_port_new (out native));

			this.handle = new HandleRef (this, native);
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_set_info (HandleRef port, ref PortInfo info);

		public void SetInfo (PortInfo info)
		{
		Error.CheckError (gp_port_set_info (this.Handle, ref info));
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_get_info (HandleRef port, out PortInfo info);

		public PortInfo GetInfo ()
		{
			PortInfo info = new PortInfo (); 

			Error.CheckError (gp_port_get_info (this.Handle, out info));

			return info;
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_open (HandleRef port);

		public void Open ()
		{
			Error.CheckError (gp_port_open (this.Handle));
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_close (HandleRef port);

		public void Close ()
		{
			Error.CheckError (gp_port_close (this.Handle));
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_read (HandleRef port, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, int size);

		public byte[] Read (int size)
		{
			byte[] data = new byte[size];

			Error.CheckError (gp_port_read (this.Handle, data, size));

			return data;
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_write (HandleRef port, [MarshalAs(UnmanagedType.LPTStr)] byte[] data, int size);

		public void Write (byte[] data)
		{
			Error.CheckError (gp_port_write (this.Handle, data, data.Length));
		}
		

		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_set_settings (HandleRef port, PortSettings settings);

		public void SetSettings (PortSettings settings)
		{
			Error.CheckError (gp_port_set_settings (this.Handle, settings));
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_get_settings (HandleRef port, out PortSettings settings);

		public PortSettings GetSettings ()
		{
			PortSettings settings;

			Error.CheckError (gp_port_get_settings (this.Handle, out settings));

			return settings;
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_get_timeout (HandleRef port, out int timeout);

		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_set_timeout (HandleRef port, int timeout);

		public int Timeout
		{
			get {
				int timeout;

				Error.CheckError (gp_port_get_timeout (this.Handle, out timeout));

				return timeout;
			}
			set {
				Error.CheckError (gp_port_set_timeout (this.Handle, value));
			}
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_get_pin (HandleRef port, Pin pin, out Level level);

		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_set_pin (HandleRef port, Pin pin, Level level);
	}
}
