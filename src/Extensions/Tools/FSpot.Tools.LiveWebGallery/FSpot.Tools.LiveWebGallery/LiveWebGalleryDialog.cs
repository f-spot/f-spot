//
// LiveWebGalleryDialog.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Reflection;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Widgets;

using Gtk;

using Hyena;

namespace FSpot.Tools.LiveWebGallery
{
	class LiveWebGalleryDialog : FSpot.UI.Dialog.BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.LinkButton url_button;
		[GtkBeans.Builder.Object] Gtk.ToggleButton activate_button;
		[GtkBeans.Builder.Object] Gtk.Button copy_button;
		[GtkBeans.Builder.Object] Gtk.Label stats_label;
		[GtkBeans.Builder.Object] Gtk.RadioButton current_view_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton tagged_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton selected_radio;
		[GtkBeans.Builder.Object] Gtk.Button tag_button;
		[GtkBeans.Builder.Object] Gtk.CheckButton limit_checkbox;
		[GtkBeans.Builder.Object] Gtk.SpinButton limit_spin;
		[GtkBeans.Builder.Object] Gtk.CheckButton allow_tagging_checkbox;
		[GtkBeans.Builder.Object] Gtk.Button tag_edit_button;
#pragma warning restore 649

		SimpleWebServer server;
		ILiveWebGalleryOptions options;
		LiveWebGalleryStats stats;
		IPAddress last_ip;
		string last_client;

		public LiveWebGalleryDialog (SimpleWebServer server, ILiveWebGalleryOptions options, LiveWebGalleryStats stats)
			: base (Assembly.GetExecutingAssembly (), "LiveWebGallery.ui", "live_web_gallery_dialog")
		{
			this.server = server;
			this.options = options;
			this.stats = stats;
			Modal = false;

			activate_button.Active = server.Active;
			UpdateGalleryURL ();
			limit_checkbox.Active = options.LimitMaxPhotos;
			limit_spin.Sensitive = options.LimitMaxPhotos;
			limit_spin.Value = options.MaxPhotos;
			UpdateQueryRadios ();
			HandleQueryTagSelected (options.QueryTag ?? App.Instance.Database.Tags.GetTagById (1));
			allow_tagging_checkbox.Active = options.TaggingAllowed;
			tag_edit_button.Sensitive = options.TaggingAllowed;
			HandleEditableTagSelected (options.EditableTag ?? App.Instance.Database.Tags.GetTagById (3));
			HandleStatsChanged (null, null);

			activate_button.Toggled += HandleActivated;
			copy_button.Clicked += HandleCopyClicked;
			current_view_radio.Toggled += HandleRadioChanged;
			tagged_radio.Toggled += HandleRadioChanged;
			selected_radio.Toggled += HandleRadioChanged;
			tag_button.Clicked += HandleQueryTagClicked;
			limit_checkbox.Toggled += HandleLimitToggled;
			limit_spin.ValueChanged += HandleLimitValueChanged;
			allow_tagging_checkbox.Toggled += HandleAllowTaggingToggled;
			tag_edit_button.Clicked += HandleTagForEditClicked;
			stats.StatsChanged += HandleStatsChanged;
		}

		void HandleCopyClicked (object sender, EventArgs e)
		{
			Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", true)).Text = url_button.Uri;
		}

		void HandleStatsChanged (object sender, EventArgs e)
		{
			ThreadAssist.ProxyToMain (() => {
				if (last_ip == null || !last_ip.Equals (stats.LastIP)) {
					last_ip = stats.LastIP;
					try {
						last_client = Dns.GetHostEntry (last_ip).HostName;
					} catch (Exception) {
						last_client = last_ip != null ? last_ip.ToString () : Strings.None;
					}
				}
				stats_label.Text = string.Format (Strings.GalleryXPhotosYLastClientZ,
												 stats.GalleryViews, stats.PhotoViews, stats.BytesSent / 1024, last_client);
			});
		}

		void HandleLimitToggled (object sender, EventArgs e)
		{
			options.LimitMaxPhotos = limit_checkbox.Active;
			limit_spin.Sensitive = limit_checkbox.Active;
			HandleLimitValueChanged (sender, e);
		}

		void HandleLimitValueChanged (object sender, EventArgs e)
		{
			options.MaxPhotos = limit_spin.ValueAsInt;
		}

		void HandleRadioChanged (object o, EventArgs e)
		{
			tag_button.Sensitive = tagged_radio.Active;
			if (tagged_radio.Active)
				options.QueryType = QueryType.ByTag;
			else if (current_view_radio.Active)
				options.QueryType = QueryType.CurrentView;
			else
				options.QueryType = QueryType.Selected;
		}

		void UpdateQueryRadios ()
		{
			switch (options.QueryType) {
			case QueryType.ByTag:
				tagged_radio.Active = true;
				break;
			case QueryType.CurrentView:
				current_view_radio.Active = true;
				break;
			case QueryType.Selected:
			default:
				selected_radio.Active = true;
				break;
			}
			HandleRadioChanged (null, null);
		}

		void HandleActivated (object o, EventArgs e)
		{
			if (activate_button.Active)
				server.Start ();
			else
				server.Stop ();

			UpdateGalleryURL ();
		}

		void UpdateGalleryURL ()
		{
			url_button.Sensitive = server.Active;
			copy_button.Sensitive = server.Active;
			if (server.Active) {
				url_button.Uri = "http://" + server.HostPort;
				url_button.Label = url_button.Uri;
			} else {
				url_button.Label = Strings.GalleryIsInactive;
			}
		}

		void ShowTagMenuFor (Widget widget, TagMenu.TagSelectedHandler handler)
		{
			var tag_menu = new TagMenu (null, App.Instance.Database.Tags);
			tag_menu.TagSelected += handler;
			tag_menu.Populate ();
			GetPosition (out var x, out var y);
			x += widget.Allocation.X; y += widget.Allocation.Y;
			tag_menu.Popup (null, null, delegate (Menu menu, out int x_, out int y_, out bool push_in) { x_ = x; y_ = y; push_in = true; }, 0, 0);
		}

		void HandleQueryTagClicked (object sender, EventArgs e)
		{
			ShowTagMenuFor (tag_button, HandleQueryTagSelected);
		}

		void HandleQueryTagSelected (Tag tag)
		{
			options.QueryTag = tag;
			tag_button.Label = tag.Name;
			tag_button.Image = tag.Icon != null ? new Gtk.Image (tag.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear)) : null;
		}

		void HandleAllowTaggingToggled (object sender, EventArgs e)
		{
			tag_edit_button.Sensitive = allow_tagging_checkbox.Active;
			options.TaggingAllowed = allow_tagging_checkbox.Active;
		}

		void HandleTagForEditClicked (object sender, EventArgs e)
		{
			ShowTagMenuFor (tag_edit_button, HandleEditableTagSelected);
		}

		void HandleEditableTagSelected (Tag tag)
		{
			options.EditableTag = tag;
			tag_edit_button.Label = tag.Name;
			tag_edit_button.Image = tag.Icon != null ? new Gtk.Image (tag.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear)) : null;
		}
	}
}
