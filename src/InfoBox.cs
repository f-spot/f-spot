using Gtk;
using System;
using System.IO;

public class InfoBox : VBox {

	private string photo_path;
	public string PhotoPath {
		set {
			photo_path = value;
			Update ();
		}

		get {
			return photo_path;
		}
	}


	// Widgetry.

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
		alignment.SetSizeRequest (1, -1);

		table.Attach (alignment, 1, 2, (uint) row_num, (uint) row_num + 1,
			      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill,
			      (uint) entry.Style.XThickness + 3, (uint) entry.Style.YThickness);

		return label;
	}

	private void SetupWidgets ()
	{
		Table table = new Table (4, 2, false);

		table.Attach (CreateRightAlignedLabel ("Name:"), 0, 1, 0, 1,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Date:"), 0, 1, 1, 2,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Size:"), 0, 1, 2, 3,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);
		table.Attach (CreateRightAlignedLabel ("Exposure:"), 0, 1, 3, 4,
			      AttachOptions.Fill, AttachOptions.Fill, 3, 3);

		name_entry = new Entry ();
		name_entry.SetSizeRequest (1, -1);
		table.Attach (name_entry, 1, 2, 0, 1,
			      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill,
			      3, 0);

		date_label = AttachLabel (table, 1, name_entry);
		size_label = AttachLabel (table, 2, name_entry);
		exposure_info_label = AttachLabel (table, 3, name_entry);

		table.SetSizeRequest (1, -1);
		table.ShowAll ();

		Add (table);
	}

	private void Clear ()
	{
		name_entry.Sensitive = false;
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

	private void Update ()
	{
		if (photo_path == null) {
			Clear ();
			return;
		}

		name_entry.Text = System.IO.Path.GetFileName (photo_path);

		ExifUtils.ExposureInfo exposure_info;
		try {
			exposure_info = ExifUtils.GetExposureInfo (photo_path);
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
			JpegUtils.GetSize (photo_path, out width, out height);
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
	}


	// Constructor.

	public InfoBox () : base (false, 0)
	{
		SetupWidgets ();
		Update ();

		BorderWidth = 6;
	}

}
