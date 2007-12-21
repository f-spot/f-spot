/*
 * DirectoryCollection.cs
 *
 * Author(s):
 *	Larry Ewing  (lewing@novell.com)
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Collections;
using System.Xml;

namespace FSpot {
	public class DirectoryCollection : UriCollection
	{
		string path;

		public DirectoryCollection (string path)
		{
			this.path = path;
			Load ();
		}

		// Methods
		public string Path {
			get {
				return path;
			}
			set {
				path = value;
				Load ();
				System.Console.WriteLine ("XXXXX after load");
			}
		}

		void Load () {
			// FIXME this should probably actually throw and exception
			// if the directory doesn't exist.

			if (Directory.Exists (path)) {
				DirectoryInfo info = new DirectoryInfo (path);
				LoadItems (info.GetFiles ());
			} else if (File.Exists (path)) {
				list.Clear ();
				Add (new FileBrowsableItem (path));
			}
		}
	}
}
