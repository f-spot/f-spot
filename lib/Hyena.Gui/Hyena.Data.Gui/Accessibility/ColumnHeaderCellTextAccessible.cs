//
// ColumnHeaderCellTextAccessible.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace Hyena.Data.Gui.Accessibility
{
	class ColumnHeaderCellTextAccessible : ColumnCellTextAccessible, Atk.ActionImplementor
	{
		static string[] action_descriptions = new string[] { "", Strings.OpenContextMenu };
		static string[] action_names_localized = new string[] { Strings.Click, Strings.Menu };

		enum Actions
		{
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
			var parent = (ICellAccessibleParent)Parent;
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
