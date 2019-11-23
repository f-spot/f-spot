//
// ActionManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Gtk;
using Action = Gtk.Action;

using Hyena;

namespace Hyena.Gui
{
    public class ActionManager
    {
        private UIManager ui_manager;
        private Dictionary<string, ActionGroup> action_groups = new Dictionary<string, ActionGroup> ();

        public ActionManager ()
        {
            ui_manager = new UIManager ();
        }

        public virtual void Initialize ()
        {
        }

        private void InnerAddActionGroup (ActionGroup group)
        {
            action_groups.Add (group.Name, group);
            ui_manager.InsertActionGroup (group, 0);
        }

        public void AddActionGroup (string name)
        {
            lock (this) {
                if (action_groups.ContainsKey (name)) {
                    throw new ApplicationException ("Group already exists");
                }

                InnerAddActionGroup (new ActionGroup (name));
            }
        }

        public void AddActionGroup (ActionGroup group)
        {
            lock (this) {
                if (action_groups.ContainsKey (group.Name)) {
                    throw new ApplicationException ("Group already exists");
                }

                InnerAddActionGroup (group);
            }
        }

        public void RemoveActionGroup (string name)
        {
            lock (this) {
                if (action_groups.ContainsKey (name)) {
                    ActionGroup group = action_groups[name];
                    ui_manager.RemoveActionGroup (group);
                    action_groups.Remove (name);
                }
            }
        }

        public void RemoveActionGroup (ActionGroup group)
        {
            RemoveActionGroup (group.Name);
        }

        public ActionGroup FindActionGroup (string actionGroupId)
        {
            foreach (ActionGroup group in action_groups.Values) {
                if (group.Name == actionGroupId) {
                    return group;
                }
            }

            return null;
        }

        public Gtk.Action FindAction (string actionId)
        {
            string [] parts = actionId.Split ('.');

            if (parts == null || parts.Length < 2) {
                return null;
            }

            string group_name = parts[0];
            string action_name = parts[1];

            ActionGroup group = FindActionGroup (group_name);
            return group == null ? null : group.GetAction (action_name);
        }

        public void PopulateToolbarPlaceholder (Toolbar toolbar, string path, Widget item)
        {
            PopulateToolbarPlaceholder (toolbar, path, item, false);
        }

        public void PopulateToolbarPlaceholder (Toolbar toolbar, string path, Widget item, bool expand)
        {
            ToolItem placeholder = (ToolItem)UIManager.GetWidget (path);
            int position = toolbar.GetItemIndex (placeholder);
            toolbar.Remove (placeholder);

            if (item is ToolItem) {
                ((ToolItem)item).Expand = expand;
                toolbar.Insert ((ToolItem)item, position);
            } else {
                ToolItem container_item = new Hyena.Widgets.GenericToolItem<Widget> (item);
                container_item.Expand = expand;
                container_item.Show ();
                container_item.ToolbarReconfigured += (o, a) => {
                    SetItemSize (container_item, container_item.IconSize);
                };
                toolbar.Insert (container_item, position);
            }
        }

        private void SetItemSize (Gtk.Widget widget, Gtk.IconSize size)
        {
            var container = widget as Container;
            if (container != null) {
                foreach (var child in container.Children) {
                    SetItemSize (child, size);
                }
            }

            var img = widget as Image;
            if (img != null) {
                img.IconSize = (int)size;
            }
        }

        public uint AddUiFromFileInCurrentAssembly (string ui_file)
        {
            return AddUiFromFile (ui_file, Assembly.GetCallingAssembly ());
        }

        public uint AddUiFromFile (string ui_file, Assembly assembly)
        {
            if (ui_file != null) {
                using (StreamReader reader = new StreamReader (assembly.GetManifestResourceStream (ui_file))) {
                    return ui_manager.AddUiFromString (reader.ReadToEnd ());
                }
            }
            return 0;
        }

        public Gtk.Action this[string actionId] {
            get { return FindAction (actionId); }
        }

        public UIManager UIManager {
            get { return ui_manager; }
        }
    }
}
