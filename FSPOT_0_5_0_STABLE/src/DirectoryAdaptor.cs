using System;
using System.Collections;

namespace FSpot {
	public class DirectoryAdaptor : GroupAdaptor {
		System.Collections.DictionaryEntry [] dirs;

		// FIXME store the Photo.Id list here not just the count
		private class Group : IComparer {
			public int Count = 1;

			public int Compare (object obj1, object obj2)
			{
				// FIXME use a real exception
				if (obj1 is DictionaryEntry && obj2 is DictionaryEntry)
					return Compare ((DictionaryEntry)obj1, (DictionaryEntry)obj2);
				else 
					throw new Exception ("I can't compare that");
				
				
			}
		
			private static int Compare (DictionaryEntry de1, DictionaryEntry de2)
			{
				return string.Compare ((string)de1.Key, (string)de2.Key);
			}
		}


		public override event GlassSetHandler GlassSet;

		public override void SetGlass (int group)
		{
			if (group < 0 || group > dirs.Length)
				return;

			Console.WriteLine ("Selected Path {0}", dirs [group].Key);
	
			int item = LookupItem (group);

			if (GlassSet != null)
				GlassSet (this, item);
		}

		public override int Count ()
		{
			return dirs.Length;
		}

		public override string GlassLabel (int item)
		{
			return (string)dirs [item].Key;
		}
		
		public override string TickLabel (int item)
		{
			return null;
		}

		public override int Value (int item) 
		{
			return ((DirectoryAdaptor.Group)dirs [item].Value).Count;
		}	

		public override event ChangedHandler Changed;
		protected override void Reload () 
		{
			System.Collections.Hashtable ht = new System.Collections.Hashtable ();
			Photo [] photos = query.Store.Query ((Tag [])null, null, null, null);
			
			foreach (Photo p in photos) {
				if (ht.Contains (p.DirectoryPath)) {
					DirectoryAdaptor.Group group = (DirectoryAdaptor.Group) ht [p.DirectoryPath];
					group.Count += 1;
				} else 
					ht [p.DirectoryPath] = new DirectoryAdaptor.Group ();
			}
			
			Console.WriteLine ("Count = {0}", ht.Count);
			dirs = new System.Collections.DictionaryEntry [ht.Count];
			ht.CopyTo (dirs, 0);
			
			Array.Sort (dirs, new DirectoryAdaptor.Group ());
			Array.Sort (query.Photos, new Photo.CompareDirectory ());
			
			if (!order_ascending) {
				Array.Reverse (dirs);
				Array.Reverse (query.Photos);
			}
			
			if (Changed != null)
				Changed (this);
		}

		public override int IndexFromPhoto(FSpot.IBrowsableItem item)
		{
			Photo photo = (Photo)item;
			string directory_path = photo.DirectoryPath;
			
			for (int i = 0; i < dirs.Length; i++) {
				if ((string)dirs [i].Key == directory_path) {
					return i;
				}
			}
			
			// FIXME not truly implemented
			return 0;
		}

		public override FSpot.IBrowsableItem PhotoFromIndex (int item) 
		{
			return query [LookupItem (item)];
		}

		private int LookupItem (int group)
		{
			int i = 0;
			while (i < query.Count) {
				if (((Photo)(query [i])).DirectoryPath == (string)dirs [group].Key) {
					return i;
				}
				i++;
			}
			return 0;
		}

		public DirectoryAdaptor (PhotoQuery query, bool order_ascending)
			: base (query, order_ascending)
		{ }
	}
}
