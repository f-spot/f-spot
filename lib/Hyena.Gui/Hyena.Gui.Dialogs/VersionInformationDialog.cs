//
// VersionInformationDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using Gtk;
using Mono.Unix;

namespace Hyena.Gui.Dialogs
{
    public class VersionInformationDialog : Dialog
    {
        private Label path_label;
        private TreeView version_tree;
        private TreeStore version_store;

        public VersionInformationDialog() : base()
        {
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);
            Modal = true;

            Button button = new Button("gtk-close");
            button.CanDefault = true;
            button.UseStock = true;
            button.Show();
            DefaultResponse = ResponseType.Close;
            button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Escape,
                0, Gtk.AccelFlags.Visible);

            AddActionWidget(button, ResponseType.Close);

            Title = Catalog.GetString("Assembly Version Information");
            BorderWidth = 10;

            version_tree = new TreeView();

            version_tree.RulesHint = true;
            version_tree.AppendColumn(Catalog.GetString("Assembly Name"),
                new CellRendererText(), "text", 0);
            version_tree.AppendColumn(Catalog.GetString("Version"),
                new CellRendererText(), "text", 1);

            version_tree.Model = FillStore();
            version_tree.CursorChanged += OnCursorChanged;

            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(version_tree);
            scroll.ShadowType = ShadowType.In;
            scroll.SetSizeRequest(420, 200);

            VBox.PackStart(scroll, true, true, 0);
            VBox.Spacing = 5;

            path_label = new Label();
            path_label.Ellipsize = Pango.EllipsizeMode.End;
            path_label.Hide();
            path_label.Xalign = 0.0f;
            path_label.Yalign = 1.0f;
            VBox.PackStart(path_label, false, true, 0);

            scroll.ShowAll();
        }

        private void OnCursorChanged(object o, EventArgs args)
        {
            TreeIter iter;

            if(!version_tree.Selection.GetSelected(out iter)) {
                path_label.Hide();
                return;
            }

            object path = version_store.GetValue(iter, 2);

            if(path == null) {
                path_label.Hide();
                return;
            }

            path_label.Text = path as string;
            path_label.Show();
        }

        private TreeStore FillStore()
        {
            version_store = new TreeStore(typeof(string),
                typeof(string), typeof(string));

            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                string loc;
                AssemblyName name = asm.GetName();

                try {
                    loc = System.IO.Path.GetFullPath(asm.Location);
                } catch(Exception) {
                    loc = "dynamic";
                }

                version_store.AppendValues(name.Name, name.Version.ToString(), loc);
            }

            version_store.SetSortColumnId(0, SortType.Ascending);
            return version_store;
        }
    }
}
