//
// ColumnHeaderCellTextAccessible.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
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
using Mono.Unix;

namespace Hyena.Data.Gui.Accessibility
{
    class ColumnHeaderCellTextAccessible: ColumnCellTextAccessible, Atk.ActionImplementor
    {
        private static string [] action_descriptions  = new string[] {"", Catalog.GetString ("open context menu")};
        private static string [] action_names_localized = new string[] {Catalog.GetString ("click"), Catalog.GetString ("menu")};

        private enum Actions {
            Click,
            Menu,
            Last
        };

        public ColumnHeaderCellTextAccessible (object bound_object, ColumnHeaderCellText cell, ICellAccessibleParent parent)
            : base (bound_object, cell as ColumnCellText, parent)
        {
            Role = Atk.Role.TableColumnHeader;
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            Atk.StateSet states = base.OnRefStateSet ();
            states.RemoveState (Atk.StateType.Selectable);
            states.RemoveState (Atk.StateType.Transient);
            return states;
        }

        public string GetLocalizedName (int action)
        {
            if (action >= action_names_localized.Length)
                return "";

            return action_names_localized[action];
        }

        public string GetName (int action)
        {
            if (action >= (int)Actions.Last)
                return "";

            return ((Actions)action).ToString ().ToLower ();
        }

        public string GetDescription (int action)
        {
            if (action >= action_descriptions.Length)
                return "";

            return action_descriptions[action];
        }

        public string GetKeybinding (int action)
        {
            return "";
        }

        public int NActions {
            get { return (int)Actions.Last; }
        }

        public bool DoAction (int action)
        {
            ICellAccessibleParent parent = (ICellAccessibleParent)Parent;
            switch ((Actions)action) {
                case Actions.Menu: parent.InvokeColumnHeaderMenu (this); break;
                case Actions.Click: parent.ClickColumnHeader (this); break;
            }

            if (action == (int)Actions.Menu) {
                ((ICellAccessibleParent)Parent).InvokeColumnHeaderMenu (this);
            }

            return true;
        }

        public bool SetDescription (int action, string description)
        {
            return false;
        }
    }
}
