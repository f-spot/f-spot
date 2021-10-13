// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2003-2009 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2004-2006 Larry Ewing
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Models;

using Hyena;

using Mono.Unix;

namespace FSpot.Database
{
	public class InvalidTagOperationException : InvalidOperationException
	{
		public Tag Tag { get; }

		public InvalidTagOperationException (Tag t, string message) : base (message)
		{
			Tag = t;
		}
	}

	// Sorts tags into an order that it will be safe to delete
	// them in (eg children first).
	public class TagRemoveComparer : IComparer<Tag>
	{
		public int Compare (Tag t1, Tag t2)
		{
			if (t1.IsAncestorOf (t2))
				return 1;

			if (t2.IsAncestorOf (t1))
				return -1;

			return 0;
		}
	}

	public class TagStore : DbStore<Tag>
	{
		public const string StockIconDbPrefix = "stock_icon:";

		Tag hidden;

		public Tag RootCategory { get; }

		public Tag Hidden {
			get { return hidden; }
			private set {
				hidden = value;
				// FIXME, where did HiddenTag go?
				//HiddenTag.Tag = value;
			}
		}

		public Tag GetTagByName (string name)
		{
			var tag = Context.Tags.FirstOrDefault (x => x.Name == name);
			return tag;
		}

		public Tag GetTagById (Guid id)
		{
			var tag = Context.Tags.FirstOrDefault (x => x.Id == id);
			return tag;
		}

		public List<Tag> TagsStartWith (string s)
		{
			var tags = Context.Tags
				.Where (x => s.ToLower ().StartsWith (x.Name.ToLower ()))
				.ToList ();

			if (!tags.Any ())
				return null;

			tags.Sort ((t1, t2) => t2.Popularity.CompareTo (t1.Popularity));

			return tags;
		}

		// In this store we keep all the items (i.e. the tags) in memory at all times.  This is
		// mostly to simplify handling of the parent relationship between tags, but it also makes it
		// a little bit faster.  We achieve this by passing "true" as the cache_is_immortal to our
		// base class.
		void LoadAllTags ()
		{
			// Pass 1, get all the tags.
			var tags = Context.Tags;

			// Pass 2, set the parents.
			foreach (var tag in tags) {
				try {
					tag.Category = Get (tag.CategoryId);
				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
				}

				if (tag.Category == null)
					Log.Warning ("Tag Without category found");
			}

			//Pass 3, set popularity
			var groups = Context.PhotoTags
				.GroupBy (x => x.TagId)
				.Select (n => new {
					TagId = n.Key,
					PhotoCount = n.Count ()
				});

			foreach (var tag in tags) {
				var result = groups.FirstOrDefault (x => x.TagId == tag.Id);
				tag.Popularity = result?.PhotoCount ?? 0;
			}

			//if (Context.Meta.HiddenTagId.Value != null)
			//	Hidden = LookupInCache ((uint)Db.Meta.HiddenTagId.ValueAsInt);
		}

		public TagStore ()
		{
			// The label for the root category is used in new and edit tag dialogs
			RootCategory = new Tag (null, Guid.Empty, Catalog.GetString ("(None)"));
			LoadAllTags ();
		}

		Tag InsertTagIntoTable (Tag parentCategory, string name, bool isCategory, bool autoicon)
		{
			var tag = new Tag (parentCategory) {
				Name = name,
				IsCategory = isCategory,
				CategoryId = parentCategory.Id,
				Icon = autoicon ? null : string.Empty,
				SortPriority = 0
			};

			Context.Tags.Add (tag);
			Context.SaveChanges ();

			// The table in the database is setup to be an INTEGER.
			return tag;
		}

		public Tag CreateTag (Tag newTag, string name, bool autoicon, bool isCategory)
		{
			if (newTag == null)
				newTag = RootCategory;

			var tag = InsertTagIntoTable (newTag, name, isCategory, autoicon);
			tag.IconWasCleared = !autoicon;

			EmitAdded (tag);

			return tag;
		}

		public override Tag Get (Guid id)
		{
			return id == Guid.Empty ? RootCategory : Context.Tags.FirstOrDefault (x => x.Id == id);
		}

		public void GetChildren (Tag tag)
		{
			if (tag == null)
				throw new ArgumentNullException (nameof (tag));

			var children = Context.Tags.Where (x => x.CategoryId == tag.Id).ToList ();
			children.Sort ();
			tag.Children = children;
		}

		public override void Remove (Tag item)
		{
			var category = item;
			if (category?.Children?.Count > 0)
				throw new InvalidTagOperationException (category, "Cannot remove category that contains children");

			Context.Remove (item);
			Context.SaveChanges ();

			EmitRemoved (item);
		}

		public override void Commit (Tag item)
		{
			Commit (item, false);
		}

		public void Commit (Tag tag, bool updateXmp = false)
		{
			Commit (new[] { tag }, updateXmp);
		}

		public void Commit (Tag[] tags, bool updateXmp)
		{
			//foreach (Tag tag in tags) {
			//	Database.Execute (new HyenaSqliteCommand ("UPDATE tags SET name = ?, category_id = ?, "
			//				+ "is_category = ?, sort_priority = ?, icon = ? WHERE id = ?",
			//					  tag.Name,
			//					  tag.Category.Id,
			//					  tag is Category ? 1 : 0,
			//					  tag.SortPriority,
			//					  GetIconString (tag),
			//					  tag.Id));

			//	if (updateXmp && Preferences.Get<bool> (Preferences.MetadataEmbedInImage)) {
			//		Photo[] photos = Db.Photos.Query (new TagTerm (tag));
			//		foreach (Photo p in photos)
			//			if (p.HasTag (tag)) // the query returns all the pics of the tag and all its child. this avoids updating child tags
			//				SyncMetadataJob.Create (Db.Jobs, p);
			//	}
			//	context.Tags.AddRange (tag);
			//}
			//context.SaveChanges ();

			//EmitChanged (tags);
		}
	}
}
