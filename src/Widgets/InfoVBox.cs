/*
 * InfoVBox.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */
 
using Gtk;
using System;
using System.IO;
using SemWeb;
using Mono.Unix;

namespace FSpot.Widgets {
	public class InfoVBox : VBox {
		
		public delegate void VersionIdChangedHandler (InfoVBox info_box, uint version_id);
		public event VersionIdChangedHandler VersionIdChanged;		
		
		// Widgetry.	
		private Label name_label;
		private Label date_label;
		private Label size_label;
		private Label exposure_info_label;
		private Label focal_length_label;
		private Label camera_model_label;
		private Label file_size_label;
		
		private TreeView version_chooser;
		private OptionMenu version_option_menu;
		
		private void HandleVersionIdChanged (PhotoVersionMenu menu)
		{
			if (VersionIdChanged != null)
				VersionIdChanged (this, menu.VersionId);
		}
		
		private Widget CreateLeftAlignedLabel (string text)
		{
			Label label = new Label (text);
			label.Xalign = 0;

			return label;
		}

		static private Label AttachLabel (Table table, int row_num, Widget entry)
		{
			Label label = new Label (String.Empty);
			label.Xalign = 0;
			label.Selectable = true;
			label.Ellipsize = Pango.EllipsizeMode.End;
			label.Show ();

			table.Attach (label, 1, 2, (uint) row_num, (uint) row_num + 1,
				      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill,
				      (uint) entry.Style.XThickness + 3, (uint) entry.Style.YThickness);

			return label;
		}

		private void SetupWidgets ()
		{
			Spacing = 12;
			
			Table table = new Table (8, 2, false);

			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Name:")), 0, 1, 0, 1,
				      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Version:")), 0, 1, 1, 2,
				      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Date:")), 0, 1, 2, 3,
				      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Size:")), 0, 1, 3, 4,
				      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Exposure:")), 0, 1, 4, 5,
			          AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Focal Length:")), 0, 1, 5, 6,
			          AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("Camera:")), 0, 1, 6, 7,
			                AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			table.Attach (CreateLeftAlignedLabel (Catalog.GetString ("File Size:")), 0, 1, 7, 8,
			          AttachOptions.Fill, AttachOptions.Fill, 3, 3);
			
			name_label = new Label ();
			name_label.Ellipsize = Pango.EllipsizeMode.Middle;
			name_label.Justify = Gtk.Justification.Left;
			name_label.Selectable = true;
			name_label.Xalign = 0;
			table.Attach (name_label, 1, 2, 0, 1,
				      AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill,
				      3, 0);
			
			date_label = AttachLabel (table, 2, name_label);
			size_label = AttachLabel (table, 3, name_label);
			exposure_info_label = AttachLabel (table, 4, name_label);
			focal_length_label = AttachLabel (table, 5, name_label);
			camera_model_label = AttachLabel (table, 6, name_label);
			file_size_label = AttachLabel (table, 7, name_label);
			
			version_option_menu = new OptionMenu ();
			table.Attach (version_option_menu, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 3);

			date_label.Text = Environment.NewLine;
			exposure_info_label.Text = Environment.NewLine;
			
			PackStart (table, false, false, 3);

			ShowAll ();
		}
		
		private class ImageInfo : StatementSink {
			string width;
			string height;
			string aperture;
			string fnumber;
			string exposure;
			string iso_speed;
			string focal_length;
			string camera_model;
			bool add = true;
			Resource iso_anon;

			MemoryStore store;
			
	#if USE_EXIF_DATE
			DateTime date;
	#endif
			public ImageInfo (ImageFile img) 
			{
				// FIXME We use the memory store to hold the anonymous statements
				// as they are added so that we can query for them later to 
				// resolve anonymous nodes.
				store = new MemoryStore ();

				if (img == null) 
					return;

				if (img is StatementSource) {
					SemWeb.StatementSource source = (SemWeb.StatementSource)img;
					source.Select (this);

					// If we couldn't find the ISO speed because of the ordering
					// search the memory store for the values
					if (iso_speed == null && iso_anon != null) {
						add = false;
						store.Select (this);
					}
				}

				if (img is JpegFile) {
					int real_width;
					int real_height;

					JpegUtils.GetSize (img.Uri.LocalPath, out real_width, out real_height);
					width = real_width.ToString ();
					height = real_height.ToString ();
				}
	#if USE_EXIF_DATE
				date = img.Date.ToLocalTime ();
	#endif
			}

			public bool Add (SemWeb.Statement stmt)
			{
				if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("tiff:ImageWidth")) {
					if (width == null)
						width = ((SemWeb.Literal)stmt.Object).Value;
					} else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("tiff:ImageLength")) {
					if (height == null)
						height = ((SemWeb.Literal)stmt.Object).Value;
				} else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:PixelXDimension"))
					width = ((SemWeb.Literal)stmt.Object).Value;						      
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:PixelYDimension"))
					height = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:FocalLength"))
					focal_length = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("tiff:Model"))
					camera_model = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ExposureTime"))
					exposure = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ApertureValue"))
					aperture = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:FNumber"))
					fnumber = ((SemWeb.Literal)stmt.Object).Value;
				else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ISOSpeedRatings"))
					iso_anon = stmt.Object;
				else if (stmt.Subject == iso_anon && stmt.Predicate == MetadataStore.Namespaces.Resolve ("rdf:li"))
					iso_speed = ((SemWeb.Literal)stmt.Object).Value;
				else if (add && stmt.Subject.Uri == null)
					store.Add (stmt);
				
				
				
				if (width == null || height == null || focal_length == null
				        || camera_model == null || exposure == null || aperture == null
				        || fnumber == null || iso_speed == null)
					return true;
				else
					return false;
			}
			
			public string FocalLength {
				get {
					if (focal_length == null)
						return Catalog.GetString ("(Unknown)");
					
					string fl = focal_length;
					
					if (focal_length.Contains("/"))
					{
						string[] strings = focal_length.Split('/');
						try {
							if (strings.Length == 2)
								fl = (double.Parse (strings[0]) / double.Parse (strings[1])).ToString ();
						} catch (FormatException e) {
						}
					}
					
					return fl + " mm";
				}
			}
			
			public string CameraModel {
				get {
					if (focal_length != null)
						return camera_model;
					else
						return Catalog.GetString ("(Unknown)");
				}
			}

			public string ExposureInfo {
				get {
					string info = String.Empty;

					if  (fnumber != null && fnumber != String.Empty) {
						FSpot.Tiff.Rational rat = new FSpot.Tiff.Rational (fnumber);
						info += String.Format ("f/{0:.0} ", rat.Value);
					} else if (aperture != null && aperture != String.Empty) {
						// Convert from APEX to fnumber
						FSpot.Tiff.Rational rat = new FSpot.Tiff.Rational (aperture);
						info += String.Format ("f/{0:.0} ", Math.Pow (2, rat.Value / 2));
					}

					if (exposure != null && exposure != String.Empty)
						info += exposure + " sec ";

					if (iso_speed != null && iso_speed != String.Empty)
						info += Environment.NewLine + "ISO " + iso_speed;
					
					if (info == String.Empty)
						return Catalog.GetString ("(None)");
					
					return info;
				}
			}

			public string Dimensions {
				get {
					if (width != null && height != null)
						return String.Format ("{0}x{1}", width, height);
					else 
						return Catalog.GetString ("(Unknown)");
				}
			}
	#if USE_EXIF_DATE
			public string Date {
				get {
					if (date > DateTime.MinValue && date < DateTime.MaxValue)
						return date.ToShortDateString () + Environment.NewLine + date.ToShortTimeString ();
					else 
						return Catalog.GetString ("(Unknown)");
				}
			}
	#endif
		}
		
		public void Clear ()
		{
			name_label.Sensitive = false;

			version_option_menu.Sensitive = false;
			version_option_menu.Menu = new Menu ();	// GTK doesn't like NULL here although that's what we want.

			name_label.Text = String.Empty;
			date_label.Text = Environment.NewLine;
			size_label.Text = String.Empty;
			exposure_info_label.Text = Environment.NewLine;
			focal_length_label.Text = String.Empty;
			camera_model_label.Text = String.Empty;
			file_size_label.Text = String.Empty;
		}
		
		public void UpdateMultipleSelection (IBrowsableCollection collection)
		{
			name_label.Sensitive = false;

			version_option_menu.Sensitive = false;
			version_option_menu.Menu = new Menu ();	// GTK doesn't like NULL here although that's what we want.

			name_label.Text = String.Empty;
			date_label.Text = Environment.NewLine;
			size_label.Text = String.Empty;
			exposure_info_label.Text = Environment.NewLine;
			focal_length_label.Text = String.Empty;
			camera_model_label.Text = String.Empty;
			file_size_label.Text = String.Empty;
			
			if (collection != null && collection.Count > 1) {
				long size = 0;
				try {
					foreach (IBrowsableItem p in collection.Items) {
						Gnome.Vfs.FileInfo file_info = new Gnome.Vfs.FileInfo (p.DefaultVersionUri.ToString ());
						size += file_info.Size;
					}
					file_size_label.Text = Gnome.Vfs.Format.FileSizeForDisplay (size);
				} catch (System.IO.FileNotFoundException) {
					file_size_label.Text = Catalog.GetString("(One or more files not found)");
				}
			}
		}
		
		public void UpdateSingleSelection (IBrowsableItem photo)
		{
			ImageInfo info;
			
			if (photo == null) {
				Clear ();
				return;
			}
			
			name_label.Text = photo.Name != null ? photo.Name : String.Empty;
			try {
				//using (new Timer ("building info")) {
					using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri))
					{
						info = new ImageInfo (img);
					}
					//}
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				info = new ImageInfo (null);			
			}


			name_label.Sensitive = true;
			exposure_info_label.Text = info.ExposureInfo;
			size_label.Text = info.Dimensions;
			focal_length_label.Text = info.FocalLength;
			camera_model_label.Text = info.CameraModel;
			
	#if USE_EXIF_DATE
			date_label.Text = info.Date;
	#else
			date_label.Text = String.Format ("{0}{2}{1}",
							 photo.Time.ToLocalTime ().ToShortDateString (),
							 photo.Time.ToLocalTime ().ToShortTimeString (),
							 Environment.NewLine);
	#endif

			Photo p = photo as Photo;
			if (p != null) {
				version_option_menu.Visible = true;
				version_option_menu.Sensitive = true;
				PhotoVersionMenu menu = new PhotoVersionMenu (p);
				menu.VersionIdChanged += new PhotoVersionMenu.VersionIdChangedHandler (HandleVersionIdChanged);
				// FIXME: here is a problem with FullScreenView and the VersionMenu
				//menu.WidthRequest = version_option_menu.Allocation.Width;
				version_option_menu.Menu = menu;
				
				uint i = 0;
				foreach (uint version_id in p.VersionIds) {
					if (version_id == p.DefaultVersionId) {
						// FIXME GTK# why not just .History = i ?
						version_option_menu.SetHistory (i);
						break;
					}
					i++;
				}
				
				try {
					Gnome.Vfs.FileInfo file_info = new Gnome.Vfs.FileInfo (photo.DefaultVersionUri.ToString ());
					file_size_label.Text = Gnome.Vfs.Format.FileSizeForDisplay (file_info.Size);
				} catch (System.IO.FileNotFoundException) {
					file_size_label.Text = Catalog.GetString("(File not found)");
				}
	            	    
			} else {
				version_option_menu.Visible = false;
				version_option_menu.Sensitive = false;
				version_option_menu.Menu = null;
				file_size_label.Text = String.Empty;
			}
				
		}
		
		public InfoVBox ()
		{
			SetupWidgets ();
		}
	}
}
