/*
 * PhotoStore.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@gnome.org>
 *	Stephane Delcroix <stephane@delcroix.org>
 *	Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 * 
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public class PhotoVersion : IBrowsableItemVersion
	{
		IBrowsableItem photo;
		uint version_id;
		System.Uri uri;
		string md5_sum;
		string name;
		bool is_protected;
	
		public string Name {
			get { return name; }
			set { name = value; }
		}
	
		public IBrowsableItem Photo {
			get { return photo; }
		}
	
		public System.Uri Uri {
			get { return uri; }
			set { 
				if (value == null)
					throw new System.ArgumentNullException ("uri");
				uri = value;
			}
		}

		public string MD5Sum {
			get { return md5_sum; } 
			internal set { md5_sum = value; }
		}
	
		public uint VersionId {
			get { return version_id; }
		}
	
		public bool IsProtected {
			get { return is_protected; }
		}
	
		public PhotoVersion (IBrowsableItem photo, uint version_id, System.Uri uri, string md5_sum, string name, bool is_protected)
		{
			this.photo = photo;
			this.version_id = version_id;
			this.uri = uri;
			this.md5_sum = md5_sum;
			this.name = name;
			this.is_protected = is_protected;
		}
	}
}
