/*
 * DeviceType.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */


using System;

namespace GPhoto2
{
	[Flags]
	public enum DeviceType {
		StillCamera 		= 0,
		AudioPlayer		= 1 << 0,
	}
}		
