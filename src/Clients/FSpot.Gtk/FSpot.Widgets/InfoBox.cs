//
// InfoBox.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Mike Gemuende <mike@gemuende.de>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Mike Gemuende
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Utils;

using Gtk;

// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

namespace FSpot.Widgets
{
	public class InfoBox : VBox
	{
		readonly DelayedOperation update_delay;

		public struct InfoEntry
		{
			public bool TwoColumns;
			public bool AlwaysVisible;
			public bool DefaultVisibility;
			public string Id;
			public string Description;
			public Widget LabelWidget;
			public Widget InfoWidget;
			public Action<Widget, IPhoto, TagLib.Image.File> SetSingle;
			public Action<Widget, IPhoto[]> SetMultiple;
		}

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

		readonly List<InfoEntry> entries = new List<InfoEntry> ();

		void AddEntry (string id, string name, string description, Widget info_widget, float label_y_align,
							   bool default_visibility,
							   Action<Widget, IPhoto, TagLib.Image.File> set_single,
							   Action<Widget, IPhoto[]> set_multiple)
		{
			entries.Add (new InfoEntry {
				TwoColumns = (name == null),
				AlwaysVisible = (id == null) || (description == null),
				DefaultVisibility = default_visibility,
				Id = id,
				Description = description,
				LabelWidget = CreateRightAlignedLabel ($"<b>{name}</b>", label_y_align),
				InfoWidget = info_widget,
				SetSingle = set_single,
				SetMultiple = set_multiple
			});
		}

		void AddEntry (string id, string name, string description, Widget info_widget, float label_y_align,
							   Action<Widget, IPhoto, TagLib.Image.File> set_single,
							   Action<Widget, IPhoto[]> set_multiple)
		{
			AddEntry (id, name, description, info_widget, label_y_align, true, set_single, set_multiple);
		}

		void AddEntry (string id, string name, string description, Widget info_widget, bool default_visibility,
							   Action<Widget, IPhoto, TagLib.Image.File> set_single,
							   Action<Widget, IPhoto[]> set_multiple)
		{
			AddEntry (id, name, description, info_widget, 0.0f, default_visibility, set_single, set_multiple);
		}

		void AddEntry (string id, string name, string description, Widget info_widget,
							   Action<Widget, IPhoto, TagLib.Image.File> set_single,
							   Action<Widget, IPhoto[]> set_multiple)
		{
			AddEntry (id, name, description, info_widget, 0.0f, set_single, set_multiple);
		}

		void AddLabelEntry (string id, string name, string description,
									Func<IPhoto, TagLib.Image.File, string> single_string,
									Func<IPhoto[], string> multiple_string)
		{
			AddLabelEntry (id, name, description, true, single_string, multiple_string);
		}

		void AddLabelEntry (string id, string name, string description, bool default_visibility,
									Func<IPhoto, TagLib.Image.File, string> single_string,
									Func<IPhoto[], string> multiple_string)
		{
			Action<Widget, IPhoto, TagLib.Image.File> setSingle = (widget, photo, metadata) => {
				if (metadata != null)
					(widget as Label).Text = single_string (photo, metadata);
				else
					(widget as Label).Text = Strings.ParenUnknownParen;
			};

			Action<Widget, IPhoto[]> set_multiple = (widget, photos) => {
				(widget as Label).Text = multiple_string (photos);
			};

			AddEntry (id, name, description, CreateLeftAlignedLabel (string.Empty), default_visibility,
					  single_string == null ? null : setSingle,
					  multiple_string == null ? null : set_multiple);
		}


		IPhoto[] photos = new IPhoto[0];
		public IPhoto[] Photos {
			private get { return photos; }
			set {
				photos = value;
				update_delay.Start ();
			}
		}

		public IPhoto Photo {
			set {
				if (value != null) {
					Photos = new[] { value };
				}
			}
		}

		bool show_tags;
		public bool ShowTags {
			get { return show_tags; }
			set {
				if (show_tags == value)
					return;

				show_tags = value;
				//      tag_view.Visible = show_tags;
			}
		}

		bool show_rating;
		public bool ShowRating {
			get { return show_rating; }
			set {
				if (show_rating == value)
					return;

				show_rating = value;
				//      rating_label.Visible = show_rating;
				//      rating_view.Visible = show_rating;
			}
		}

		public delegate void VersionChangedHandler (InfoBox info_box, IPhotoVersion version);
		public event VersionChangedHandler VersionChanged;

		Expander info_expander;
		Expander histogram_expander;

		Image histogram_image;
		Histogram histogram;
		readonly DelayedOperation histogram_delay;

		// Context switching (toggles visibility).
		public event EventHandler ContextChanged;

		ViewContext view_context = ViewContext.Unknown;
		public ViewContext Context {
			get { return view_context; }
			set {
				view_context = value;
				ContextChanged?.Invoke (this, null);
			}
		}

		readonly InfoBoxContextSwitchStrategy ContextSwitchStrategy;

		// Widgetry.
		ListStore version_list;
		ComboBox version_combo;


		void HandleRatingChanged (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRatingMenuSelected ((o as RatingEntry).Value);
		}

		Label CreateRightAlignedLabel (string text, float yalign)
		{
			var label = new Label {
				UseMarkup = true,
				Markup = text,
				Xalign = 1.0f,
				Yalign = yalign
			};

			return label;
		}

		Label CreateLeftAlignedLabel (string text)
		{
			var label = new Label {
				UseMarkup = true,
				Markup = text,
				Xalign = 0.0f,
				Yalign = 0.0f,
				Selectable = true,
				Ellipsize = Pango.EllipsizeMode.End
			};

			return label;
		}

		Table info_table;

		void AttachRow (int row, InfoEntry entry)
		{
			if (!entry.TwoColumns) {
				info_table.Attach (entry.LabelWidget, 0, 1, (uint)row, (uint)row + 1, AttachOptions.Fill, AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);
			}

			info_table.Attach (entry.InfoWidget, entry.TwoColumns ? 0u : 1u, 2, (uint)row, (uint)row + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, TABLE_XPADDING, TABLE_YPADDING);

			var info_label = entry.InfoWidget as Label;
			if (info_label != null)
				info_label.PopulatePopup += HandlePopulatePopup;

			var info_entry = entry.InfoWidget as Entry;
			if (info_entry != null)
				info_entry.PopulatePopup += HandlePopulatePopup;
		}

		void UpdateTable ()
		{
			info_table.Resize ((uint)(head_rows + entries.Count), 2);
			int i = 0;
			foreach (var entry in entries) {
				AttachRow (head_rows + i, entry);
				i++;
			}
		}


		void SetEntryWidgetVisibility (InfoEntry entry, bool def)
		{
			entry.InfoWidget.Visible = ContextSwitchStrategy.InfoEntryVisible (Context, entry) && def;
			entry.LabelWidget.Visible = ContextSwitchStrategy.InfoEntryVisible (Context, entry) && def;
		}

		void UpdateEntries ()
		{

		}

		const int TABLE_XPADDING = 3;
		const int TABLE_YPADDING = 3;
		Label AttachLabel (Table table, int row_num, Widget entry)
		{
			var label = new Label (string.Empty);
			label.Xalign = 0;
			label.Selectable = true;
			label.Ellipsize = Pango.EllipsizeMode.End;
			label.Show ();

			label.PopulatePopup += HandlePopulatePopup;

			table.Attach (label, 1, 2, (uint)row_num, (uint)row_num + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, (uint)entry.Style.XThickness + TABLE_XPADDING, (uint)entry.Style.YThickness);

			return label;
		}

		const int head_rows = 0;

		void SetupWidgets ()
		{
			histogram_expander = new Expander (Strings.Histogram);
			histogram_expander.Activated += (s, e) => {
				ContextSwitchStrategy.SetHistogramVisible (Context, histogram_expander.Expanded);
				UpdateHistogram ();
			};

			histogram_expander.StyleSet += (s, a) => {
				Gdk.Color c = Toplevel.Style.Backgrounds[(int)Gtk.StateType.Active];
				histogram.RedColorHint = (byte)(c.Red / 0xff);
				histogram.GreenColorHint = (byte)(c.Green / 0xff);
				histogram.BlueColorHint = (byte)(c.Blue / 0xff);
				histogram.BackgroundColorHint = 0xff;
				UpdateHistogram ();
			};

			histogram_image = new Image ();
			histogram = new Histogram ();
			histogram_expander.Add (histogram_image);

			Add (histogram_expander);

			info_expander = new Expander (Strings.ImageInformation);
			info_expander.Activated += (s, e) => {
				ContextSwitchStrategy.SetInfoBoxVisible (Context, info_expander.Expanded);
			};

			info_table = new Table (head_rows, 2, false) { BorderWidth = 0 };

			AddLabelEntry (null, null, null, null,
						   photos => { return string.Format (Strings.XPhotos, photos.Length); });

			AddLabelEntry (null, Strings.Name, null,
						   (photo, file) => { return photo.Name ?? string.Empty; }, null);

			version_list = new ListStore (typeof (IPhotoVersion), typeof (string), typeof (bool));
			version_combo = new ComboBox ();
			var version_name_cell = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};
			version_combo.PackStart (version_name_cell, true);
			version_combo.SetCellDataFunc (version_name_cell, new CellLayoutDataFunc (VersionNameCellFunc));
			version_combo.Model = version_list;
			version_combo.Changed += OnVersionComboChanged;

			AddEntry (null, Strings.Version, null, version_combo, 0.5f,
					  (widget, photo, file) => {
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
							  version_combo.TooltipText = Strings.ParenNoEditsParen;
						  } else {
							  version_combo.Sensitive = true;
							  version_combo.TooltipText =
								  string.Format (count - 1 <= 1 ? Strings.ParenOneEditParen : Strings.ParenXEditsParen, count - 1);
						  }
						  version_combo.Changed += OnVersionComboChanged;
					  }, null);

			AddLabelEntry ("date", Strings.Date, Strings.ShowDate,
						   (photo, file) => {
							   return $"{photo.UtcTime.ToShortDateString ()}{Environment.NewLine}{photo.UtcTime.ToShortTimeString ()}";
						   },
						   photos => {
							   IPhoto first = photos[photos.Length - 1];
							   IPhoto last = photos[0];
							   if (first.UtcTime.Date == last.UtcTime.Date) {
								   //Note for translators: {0} is a date, {1} and {2} are times.
								   return string.Format (Strings.OnXBetweenYAndZ,
														 first.UtcTime.ToShortDateString (),
														 first.UtcTime.ToShortTimeString (),
														 last.UtcTime.ToShortTimeString ());
							   } else {
								   return string.Format (Strings.BetweenXAndY,
														 first.UtcTime.ToShortDateString (),
														 last.UtcTime.ToShortDateString ());
							   }
						   });

			AddLabelEntry ("size", Strings.Size, Strings.ShowSize,
						   (photo, metadata) => {
							   int width = 0;
							   int height = 0;
							   if (null != metadata.Properties) {
								   width = metadata.Properties.PhotoWidth;
								   height = metadata.Properties.PhotoHeight;
							   }

							   if (width != 0 && height != 0)
								   return $"{width}x{height}";

							   return Strings.ParenUnknownParen;
						   }, null);

			AddLabelEntry ("exposure", Strings.Exposure, Strings.ShowExposure,
						   (photo, metadata) => {
							   var fnumber = metadata.ImageTag.FNumber;
							   var exposure_time = metadata.ImageTag.ExposureTime;
							   var iso_speed = metadata.ImageTag.ISOSpeedRatings;

							   string info = string.Empty;

							   if (fnumber.HasValue && fnumber.Value != 0.0) {
								   info += $"f/{fnumber.Value:.0} ";
							   }

							   if (exposure_time.HasValue) {
								   if (Math.Abs (exposure_time.Value) >= 1.0) {
									   info += $"{exposure_time.Value} sec ";
								   } else {
									   info += $"1/{(int)(1 / exposure_time.Value)} sec ";
								   }
							   }

							   if (iso_speed.HasValue) {
								   info += $"{Environment.NewLine}ISO {iso_speed.Value}";
							   }

							   var exif = metadata.ImageTag.Exif;
							   if (exif != null) {
								   var flash = exif.ExifIFD.GetLongValue (0, (ushort)TagLib.IFD.Tags.ExifEntryTag.Flash);

								   if (flash.HasValue) {
									   if ((flash.Value & 0x01) == 0x01)
										   info += $", {Strings.Flashfired}";
									   else
										   info += $", {Strings.FlashDidntFire}";
								   }
							   }

							   if (string.IsNullOrEmpty (info))
								   return Strings.ParenNoneParen;

							   return info;
						   }, null);

			AddLabelEntry ("focal_length", Strings.FocalLength, Strings.ShowFocalLength,
						   false, (photo, metadata) => {
							   var focal_length = metadata.ImageTag.FocalLength;

							   if (focal_length == null)
								   return Strings.ParenUnknownParen;

							   return $"{focal_length.Value} mm";
						   }, null);

			AddLabelEntry ("camera", Strings.Camera, Strings.ShowCamera, false,
						   (photo, metadata) => { return metadata.ImageTag.Model ?? Strings.ParenUnknownParen; },
						   null);

			AddLabelEntry ("creator", Strings.Creator, Strings.ShowCreator,
						   (photo, metadata) => { return metadata.ImageTag.Creator ?? Strings.ParenUnknownParen; },
						   null);

			AddLabelEntry ("file_size", Strings.FileSize, Strings.ShowFileSize, false,
						   (photo, metadata) => {
							   try {
								   return new DotNetFile ().GetSize (photo.DefaultVersion.Uri).ToString ();
							   } catch (Exception e) {
								   Logger.Log.Debug (e, "");
								   return Strings.ParenFileReadErrorParen;
							   }
						   }, null);

			var rating_entry = new RatingEntry { HasFrame = false, AlwaysShowEmptyStars = true };
			rating_entry.Changed += HandleRatingChanged;
			var rating_align = new Gtk.Alignment (0, 0, 0, 0);
			rating_align.Add (rating_entry);
			AddEntry ("rating", Strings.Rating, Strings.ShowRating, rating_align, false,
					  (widget, photo, metadata) => { ((widget as Alignment).Child as RatingEntry).Value = (int)photo.Rating; },
					  null);

			AddEntry ("tag", null, Strings.ShowTags, new TagView (), false,
					  (widget, photo, metadata) => { (widget as TagView).Current = photo; }, null);

			UpdateTable ();

			var eb = new EventBox { info_table };
			info_expander.Add (eb);
			eb.ButtonPressEvent += HandleButtonPressEvent;

			Add (info_expander);
		}

		public bool Update ()
		{
			if (Photos == null || Photos.Length == 0) {
				Hide ();
			} else if (Photos.Length == 1) {
				var photo = Photos[0];

				histogram_expander.Visible = true;
				UpdateHistogram ();

				using (var metadata = MetadataUtils.Parse (photo.DefaultVersion.Uri)) {
					foreach (var entry in entries) {
						bool is_single = (entry.SetSingle != null);

						if (is_single)
							entry.SetSingle (entry.InfoWidget, photo, metadata);

						SetEntryWidgetVisibility (entry, is_single);
					}
				}
				Show ();
			} else if (Photos.Length > 1) {
				foreach (var entry in entries) {
					bool is_multiple = (entry.SetMultiple != null);

					if (is_multiple)
						entry.SetMultiple (entry.InfoWidget, Photos);

					SetEntryWidgetVisibility (entry, is_multiple);
				}
				histogram_expander.Visible = false;
				Show ();
			}
			return false;
		}

		void VersionNameCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 1);
			(cell as CellRendererText).Text = name;

			cell.Sensitive = (bool)tree_model.GetValue (iter, 2);
		}

		void OnVersionComboChanged (object o, EventArgs e)
		{
			var combo = o as ComboBox;
			if (combo == null)
				return;

			if (combo.GetActiveIter (out var iter))
				VersionChanged (this, (IPhotoVersion)version_list.GetValue (iter, 0));
		}

		Gdk.Pixbuf histogram_hint;

		void UpdateHistogram ()
		{
			if (histogram_expander.Expanded)
				histogram_delay.Start ();
		}

		public void UpdateHistogram (Gdk.Pixbuf pixbuf)
		{
			histogram_hint = pixbuf;
			UpdateHistogram ();
		}

		bool DelayedUpdateHistogram ()
		{
			if (Photos.Length == 0)
				return false;

			IPhoto photo = Photos[0];

			Gdk.Pixbuf hint = histogram_hint;
			histogram_hint = null;
			int max = histogram_expander.Allocation.Width;

			try {
				if (hint == null)
					using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (photo.DefaultVersion.Uri)) {
						hint = img.Load (256, 256);
					}

				histogram_image.Pixbuf = histogram.Generate (hint, max);

				hint.Dispose ();
			} catch (Exception e) {
				Logger.Log.Debug (e, "");
				using var empty = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 256, 256);
				empty.Fill (0x0);
				histogram_image.Pixbuf = histogram.Generate (empty, max);
			}

			return false;
		}

		// Context switching

		void HandleContextChanged (object sender, EventArgs args)
		{
			bool infobox_visible = ContextSwitchStrategy.InfoBoxVisible (Context);
			info_expander.Expanded = infobox_visible;

			bool histogram_visible = ContextSwitchStrategy.HistogramVisible (Context);
			histogram_expander.Expanded = histogram_visible;

			if (infobox_visible)
				update_delay.Start ();
		}

		public void HandleMainWindowViewModeChanged (object o, EventArgs args)
		{
			var mode = App.Instance.Organizer.ViewMode;
			if (mode == MainWindow.ModeType.IconView)
				Context = ViewContext.Library;
			else if (mode == MainWindow.ModeType.PhotoView)
				Context = ViewContext.Edit;
		}

		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				using (var popup_menu = new Menu ()) {
					AddMenuItems (popup_menu);

					if (args.Event != null)
						popup_menu.Popup (null, null, null, args.Event.Button, args.Event.Time);
					else
						popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
				}

				args.RetVal = true;
			}
		}

		void HandlePopulatePopup (object sender, PopulatePopupArgs args)
		{
			AddMenuItems (args.Menu);

			args.RetVal = true;
		}

		void AddMenuItems (Menu popup_menu)
		{
			var items = new Dictionary<MenuItem, InfoEntry> ();

			if (popup_menu.Children.Length > 0 && entries.Count > 0) {
				GtkUtil.MakeMenuSeparator (popup_menu);
			}

			foreach (var entry in entries) {
				if (entry.AlwaysVisible)
					continue;

				var item =
					GtkUtil.MakeCheckMenuItem (popup_menu, entry.Description, (sender, args) => {
						ContextSwitchStrategy.SetInfoEntryVisible (Context, items[sender as CheckMenuItem], (sender as CheckMenuItem).Active);
						Update ();
					},
					true, ContextSwitchStrategy.InfoEntryVisible (Context, entry), false);

				items.Add (item, entry);
			}
		}

		void HandleMenuItemSelected (object sender, EventArgs args)
		{

		}
	}

	// Decides whether infobox / histogram should be shown for each context. Implemented
	// using the Strategy pattern, to make it swappable easily, in case the
	// default MRUInfoBoxContextSwitchStrategy is not sufficiently usable.
	public abstract class InfoBoxContextSwitchStrategy
	{
		public abstract bool InfoBoxVisible (ViewContext context);
		public abstract bool HistogramVisible (ViewContext context);

		public abstract bool InfoEntryVisible (ViewContext context, InfoBox.InfoEntry entry);

		public abstract void SetInfoBoxVisible (ViewContext context, bool visible);
		public abstract void SetHistogramVisible (ViewContext context, bool visible);

		public abstract void SetInfoEntryVisible (ViewContext context, InfoBox.InfoEntry entry, bool visible);
	}

	// Values are stored as strings, because bool is not nullable through Preferences.
	public class MRUInfoBoxContextSwitchStrategy : InfoBoxContextSwitchStrategy
	{
		string PrefKeyForContext (ViewContext context, string item)
		{
			return $"{Preferences.UIKey}{item}_visible/{context}";
		}

		string PrefKeyForContext (ViewContext context, string parent, string item)
		{
			return $"{Preferences.UIKey}{parent}_visible/{item}/{context}";
		}

		bool VisibilityForContext (ViewContext context, string item, bool default_value)
		{
			string visible = Preferences.Get<string> (PrefKeyForContext (context, item));
			if (visible == null)
				return default_value;

			return visible == "1";
		}

		bool VisibilityForContext (ViewContext context, string parent, string item, bool default_value)
		{
			string visible = Preferences.Get<string> (PrefKeyForContext (context, parent, item));
			if (visible == null)
				return default_value;

			return visible == "1";
		}

		void SetVisibilityForContext (ViewContext context, string item, bool visible)
		{
			Preferences.Set (PrefKeyForContext (context, item), visible ? "1" : "0");
		}

		void SetVisibilityForContext (ViewContext context, string parent, string item, bool visible)
		{
			Preferences.Set (PrefKeyForContext (context, parent, item), visible ? "1" : "0");
		}

		public override bool InfoBoxVisible (ViewContext context)
		{
			return VisibilityForContext (context, "infobox", true);
		}

		public override bool HistogramVisible (ViewContext context)
		{
			return VisibilityForContext (context, "histogram", true);
		}

		public override bool InfoEntryVisible (ViewContext context, InfoBox.InfoEntry entry)
		{
			if (entry.AlwaysVisible)
				return true;

			return VisibilityForContext (context, "infobox", entry.Id, true);
		}

		public override void SetInfoBoxVisible (ViewContext context, bool visible)
		{
			SetVisibilityForContext (context, "infobox", visible);
		}

		public override void SetHistogramVisible (ViewContext context, bool visible)
		{
			SetVisibilityForContext (context, "histogram", visible);
		}

		public override void SetInfoEntryVisible (ViewContext context, InfoBox.InfoEntry entry, bool visible)
		{
			Logger.Log.Debug ($"Set Visibility for Entry {entry.Id} to {visible}");
			if (entry.AlwaysVisible)
				throw new Exception ("entry visibility cannot be set");

			SetVisibilityForContext (context, "infobox", entry.Id, visible);
		}
	}
}
