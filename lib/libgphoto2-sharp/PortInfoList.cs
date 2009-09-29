/*
 * PortInfoList.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	public class PortInfoList : GPList<PortInfo> 
	{
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_new (out IntPtr handle);
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_free (HandleRef handle);
		
		public PortInfoList () : base (gp_port_info_list_free)
		{
			IntPtr native;
			Error.CheckError (gp_port_info_list_new (out native));

			this.handle = new HandleRef (this, native);
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_load (HandleRef handle);

		public void Load ()
		{
			Error.CheckError (gp_port_info_list_load (this.Handle));
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_count (HandleRef handle);

		public override int Count {
			get { return Error.CheckError (gp_port_info_list_count (this.Handle)); }
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal unsafe static extern ErrorCode gp_port_info_list_get_info (HandleRef handle, int n, out PortInfo info);

		public override PortInfo this [int n] {
			get {
				PortInfo info;
				Error.CheckError (gp_port_info_list_get_info (this.handle, n,  out info));
				return info;
			}
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_lookup_path (HandleRef handle, [MarshalAs(UnmanagedType.LPTStr)]string path);

		public PortInfo LookupPath (string path)
		{
			return this [Error.CheckError (gp_port_info_list_lookup_path(this.handle, path))];
		}
		
		[DllImport ("libgphoto2_port.so")]
		internal static extern ErrorCode gp_port_info_list_lookup_name (HandleRef handle, string name);

		public PortInfo LookupName (string name)
		{
			return this [Error.CheckError (gp_port_info_list_lookup_name (this.Handle, name))];
		}
	}
}
