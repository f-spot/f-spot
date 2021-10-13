// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  

namespace FSpot.Cms
{
	public enum IccProfileClass : uint {
		Input      = 0x73636e72,  /* 'scnr' */
		Display    = 0x6D6e7472,  /* 'mntr' */
		Output     = 0x70727472,  /* 'prtr' */
		Link       = 0x6c696e6b,  /* 'link' */
		Abstract   = 0x61627374,  /* 'abst' */
		ColorSpace = 0x73706163,  /* 'spac' */
		NamedColor = 0x6e6d636c,  /* 'nmcl' */
		Max        = 0xffffffff, 
	}
}
