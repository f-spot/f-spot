//
// DateEditFlags.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Widgets
{
	[System.Flags]
	public enum DateEditFlags
	{
		None = 0,
		ShowTime = 1 << 0,
		ShowSeconds = 1 << 1,
		ShowOffset = 1 << 2,
	}
}
