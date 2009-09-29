/*
 * CameraText.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */

using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraText
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=(32*1024))] string text;
		
		public string Text {
			get { return text; }
			set { text = value; }
		}
	}
}	
