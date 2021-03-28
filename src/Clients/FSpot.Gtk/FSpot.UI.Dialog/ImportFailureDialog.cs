//
// ImportFailureDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Utils;

using Gtk;

using Hyena;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class ImportFailureDialog : Gtk.Dialog
	{
		// FIXME: Replace with ErrorListDialog from Banshee when possible

		VBox inner_vbox;

		Label header_label;
		Hyena.Widgets.WrapLabel message_label;
		Expander details_expander;

		protected AccelGroup AccelGroup { get; private set; }

		public string Header {
			set {
				header_label.Markup = $"<b><big>{GLib.Markup.EscapeText (value)}</big></b>";
			}
		}

		public string Message {
			set { message_label.Text = value; }
		}

		public TreeView ListView { get; private set; }

		public ImportFailureDialog (IEnumerable<SafeUri> files)
		{
			BuildUI ();

			ListView.Model = new ListStore (typeof (string), typeof (string));
			ListView.AppendColumn ("Filename", new CellRendererText (), "text", 0);
			ListView.AppendColumn ("Path", new CellRendererText (), "text", 1);
			ListView.HeadersVisible = false;

			Title = Catalog.GetString ("Import failures");
			Header = Catalog.GetString ("Some files failed to import");
			Message = Catalog.GetString ("Some files could not be imported, they might be corrupt "
					+ "or there might be something wrong with the storage on which they reside.");

			foreach (SafeUri uri in files) {
				(ListView.Model as ListStore).AppendValues (uri.GetFilename (), uri.GetBaseUri ().ToString ());
			}
		}

		void BuildUI ()
		{
			// The BorderWidth situation here is a bit nuts b/c the
			// ActionArea's is set to 5.  So we work everything else out
			// so it all totals to 12.
			//
			// WIDGET           BorderWidth
			// Dialog           5
			//   VBox           2
			//     inner_vbox   5 => total = 12
			//     ActionArea   5 => total = 12
			BorderWidth = 5;
			VBox.BorderWidth = 0;

			// This spacing is 2 b/c the inner_vbox and ActionArea should be
			// 12 apart, and they already have BorderWidth 5 each
			VBox.Spacing = 2;

			inner_vbox = new VBox { Spacing = 12, BorderWidth = 5, Visible = true };
			VBox.PackStart (inner_vbox, true, true, 0);

			Visible = false;
			HasSeparator = false;

			using var table = new Table (3, 2, false) {
				RowSpacing = 12,
				ColumnSpacing = 16
			};

			using var image = new Image {
				IconName = "dialog-error",
				IconSize = (int)IconSize.Dialog,
				Yalign = 0.0f
			};
			table.Attach (image, 0, 1, 0, 3, AttachOptions.Shrink, AttachOptions.Fill | AttachOptions.Expand, 0, 0);

			table.Attach (header_label = new Label { Xalign = 0.0f }, 1, 2, 0, 1,
				AttachOptions.Fill | AttachOptions.Expand,
				AttachOptions.Shrink, 0, 0);

			table.Attach (message_label = new Hyena.Widgets.WrapLabel (), 1, 2, 1, 2,
				AttachOptions.Fill | AttachOptions.Expand,
				AttachOptions.Shrink, 0, 0);

			using var scrolledWindow = new ScrolledWindow {
				HscrollbarPolicy = PolicyType.Automatic,
				VscrollbarPolicy = PolicyType.Automatic,
				ShadowType = ShadowType.In
			};

			ListView = new TreeView {
				HeightRequest = 120,
				WidthRequest = 200
			};
			scrolledWindow.Add (ListView);

			table.Attach (details_expander = new Expander (Catalog.GetString ("Details")),
				1, 2, 2, 3,
				AttachOptions.Fill | AttachOptions.Expand,
				AttachOptions.Fill | AttachOptions.Expand,
				0, 0);
			details_expander.Add (scrolledWindow);

			VBox.PackStart (table, true, true, 0);
			VBox.Spacing = 12;
			VBox.ShowAll ();

			AccelGroup = new AccelGroup ();
			AddAccelGroup (AccelGroup);

			using var button = new Button (Stock.Close) {
				CanDefault = true,
				UseStock = true
			};
			button.Show ();
			button.Clicked += (o, a) => {
				Destroy ();
			};

			AddActionWidget (button, ResponseType.Close);

			DefaultResponse = ResponseType.Close;
			button.AddAccelerator ("activate", AccelGroup, (uint)Gdk.Key.Return, 0, AccelFlags.Visible);
		}
	}
}
