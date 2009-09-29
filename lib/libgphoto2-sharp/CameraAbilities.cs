/*
 * CameraAbilities.cs
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
		
#pragma warning disable 169
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string library;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string id;
#pragma warning restore 169

		DeviceType device_type;
		
#pragma warning disable 169
		int reserved2;
		int reserved3;
		int reserved4;
		int reserved5;
		int reserved6;
		int reserved7;
		int reserved8;
#pragma warning restore 169

		public override string ToString ()
		{
			string ret = String.Format ("{0} ({1})", Model, PortType);
			if (DriverStatus != CameraDriverStatus.Production)
				ret += String.Format (" <{0}>", DriverStatus);
			return ret;
		}

		public string Model {
			get { return model; }
		}

		public CameraDriverStatus DriverStatus {
			get { return status; }
		}
		public PortType PortType {
			get { return port; }
		}

		public int[] Speeds {
			get { return speed; } 
		}

		public CameraOperation CameraOperation {
			get { return operations; }
		}

		public CameraFileOperation CameraFileOperation {
			get { return file_operations; }
		}

		public CameraFolderOperation CameraFolderOperation {
			get { return folder_operations; }
		}

		public int UsbVendor {
			get { return usb_vendor; }
		}

		public int UsbProduct {
			get { return usb_product; }
		}

		public int UsbClass {
			get { return usb_class; }
		}

		public int UsbSubclass {
			get { return usb_subclass; }
		}

		public int UsbProtocol {
			get { return usb_protocol; }
		}

		public DeviceType DeviceType {
			get { return device_type;}
		}
	}
}
