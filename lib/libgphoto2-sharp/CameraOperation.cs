/*
 * CameraOperation.cs
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
	public enum CameraOperation {
		None		= 0,
		CaptureImage	= 1 << 0,
		CaptureVideo	= 1 << 1,
		CaptureAudio	= 1 << 2,
		CapturePreview	= 1 << 3,
		Config		= 1 << 4,
	}
}
