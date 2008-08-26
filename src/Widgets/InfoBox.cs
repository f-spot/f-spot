/*
 * FSpot.Widgets.InfoBox
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing  <lewing@novell.com>
 * 	Gabriel Burt
 *	Stephane Delcroix  <stephane@delcroix.org>
 *	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */


using Gtk;
using System;
using System.IO;
using FSpot;
using SemWeb;
using Mono.Unix;
using FSpot.Utils;

// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

namespace FSpot.Widgets
{
	public class InfoBox : VBox {
		Delay update_delay;
	
		private Photo [] photos = new Photo[0];
		public Photo [] Photos {
			set {
				photos = value;
				update_delay.Start ();
			}
			private get {
				return photos;
			}
		}

		public Photo Photo {
			set {
				if (value != null) {
					Photos = new Photo[] { value };
				}
			}
		}
	
		private bool show_tags = false;
		public bool ShowTags {
			get { return show_tags; }
			set {
				if (show_tags == value)
					return;

				show_tags = value;
				tag_view.Visible = show_tags;
			}
		}

		public delegate void VersionIdChangedHandler (InfoBox info_box, uint version_id);
		public event VersionIdChangedHandler VersionIdChanged;
	
		private Expander histogram_expander;

		private Gtk.Image histogram_image;
		private Histogram histogram;

		private Delay histogram_delay;

	
		// Widgetry.	
		private Label name_label;
		private Label name_value_label;

		private Label version_label;
		private OptionMenu version_option_menu;

		private Label date_label;
		private Label date_value_label;

		private Label size_label;
		private Label size_value_label;

		private Label exposure_label;
		private Label exposure_value_label;

		private TagView tag_view;
		private string default_exposure_string;

		private void HandleVersionIdChanged (PhotoVersionMenu menu)
		{
			if (VersionIdChanged != null)
				VersionIdChanged (this, menu.VersionId);
		}
	
		private Label CreateRightAlignedLabel (string text)
		{
			Label label = new Label ();
			label.UseMarkup = true;
			label.Markup = text;
			label.Xalign = 1;
	
			return label;
		}
	
		const int TABLE_XPADDING = 3;
		const int TABLE_YPADDING = 3;
		static private Label AttachLabel (Table table, int row_num, Widget entry)
		{
			Label label = new Label (String.Empty);
			label.Xalign = 0;
			label.Selectable = true;
			label.Ellipsize = Pango.EllipsizeMode.End;
			label.Show ();
	
			table.Attach (label, 1, 2, (uint) row_num, (uint) row_num + 1,
				      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill,
				      (uint) entry.Style.XThickness + TABLE_XPADDING, (uint) entry.Style.YThickness);
	
			return label;
		}
	
		private void SetupWidgets ()
		{

			histogram_expander = new Expander (Catalog.GetString ("Histogram"));
			histogram_expander.Expanded = Preferences.Get<bool> (Preferences.INFOBOX_HISTOGRAM_VISIBLE);
			histogram_expander.Activated += delegate (object sender, EventArgs e) { 
				Preferences.Set (Preferences.INFOBOX_HISTOGRAM_VISIBLE, histogram_expander.Expanded);
				UpdateHistogram ();
			};
			histogram_image = new Gtk.Image ();
			histogram = new Histogram ();
			histogram_expander.Add (histogram_image);

			Window window = MainWindow.Toplevel.Window;
			Gdk.Color c = window.Style.Backgrounds [(int)Gtk.StateType.Active];
			histogram.RedColorHint = (byte) (c.Red / 0xff);
			histogram.GreenColorHint = (byte) (c.Green / 0xff);
			histogram.BlueColorHint = (byte) (c.Blue / 0xff);
			histogram.BackgroundColorHint = 0xff;

			Add (histogram_expander);

			Expander info_expander = new Expander (Catalog.GetString ("Image Information"));
			info_expander.Expanded = Preferences.Get<bool> (Preferences.INFOBOX_INFO_VISIBLE);
			info_expander.Activated += delegate (object sender, EventArgs e) {
				Preferences.Set (Preferences.INFOBOX_INFO_VISIBLE, info_expander.Expanded);
			};

			Table info_table = new Table (6, 2, false);
			info_table.BorderWidth = 0;
	
			string name_pre = "<b>";
			string name_post = "</b>";

			name_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Name") + name_post);
			info_table.Attach (name_label, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			version_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Version") + name_post); 
			info_table.Attach (version_label, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			date_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Date") + name_post + Environment.NewLine);
			info_table.Attach (date_label, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			size_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Size") + name_post);
			info_table.Attach (size_label, 0, 1, 3, 4, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			default_exposure_string = name_pre + Catalog.GetString ("Exposure") + name_post;
			exposure_label = CreateRightAlignedLabel (default_exposure_string);
			info_table.Attach (exposure_label, 0, 1, 4, 5, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);
	
			name_value_label = new Label ();
			name_value_label.Ellipsize = Pango.EllipsizeMode.Middle;
			name_value_label.Justify = Gtk.Justification.Left;
			name_value_label.Selectable = true;
			name_value_label.Xalign = 0;
			info_table.Attach (name_value_label, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 3, 0);
			
			date_value_label = AttachLabel (info_table, 2, name_value_label);
			size_value_label = AttachLabel (info_table, 3, name_value_label);
			exposure_value_label = AttachLabel (info_table, 4, name_value_label);
	
			version_option_menu = new OptionMenu ();
			info_table.Attach (version_option_menu, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);
	
			date_value_label.Text = Environment.NewLine;
			exposure_value_label.Text = Environment.NewLine;
	
			tag_view = new TagView (MainWindow.ToolTips);
			info_table.Attach (tag_view, 0, 2, 5, 6, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);
			tag_view.Show ();
			info_table.ShowAll ();
	
			info_expander.Add (info_table);
			Add (info_expander);
		}
	
		private class ImageInfo : StatementSink {
			string width;
			string height;
			string aperture;
			string fnumber;
			string exposure;
			string iso_speed;
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
	
				if (width == null || height == null || exposure == null || aperture == null || iso_speed == null)
					return true;
				else
					return false;
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
			
	
		public bool Update ()
		{
			if (Photos == null || Photos.Length == 0) {
				Hide ();
			} else if (Photos.Length == 1) {
				UpdateSingle ();
			} else if (Photos.Length > 1) {
				UpdateMultiple ();
			}
			return false;
		}	
	
		private void UpdateSingle () 
		{
			ImageInfo info;

			Photo photo = Photos[0];

			histogram_expander.Visible = true;
			UpdateHistogram ();

			name_label.Visible = true;
			name_value_label.Text = photo.Name != null ? System.Uri.UnescapeDataString(photo.Name) : String.Empty;
			try {
				//using (new Timer ("building info")) {
					using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri))
					{
						info = new ImageInfo (img);
					}
					//}
			} catch (System.Exception e) {
				Log.Debug (e.StackTrace);
				info = new ImageInfo (null);			
			}

			exposure_value_label.Text = info.ExposureInfo;
			if (exposure_value_label.Text.IndexOf (Environment.NewLine) != -1)
				exposure_label.Markup = default_exposure_string + Environment.NewLine;
			else
				exposure_label.Markup = default_exposure_string;
			exposure_label.Visible = true;
			exposure_value_label.Visible = true;
	
			size_value_label.Text = info.Dimensions;
			size_label.Visible = true;
			size_value_label.Visible = true;

	#if USE_EXIF_DATE
			date_value_label.Text = info.Date;
	#else
			DateTime local_time = photo.Time.ToLocalTime ();
			date_value_label.Text = String.Format ("{0}{2}{1}",
				local_time.ToShortDateString (),
				local_time.ToShortTimeString (),
				Environment.NewLine
			);
	#endif
			
	
			version_label.Visible = true;
			version_option_menu.Visible = true;
			PhotoVersionMenu menu = new PhotoVersionMenu (photo);
			menu.VersionIdChanged += new PhotoVersionMenu.VersionIdChangedHandler (HandleVersionIdChanged);
			menu.WidthRequest = version_option_menu.Allocation.Width;
			version_option_menu.Menu = menu;
			
			uint i = 0;
			foreach (uint version_id in photo.VersionIds) {
				if (version_id == photo.DefaultVersionId) {
					// FIXME GTK# why not just .History = i ?
					version_option_menu.SetHistory (i);
					break;
				}
				i++;
			}
			if (show_tags)
				tag_view.Current = photo;
	
            Show ();
		}

		private void UpdateMultiple ()
		{
			histogram_expander.Visible = false;

			name_label.Visible = false;
			name_value_label.Text = String.Format(Catalog.GetString("{0} Photos"), Photos.Length);

			version_label.Visible = false;
			version_option_menu.Visible = false;

			exposure_label.Visible = false;
			exposure_value_label.Visible = false;

			Photo first = Photos[Photos.Length-1];
			Photo last = Photos[0];
			if (first.Time.Date == last.Time.Date) {
				//Note for translators: {0} is a date, {1} and {2} are times.
				date_value_label.Text = String.Format(Catalog.GetString("On {0} between \n{1} and {2}"), 
						first.Time.ToLocalTime ().ToShortDateString (),
						first.Time.ToLocalTime ().ToShortTimeString (),
						last.Time.ToLocalTime ().ToShortTimeString ());
			} else {
				date_value_label.Text = String.Format(Catalog.GetString("Between {0} \nand {1}"),
						first.Time.ToLocalTime ().ToShortDateString (),
						last.Time.ToLocalTime ().ToShortDateString ());
			}

			size_label.Visible = false;
			size_value_label.Visible = false;
		}

		private Gdk.Pixbuf histogram_hint;

		private void UpdateHistogram ()
		{
			if (histogram_expander.Expanded)
				histogram_delay.Start ();
		}

		public void UpdateHistogram (Gdk.Pixbuf pixbuf) {
			histogram_hint = pixbuf;
			UpdateHistogram ();
		}

		private bool DelayedUpdateHistogram () {
			if (Photos.Length == 0)
				return false;

			Photo photo = Photos[0];

			Gdk.Pixbuf hint = histogram_hint;
			histogram_hint = null;

			try {
				if (hint == null)
					using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri))
						hint = img.Load (256, 256);

				int max = histogram_expander.Allocation.Width;
				histogram_image.Pixbuf = histogram.Generate (hint, max);

				hint.Dispose ();
			} catch (System.Exception e) {
				Log.Debug (e.StackTrace);
			}

			return false;
		}
	
	
		// Constructor.
	
		public InfoBox () : base (false, 0)
		{
			SetupWidgets ();
			update_delay = new Delay (Update);
			update_delay.Start ();

			histogram_delay = new Delay (DelayedUpdateHistogram);
	
			BorderWidth = 2;
            Hide ();
		}
	}
}
