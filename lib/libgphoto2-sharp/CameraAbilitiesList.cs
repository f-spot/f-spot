/*
 * CameraAbilitiesList.cs
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
using System.Collections.Generic;
using System.Collections;

namespace GPhoto2
{
	public class CameraAbilitiesList : GPList<CameraAbilities>
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_new (out IntPtr native);
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_free (HandleRef list);

		public CameraAbilitiesList () : base (gp_abilities_list_free)
		{
			IntPtr native;
			Error.CheckError (gp_abilities_list_new (out native));
			this.handle = new HandleRef (this, native);
		}
		
		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_abilities_list_load (HandleRef list, HandleRef context);

		public void Load (Context context)
		{
			Error.CheckError (gp_abilities_list_load (this.Handle, context.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_abilities_list_detect (HandleRef list, HandleRef info_list, HandleRef l, HandleRef context);

		public CameraList Detect (PortInfoList info_list, Context context)
		{
			CameraList camera_list = new CameraList ();
			Error.CheckError (gp_abilities_list_detect (Handle, info_list.Handle, 
								    camera_list.Handle, context.Handle));
			return camera_list;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_count (HandleRef list);

		public override int Count {
			get { return Error.CheckError (gp_abilities_list_count (this.handle)); }
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_get_abilities (HandleRef list, int index, out CameraAbilities abilities);

		public override CameraAbilities this [int index] {
			get {
				CameraAbilities abilities;
				Error.CheckError (gp_abilities_list_get_abilities(this.Handle, index, out abilities));

				return abilities;
			}
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_abilities_list_lookup_model (HandleRef list, [MarshalAs (UnmanagedType.LPTStr)]string model);

		public CameraAbilities this [string model] {
			get { return this [Error.CheckError (gp_abilities_list_lookup_model(this.handle, model))]; }
		}
			
	}
}
