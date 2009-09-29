/*
 * CameraFolderOperation.cs
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
	public enum CameraFolderOperation {
		None			= 0,
		DeleteAll		= 1 << 0,
		PutFile			= 1 << 1,
		MakeDirectory		= 1 << 2,
		RemoveDirectory		= 1 << 3,
	}
}	
