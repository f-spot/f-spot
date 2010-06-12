/*
 * FSpot.IBrowsableItem.cs
 * 
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public interface IBrowsableItem {
		System.DateTime Time {
			get;
		}
		
		Tag [] Tags {
			get;
		}

		IBrowsableItemVersion DefaultVersion {
			get;
		}

		string Description {
			get;
		}

		string Name {
			get; 
		}

		uint Rating {
			get; 
		}
	}
}
