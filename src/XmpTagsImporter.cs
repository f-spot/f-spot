// XmpTagsImporter.cs: Creates tags based on embedded XMP tags in the photo/sidecar.
//
// Author(s):
//   Bengt Thuree (bengt@thuree.com)
//   Larry Ewing <lewing@novell.com>
//
// (C) 2006 Bengt Thuree, 
//     2006 Novell Inc.
// 

using Gtk;
using System;
using System.Collections;
using System.IO;
using System.Xml;
using FSpot.Xmp;
using SemWeb;
using SemWeb.Util;
using Mono.Unix;

namespace FSpot.Xmp {
        internal class XmpTagsImporter {
		private TagStore tag_store;
		private Stack tags_created;

		static private string LastImportIcon = "f-spot-imported-xmp-tags.png";
		static private string PlacesIcon = "f-spot-places.png";

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
			tags_created = new Stack ();
			// Prepare the Last Import root tag
			
			li_root_tag = new TagInfo (Catalog.GetString ("Import Tags"), LastImportIcon);
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
							info.TagName);
			
			if (info.HasIcon) {
				tag.StockIconName = info.IconName;
				tag_store.Commit(tag);
			}
			
			tags_created.Push (tag);
			return tag;
		}
		
		private void AddTagToPhoto (Photo photo, Resource value, TagInfo sub_tag)
		{
			Literal l = value as Literal;
			if (l != null && l.Value != null && l.Value.Length > 0) {
				string tag_name = l.Value;
				if (Char.IsControl (l.Value [l.Value.Length - 1]))
					tag_name = l.Value.Substring (0,l.Value.Length - 1);
				AddTagToPhoto (photo, tag_name, sub_tag);
			}
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
			Hashtable desc = new Hashtable ();

			foreach (Statement stmt in store) {
				StatementList list = null;
				
				switch (stmt.Predicate.Uri) {
				case Description:
				case Headline:
				case Caption:
				case Title:
				case UserComment:
					list = (StatementList) desc [stmt.Predicate];

					if (list == null)
						desc [stmt.Predicate] = list = new StatementList ();
						
					list.Add (stmt);
					break;

				case State:
				case City:
				case Country:
				case Location:
				case Source:
					AddTagToPhoto (photo, stmt.Object as Literal, taginfo_table [stmt.Predicate] as TagInfo);
					break;
					
				case Subject:
				case SupplementalCategories:
				case People:
					if (!(stmt.Object is Entity))
						break;

					foreach (Statement tag in store.Select (new Statement (stmt.Object as Entity, null, null))) {
						
						if (tag.Predicate != RdfType)
							AddTagToPhoto (photo, tag.Object as Literal, null);

					}
					break;
				}
			}

#if false
			/* 
			 * FIXME I need to think through what bengt was doing here before I put it in 
			 * it looks like we are doing some questionable repurposing of tags here and above
			 */
			

			if (xmd.GetXmpBag_0 ("UserComment") != null)
				photo.Description = xmd.GetXmpBag_0 ("UserComment");
			
			// We want to construct the following : Description = <Headline> :: <Caption>         
			
			// only check for more title/comment if you still do not have one.
			if ((photo.Description == null) || (photo.Description.Length == 0)) {
				if (xmd.GetXmpTag ("Headline") != null) 
					photo.Description = xmd.GetXmpTag ("Headline");
				// Lets add the Caption to the existing Description (Headline).
				if (xmd.GetXmpTag ("Caption") != null)
					photo.Description += (( (photo.Description == null) ? "" : " :: ") + xmd.GetXmpTag ("Caption"));
			}	
			
			// only check for more title/comment if you still do not have one. 
			if ((photo.Description == null) || (photo.Description.Length == 0)) {
				if (xmd.GetXmpTag ("title") != null) 
					photo.Description = xmd.GetXmpTag ("title");
				// Lets add the Description  to the existing Description (Title).
				if (xmd.GetXmpTag ("description") != null)
					photo.Description += (( (photo.Description == null) ? "" : " :: ") + xmd.GetXmpTag ("description"));
			}
#endif
		}
		
		public bool Import (Photo photo, string path, string orig_path)
		{
			XmpFile xmp;
			
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
			
			using (ImageFile img = ImageFile.Create (path)) {
				StatementSource source = img as StatementSource;
				if (source != null) {
					source.Select (xmp);
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
				tag_store.Remove ((DbItem) tags_created.Pop());
			
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
