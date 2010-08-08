/*
 * IBrowsableItemChanges.cs
 *
 * Author(s):
 * 	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

namespace FSpot.Core
{
	public interface IBrowsableItemChanges
	{
		bool DataChanged {get;}
		bool MetadataChanged {get;}
	}

	public class FullInvalidate : IBrowsableItemChanges
	{
		static FullInvalidate instance = new FullInvalidate ();
		public static FullInvalidate Instance {
			get { return instance; }
		}

		public bool DataChanged {
			get { return true; }
		}
		public bool MetadataChanged {
			get { return true; }
		}
	}

	public class InvalidateData : IBrowsableItemChanges
	{
		static InvalidateData instance = new InvalidateData ();
		public static InvalidateData Instance {
			get { return instance; }
		}

		public bool DataChanged { get { return true; } }
		public bool MetadataChanged { get { return false; } }
	}
}
