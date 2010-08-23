/*
 * FSpot.Widgets.InfoBox
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing  <lewing@novell.com>
 * 	Gabriel Burt
 *	Stephane Delcroix  <stephane@delcroix.org>
 *	Ruben Vermeersch <ruben@savanne.be>
 *	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using Gtk;
using System;
using System.IO;
using FSpot.Core;
using FSpot.Imaging;
using Mono.Unix;
using FSpot.Utils;
using GLib;
using GFile = GLib.File;
using GFileInfo = GLib.FileInfo;
using Hyena;

// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

namespace FSpot.Widgets
{
	public class InfoBox : VBox {
		DelayedOperation update_delay;

		private IPhoto [] photos = new IPhoto [0];
		public IPhoto [] Photos {
			set {
				photos = value;
				update_delay.Start ();
			}
			private get {
				return photos;
			}
		}

		public IPhoto Photo {
			set {
				if (value != null) {
					Photos = new IPhoto [] { value };
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

		private bool show_rating = false;
		public bool ShowRating {
			get { return show_rating; }
			set {
				if (show_rating == value)
					return;

				show_rating = value;
				rating_label.Visible = show_rating;
				rating_view.Visible = show_rating;
			}
		}

		public delegate void VersionChangedHandler (InfoBox info_box, IPhotoVersion version);
		public event VersionChangedHandler VersionChanged;

		private Expander info_expander;
		private Expander histogram_expander;

		private Gtk.Image histogram_image;
		private Histogram histogram;

		private DelayedOperation histogram_delay;

		// Context switching (toggles visibility).
		public event EventHandler ContextChanged;

		private ViewContext view_context = ViewContext.Unknown;
		public ViewContext Context {
			get { return view_context; }
			set {
				view_context = value;
				if (ContextChanged != null)
					ContextChanged (this, null);
			}
		}

		private readonly InfoBoxContextSwitchStrategy ContextSwitchStrategy;

		// Widgetry.
		private Label name_label;
		private Label name_value_label;

		private Label version_label;
		private ListStore version_list;
		private ComboBox version_combo;

		private Label date_label;
		private Label date_value_label;

		private Label size_label;
		private Label size_value_label;

		private Label exposure_label;
		private Label exposure_value_label;

		private Label focal_length_label;
		private Label focal_length_value_label;

		private Label camera_label;
		private Label camera_value_label;

		private Label file_size_label;
		private Label file_size_value_label;

		private Label rating_label;
		private RatingEntry rating_view;

		private TagView tag_view;
		private string default_exposure_string;

		private bool show_name;
		private bool show_date;
		private bool show_size;
		private bool show_exposure;
		private bool show_focal_length;
		private bool show_camera;
		private bool show_file_size;

		private void HandleRatingChanged (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRatingMenuSelected ((o as Widgets.RatingEntry).Value);
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
		private Label AttachLabel (Table table, int row_num, Widget entry)
		{
			Label label = new Label (String.Empty);
			label.Xalign = 0;
			label.Selectable = true;
			label.Ellipsize = Pango.EllipsizeMode.End;
			label.Show ();

			label.PopulatePopup += HandlePopulatePopup;

			table.Attach (label, 1, 2, (uint) row_num, (uint) row_num + 1,
				      AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill,
				      (uint) entry.Style.XThickness + TABLE_XPADDING, (uint) entry.Style.YThickness);

			return label;
		}

		private void SetupWidgets ()
		{

			histogram_expander = new Expander (Catalog.GetString ("Histogram"));
			histogram_expander.Activated += delegate (object sender, EventArgs e) {
				ContextSwitchStrategy.SetHistogramVisible (Context, histogram_expander.Expanded);
				UpdateHistogram ();
			};
			histogram_expander.StyleSet += delegate (object sender, StyleSetArgs args) {
				Gdk.Color c = this.Toplevel.Style.Backgrounds [(int)Gtk.StateType.Active];
				histogram.RedColorHint = (byte) (c.Red / 0xff);
				histogram.GreenColorHint = (byte) (c.Green / 0xff);
				histogram.BlueColorHint = (byte) (c.Blue / 0xff);
				histogram.BackgroundColorHint = 0xff;
				UpdateHistogram ();
			};
			histogram_image = new Gtk.Image ();
			histogram = new Histogram ();
			histogram_expander.Add (histogram_image);

			Add (histogram_expander);

			info_expander = new Expander (Catalog.GetString ("Image Information"));
			info_expander.Activated += delegate (object sender, EventArgs e) {
				ContextSwitchStrategy.SetInfoBoxVisible (Context, info_expander.Expanded);
			};

			Table info_table = new Table (10, 2, false);
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

			focal_length_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Focal Length") + name_post);
			info_table.Attach (focal_length_label, 0, 1, 5, 6, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			camera_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Camera") + name_post);
			info_table.Attach (camera_label, 0, 1, 6, 7, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			file_size_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("File Size") + name_post);
			info_table.Attach (file_size_label, 0, 1, 7, 8, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			rating_label = CreateRightAlignedLabel (name_pre + Catalog.GetString ("Rating") + name_post);
			info_table.Attach (rating_label, 0, 1, 8, 9, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);
			rating_label.Visible = false;

			name_value_label = new Label ();
			name_value_label.Ellipsize = Pango.EllipsizeMode.Middle;
			name_value_label.Justify = Gtk.Justification.Left;
			name_value_label.Selectable = true;
			name_value_label.Xalign = 0;
			name_value_label.PopulatePopup += HandlePopulatePopup;

			info_table.Attach (name_value_label, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 3, 0);

			date_value_label = AttachLabel (info_table, 2, name_value_label);
			size_value_label = AttachLabel (info_table, 3, name_value_label);
			exposure_value_label = AttachLabel (info_table, 4, name_value_label);

			version_list = new ListStore (typeof (IPhotoVersion), typeof (string), typeof (bool));
			version_combo = new ComboBox ();
			CellRendererText version_name_cell = new CellRendererText ();
			version_name_cell.Ellipsize = Pango.EllipsizeMode.End;
			version_combo.PackStart (version_name_cell, true);
			version_combo.SetCellDataFunc (version_name_cell, new CellLayoutDataFunc (VersionNameCellFunc));
			version_combo.Model = version_list;
			version_combo.Changed += OnVersionComboChanged;
			info_table.Attach (version_combo, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			date_value_label.Text = Environment.NewLine;
			exposure_value_label.Text = Environment.NewLine;
			focal_length_value_label = AttachLabel (info_table, 5, name_value_label);
			camera_value_label = AttachLabel (info_table, 6, name_value_label);
			file_size_value_label = AttachLabel (info_table, 7, name_value_label);

			Gtk.Alignment rating_align = new Gtk.Alignment( 0, 0, 0, 0);
			info_table.Attach (rating_align, 1, 2, 8, 9, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			rating_view = new RatingEntry () { HasFrame = false };
			rating_view.Visible = false;
			rating_view.Changed += HandleRatingChanged;
			rating_align.Add (rating_view);

			tag_view = new TagView ();
			info_table.Attach (tag_view, 0, 2, 9, 10, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			info_table.ShowAll ();

			EventBox eb = new EventBox ();
			eb.Add (info_table);
			info_expander.Add (eb);
			eb.ButtonPressEvent += HandleButtonPressEvent;

			Add (info_expander);
			rating_label.Visible = show_rating;
			rating_view.Visible = show_rating;
		}

        // FIXME: We should pull this info directly out of IBrowsableItem
		private class ImageInfo {
			int width;
			int height;
			double? fnumber;
			double? exposure_time;
			uint? iso_speed;
			double? focal_length;
			string camera_model;

			public ImageInfo (IImageFile img)
			{
				if (img == null)
					return;

				using (var metadata = Metadata.Parse (img.Uri)) {
					width = metadata.Properties.PhotoWidth;
					height = metadata.Properties.PhotoHeight;
					fnumber = metadata.ImageTag.FNumber;
					exposure_time = metadata.ImageTag.ExposureTime;
					iso_speed = metadata.ImageTag.ISOSpeedRatings;
					focal_length = metadata.ImageTag.FocalLength;
					camera_model = metadata.ImageTag.Model;
				}
			}

			public string ExposureInfo {
				get {
					string info = String.Empty;

					if (fnumber.HasValue && fnumber.Value != 0.0) {
						try {
							info += String.Format ("f/{0:.0} ", fnumber.Value);
						} catch (FormatException) {
							return Catalog.GetString("(wrong format)");
						}
					}

					if (exposure_time.HasValue) {
						if (Math.Abs (exposure_time.Value) >= 1.0) {
							info += String.Format ("{0} sec ", exposure_time.Value);
						} else {
							info += String.Format ("1/{0} sec ", (int) (1 / exposure_time.Value));
						}
					}

					if (iso_speed.HasValue) {
						info += String.Format ("{0}ISO {1}", Environment.NewLine, iso_speed.Value);
					}

					if (info == String.Empty)
						return Catalog.GetString ("(None)");

					return info;
				}
			}

			public string FocalLength {
				get {
					if (focal_length == null)
						return Catalog.GetString ("(Unknown)");

					return String.Format ("{0} mm", focal_length.Value);
				}
			}

			public string CameraModel {
				get {
					if (camera_model != String.Empty)
						return camera_model;
					else
						return Catalog.GetString ("(Unknown)");
				}
			}


			public string Dimensions {
				get {
					if (width != 0 && height != 0)
						return String.Format ("{0}x{1}", width, height);
					else
						return Catalog.GetString ("(Unknown)");
				}
			}
		}


		public bool Update ()
		{
			if (Photos == null || Photos.Length == 0) {
				Hide ();
			} else if (Photos.Length == 1) {
				Show ();
				UpdateSingle ();
			} else if (Photos.Length > 1) {
				Show ();
				UpdateMultiple ();
			}
			return false;
		}

		private void UpdateSingle ()
		{
			ImageInfo info;

			IPhoto photo = Photos [0];

			histogram_expander.Visible = true;
			UpdateHistogram ();

			if (show_name) {
				name_value_label.Text = photo.Name != null ? photo.Name : String.Empty;
			}
			name_label.Visible = show_name;
			name_value_label.Visible = show_name;

			try {
				using (var img = ImageFile.Create (photo.DefaultVersion.Uri))
				{
					info = new ImageInfo (img);
				}
			} catch (System.Exception e) {
				Hyena.Log.Debug (e.StackTrace);
				info = new ImageInfo (null);
			}

			if (show_exposure) {
				exposure_value_label.Text = info.ExposureInfo;
				if (exposure_value_label.Text.IndexOf (Environment.NewLine) != -1)
					exposure_label.Markup = default_exposure_string + Environment.NewLine;
				else
					exposure_label.Markup = default_exposure_string;
			}
			exposure_label.Visible = show_exposure;
			exposure_value_label.Visible = show_exposure;

			if (show_size)
				size_value_label.Text = info.Dimensions;
			size_label.Visible = show_size;
			size_value_label.Visible = show_size;

			if (show_date) {
				DateTime local_time = photo.Time;
				date_value_label.Text = String.Format ("{0}{2}{1}",
				                                       local_time.ToShortDateString (),
				                                       local_time.ToShortTimeString (),
				                                       Environment.NewLine
				                                       );
			}
			date_label.Visible = show_date;
			date_value_label.Visible = show_date;

			if (show_focal_length)
				focal_length_value_label.Text = info.FocalLength;
			focal_length_label.Visible = show_focal_length;
			focal_length_value_label.Visible = show_focal_length;

			if (show_camera)
				camera_value_label.Text = info.CameraModel;
			camera_label.Visible = show_camera;
			camera_value_label.Visible = show_camera;

			version_label.Visible = true;
			version_combo.Visible = true;
			version_list.Clear ();
			version_combo.Changed -= OnVersionComboChanged;

			int count = 0;
			foreach (IPhotoVersion version in photo.Versions) {
				version_list.AppendValues (version, version.Name, true);
				if (version == photo.DefaultVersion)
					version_combo.Active = count;
				count++;
			}

			if (count <= 1) {
				version_combo.Sensitive = false;
				version_combo.TooltipText = Catalog.GetString ("(No Edits)");
			} else {
				version_combo.Sensitive = true;
				version_combo.TooltipText =
					String.Format (Catalog.GetPluralString ("(One Edit)", "({0} Edits)", count - 1), count - 1);
			}
			version_combo.Changed += OnVersionComboChanged;

			if (show_file_size) {
				try {
					GFile file = FileFactory.NewForUri (photo.DefaultVersion.Uri);
					GFileInfo file_info = file.QueryInfo ("standard::size", FileQueryInfoFlags.None, null);
					file_size_value_label.Text = Format.SizeForDisplay (file_info.Size);
				} catch (GLib.GException e) {
					file_size_value_label.Text = Catalog.GetString("(File read error)");
					Hyena.Log.DebugException (e);
				}
			}

			file_size_label.Visible = show_file_size;
			file_size_value_label.Visible = show_file_size;

			if (show_tags)
				tag_view.Current = photo;
			rating_label.Visible = show_rating;
			rating_view.Visible = show_rating;
			if (show_rating) {
				rating_view.Value = (int) photo.Rating;
			}

			Show ();
		}

		void VersionNameCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 1);
			(cell as CellRendererText).Text = name;

			cell.Sensitive = (bool)tree_model.GetValue (iter, 2);
		}


		void OnVersionComboChanged (object o, EventArgs e)
		{
			ComboBox combo = o as ComboBox;
			if (combo == null)
				return;

			TreeIter iter;

			if (combo.GetActiveIter (out iter))
				VersionChanged (this, (IPhotoVersion)version_list.GetValue (iter, 0));
		}

		private void UpdateMultiple ()
		{
			histogram_expander.Visible = false;

			name_label.Visible = false;
			name_value_label.Text = String.Format(Catalog.GetString("{0} Photos"), Photos.Length);
			name_value_label.Visible = true;

			version_label.Visible = false;
			version_combo.Visible = false;

			exposure_label.Visible = false;
			exposure_value_label.Visible = false;

			focal_length_label.Visible = false;
			focal_length_value_label.Visible = false;

			camera_label.Visible = false;
			camera_value_label.Visible = false;

			if (show_date) {
				IPhoto first = Photos[Photos.Length-1];
				IPhoto last = Photos [0];
				if (first.Time.Date == last.Time.Date) {
					//Note for translators: {0} is a date, {1} and {2} are times.
					date_value_label.Text = String.Format(Catalog.GetString("On {0} between \n{1} and {2}"),
					                                      first.Time.ToShortDateString (),
					                                      first.Time.ToShortTimeString (),
					                                      last.Time.ToShortTimeString ());
				} else {
					date_value_label.Text = String.Format(Catalog.GetString("Between {0} \nand {1}"),
					                                      first.Time.ToShortDateString (),
					                                      last.Time.ToShortDateString ());
				}
			}
			date_label.Visible = show_date;
			date_value_label.Visible = show_date;

			if (show_file_size) {
				long file_size = 0;
				foreach (IPhoto photo in Photos) {

					try {
						GFile file = FileFactory.NewForUri (photo.DefaultVersion.Uri);
						GFileInfo file_info = file.QueryInfo ("standard::size", FileQueryInfoFlags.None, null);
						file_size += file_info.Size;
					} catch (GLib.GException e) {
						file_size = -1;
						Hyena.Log.DebugException (e);
						break;
					}
				}

				if (file_size != -1)
					file_size_value_label.Text = Format.SizeForDisplay (file_size);

				else
					file_size_value_label.Text = Catalog.GetString("(At least one File not found)");
			}
			file_size_label.Visible = show_file_size;
			file_size_value_label.Visible = show_file_size;

			size_label.Visible = false;
			size_value_label.Visible = false;

			rating_label.Visible = false;
			rating_view.Visible = false;
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

			IPhoto photo = Photos [0];

			Gdk.Pixbuf hint = histogram_hint;
			histogram_hint = null;
			int max = histogram_expander.Allocation.Width;

			try {
				if (hint == null)
					using (var img = ImageFile.Create (photo.DefaultVersion.Uri))
						hint = img.Load (256, 256);

				histogram_image.Pixbuf = histogram.Generate (hint, max);

				hint.Dispose ();
			} catch (System.Exception e) {
				Hyena.Log.Debug (e.StackTrace);
				using (Gdk.Pixbuf empty = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 256, 256)) {
					empty.Fill (0x0);
					histogram_image.Pixbuf = histogram.Generate (empty, max);
				}
			}

			return false;
		}

		// Context switching

		private void HandleContextChanged (object sender, EventArgs args)
		{
			bool infobox_visible = ContextSwitchStrategy.InfoBoxVisible (Context);
			info_expander.Expanded = infobox_visible;

			bool histogram_visible = ContextSwitchStrategy.HistogramVisible (Context);
			histogram_expander.Expanded = histogram_visible;

			show_name = ContextSwitchStrategy.InfoBoxNameVisible (Context);
			show_date = ContextSwitchStrategy.InfoBoxDateVisible (Context);
			show_size = ContextSwitchStrategy.InfoBoxSizeVisible (Context);
			show_exposure = ContextSwitchStrategy.InfoBoxExposureVisible (Context);
			show_focal_length = ContextSwitchStrategy.InfoBoxFocalLengthVisible (Context);
			show_camera = ContextSwitchStrategy.InfoBoxCameraVisible (Context);
			show_file_size = ContextSwitchStrategy.InfoBoxFileSizeVisible (Context);

			if (infobox_visible)
				update_delay.Start ();
		}

		public void HandleMainWindowViewModeChanged (object o, EventArgs args)
		{
			MainWindow.ModeType mode = App.Instance.Organizer.ViewMode;
			if (mode == MainWindow.ModeType.IconView)
				Context = ViewContext.Library;
			else if (mode == MainWindow.ModeType.PhotoView)
				Context = ViewContext.Edit;
		}

		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				Menu popup_menu = new Menu ();

				AddMenuItems (popup_menu);

				if (args.Event != null)
					popup_menu.Popup (null, null, null, args.Event.Button, args.Event.Time);
				else
					popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);

				args.RetVal = true;
			}
		}

		void HandlePopulatePopup (object sender, PopulatePopupArgs args)
		{
			AddMenuItems (args.Menu);

			args.RetVal = true;
		}

		private void AddMenuItems (Menu popup_menu) {

			if (popup_menu.Children.Length > 0) {
				GtkUtil.MakeMenuSeparator (popup_menu);
			}

			MenuItem item;

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                                  Catalog.GetString ("Show Photo Name"),
			                                  HandleMenuItemSelected,
			                                  true,
			                                  show_name,
			                                  false);

			item.SetData ("cb", name_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show Date"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_date,
			                           false);

			item.SetData ("cb", date_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show Size"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_size,
			                           false);

			item.SetData ("cb", size_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show Exposure"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_exposure,
			                           false);

			item.SetData ("cb", exposure_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show Focal Length"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_focal_length,
			                           false);

			item.SetData ("cb", focal_length_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show Camera"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_camera,
			                           false);

			item.SetData ("cb", camera_label.Handle);

			item = GtkUtil.MakeCheckMenuItem (popup_menu,
			                           Catalog.GetString ("Show File Size"),
			                           HandleMenuItemSelected,
			                           true,
			                           show_file_size,
			                           false);

			item.SetData ("cb", file_size_label.Handle);
		}

		private void HandleMenuItemSelected (object sender, EventArgs args)
		{
			IntPtr handle = (sender as CheckMenuItem).GetData ("cb");

			if (handle == name_label.Handle) {
				show_name = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxNameVisible (Context, show_name);
			} else if (handle == date_label.Handle) {
				show_date = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxDateVisible (Context, show_date);
			} else if (handle == size_label.Handle) {
				show_size = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxSizeVisible (Context, show_size);
			} else if (handle == exposure_label.Handle) {
				show_exposure = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxExposureVisible (Context, show_exposure);
			} else if (handle == focal_length_label.Handle) {
				show_focal_length = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxFocalLengthVisible (Context, show_focal_length);
			} else if (handle == camera_label.Handle) {
				show_camera = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxCameraVisible (Context, show_camera);
			} else if (handle == file_size_label.Handle) {
				show_file_size = (sender as CheckMenuItem).Active;
				ContextSwitchStrategy.SetInfoBoxFileSizeVisible (Context, show_file_size);
			}

			update_delay.Start ();
		}

		// Constructor.

		public InfoBox () : base (false, 0)
		{
			ContextSwitchStrategy = new MRUInfoBoxContextSwitchStrategy ();
			ContextChanged += HandleContextChanged;

			SetupWidgets ();

			update_delay = new DelayedOperation (Update);
			update_delay.Start ();

			histogram_delay = new DelayedOperation (DelayedUpdateHistogram);

			BorderWidth = 2;
			Hide ();
		}
	}

	// Decides whether infobox / histogram should be shown for each context. Implemented
	// using the Strategy pattern, to make it swappable easily, in case the
	// default MRUInfoBoxContextSwitchStrategy is not sufficiently usable.
	public abstract class InfoBoxContextSwitchStrategy {
		public abstract bool InfoBoxVisible (ViewContext context);
		public abstract bool HistogramVisible (ViewContext context);
		public abstract bool InfoBoxNameVisible (ViewContext context);
		public abstract bool InfoBoxDateVisible (ViewContext context);
		public abstract bool InfoBoxSizeVisible (ViewContext context);
		public abstract bool InfoBoxExposureVisible (ViewContext context);
		public abstract bool InfoBoxFocalLengthVisible (ViewContext context);
		public abstract bool InfoBoxCameraVisible (ViewContext context);
		public abstract bool InfoBoxFileSizeVisible (ViewContext context);

		public abstract void SetInfoBoxVisible (ViewContext context, bool visible);
		public abstract void SetHistogramVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxNameVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxDateVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxSizeVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxExposureVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxFocalLengthVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxCameraVisible (ViewContext context, bool visible);
		public abstract void SetInfoBoxFileSizeVisible (ViewContext context, bool visible);
	}

	// Values are stored as strings, because bool is not nullable through Preferences.
	public class MRUInfoBoxContextSwitchStrategy : InfoBoxContextSwitchStrategy {
		public const string PREF_PREFIX = Preferences.APP_FSPOT + "ui";

		private string PrefKeyForContext (ViewContext context, string item) {
			return String.Format ("{0}/{1}_visible/{2}", PREF_PREFIX, item, context);
		}

		private string PrefKeyForContext (ViewContext context, string parent, string item) {
			return String.Format ("{0}/{1}_visible/{2}/{3}", PREF_PREFIX, parent, item, context);
		}

		private bool VisibilityForContext (ViewContext context, string item, bool default_value) {
			string visible = Preferences.Get<string> (PrefKeyForContext (context, item));
			if (visible == null)
				return default_value;
			else
				return visible == "1";
		}

		private bool VisibilityForContext (ViewContext context, string parent, string item, bool default_value) {
			string visible = Preferences.Get<string> (PrefKeyForContext (context, parent, item));
			if (visible == null)
				return default_value;
			else
				return visible == "1";
		}

		private void SetVisibilityForContext (ViewContext context, string item, bool visible) {
			Preferences.Set (PrefKeyForContext (context, item), visible ? "1" : "0");
		}

		private void SetVisibilityForContext (ViewContext context, string parent, string item, bool visible) {
			Preferences.Set (PrefKeyForContext (context, parent, item), visible ? "1" : "0");
		}

		public override bool InfoBoxVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", true);
		}

		public override bool HistogramVisible (ViewContext context) {
			return VisibilityForContext (context, "histogram", true);
		}

		public override bool InfoBoxNameVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "name", true);
		}

		public override bool InfoBoxDateVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "date", true);
		}

		public override bool InfoBoxSizeVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "size", true);
		}

		public override bool InfoBoxExposureVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "exposure", true);
		}

		public override bool InfoBoxFocalLengthVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "focal_length", false);
		}

		public override bool InfoBoxCameraVisible (ViewContext context)  {
			return VisibilityForContext (context, "infobox", "camera", false);
		}

		public override bool InfoBoxFileSizeVisible (ViewContext context) {
			return VisibilityForContext (context, "infobox", "file_size", false);
		}

		public override void SetInfoBoxVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", visible);
		}

		public override void SetHistogramVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "histogram", visible);
		}

		public override void SetInfoBoxNameVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "name", visible);
		}

		public override void SetInfoBoxDateVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "date", visible);
		}

		public override void SetInfoBoxSizeVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "size", visible);
		}

		public override void SetInfoBoxExposureVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "exposure", visible);
		}

		public override void SetInfoBoxFocalLengthVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "focal_length", visible);
		}

		public override void SetInfoBoxCameraVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "camera", visible);
		}

		public override void SetInfoBoxFileSizeVisible (ViewContext context, bool visible) {
			SetVisibilityForContext (context, "infobox", "file_size", visible);
		}
	}
}
