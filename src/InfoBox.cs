using Gtk;
using System;
using System.IO;

// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

public class InfoBox : VBox {

	private Photo photo;
	public Photo Photo {
		set {
			photo = value;
			Update ();
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

	private static string ExposureInfoText (string aperture,
						string exposure,
						string iso_speed)
	{
		string info = "";

		if (aperture != null && aperture != "")
			info += aperture + " ";
		if (exposure != null && exposure != "")
			info += exposure + " ";
		if (iso_speed != null && iso_speed != "")
			info += "\nISO " + iso_speed;

		return info;
	}

	public void Update ()
	{
		if (photo == null) {
			Clear ();
			return;
		}

		name_entry.Text = System.IO.Path.GetFileName (photo.Path);

		ExifUtils.ExposureInfo exposure_info;
		try {
			exposure_info = ExifUtils.GetExposureInfo (photo.Path);
		} catch {
			exposure_info = new ExifUtils.ExposureInfo ();
		}

		name_entry.Sensitive = true;

		string text = ExposureInfoText (exposure_info.ApertureValue,
						exposure_info.ExposureTime,
						exposure_info.IsoSpeed);

		if (text != "")
			exposure_info_label.Text = text;
		else
			exposure_info_label.Text = Mono.Posix.Catalog.GetString ("(None)");

		int width = 0, height = 0;
		try {
			JpegUtils.GetSize (photo.DefaultVersionPath, out width, out height);
		} catch {
		}

		if (width == 0 || height == 0)
			size_label.Text = "(Unknown)";
		else
			size_label.Text = String.Format ("{0}x{1}", width, height);

		if (exposure_info.DateTime != null && exposure_info.DateTime != "") {
			date_label.Text = photo.Time.ToShortDateString () + "\n" + photo.Time.ToShortTimeString ();
		} else
			date_label.Text = "(Unknown)";

		PhotoVersionMenu menu = new PhotoVersionMenu (photo);
		menu.VersionIdChanged += new PhotoVersionMenu.VersionIdChangedHandler (HandleVersionIdChanged);
		version_option_menu.Menu = menu;

		uint i = 0;
		foreach (uint version_id in photo.VersionIds) {
			if (version_id == photo.DefaultVersionId) {
				// FIXME GTK# why not just .History = i ?
				version_option_menu.SetHistory (i);
				break;
			}
			i ++;
		}

		version_option_menu.Sensitive = true;
	}


	// Constructor.

	public InfoBox () : base (false, 0)
	{
		SetupWidgets ();
		Update ();

		BorderWidth = 6;
	}

}
