using System;
using System.Collections;

using SemWeb;

namespace SemWeb.Util {

	internal class ResSet : ICollection {
		Hashtable items = new Hashtable();
		ICollection keys;
		
		public ResSet() {
		}
		
		private ResSet(Hashtable items) {
			this.items = items;
		}

		public void Add(Resource res) {
			items[res] = items;
			keys = null;
		}	
		public void Remove(Resource res) {
			items.Remove(res);
			keys = null;
		}
		
		public bool Contains(Resource res) {
			return items.ContainsKey(res);
		}
		
		public ICollection Items {
			get {
				if (keys == null)
					keys = items.Keys;
				return keys;
			}
		}
		
		public void AddRange(ResSet set) {
			foreach (Resource r in set.Items) {
				Add(r);
			}
		}
		
		public void Clear() {
			items.Clear();
			keys = null;
		}
		
		public ResSet Clone() {
			return new ResSet((Hashtable)items.Clone());
		}
		
		public int Count { get { return items.Count; } }
		
		public IEnumerator GetEnumerator() { return items.Keys.GetEnumerator(); }
		
		bool ICollection.IsSynchronized { get { return false; } }
		object ICollection.SyncRoot { get { return null; } }
		
		public void CopyTo(System.Array array, int index) {
			foreach (Resource r in this)
				array.SetValue(r, index++);
		}
		
		/*Hashtable Intersect(Hashtable x, Hashtable y) {
			Hashtable a, b;
			if (x.Count < y.Count) { a = x; b = y; }
			else { b = x; a = y; }
			Hashtable c = new Hashtable();
			foreach (Resource r in a)
				if (b.ContainsKey(r))
					c[r] = c;
			return c;
		}*/		
	}


}