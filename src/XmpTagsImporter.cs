// XmpTagsImporter.cs: Creates tags based on embedded XMP tags in the photo/sidecar.
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// (C) 2006 Bengt Thuree
// 

using Gtk;
using System;
using System.Collections;
using System.IO;
using System.Xml;
using FSpot.Xmp;

namespace FSpot.Xmp {
	public class XmpTagsImporter {
		private PhotoStore photo_store;
		private TagStore tag_store;
		private Stack tags_created;
		private Gtk.Window parent;
		static private string LastImportStr = "Imported Tags";
		static private string LastImportIcon = "f-spot-imported-xmp-tags.png";
		static private string CityStr = "City";
		static private string CountryStr = "Country";
		static private string LocationStr = "Location";
		static private string StateStr = "State";
		static private string PlacesIcon = "f-spot-places.png";
		static private string XMPSidecarStr = "Tags in a XMP Sidecar";

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
		// The following are the various sub tags under Last Import.
		TagInfo li_subroot_country, li_subroot_city, li_subroot_state, li_subroot_loc;
					
		public XmpTagsImporter (PhotoStore photo_store, TagStore tag_store)
		{
			this.photo_store = photo_store;
			this.tag_store = tag_store;
			tags_created = new Stack ();
			// Prepare the Last Import root tag
			li_root_tag = new TagInfo (LastImportStr, LastImportIcon);
			// Prepare possible sub root's under the Last Import root tag
			li_subroot_loc = new TagInfo (LocationStr, PlacesIcon);
			li_subroot_country = new TagInfo (CountryStr, PlacesIcon);
			li_subroot_city = new TagInfo (CityStr, PlacesIcon);
			li_subroot_state = new TagInfo (StateStr, PlacesIcon);
		}

		private Tag AddImportRootTagIfNotExist (TagInfo new_tag, Category parent)
		{
			Tag root_tag = tag_store.GetTagByName (Mono.Posix.Catalog.GetString (new_tag.TagName));
			if (root_tag == null) {
				root_tag = tag_store.GetTagByName (new_tag.TagName);

				if (root_tag == null) {
					root_tag = tag_store.CreateCategory (
						parent, 
						Mono.Posix.Catalog.GetString (new_tag.TagName));
						if (new_tag.HasIcon)
							root_tag.StockIconName = new_tag.IconName;
					tags_created.Push (root_tag);
				}
			}
			return root_tag;
		}

		private void AddTagToPhoto (Photo photo, string new_tag_name, TagInfo sub_tag_root)
		{
			if (new_tag_name != null) {
				// Check if a tag exists for this new_tag_name
				Tag tag = tag_store.GetTagByName (new_tag_name);

				// If not, make sure the rootstr tag exists and add this new_tag_name under it
				if (tag == null) {
					Tag root_tag = AddImportRootTagIfNotExist (li_root_tag, tag_store.RootCategory);

					// If we should have a sub root, add it now.
					if (sub_tag_root != null)	{
						Tag subroot_tag = AddImportRootTagIfNotExist (sub_tag_root, root_tag as Category);
						root_tag = subroot_tag; // Ensure we attach to subroot_tag 
					} // end adding subroot
					
					// add the new tag, and put it under the root/subroot
					// but only if it does not exist (same name as root tag)
					tag = tag_store.GetTagByName (new_tag_name);
					if (tag == null) {
						tag = tag_store.CreateCategory (root_tag as Category, new_tag_name);
						tags_created.Push (tag);
					}
				}
				
				// Now we have the tag for this place, add the photo to it
				photo.AddTag (tag);
			} // if new_tag_name != null
		}

		private void AddTagToPhoto (Photo photo, string [] new_tag_names, TagInfo sub_tag_root)
		{
			if (new_tag_names != null)
				foreach (string tag in new_tag_names)
					AddTagToPhoto (photo, tag, sub_tag_root);
		}
		

		
		public bool Import (Photo photo, string path, string orig_path)
		{
			bool needs_commit = false; // Default to no XMP data embedded in picture

			XmpTagsMetadata xmd = new XmpTagsMetadata (path, orig_path);
			
			// Read the XMP tags (embedded tags takes priority)
			xmd.Read_Sidecar_tags();	// first read xmp tags from a possible sidecar file
			xmd.Read_Embedded_tags();	// second read the tags embedded in the photo (exif/xmp etc)
			
			// If no tags (EXIF/XMP etc) were found, return false
			if (xmd.EmptyTags())
				return needs_commit;

			// Ok, we have XMP data embeeded in the photo. 
			needs_commit = true;

			// F-Spot do not keep Headline/Caption but only a description field, 
			// so we need to combine the two headline/caption to one field (with a " : " between)
			// Also, there are at least two different options here. So we assume that if
			// something is set, we should use it. Start to check for the title, then description.

			// Since F-Spot do not handle the EXIF description to good yet, we leave this one out.
			// photo.Description = xmd.GetXmpTag ("exif_description");

			// F-Spot only stores the description into XMP User Comment, so if this one is set, we use it.
			
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
			
			// F-Spot uses exif time for the time beeing.
//			if (xmd.GetXmpTag ("DateTimeOriginal) != null)
//				photo.Time = xmd.GetXmpTag ("DateTimeOriginal);

			if (xmd.GetXmpTag ("State") != null)
				AddTagToPhoto (photo, xmd.GetXmpTag ("State"), li_subroot_state);
			if (xmd.GetXmpTag ("City") != null)
				AddTagToPhoto (photo, xmd.GetXmpTag ("City"), li_subroot_city);
			if (xmd.GetXmpTag ("Country") != null)
				AddTagToPhoto (photo, xmd.GetXmpTag ("Country"), li_subroot_country);
			if (xmd.GetXmpTag ("Location") != null)
				AddTagToPhoto (photo, xmd.GetXmpTag ("Location"), li_subroot_loc);

			if (xmd.GetXmpTag ("Source") != null)
				AddTagToPhoto (photo, xmd.GetXmpTag ("Source"), null);

			if (xmd.GetXmpBag ("subject") != null)
				if (xmd.GetXmpBag ("subject") is string [])
					AddTagToPhoto (photo, xmd.GetXmpBag ("subject") as string [], null);
				else
					AddTagToPhoto (photo, xmd.GetXmpBag ("subject") as string, null);

			if (xmd.GetXmpBag ("SupplementalCategories") != null)
				if (xmd.GetXmpBag ("SupplementalCategories") is string [])
					AddTagToPhoto (photo, xmd.GetXmpBag ("SupplementalCategories") as string [], null);
				else
					AddTagToPhoto (photo, xmd.GetXmpBag ("SupplementalCategories") as string, null);	

			if (xmd.GetXmpBag ("People") != null)
				if (xmd.GetXmpBag ("People") is string [])
					AddTagToPhoto (photo, xmd.GetXmpBag ("People") as string [], null);
				else
					AddTagToPhoto (photo, xmd.GetXmpBag ("People") as string, null);	


			
			// Ok, not good. But at least one indication which photo has tags in a sidecar.
			// FIXME : Should be changed to possible an internal representation later.
			//if (xmd.AnySidecarTags())
			//	AddTagToPhoto (photo, Mono.Posix.Catalog.GetString (XMPSidecarStr), null);
			
			return needs_commit;
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
