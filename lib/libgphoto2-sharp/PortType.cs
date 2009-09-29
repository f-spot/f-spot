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

namespace GPhoto2
{
	[Flags]
	public enum PortType
	{
		None 	= 0,
		Serial 	= 1 << 0,
		USB 	= 1 << 2,
		Disk 	= 1 << 3,
		PtpIP	= 1 << 4,
	}
}	
