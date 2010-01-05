/*
 * Cms.IccProfileClass.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Pascal de Bruijn  <pmjdebruijn@pcode.nl>
 *
 * This is free software. See COPYING for details.
 */

namespace Cms {
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
