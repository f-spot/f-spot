//
// FSpot.Widgets.DateEditFlags
//
// Author(s)
//   Stephane Delcroix  <stephane@delcroix.org>
//
// Copyright (c) 2009 Novell, Inc.
//
// This is free software. See COPYING for details.
//

namespace FSpot.Widgets
{
	[System.Flags]
	public enum DateEditFlags
	{
		None 			= 0,
		ShowTime 		= 1 << 0,
		ShowSeconds		= 1 << 1,
		ShowOffset		= 1 << 2,
	}
}
