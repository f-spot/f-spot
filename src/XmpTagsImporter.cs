/*
 * FSpot.Xmp.XmpTagsImporter: Creates tags based on embedded XMP tags in the photo/sidecar.
 *
 * Author(s)
 * 	Bengt Thuree (bengt@thuree.com)
 * 	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using Gtk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using FSpot.Xmp;
using SemWeb;
using SemWeb.Util;
using Mono.Unix;
using Hyena;

namespace FSpot.Xmp {
        internal class XmpTagsImporter {
		private TagStore tag_store;
		private Stack<Tag> tags_created;

		static private string LastImportIcon = "gtk-new";
		static private string PlacesIcon = "emblem-places";

	        const string UserComment = MetadataStore.ExifNS + "UserComment";
		const string Headline = MetadataStore.PhotoshopNS + "Headline";
		const string Caption = MetadataStore.PhotoshopNS + "Caption";
		const string CaptionWriter = MetadataStore.PhotoshopNS + "CaptionWriter";
		const string Credit = MetadataStore.PhotoshopNS + "Credit";
		const string Category = MetadataStore.PhotoshopNS + "Category";
		const string Source = MetadataStore.PhotoshopNS + "Source";
		const string State = MetadataStore.PhotoshopNS + "State";
		const string Country = MetadataStore.PhotoshopNS + "Country";
		const string City = MetadataStore.PhotoshopNS + "City";
		const string SupplementalCategories = MetadataStore.PhotoshopNS + "SupplementalCategories";
		const string Location = MetadataStore.Iptc4xmpCoreNS + "Location";
		const string Title = MetadataStore.DcNS + "title";
		const string Description = MetadataStore.DcNS + "description";
		const string People = MetadataStore.IViewNS + "People";
		const string Subject = MetadataStore.DcNS + "subject";
		const string RdfType = MetadataStore.RdfNS + "type";
		const string Rating = MetadataStore.XmpNS + "Rating";
		const string Urgency = MetadataStore.PhotoshopNS + "Urgency";
		
		private class TagInfo {
			// This class contains the Root tag name, and its Icon name (if any)
			string tag_name;
			string icon_name; 
		       
			public string TagName {
				get { return tag_name; }
			}
			
			public string IconName {
				get { return icon_name; }
			}

			public bool HasIcon {
				get { return icon_name != null; }
			}

			public TagInfo (string t_name, string i_name)
			{
				tag_name = t_name;
				icon_name = i_name;
			}
			
			public TagInfo (string t_name)
			{
				tag_name = t_name;
				icon_name = null;
			}
		} // TagInfo
		
		TagInfo li_root_tag; // This is the Last Import root tag
		Hashtable taginfo_table = new Hashtable ();
		
		public XmpTagsImporter (PhotoStore photo_store, TagStore tag_store)
		{
			this.tag_store = tag_store;
			tags_created = new Stack<Tag> ();
			
			li_root_tag = new TagInfo (Catalog.GetString ("Imported Tags"), LastImportIcon);
			taginfo_table [(Entity)Location] = new TagInfo (Catalog.GetString ("Location"), PlacesIcon);
			taginfo_table [(Entity)Country] = new TagInfo (Catalog.GetString ("Country"), PlacesIcon);
			taginfo_table [(Entity)City] = new TagInfo (Catalog.GetString ("City"), PlacesIcon);
			taginfo_table [(Entity)State] = new TagInfo (Catalog.GetString ("State"), PlacesIcon);
		}
		
		private Tag EnsureTag (TagInfo info, Category parent)
		{
			Tag tag = tag_store.GetTagByName (info.TagName);
			
			if (tag != null)
				return tag;
			
			tag = tag_store.CreateCategory (parent,
							info.TagName,
							false);
			
			if (info.HasIcon) {
				tag.ThemeIconName = info.IconName;
				tag_store.Commit(tag);
			}
			
			tags_created.Push (tag);
			return tag;
		}
		
		private static string GetTextField (Resource value)
		{
			string text_field = null;
			SemWeb.Literal l = value as SemWeb.Literal;
			if (l != null && l.Value != null && l.Value.Length > 0) {
				text_field = l.Value;
				if (Char.IsControl (l.Value [l.Value.Length - 1]))
					text_field = l.Value.Substring (0,l.Value.Length - 1);
			}
			return text_field;
		}

		private void AddTagToPhoto (Photo photo, Resource value, TagInfo sub_tag)
		{
			string tag_name;
			tag_name = GetTextField (value);
			if (tag_name != null && tag_name.Length > 0)
				AddTagToPhoto (photo, tag_name, sub_tag);
		}

		private void AddTagToPhoto (Photo photo, string new_tag_name, TagInfo sub_tag)
		{
			if (new_tag_name == null)
				return;

			Tag parent = EnsureTag (li_root_tag, tag_store.RootCategory);
			
			// If we should have a sub root make sure it exists
			if (sub_tag != null)
				parent = EnsureTag (sub_tag, parent as Category);
			
			Tag tag = EnsureTag (new TagInfo (new_tag_name), parent as Category);
			
			// Now we have the tag for this place, add the photo to it
			photo.AddTag (tag);
		}

		public void ProcessStore (MetadataStore store, Photo photo)
		{
			Hashtable descriptions = new Hashtable ();
			uint rating = System.UInt32.MaxValue; 
			uint urgency = System.UInt32.MaxValue;

			foreach (Statement stmt in store) {
				//StatementList list = null;
				
				switch (stmt.Predicate.Uri) {

				case Caption:
				case Headline:
					if (!descriptions.Contains (stmt.Predicate.Uri)) {
						string caption = GetTextField (stmt.Object as SemWeb.Literal);
						if (caption != null)
							caption = caption.Trim ();

						if ((caption != null) && (caption.Length > 0))
							descriptions.Add (stmt.Predicate.Uri, caption);
					}
					break;
				case Title:
				case Description:
				case UserComment:
					if (!(stmt.Object is Entity))
						break;

					foreach (Statement tag in store.Select (new Statement (stmt.Object as Entity, null, null))) {
						if ( (tag.Predicate != RdfType) && (!descriptions.Contains (stmt.Predicate.Uri)) ) {
							string title = null;
							try {
								title = (GetTextField ((SemWeb.Literal) tag.Object)).Trim ();
							} catch {
							}
							if ( (title != null) && (title.Length > 0) )
								descriptions.Add (stmt.Predicate.Uri, title);
						}
					}
					break;

				case Urgency: // Used if Rating was not found
				case Rating:
					SemWeb.Literal l = stmt.Object as SemWeb.Literal;
					if (l != null && l.Value != null && l.Value.Length > 0) {
						uint tmp_ui;
						try {
							tmp_ui = System.Convert.ToUInt32 (l.Value);
						} catch {
							// Set rating to 0, and continue
							Log.DebugFormat ("Found illegal rating >{0}< in predicate {1}. Rating cleared",
										 l.Value, stmt.Predicate.Uri);
							tmp_ui = 0;
						}
						if (tmp_ui > 5) // Max rating allowed in F-Spot
							tmp_ui = 5;
						if (stmt.Predicate.Uri == Rating)
							rating = tmp_ui;
						else
							urgency = tmp_ui == 0 ? 0 : tmp_ui - 1; // Urgency valid values 1 - 8
					}	 
					break;

				case State:
				case City:
				case Country:
				case Location:
				case Source:
					AddTagToPhoto (photo, stmt.Object as SemWeb.Literal, taginfo_table [stmt.Predicate] as TagInfo);
					break;
					
				case Subject:
				case SupplementalCategories:
				case People:
					if (!(stmt.Object is Entity))
						break;

					foreach (Statement tag in store.Select (new Statement (stmt.Object as Entity, null, null))) {
						
						if (tag.Predicate != RdfType)
							AddTagToPhoto (photo, tag.Object as SemWeb.Literal, null);

					}
					break;
				}
			}

			if (descriptions.Contains (UserComment))
				photo.Description = descriptions [UserComment] as String;

			// Use the old urgency, only if rating was not available.
			if (urgency < System.UInt32.MaxValue)
				photo.Rating = urgency;
			if (rating < System.UInt32.MaxValue)
				photo.Rating = rating;

#if false	
			//FIXME: looks like we are doing some questionable repurposing of tags here...

			// We want to construct the following : Description = <Headline> :: <Caption>         

			// only check for more title/comment if you still do not have one.
			if ((photo.Description == null) || (photo.Description.Length == 0)) {
				if (descriptions.Contains (Headline)) 
					photo.Description = descriptions [Headline] as String;
				// Lets add the Caption to the existing Description (Headline).
				if (descriptions.Contains (Caption))
					photo.Description += (( (photo.Description == null) ? "" : " :: ") + descriptions [Caption] as String);
			}	
			
			// only check for more title/comment if you still do not have one. 
			if ((photo.Description == null) || (photo.Description.Length == 0)) {
				if (descriptions.Contains (Title)) 
					photo.Description = descriptions [Title] as String;
				// Lets add the Description  to the existing Description (Title).
				if (descriptions.Contains (Description))
					photo.Description += (( (photo.Description == null) ? "" : " :: ") + descriptions [Description] as String);
			}
#endif
		}
		
		public bool Import (Photo photo, SafeUri uri, SafeUri orig_uri)
		{
			XmpFile xmp;
			
			string path = uri.AbsolutePath;
			string orig_path = orig_uri.AbsolutePath;

			string source_sidecar = String.Format ("{0}{1}{2}.xmp",
							       Path.GetDirectoryName (orig_path),
							       Path.DirectorySeparatorChar,
							       Path.GetFileName (orig_path));
			
			string dest_sidecar = String.Format ("{0}{1}{2}.xmp",
							       Path.GetDirectoryName (path),
							       Path.DirectorySeparatorChar,
							       Path.GetFileName (path));
			
			if (File.Exists (source_sidecar)) {
				xmp = new XmpFile (File.OpenRead (source_sidecar));
			} else if (File.Exists (dest_sidecar)) {
				xmp = new XmpFile (File.OpenRead (dest_sidecar));
			} else {
				xmp = new XmpFile ();
			}
			
			using (ImageFile img = ImageFile.Create (uri)) {
				StatementSource source = img as StatementSource;
				if (source != null) {
					try {
						source.Select (xmp);
					} catch  {
					}
				}
			}

			ProcessStore (xmp.Store, photo);
#if enable_debug
			xmp.Save (Console.OpenStandardOutput ());
#endif			
			return true;
		}

		public void Cancel()
		{
			// User have cancelled the import.
			// Remove all created tags
			while (tags_created.Count > 0) 
				tag_store.Remove (tags_created.Pop());
			
			// Clear the tags_created array
			tags_created.Clear();
		}

		public void Finish()
		{
			// Clear the tags_created array, since we do not need it anymore.
			tags_created.Clear();		
		}
	}
} // namespace
