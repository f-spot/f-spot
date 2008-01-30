/*
 * FSpot.PhotoQuery.cs
 * 
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using FSpot.Query;

namespace FSpot {
	public class PhotoQuery : FSpot.IBrowsableCollection {
		private Photo [] photos;
		private PhotoStore store;
		private Term terms;
		private Tag [] tags;
		private string extra_condition;
		
		// Constructor
		public PhotoQuery (PhotoStore store)
		{
			this.store = store;
			// Note: this is to let the query pick up
			// 	 photos that were added or removed over dbus
			this.store.ItemsAddedOverDBus += delegate { RequestReload(); };
			this.store.ItemsRemovedOverDBus += delegate { RequestReload(); };

			photos = store.Query ((Tag [])null, null, Range, RollSet, RatingRange);
		}

		public int Count {
			get { return photos.Length;}
		}
		
		public bool Contains (IBrowsableItem item) {
			return IndexOf (item) >= 0;
		}

		// IPhotoCollection Interface
		public event FSpot.IBrowsableCollectionChangedHandler Changed;
		public event FSpot.IBrowsableCollectionChangedHandler PreChanged;
		public event FSpot.IBrowsableCollectionItemsChangedHandler ItemsChanged;
		
		public IBrowsableItem this [int index] {
			get { return photos [index]; }
		}

		public Photo [] Photos {
			get { return photos; }
		}

		public IBrowsableItem [] Items {
			get { return (IBrowsableItem [])photos; }
		}
		
		public PhotoStore Store {
			get { return store; }
		}
		

		//Query Conditions
		private Dictionary<Type, IQueryCondition> conditions;
		private Dictionary<Type, IQueryCondition> Conditions {
			get {
				if (conditions == null)
					conditions = new Dictionary<Type, IQueryCondition> ();
				return conditions;
			}
		}

		internal bool SetCondition (IQueryCondition condition)
		{
			if (condition == null)
				throw new ArgumentNullException ("condition");
			if (Conditions.ContainsKey (condition.GetType ()) && Conditions [condition.GetType ()] == condition)
				return false;
			Conditions [condition.GetType ()] = condition;
			return true;
		}

		internal IQueryCondition GetCondition<T> ()
		{
			if (Conditions.ContainsKey (typeof (T)))
				return Conditions [typeof (T)];
			return null;
		}

		internal bool UnSetCondition<T> ()
		{
			if (!Conditions.ContainsKey (typeof(T)))
				return false;
			Conditions.Remove (typeof(T));
			return true;
		}

		public Term Terms {
			get {
				return terms;
			}
			set {
				terms = value;
				untagged = false;
				RequestReload ();
			}
		}

		public string ExtraCondition {
			get {
				return extra_condition;
			}
			
			set {
				extra_condition = value;

				if (value != null)
					untagged = false;

 				RequestReload ();
 			}
 		}
		
		public DateRange Range {
			get { return GetCondition<DateRange> () as DateRange; }
			set {
				if (value == null && UnSetCondition<DateRange> () || value != null && SetCondition (value))
					RequestReload ();
			}
		}
		
		private bool untagged = false;
		public bool Untagged {
			get { return untagged; }
			set {
				if (untagged != value) {
					untagged = value;
					if (untagged)
						extra_condition = null;
					RequestReload ();
				}
			}
		}

		public RollSet RollSet {
			get { return GetCondition<RollSet> () as RollSet; }
			set {
				if (value == null && UnSetCondition<RollSet> () || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public RatingRange RatingRange {
			get { return GetCondition<RatingRange> () as RatingRange; }
			set {
				if (value == null && UnSetCondition<RatingRange>() || value != null && SetCondition (value))
					RequestReload ();
			}
		}

		public bool Unrated {
			get {
				return (GetCondition<RatingRange> () != null && GetCondition<RatingRange> () == RatingRange.Unrated);
			}
			set {
				if (value)
					RatingRange = RatingRange.Unrated;
				else
					if (UnSetCondition<RatingRange> ())
						RequestReload ();
			}
		}
		
		public void RequestReload ()
		{
			if (untagged)
				photos = store.Query (new UntaggedCondition (), Range, RollSet, RatingRange);
			else
				photos = store.Query (terms, extra_condition, Range, RollSet, RatingRange);

			//this event will allow resorting the query content
			if (PreChanged != null)
				PreChanged (this);

			if (Changed != null)
				Changed (this);
		}
		
		public int IndexOf (IBrowsableItem photo)
		{
			return System.Array.IndexOf (photos, photo);
		}
		
		public void Commit (int index) 
		{
			store.Commit (photos[index]);
			MarkChanged (index);
		}
		
		public void MarkChanged (int index)
		{
			ItemsChanged (this, new BrowsableArgs (index));
		}
	}
}
