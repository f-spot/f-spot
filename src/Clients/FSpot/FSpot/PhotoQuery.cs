//
// PhotoQuery.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2008-2010 Ruben Vermeersch
// Copyright (C) 2004-2005 Larry Ewing
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;
using FSpot.Query;

using Hyena;

namespace FSpot
{
	public class PhotoQuery : IBrowsableCollection
	{
		class PhotoCache
		{
			static int SIZE = 100;
			public int Size {
				get { return SIZE; }
			}

			readonly Dictionary<int, Photo []> cache;
			readonly string temp_table;
			readonly PhotoStore store;

			public PhotoCache (PhotoStore store, string temp_table)
			{
				this.temp_table = temp_table;
				this.store = store;
				cache = new Dictionary<int, Photo[]> ();
			}

			public bool TryGetPhoto (int index, out Photo photo)
			{
				photo = null;
				Photo [] val;
				int offset = index - index % SIZE;
				if (!cache.TryGetValue (offset, out val))
					return false;
				photo = val [index - offset];
				return true;
			}

			public Photo Get (int index)
			{
				Photo [] val;
				int offset = index - index % SIZE;
				if (!cache.TryGetValue (offset, out val)) {
					val = store.QueryFromTemp (temp_table, offset, SIZE);
					cache [offset] = val;
				}
				return val [index - offset];
			}
		}

		PhotoCache cache;
		PhotoStore store;
		Term terms;

		static int query_count = 0;
		static int QueryCount {
			get {return query_count ++;}
		}

		Dictionary<uint, int> reverse_lookup;

		int count = -1;

		string temp_table = string.Format ("photoquery_temp_{0}", QueryCount);

		public PhotoQuery (PhotoStore store, params IQueryCondition [] conditions)
		{
			this.store = store;
			this.store.ItemsChanged += MarkChanged;
			cache = new PhotoCache (store, temp_table);
			reverse_lookup = new Dictionary<uint, int> ();
			SetCondition (OrderByTime.OrderByTimeDesc);

			foreach (IQueryCondition condition in conditions)
				SetCondition (condition);

			RequestReload ();
		}

		public int Count {
			get {
				if (count < 0)
					count = store.Count (temp_table);
				return count;
			}
		}

		public bool Contains (IPhoto item) {
			return IndexOf (item) >= 0;
		}

		// IPhotoCollection Interface
		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public IPhoto this [int index] {
			get { return cache.Get (index); }
		}

		[Obsolete ("DO NOT USE THIS, IT'S TOO SLOW")]
		public Photo [] Photos {
			get { return store.QueryFromTemp (temp_table); }
		}

		[Obsolete ("DO NOT USE Items on PhotoQuery")]
		public IPhoto [] Items {
			get { throw new NotImplementedException (); }
		}

		public PhotoStore Store {
			get { return store; }
		}

		//Query Conditions
		Dictionary<Type, IQueryCondition> conditions;
		Dictionary<Type, IQueryCondition> Conditions {
			get { return conditions ?? (conditions = new Dictionary<Type, IQueryCondition>()); }
		}

		internal bool SetCondition (IQueryCondition condition)
		{
			if (condition == null)
				throw new ArgumentNullException (nameof (condition));
			if (Conditions.ContainsKey (condition.GetType ()) && Conditions [condition.GetType ()] == condition)
				return false;
			Conditions [condition.GetType ()] = condition;
			return true;
		}

		internal T GetCondition<T> () where T : IQueryCondition
		{
			IQueryCondition val;
			Conditions.TryGetValue (typeof (T), out val);
			return (T)val;
		}

		internal bool UnSetCondition<T> ()
		{
			if (!Conditions.ContainsKey (typeof(T)))
				return false;
			Conditions.Remove (typeof(T));
			return true;
		}

		public Term Terms {
			get { return terms; }
			set {
				terms = value;
				untagged = false;
				RequestReload ();
			}
		}

		public DateRange Range {
			get { return GetCondition<DateRange> (); }
			set {
				if (value == null && UnSetCondition<DateRange> () || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		bool untagged = false;
		public bool Untagged {
			get { return untagged; }
			set {
			    if (untagged == value)
                    return;

			    untagged = value;

			    if (untagged) {
			        UnSetCondition<ConditionWrapper> ();
			        UnSetCondition<HiddenTag> ();
			    }

			    RequestReload ();
			}
		}

		public RollSet RollSet {
			get { return GetCondition<RollSet> (); }
			set {
				if (value == null && UnSetCondition<RollSet> () || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public RatingRange RatingRange {
			get { return GetCondition<RatingRange> (); }
			set {
				if (value == null && UnSetCondition<RatingRange>() || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public HiddenTag HiddenTag {
			get { return GetCondition<HiddenTag> (); }
			set {
				if (value == null && UnSetCondition<HiddenTag>() || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public ConditionWrapper TagTerm {
			get { return GetCondition<ConditionWrapper> (); }
			set {
				if (value == null && UnSetCondition<ConditionWrapper>()
				    || value != null && SetCondition (value)) {

					if (value != null) {
						untagged = false;
						SetCondition (HiddenTag.ShowHiddenTag);
					} else {
						UnSetCondition<HiddenTag> ();
					}

					RequestReload ();
				}
			}
		}

		public OrderByTime OrderByTime {
			get { return GetCondition<OrderByTime> (); }
			set {
				if (value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public bool TimeOrderAsc {
			get { return OrderByTime.Asc; }
			set {
				if (value != OrderByTime.Asc)
					OrderByTime = new OrderByTime (value);
			}
		}

		public void RequestReload ()
		{
			uint timer = Log.DebugTimerStart ();
			IQueryCondition[] condition_array;

			int i = 0;
			if (untagged) {
				condition_array = new IQueryCondition[conditions.Count + 1];
				condition_array[0] = new UntaggedCondition ();
				i = 1;
			} else {
				condition_array = new IQueryCondition[conditions.Count + 2];
		//		condition_array[0] = new ConditionWrapper (extra_condition);
				condition_array[1] = new ConditionWrapper (terms != null ? terms.SqlCondition () : null);
				i = 2;
			}

			foreach (IQueryCondition condition in Conditions.Values) {
				condition_array[i] = condition;
				i++;
			}

			store.QueryToTemp (temp_table, condition_array);

			count = -1;
			cache = new PhotoCache (store, temp_table);
			reverse_lookup = new Dictionary<uint,int> ();

			if (Changed != null)
				Changed (this);

			Log.DebugTimerPrint (timer, "Reloading the query took {0}");
		}

		public int IndexOf (IPhoto photo)
		{
			if (photo == null || !(photo is Photo))
				return -1;
			return store.IndexOf (temp_table, photo as Photo);
		}

		int [] IndicesOf (DbItem [] dbitems)
		{
			uint timer = Log.DebugTimerStart ();
			var indices = new List<int> ();
			var items_to_search = new List<uint> ();
			int cur;
			foreach (DbItem dbitem in dbitems) {
				if (reverse_lookup.TryGetValue (dbitem.Id, out cur))
					indices.Add (cur);
				else
					items_to_search.Add (dbitem.Id);
			}

			if (items_to_search.Count > 0)
				indices.AddRange (store.IndicesOf (temp_table, items_to_search.ToArray ()));
			Log.DebugTimerPrint (timer, "IndicesOf took {0}");
			return indices.ToArray ();
		}

		public int LookupItem (DateTime date)
		{
			return LookupItem (date, TimeOrderAsc);
		}

		int LookupItem (DateTime date, bool asc)
		{
			if (Count == 0)
				return -1;

			uint timer = Log.DebugTimerStart ();
			int low = 0;
			int high = Count - 1;
			int mid = (low + high) / 2;
			Photo current;
			while (low <= high) {
				mid = (low + high) / 2;
				if (!cache.TryGetPhoto (mid, out current))
					//the item we're looking for is not in the cache
					//a binary search could take up to ln2 (N/cache.SIZE) request
					//lets reduce that number to 1
					return store.IndexOf (temp_table, date, asc);

				int comp = this [mid].Time.CompareTo (date);
				if (!asc && comp < 0 || asc && comp > 0)
					high = mid - 1;
				else if (!asc && comp > 0 || asc && comp < 0)
					low = mid + 1;
				else
					return mid;
			}
			Log.DebugTimerPrint (timer, "LookupItem took {0}");
			if (asc)
				return this[mid].Time < date ? mid + 1 : mid;
			
			return this[mid].Time > date ? mid + 1 : mid;
		}

		public void Commit (int index)
		{
			Commit (new int [] {index});
		}

		public void Commit (int [] indexes)
		{
			List<Photo> to_commit = new List<Photo>();
			foreach (int index in indexes) {
				to_commit.Add (this [index] as Photo);
				reverse_lookup [(this [index] as Photo).Id] = index;
			}
			store.Commit (to_commit.ToArray ());
		}

		void MarkChanged (object sender, DbItemEventArgs<Photo> args)
		{
			int [] indexes = IndicesOf (args.Items);

			if (indexes.Length > 0 && ItemsChanged != null)
				ItemsChanged (this, new BrowsableEventArgs(indexes, (args as PhotoEventArgs).Changes));
		}

		public void MarkChanged (int index, IBrowsableItemChanges changes)
		{
			MarkChanged (new int [] {index}, changes);
		}

		public void MarkChanged (int [] indexes, IBrowsableItemChanges changes)
		{
			ItemsChanged (this, new BrowsableEventArgs (indexes, changes));
		}
	}
}
