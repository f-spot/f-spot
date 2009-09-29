/*
 * CameraFileOperation.cs
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
	public enum CameraFileOperation {
		None		= 0,
		Delete		= 1 << 1,
		Preview		= 1 << 3,
		Raw		= 1 << 4,
		Audio		= 1 << 5,
		Exif		= 1 << 6,
	}
}		
