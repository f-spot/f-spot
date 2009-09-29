/*
 * CameraList.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org
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
	public class CameraList : GPObject 
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_new (out IntPtr list);
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_unref (HandleRef list);

		public CameraList () : base (gp_list_unref)
		{
			IntPtr native;
			Error.CheckError (gp_list_new (out native));
					  
			this.handle = new HandleRef (this, native);
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_count (HandleRef list);
		
		public int Count {
			get { return Error.CheckError (gp_list_count (handle)); }
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_set_name (HandleRef list, int index, [MarshalAs(UnmanagedType.LPTStr)] string name);

		public void SetName (int n, string name)
		{
			Error.CheckError (gp_list_set_name(this.Handle, n, name));
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_get_name (HandleRef list, int index, out IntPtr name);

		public string GetName (int index)
		{
			IntPtr name;
			Error.CheckError (gp_list_get_name(this.Handle, index, out name));

			return Marshal.PtrToStringAnsi (name);
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_set_value (HandleRef list, int index, [MarshalAs (UnmanagedType.LPTStr)] string value);

		public void SetValue (int n, string value)
		{
			Error.CheckError (gp_list_set_value (this.Handle, n, value));
		}
		
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_get_value (HandleRef list, int index, out IntPtr value);

		public string GetValue (int index)
		{
			IntPtr value;
			Error.CheckError (gp_list_get_value(this.Handle, index, out value));

			return Marshal.PtrToStringAnsi (value);
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_reset (HandleRef list);

		public void Reset ()
		{
			Error.CheckError (gp_list_reset(this.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_list_sort (HandleRef list);

		public void Sort ()
		{
			Error.CheckError (gp_list_sort(this.Handle));
		}
	}
}
