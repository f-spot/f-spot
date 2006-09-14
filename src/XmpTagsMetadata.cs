// #define enable_debug
//
// XmpTagsMetaData.cs: Collects a number of embedded XMP tags from a photo
//	Will try to find an external sidecar file (<image file name>.xmp) both in
//	same place as photo, as well as from the imported location of the photo.
//
// 	Read the sidecar tags first, and then the embedded tags. (which is controlled by the calling class)
//	Doing this, we will not loose any tags and the embedded tags takes priority.
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//	With great help from Stephane, Cosme and Warren
//
// (C) 2006 Bengt Thuree
// 

using System;
using System.IO;
using System.Xml;
using FSpot;
using FSpot.Xmp;
using SemWeb;

namespace FSpot.Xmp {
	public class XmpTagsMetadata {
	
		public class Bag_Couple {
		
			private string title;
			public string Title {
				get { 
					return title;
				}
			}

			private string value;
			public string Value {
				get { 
					return value;
				}
			}
			
			public Bag_Couple (string t1, string v1)
			{
				this.title = t1;
				this.value = v1;
			}
		}
		
		private bool xmptags_sidecar_exist;	// Sets to true if a xmp sidecar (<image file name>.xmp) 
							// is found (with photo, or import location)
		private bool xmptags_found_sidecar_tag; // Indicates if the sidecar file contained tags or not.
		private bool xmptags_found_xmp_tags;	// Sets to true if EXIF or XMP tags exists.
		private string xmptags_photofile;	// full path to image file
		private string xmptags_sidecarfile;	// full path to sidecar file
		
		private System.Collections.ArrayList items_list; // Temporary storage of items before we store them in the right place.

		static System.Collections.Hashtable xmp_table;
		static System.Collections.Hashtable xmp_bag_table;

		// We probably do not need the below tables with strings for the hash tables.
		// We should be able to just store the predicates from the embedded XMP tags directly.
		// But by using the below tables we know much better which tags we do support, and it makes
		// it much easier to fetch the tag. As well as knowing which tag is a row tag, and which is a bag tag.I 

		static string [] xmp_bag_tags = {
			// http://ns.adobe.com/photoshop/1.0
			"SupplementalCategories" ,  // same as subject (keywords) 
			
			// http://purl.org/dc/elements/1.1
			"subject", 
			"creator", 
			"rights",
			"description",
			
			// http://ns.adobe.com/xap/1.0/
			"title",
			"UsageTerms",

			// http://ns.adobe.com/exif/1.0
			"UserComment",
			"ISOSpeedRatings",
			"SubjectArea",
			"ComponentsConfiguration",
			"ExifCFAPattern",
			"Flash",
			
			// http://ns.adobe.com/tiff/1.0	
			"ReferenceBlackWhite",
			
			// http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/
			"Scene",
			"CreatorContactInfo",
			"SubjectCode",
			
			// http://ns.iview-multimedia.com/mediapro/1.0
			"People",
			
			"BitsPerSample",
			"ToneCurve"
		};
						
		static string [] xmp_tags = {
			// http://ns.adobe.com/photoshop/1.0
			"City" ,
			"State" ,
			"Country" ,
			"Caption" ,
			"CaptionWriter" ,
			"Credit" ,
			"Category" ,
			"Source" ,
			"History" ,
			"Headline",
			"Urgency",
			"ICCProfile",
			"ColorMode",
			"Instructions",
			"TransmissionReference",
			"AuthorsPosition",
			"DateCreated",
			
			// http://ns.adobe.com/tiff/1.0
			"Make" ,
			"Model" ,
			"Orientation" ,
			"ResolutionUnit" ,
			"YCbCrPositioning" ,				
			"XResolution" ,				
			"YResolution" ,				
			"ImageWidth" ,				
			"ImageLength" ,	
			"NativeDigest",
			
			// http://ns.adobe.com/exif/1.0	
			"exif_description",
			"ExposureTime" ,
			"FNumber" ,
			"ShutterSpeedValue" ,
			"ApertureValue" ,
			"ExposureBiasValue" ,
			"MaxApertureValue" ,
			"MeteringMode" ,
			"FocalLength" ,
			"ColorSpace" ,
			"PixelXDimension" ,
			"PixelYDimension" ,
			"ExposureMode" ,
			"WhiteBalance" ,
			"DigitalZoomRatio" ,
			"LightSource" ,
			"ExifVersion" ,
			"DateTimeOriginal" ,
			"DateTimeDigitized" ,
			"CompressedBitsPerPixel" ,
			"FlashPixVersion" ,
			"FocalPlaneXResolution" ,
			"FocalPlaneYResolution" ,
			"FocalPlaneResolutionUnit",
			"SensingMethod" ,
			"FileSource" ,
			"CustomRendered" ,
			"SceneCaptureType" ,
			"MetadataDate" ,
			"SubjectDistance" ,
			"BrightnessValue",
			"SceneType" ,
			"ExposureProgram",
			"FocalLengthIn35mmFilm" ,
			"GainControl" ,
			"Contrast" ,
			"Saturation" ,
			"RelatedSoundFile" ,
			"Sharpness" ,
			"NativeDigest",
			"FlashpixVersion",
			
			// http://ns.iview-multimedia.com/mediapro/1.0/
			// Iptc4xmpCore:Location
			"Location" ,
			"Status",
			"Event",
			
			// http://iptc.org/std/Iptc4xmpCore/1.0/xmlns
			"CountryCode",
			"IntellectualGenre",

			// http://ns.adobe.com/xap/1.0									
			"Rating" ,
			"ModifyDate" ,
			"CreateDate" ,
			"CreatorTool" ,
			"DocumentID" ,
			"InstanceID", 
			"WebStatement",

			// http://ns.adobe.com/lightroom/1.0
			"hierarchicalKeywords" ,

			// http://purl.org/dc/elements/1.1
			"format"
		};


		public XmpTagsMetadata (string photo_path, string import_path)
		{
			this.xmptags_photofile = photo_path;
			this.xmptags_sidecar_exist = false;
			this.xmptags_found_xmp_tags = false;

			// Check if we can find a sidecar file.
			string tmpfile = String.Format ("{0}{1}{2}.xmp",
						Path.GetDirectoryName (photo_path),
						Path.DirectorySeparatorChar,
						Path.GetFileName (photo_path));

			if (File.Exists (tmpfile))
				this.xmptags_sidecar_exist = true;
			else {
				// FIXME, When F-Spot copies the sidecar file with the photo, we do not need the extra path.
				// FIXME bug #342892 http://bugzilla.gnome.org/show_bug.cgi?id=342892
				tmpfile = String.Format ("{0}{1}{2}.xmp",
							Path.GetDirectoryName (import_path),
							Path.DirectorySeparatorChar,
							Path.GetFileName (import_path));
				if (File.Exists (tmpfile))
					this.xmptags_sidecar_exist = true;
			}

			if (this.xmptags_sidecar_exist) {
				this.xmptags_sidecarfile = tmpfile;
				System.Console.WriteLine ("XMP Sidecar file found at {0}", this.xmptags_sidecarfile);
			}
			
			// Initialize the xmp tags hash tables.
			xmp_table = new System.Collections.Hashtable ();
			foreach (string t1 in xmp_tags) 
				xmp_table [t1] = null;
			xmp_bag_table = new System.Collections.Hashtable ();
			foreach (string b1 in xmp_bag_tags) 
				xmp_bag_table [b1] = null;
		}
		
		private string TrimEmptyChars (string txt)
		{
			// This small function will ensure there are no null characters in a string.

			string tmp;

			// Return if already empty
			if ( (txt == null) || (txt.Length == 0) )
				return null;

			// Clean normal blanks
			tmp = txt.Trim();
			if ( (tmp == null) || (tmp.Length == 0) )
				return null;

			// Clean char 0
			char[] emptychar = new char[1];
			emptychar[0] = (char) 0;
			tmp = tmp.Trim (emptychar);
			if ( (tmp == null) || (tmp.Length == 0) )
				return null;

			// clean normal blanks again
			tmp = tmp.Trim();

			if ( (tmp == null) || (tmp.Length == 0) )
				return null;
			return tmp;
		}

		private void UpdateXMPBagTable (string predicate, string full_path)
		{
			// Return directly if nothing to add.
			if (items_list.Count == 0)
				return;
				
			try {
				// Check if we already have some values, if so, add them to the items_list so we do not loose them
				if (xmp_bag_table [ predicate ] != null) {
					if (xmp_bag_table [ predicate ] is string [])
						foreach (string keyword in (xmp_bag_table [ predicate ] as string [])) {
							// Add keyword if it does not exist already.
							if ( !items_list.Contains (keyword)) {
								items_list.Add(keyword); // add keyword to the list of keywords
							} 
					} else if (xmp_bag_table [ predicate ] is string) {
						// Add keyword if it does not already exist
						if (!items_list.Contains (xmp_bag_table [ predicate ] as string)) {
							items_list.Add(xmp_bag_table [ predicate ] as string); // add keyword to the list of keywords
						}
					}
				}

				// Ok, lets add this bag to its proper place in the hash table
				bool found = false;
				foreach (string str in xmp_bag_tags)
					if (str == predicate) { // found the proper entry
						if (items_list[0] is string) {
							if (items_list.Count == 1) {
								xmp_bag_table [ predicate ] = items_list[0].ToString();
							
								// Remove weird characters.
								xmp_bag_table [ predicate ] = TrimEmptyChars( xmp_bag_table [ predicate ] as string);
								
							} else if (items_list.Count > 1) {
								xmp_bag_table [ predicate ] = new string [items_list.Count];
								items_list.CopyTo ( xmp_bag_table [ predicate ] as string [] );
							}
						} else if (items_list[0] is Bag_Couple) {
							if (items_list.Count == 1) {
								xmp_bag_table [ predicate ] = items_list[0];								
							} else if (items_list.Count > 1) {
								xmp_bag_table [ predicate ] = new Bag_Couple [items_list.Count];
								items_list.CopyTo ( xmp_bag_table [ predicate ] as Bag_Couple [] );
							}

						}
						found = true;
						break;
					}
				if (!found)
					Console.WriteLine ("====> Not handled importing XMP bag predicate >>{0}<<::>{1}<, >>{2}<<", predicate, full_path);							
			
			} catch {
				Console.WriteLine ("====> Not handled importing XMP bag predicate >>{0}<<, >>{1}<<", predicate, full_path);			
			}
		}
	
		
		private void UpdateXMPTable (string predicate, string tag_value, string full_path)
		{
			try {
				bool found = false;
				foreach (string str in xmp_tags)
					if (str == predicate) { // found the proper entry
						xmp_table [ predicate ] = tag_value;

						// Remove some weird empty charachters
						xmp_table [ predicate ] = TrimEmptyChars( xmp_table [ predicate ] as string);
						found = true;
						break;
					}
				if (!found)
					Console.WriteLine ("====> Not handled importing XMP row predicate >>{0}<<::>{1}<, >>{2}<<", predicate, tag_value, full_path);							
			} catch {
				Console.WriteLine ("====> Not handled importing XMP row predicate >>{0}<<::>{1}<, >>{2}<<", predicate, tag_value, full_path);			
			}
		}
		
		public string GetXmpTag (string predicate)
		{
			try {
				return xmp_table [ predicate ] as string;
			
			} catch {
				Console.WriteLine ("====> Not handled importing XMP row predicate >>{0}<<", predicate);			
			}	
			return null;
		}

		public object GetXmpBag (string predicate)
		{
			try {
				return xmp_bag_table [ predicate ];
			} catch {
				Console.WriteLine ("====> Not handled importing XMP bag predicate >>{0}<<", predicate);			
			}	
			return null;
		}
		
		public string GetXmpBag_0 (string predicate)
		{
			// This function will return a bag as a string. 
			// Either the bag is a string, or first entry in a string array
			if (GetXmpBag (predicate) is string)
				return GetXmpBag (predicate) as string;
			else if (GetXmpBag (predicate) is string []) {
				string [] tmp = GetXmpBag (predicate) as string [];
				if ( (tmp != null) && (tmp.Length > 0) )
					return tmp[0].ToString();
			}
			return null;
		}

		private void GetBagData (MemoryStore substore, string CollectionPredicate)
		{
			// This function will collect all variables that are in a bag/collection.
			// This is typically keywords, but also some other fields.
			// This function will store the bag's variable into items_list, and if there are more items
			// in the bag/collection, call itself recursively with the next item.
			string type = null;

			foreach (Statement stmt in substore) {
				if (stmt.Predicate.Uri == MetadataStore.Namespaces.Resolve ("rdf:type")) {
					string prefix; // not used
					MetadataStore.Namespaces.Normalize (stmt.Object.ToString (), out prefix, out type);
				}
			}

			foreach (Statement sub in substore) {
				if (sub.Object is Literal) {
					string predicate = sub.Predicate.Uri;
					string title = System.IO.Path.GetFileName (predicate);
					string value = ((Literal)(sub.Object)).Value;
					
					// Remember that the title will be localised in GetDescription call.
					Description.GetDescription (substore, sub, out title, out value);
					
					// Lets get the non-localized title again.
					title = System.IO.Path.GetFileName (predicate);
#if enable_debug
					Console.WriteLine ("8 type = >>{0}<<", type);
					Console.WriteLine ("9 title = >>{0}<<", title);
					Console.WriteLine ("10 value = >>{0}<<", value);
#endif

					if (type == null) {
						Bag_Couple bag_couple = new Bag_Couple (title, value);
						items_list.Add (bag_couple);
						// Nothing to collect
					} else {
						// Add this value to the collection (to be processed after we are 
						// finished with this main collection.
						items_list.Add(value);
					}
				} else {
					if (type == null) {
						MemoryStore substore2 = substore.Select (new Statement ((Entity)sub.Object, null, null, null)).Load ();
						if (substore.StatementCount > 0) {
							// Since there are more bags/statements to collect.
							// Lets call ourself recursively to collect next one.
							GetBagData (substore2, CollectionPredicate);
						}
					}
				} // else
			} // foreach
		}
		

		public bool EmptyTags()
		{
			return ( (this.xmptags_found_xmp_tags || this.xmptags_found_sidecar_tag) == false);
		}

		public bool AnySidecarTags()
		{
			return ( this.xmptags_found_sidecar_tag);
		}
		
		public void Read_Sidecar_tags()
		{
			bool tmp_bool = false;
			if (xmptags_sidecar_exist)
				tmp_bool = Read_tags ( true );
			this.xmptags_found_sidecar_tag = tmp_bool;
		}
		
		public void Read_Embedded_tags()
		{
			this.xmptags_found_xmp_tags = Read_tags ( false );
		}

		private bool Read_tags (bool read_sidecar_data)
		{
			XmpFile xmpfile = null;
			MetadataStore store = new MetadataStore ();		

			// Initialize variable
			items_list = new System.Collections.ArrayList();
			
			try {
				// First prepare the meta store, either from a sidecar of embedded 
				if (read_sidecar_data) {
#if enable_debug				
					Console.WriteLine ("Importing sidecar metadata from sidecar {0}", xmptags_sidecarfile);
#endif
					xmpfile = new XmpFile(System.IO.File.OpenRead(xmptags_sidecarfile));
					store = xmpfile.Store;
				} else {
#if enable_debug				
					Console.WriteLine ("Importing embedded metadata from {0}", xmptags_photofile);	
#endif
					ImageFile img = ImageFile.Create (xmptags_photofile);

					if (img is SemWeb.StatementSource) {
						SemWeb.StatementSource source = (SemWeb.StatementSource)img;
						source.Select (store);
					}

					// Get the EXIF description
					if (img is FSpot.JpegFile) {
						FSpot.JpegFile jimg = img as FSpot.JpegFile;
						UpdateXMPTable ("exif_description", jimg.Description, "from EXIF");
//						Console.WriteLine ("Set the description to JPEG = >>{0}<<" ,jimg.Description);	
					} else if (img is FSpot.Png.PngFile) {
						FSpot.Png.PngFile png = img as FSpot.Png.PngFile;
						UpdateXMPTable ("exif_description", png.Description, "from PNG");						
//						Console.WriteLine ("Set the description to PNG = >>{0}<<" ,png.Description);	
					}

				} // if xmptags_sidecar

				// If there are any XMP tags, go through them one by one...
				if (store.StatementCount > 0) {
					foreach (SemWeb.Statement stmt in store) {				

						// Skip anonymous subjects because they are
						// probably part of a collection
						if (stmt.Subject.Uri == null) 
							continue;

						string title;
						string predicate;
						string value;
						title = null;
						value = null;
						
						Description.GetDescription (store, stmt, out title, out value);

						// The title is translated to localised language in GetDescription, 
						// so we go back to original predicate
						predicate = System.IO.Path.GetFileName (stmt.Predicate.ToString ()); 


						bool thisIsCollection = (value == null);

#if enable_debug
						Console.WriteLine ("1 Subject = >>{0}<<" ,stmt.Subject);	
						Console.WriteLine ("2 Predicate = >>{0}<<", stmt.Predicate);
						Console.WriteLine ("3 Object = >>{0}<<", stmt.Object);						
						Console.WriteLine ("4 Meta = >>{0}<<", stmt.Meta);
						Console.WriteLine ("5 Uri = >>{0}<<", stmt.Subject.Uri);
						Console.WriteLine ("6 Title = >>{0}<<", title);
						Console.WriteLine ("7 Value = >>{0}<< --- Collection {1}", value == null ? "null" : value, value == null ? "YES" : "no" );
#endif

						string stmt_obj_str;
						if (stmt.Object is SemWeb.Literal)
							stmt_obj_str = ((SemWeb.Literal)(stmt.Object)).Value;
						else
							stmt_obj_str = stmt.Object.ToString();

						if (thisIsCollection) {
							MemoryStore substore = store.Select (new SemWeb.Statement ((Entity)stmt.Object, null, null, null)).Load ();
							
							// Since start tag was found for this list, we clear it so we have a know starting state.
							items_list.Clear();
							
							// Collect all the values stored in this collection/bag.
							// It will put the result in the items_list array
							GetBagData (substore, predicate);
							
							// Convert the items_list (from the collection/bag's values) to specific variables/keywords
							UpdateXMPBagTable (predicate, stmt.Predicate.ToString());
						} else {
							// This XMP title just contains one value, and not a collection/bag. 
							UpdateXMPTable (predicate, stmt_obj_str, stmt.Predicate.ToString());
						}
					} // foreach
				} else 
					return false; // No XMP tags found
				

			} catch (Exception e) {
				Console.WriteLine ("Exception: {0}", e.ToString ());
				return false;
			}
		
#if enable_debug		
			foreach (string tag in xmp_tags)
				if (xmp_table [ tag ] != null)
					Console.WriteLine ("{0} --> >>{1}<<", tag, xmp_table [ tag ]);
			foreach (string tag in xmp_bag_tags)
				if (xmp_bag_table [ tag ] != null) {
					Console.Write ("{0} --> ", tag );
					if (xmp_bag_table [ tag ] is string)
						Console.Write (">>{0}<<", xmp_bag_table [ tag ] as string);
					else if (xmp_bag_table [ tag ] is string [])
						foreach (string keyword in (xmp_bag_table [ tag ] as string []))
							Console.Write (">>{0}<<, ", keyword );
					else if (xmp_bag_table [ tag ] is Bag_Couple ) {
						Bag_Couple couple = xmp_bag_table [ tag ] as Bag_Couple;
						Console.Write (">>{0}::{1}<< ", couple.Title, couple.Value );
					} else if (xmp_bag_table [ tag ] is Bag_Couple [])
						foreach (Bag_Couple couple in (xmp_bag_table [ tag ] as Bag_Couple []))
							Console.Write (">>{0}::{1}<< ", couple.Title, couple.Value );


					else Console.Write ("XXXXX=====XXXXX=====XXXXX Not string, nor string[]");
					Console.WriteLine("");
				}
#endif
//			Console.WriteLine ("Done importing xmp metadata");
			return true;
		}
	}

} // namespace
