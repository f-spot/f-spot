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


	// Version option menu.

	const string VERSION_ID_KEY = "f-spot:version_option_menu_id";
	private OptionMenu version_option_menu;

	private void HandleVersionOptionMenuActivated (object sender, EventArgs args)
	{
		Console.WriteLine ("wooho version {0}", (int) (sender as GLib.Object).Data [VERSION_ID_KEY]);
	}

	private void PopulateVersionOptionMenu ()
	{
		Menu menu = new Menu ();
		uint [] version_ids = Photo.VersionIds;

		foreach (uint id in version_ids) {
			MenuItem menu_item = new MenuItem (Photo.GetVersionName (id));
			menu_item.Show ();
			menu_item.Data.Add (VERSION_ID_KEY, id);
			menu_item.Activated += new EventHandler (HandleVersionOptionMenuActivated);
			menu.Append (menu_item);
		}

		MenuItem separator = new MenuItem ();
		separator.Show ();
		menu.Append (separator);

		MenuItem last_item = new MenuItem ("(No changes)");
		last_item.Sensitive = false;
		last_item.Show ();
		menu.Append (last_item);

		version_option_menu.Menu = menu;
		version_option_menu.Sensitive = true;
	}


	// Labels.

	private Entry name_entry;
	private Label date_label;
	private Label size_label;
	private Label exposure_info_label;

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

		table.Attach (CreateRightAlignedLabel ("Name:"), 0, 1, 0, 1,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Date:"), 0, 1, 1, 2,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Size:"), 0, 1, 2, 3,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Exposure:"), 0, 1, 3, 4,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Version:"), 0, 1, 4, 5,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);

		name_entry = new Entry ();
		name_entry.WidthChars = 1;
		table.Attach (name_entry, 1, 2, 0, 1,
			      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill,
			      3, 0);

		date_label = AttachLabel (table, 1, name_entry);
		size_label = AttachLabel (table, 2, name_entry);
		exposure_info_label = AttachLabel (table, 3, name_entry);

		version_option_menu = new OptionMenu ();
		table.Attach (version_option_menu, 1, 2, 4, 5, AttachOptions.Fill, AttachOptions.Fill, 3, 3);

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
			info += "ISO " + iso_speed;

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
			exposure_info_label.Text = "(None)";

		int width = 0, height = 0;
		try {
			JpegUtils.GetSize (photo.Path, out width, out height);
		} catch {
		}

		if (width == 0 || height == 0)
			size_label.Text = "(Unknown)";
		else
			size_label.Text = String.Format ("{0}x{1}", width, height);

		if (exposure_info.DateTime != null && exposure_info.DateTime != "")
			date_label.Text = exposure_info.DateTime;
		else
			date_label.Text = "(Unknown)";

		PopulateVersionOptionMenu ();
	}


	// Constructor.

	public InfoBox () : base (false, 0)
	{
		SetupWidgets ();
		Update ();

		BorderWidth = 6;
	}

}
