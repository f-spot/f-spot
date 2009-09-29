/*
 * PortInfo.cs
 *
 * Author(s):
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
	public unsafe struct PortInfo
	{
		PortType type;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] string name;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] string path;

		/* Private */
#pragma warning disable 169
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] string library_filename;
#pragma warning restore 169


		public PortType Type {
			get { return type; }
		}

		public string Name {
			get { return name; }
		}

		public string Path {
			get { return path; }
		}

		public override string ToString ()
		{
			return String.Format ("PortInfo: {0}\t{1} ({2})", Name, Path, Type);
		}
	}
}
