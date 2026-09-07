//
// IccProfileClass.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Cms
{
	public enum IccProfileClass : uint
	{
		Display = 0x6D6e7472,  /* 'mntr' */
		Output = 0x70727472,  /* 'prtr' */
		Link = 0x6c696e6b,  /* 'link' */
	}
}
