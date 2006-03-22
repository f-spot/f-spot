using Gtk;
using System;
using System.IO;
using FSpot;
using SemWeb;

// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

public class InfoBox : VBox {
	Delay update_delay;
	private Photo photo;
	public Photo Photo {
		set {
			photo = value;
			update_delay.Start ();
		}

		get {
			return photo;
		}
	}

	public delegate void VersionIdChangedHandler (InfoBox info_box, uint version_id);
	public event VersionIdChangedHandler VersionIdChanged;


	// Widgetry.

	private Entry name_entry;
	private Label date_label;
	private Label size_label;
	private Label exposure_info_label;
	private OptionMenu version_option_menu;

	private void HandleVersionIdChanged (PhotoVersionMenu menu)
	{
		if (VersionIdChanged != null)
			VersionIdChanged (this, menu.VersionId);
	}

	private Widget CreateRightAlignedLabel (string text)
	{
		Label label = new Label (text);

		Alignment alignment = new Alignment ((float) 1.0, (float) 0.5, (float) 0.0, (float) 0.0);
		alignment.Add (label);
		alignment.ShowAll ();

		return alignment;
	}

	static private Label AttachLabel (Table table, int row_num, Widget entry)
	{
		Label label = new Label ("");
		Alignment alignment = new Alignment ((float) 0.0, (float) 0.5, (float) 0.0, (float) 0.0);

		alignment.Add (label);
		alignment.ShowAll ();

		table.Attach (alignment, 1, 2, (uint) row_num, (uint) row_num + 1,
			      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill,
			      (uint) entry.Style.XThickness + 3, (uint) entry.Style.YThickness);

		return label;
	}

	private void SetupWidgets ()
	{
		Table table = new Table (5, 2, false);

		table.Attach (CreateRightAlignedLabel (Mono.Posix.Catalog.GetString ("Name:")), 0, 1, 0, 1,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel (Mono.Posix.Catalog.GetString ("Version:")), 0, 1, 1, 2,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel (Mono.Posix.Catalog.GetString ("Date:")), 0, 1, 2, 3,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel (Mono.Posix.Catalog.GetString ("Size:")), 0, 1, 3, 4,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel (Mono.Posix.Catalog.GetString ("Exposure:")), 0, 1, 4, 5,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);

		name_entry = new Entry ();
		name_entry.WidthChars = 1;
		name_entry.Editable = false;
		table.Attach (name_entry, 1, 2, 0, 1,
			      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill,
			      3, 0);

		date_label = AttachLabel (table, 2, name_entry);
		size_label = AttachLabel (table, 3, name_entry);
		exposure_info_label = AttachLabel (table, 4, name_entry);

		version_option_menu = new OptionMenu ();
		table.Attach (version_option_menu, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 3, 3);

		table.ShowAll ();

		Add (table);
	}

	private void Clear ()
	{
		name_entry.Sensitive = false;

		version_option_menu.Sensitive = false;
		version_option_menu.Menu = new Menu ();	// GTK doesn't like NULL here although that's what we want.

		name_entry.Text = "";
		date_label.Text = "";
		size_label.Text = "";
		exposure_info_label.Text = "";
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
					width = ((Literal)stmt.Object).Value;
				} else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("tiff:ImageLength")) {
				if (height == null)
					height = ((Literal)stmt.Object).Value;
			} else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:PixelXDimension"))
				width = ((Literal)stmt.Object).Value;						      
			else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:PixelYDimension"))
				height = ((Literal)stmt.Object).Value;
			else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ExposureTime"))
				exposure = ((Literal)stmt.Object).Value;
			else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ApertureValue"))
				aperture = ((Literal)stmt.Object).Value;
			else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:FNumber"))
				fnumber = ((Literal)stmt.Object).Value;
			else if (stmt.Predicate == MetadataStore.Namespaces.Resolve ("exif:ISOSpeedRatings"))
				iso_anon = stmt.Object;
			else if (stmt.Subject == iso_anon && stmt.Predicate == MetadataStore.Namespaces.Resolve ("rdf:li"))
				iso_speed = ((Literal)stmt.Object).Value;
			else if (add && stmt.Subject.Uri == null)
				store.Add (stmt);

			if (width == null || height == null || exposure == null || aperture == null || iso_speed == null)
				return true;
			else
				return false;
		}

		public string ExposureInfo {
			get {
				string info = "";

				if  (fnumber != null && fnumber != "") {
					FSpot.Tiff.Rational rat = new FSpot.Tiff.Rational (fnumber);
					info += String.Format ("f/{0:.0} ", rat.Value);
				} else if (aperture != null && aperture != "") {
					// Convert from APEX to fnumber
					FSpot.Tiff.Rational rat = new FSpot.Tiff.Rational (aperture);
					info += String.Format ("f/{0:.0} ", Math.Pow (2, rat.Value / 2));
				}

				if (exposure != null && exposure != "")
					info += exposure + " sec ";

				if (iso_speed != null && iso_speed != "")
					info += "\nISO " + iso_speed;
				
				if (info == "")
					return Mono.Posix.Catalog.GetString ("(None)");
				
				return info;
			}
		}

		public string Dimensions {
			get {
				if (width != null && height != null)
					return String.Format ("{0}x{1}", width, height);
				else 
					return Mono.Posix.Catalog.GetString ("(Unknown)");
			}
		}
#if USE_EXIF_DATE
		public string Date {
			get {
				if (date > DateTime.MinValue && date < DateTime.MaxValue)
					return date.ToShortDateString () + "\n" + date.ToShortTimeString ();
				else 
					return Mono.Posix.Catalog.GetString ("(Unknown)");
			}
		}
#endif
	}
		

	public bool Update ()
	{
		ImageFile img;
		ImageInfo info;

		if (photo == null) {
			Clear ();
			return false;
		}
		
		name_entry.Text = photo.Name;
		try {
			//using (new Timer ("building info")) {
				img = ImageFile.Create (photo.DefaultVersionUri);
				info = new ImageInfo (img);
				//}
		} catch (System.Exception e) {
			System.Console.WriteLine (e);
			info = new ImageInfo (null);			
		}


		name_entry.Sensitive = true;
		exposure_info_label.Text = info.ExposureInfo;
		size_label.Text = info.Dimensions;
#if USE_EXIF_DATE
		date_label.Text = info.Date;
#else
		date_label.Text = String.Format ("{0}\n{1}",
						 photo.Time.ToShortDateString (),
						 photo.Time.ToShortTimeString ());
#endif
		version_option_menu.Sensitive = true;
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

		return false;
	}


	// Constructor.

	public InfoBox () : base (false, 0)
	{
		SetupWidgets ();
		update_delay = new Delay (Update);
		update_delay.Start ();

		BorderWidth = 6;
	}

}
