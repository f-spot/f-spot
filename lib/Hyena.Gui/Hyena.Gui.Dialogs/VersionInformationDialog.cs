//
// VersionInformationDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Gui.Dialogs
{
	public class VersionInformationDialog : Dialog
	{
		Label path_label;
		TreeView version_tree;
		TreeStore version_store;

		public VersionInformationDialog () : base ()
		{
			var accel_group = new AccelGroup ();
			AddAccelGroup (accel_group);
			Modal = true;

			var button = new Button ("gtk-close");
			button.CanDefault = true;
			button.UseStock = true;
			button.Show ();
			DefaultResponse = ResponseType.Close;
			button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Escape,
				0, Gtk.AccelFlags.Visible);

			AddActionWidget (button, ResponseType.Close);

			Title = Strings.AssemblyVersionInformation;
			BorderWidth = 10;

			version_tree = new TreeView ();

			version_tree.RulesHint = true;
			version_tree.AppendColumn (Strings.AssemblyName, new CellRendererText (), "text", 0);
			version_tree.AppendColumn (Strings.Version, new CellRendererText (), "text", 1);

			version_tree.Model = FillStore ();
			version_tree.CursorChanged += OnCursorChanged;

			var scroll = new ScrolledWindow ();
			scroll.Add (version_tree);
			scroll.ShadowType = ShadowType.In;
			scroll.SetSizeRequest (420, 200);

			VBox.PackStart (scroll, true, true, 0);
			VBox.Spacing = 5;

			path_label = new Label ();
			path_label.Ellipsize = Pango.EllipsizeMode.End;
			path_label.Hide ();
			path_label.Xalign = 0.0f;
			path_label.Yalign = 1.0f;
			VBox.PackStart (path_label, false, true, 0);

			scroll.ShowAll ();
		}

		void OnCursorChanged (object o, EventArgs args)
		{

			if (!version_tree.Selection.GetSelected (out var iter)) {
				path_label.Hide ();
				return;
			}

			object path = version_store.GetValue (iter, 2);

			if (path == null) {
				path_label.Hide ();
				return;
			}

			path_label.Text = path as string;
			path_label.Show ();
		}

		TreeStore FillStore ()
		{
			version_store = new TreeStore (typeof (string),
				typeof (string), typeof (string));

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				string loc;
				AssemblyName name = asm.GetName ();

				try {
					loc = System.IO.Path.GetFullPath (asm.Location);
				} catch (Exception) {
					loc = "dynamic";
				}

				version_store.AppendValues (name.Name, name.Version.ToString (), loc);
			}

			version_store.SetSortColumnId (0, SortType.Ascending);
			return version_store;
		}
	}
}
