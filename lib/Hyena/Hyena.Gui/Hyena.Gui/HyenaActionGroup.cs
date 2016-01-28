//
// HyenaActionGroup.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;

using Gtk;

namespace Hyena.Gui
{
    public class HyenaActionGroup : ActionGroup
    {
        private List<uint> ui_merge_ids = new List<uint> ();
        private ActionManager action_manager;

        private bool important_by_default = true;
        protected bool ImportantByDefault {
            get { return important_by_default; }
            set { important_by_default = value; }
        }

        public HyenaActionGroup (ActionManager action_manager, string name) : base (name)
        {
            this.action_manager = action_manager;
        }

        public void AddUiFromFile (string ui_file)
        {
            Hyena.ThreadAssist.AssertInMainThread ();
            ui_merge_ids.Add (ActionManager.AddUiFromFile (ui_file, System.Reflection.Assembly.GetCallingAssembly ()));
        }

        public void AddUiFromString (string ui_string)
        {
            Hyena.ThreadAssist.AssertInMainThread ();
            ui_merge_ids.Add (ActionManager.UIManager.AddUiFromString (ui_string));
        }

        public void Register ()
        {
            if (ActionManager.FindActionGroup (this.Name) == null) {
                ActionManager.AddActionGroup (this);
            }
        }

        public void UnRegister ()
        {
            if (ActionManager.FindActionGroup (this.Name) != null) {
                ActionManager.RemoveActionGroup (this);
            }
        }

        public override void Dispose ()
        {
            Hyena.ThreadAssist.ProxyToMain (delegate {
                UnRegister ();

                foreach (uint merge_id in ui_merge_ids) {
                    if (merge_id > 0) {
                        ActionManager.UIManager.RemoveUi (merge_id);
                    }
                }
                ui_merge_ids.Clear ();

                base.Dispose ();
            });
        }

        public new void Add (params ActionEntry [] action_entries)
        {
            if (ImportantByDefault) {
                AddImportant (action_entries);
            } else {
                base.Add (action_entries);
            }
        }

        public void AddImportant (params ActionEntry [] action_entries)
        {
            base.Add (action_entries);

            foreach (ActionEntry entry in action_entries) {
                this[entry.name].IsImportant = true;
            }
        }

        public void AddImportant (params ToggleActionEntry [] action_entries)
        {
            base.Add (action_entries);

            foreach (ToggleActionEntry entry in action_entries) {
                this[entry.name].IsImportant = true;
            }
        }

        public void Remove (string actionName)
        {
            Gtk.Action action = this[actionName];
            if (action != null) {
                Remove (action);
            }
        }

        public void UpdateActions (bool visible, bool sensitive, params string [] action_names)
        {
            foreach (string name in action_names) {
                UpdateAction (this[name], visible, sensitive);
            }
        }

        public void UpdateAction (string action_name, bool visible_and_sensitive)
        {
            UpdateAction (this[action_name], visible_and_sensitive, visible_and_sensitive);
        }

        public void UpdateAction (string action_name, bool visible, bool sensitive)
        {
            UpdateAction (this[action_name], visible, sensitive);
        }

        public static void UpdateAction (Gtk.Action action, bool visible_and_sensitive)
        {
            UpdateAction (action, visible_and_sensitive, visible_and_sensitive);
        }

        public static void UpdateAction (Gtk.Action action, bool visible, bool sensitive)
        {
            action.Visible = visible;
            action.Sensitive = visible && sensitive;
        }

        protected void ShowContextMenu (string menu_name)
        {
            Gtk.Menu menu = ActionManager.UIManager.GetWidget (menu_name) as Menu;
            if (menu == null || menu.Children.Length == 0) {
                return;
            }

            int visible_children = 0;
            foreach (Widget child in menu)
                if (child.Visible)
                    visible_children++;

            if (visible_children == 0) {
                return;
            }

            menu.Show ();
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
        }

        public ActionManager ActionManager {
            get { return action_manager; }
        }
    }
}
