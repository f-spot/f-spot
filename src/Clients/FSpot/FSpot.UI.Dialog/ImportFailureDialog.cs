//
// ImportFailureDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;

using Gtk;

using Mono.Unix;

using Hyena;
using FSpot.Utils;

namespace FSpot.UI.Dialog
{
	public class ImportFailureDialog : Gtk.Dialog
	{
		// FIXME: Replace with ErrorListDialog from Banshee when possible

        VBox inner_vbox;

        Label header_label;
        Hyena.Widgets.WrapLabel message_label;
        TreeView list_view;
        Expander details_expander;

        AccelGroup accel_group;
        protected AccelGroup AccelGroup {
            get { return accel_group; }
        }

		public ImportFailureDialog (List<SafeUri> files)
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

            var table = new Table (3, 2, false) {
                RowSpacing = 12,
                ColumnSpacing = 16
            };

            table.Attach (new Image {
                    IconName = "dialog-error",
                    IconSize = (int)IconSize.Dialog,
                    Yalign = 0.0f
                }, 0, 1, 0, 3, AttachOptions.Shrink, AttachOptions.Fill | AttachOptions.Expand, 0, 0);

            table.Attach (header_label = new Label { Xalign = 0.0f }, 1, 2, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);

            table.Attach (message_label = new Hyena.Widgets.WrapLabel (), 1, 2, 1, 2,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);

            var scrolled_window = new ScrolledWindow {
                HscrollbarPolicy = PolicyType.Automatic,
                VscrollbarPolicy = PolicyType.Automatic,
                ShadowType = ShadowType.In
            };

            list_view = new TreeView () {
                HeightRequest = 120,
                WidthRequest = 200
            };
            scrolled_window.Add (list_view);

            table.Attach (details_expander = new Expander (Catalog.GetString ("Details")),
                1, 2, 2, 3,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill | AttachOptions.Expand,
                0, 0);
            details_expander.Add (scrolled_window);

            VBox.PackStart (table, true, true, 0);
            VBox.Spacing = 12;
            VBox.ShowAll ();

            accel_group = new AccelGroup ();
            AddAccelGroup (accel_group);

            Button button = new Button (Stock.Close);
            button.CanDefault = true;
            button.UseStock = true;
            button.Show ();
			button.Clicked += (o, a) => {
				Destroy ();
			};

            AddActionWidget (button, ResponseType.Close);

			DefaultResponse = ResponseType.Close;
			button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Return, 0, AccelFlags.Visible);
		}

        public string Header {
            set {
                header_label.Markup = string.Format("<b><big>{0}</big></b>",
                    GLib.Markup.EscapeText(value));
            }
        }

        public string Message {
            set { message_label.Text = value; }
        }

        public TreeView ListView {
            get { return list_view; }
        }
	}
}
