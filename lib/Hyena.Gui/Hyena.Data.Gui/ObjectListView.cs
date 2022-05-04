//
// ObjectListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public class ObjectListView : ListView<object>
	{
		public ObjectListView () : base ()
		{
			ColumnController = new ColumnController ();
		}

		protected override void OnModelReloaded ()
		{
			ColumnController.Clear ();
			foreach (ColumnDescription column_description in Model.ColumnDescriptions) {
				ColumnController.Add (new Column (column_description));
			}
		}

		public new IObjectListModel Model {
			get { return (IObjectListModel)base.Model; }
		}
	}
}
