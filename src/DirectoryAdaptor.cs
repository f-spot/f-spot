using System;
using System.Collections;

namespace FSpot {
	public class DirectoryAdaptor : GroupAdaptor {
		public PhotoQuery query;
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
	
			int item = 0;
			int i = 0;
			while (i < query.Photos.Length) {
				if (query.Photos [i].DirectoryPath == (string)dirs [group].Key) {
					item = i;
					break;
				}
				i++;
			}

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

		private void HandleChanged (FSpot.IBrowsableCollection query)
		{
			Console.WriteLine ("Reloading directory");
			Reload ();
		}
		
		public override event ChangedHandler Changed;
		public override void Reload () 
		{
			System.Collections.Hashtable ht = new System.Collections.Hashtable ();
			Photo [] photos = query.Store.Query (null, null);
			
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
			
			if (Changed != null)
				Changed (this);
		}

		public DirectoryAdaptor (PhotoQuery query) {
			this.query = query;
			this.query.Changed += HandleChanged;

			Reload ();
		}
	}
}
