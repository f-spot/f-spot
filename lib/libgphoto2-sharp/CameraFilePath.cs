/*
 * xxx.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
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
	public unsafe struct CameraFilePath
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] string name;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string folder;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public string Folder {
			get { return folder; }
			set { folder = value; }
		}
	}
}
