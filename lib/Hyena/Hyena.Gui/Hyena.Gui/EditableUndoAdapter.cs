//
// EditableUndoAdapter.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Hyena;

namespace Hyena.Gui
{
    public class EditableUndoAdapter<T> where T : Widget, Editable
    {
        private T editable;
        private UndoManager undo_manager = new UndoManager ();
        private AccelGroup accel_group = new AccelGroup ();
        private EventInfo popup_event_info;
        private Delegate populate_popup_handler;

        public EditableUndoAdapter (T editable)
        {
            this.editable = editable;
            popup_event_info = editable.GetType ().GetEvent ("PopulatePopup");
            if (popup_event_info != null) {
                populate_popup_handler = new PopulatePopupHandler (OnPopulatePopup);
            }
        }

        public void Connect ()
        {
            editable.KeyPressEvent += OnKeyPressEvent;
            editable.TextDeleted += OnTextDeleted;
            editable.TextInserted += OnTextInserted;
            TogglePopupConnection (true);
        }

        public void Disconnect ()
        {
            editable.KeyPressEvent -= OnKeyPressEvent;
            editable.TextDeleted -= OnTextDeleted;
            editable.TextInserted -= OnTextInserted;
            TogglePopupConnection (false);
        }

        private void TogglePopupConnection (bool connect)
        {
            // Ugh, stupid Gtk+/Gtk# and lack of interfaces
            if (popup_event_info != null && populate_popup_handler != null) {
                if (connect) {
                    popup_event_info.AddEventHandler (editable, populate_popup_handler);
                } else {
                    popup_event_info.RemoveEventHandler (editable, populate_popup_handler);
                }
            }
        }

        private void OnKeyPressEvent (object o, KeyPressEventArgs args)
        {
            if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                switch (args.Event.Key) {
                    case Gdk.Key.z:
                        undo_manager.Undo ();
                        args.RetVal = true;
                        break;
                    case Gdk.Key.Z:
                    case Gdk.Key.y:
                        undo_manager.Redo ();
                        args.RetVal = true;
                        break;
                }
            }

            args.RetVal = false;
        }

        [GLib.ConnectBefore]
        private void OnTextDeleted (object o, TextDeletedArgs args)
        {
            if (args.StartPos != args.EndPos) {
                undo_manager.AddUndoAction (new EditableEraseAction (editable, args.StartPos, args.EndPos));
            }
        }

        [GLib.ConnectBefore]
        private void OnTextInserted (object o, TextInsertedArgs args)
        {
            undo_manager.AddUndoAction (new EditableInsertAction (editable, args.Position, args.Text, args.Length));
        }

        private void OnPopulatePopup (object o, PopulatePopupArgs args)
        {
            Menu menu = args.Menu;
            MenuItem item;

            item = new SeparatorMenuItem ();
            item.Show ();
            menu.Prepend (item);

            item = new ImageMenuItem (Stock.Redo, null);
            item.Sensitive = undo_manager.CanRedo;
            item.Activated += delegate { undo_manager.Redo (); };
            item.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.z,
                Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask,
                AccelFlags.Visible);
            item.Show ();
            menu.Prepend (item);

            item = new ImageMenuItem (Stock.Undo, null);
            item.Sensitive = undo_manager.CanUndo;
            item.Activated += delegate { undo_manager.Undo (); };
            item.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.z,
                Gdk.ModifierType.ControlMask, AccelFlags.Visible);
            item.Show ();
            menu.Prepend (item);
        }

        public UndoManager UndoManager {
            get { return undo_manager; }
        }
    }
}
