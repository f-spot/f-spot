//
// QueryLimit.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	public class QueryLimit
	{
		string name;
		public string Name {
			get { return name; }
		}

		string label;
		public string Label {
			get { return label; }
			set { label = value; }
		}

		bool row_based;
		public bool RowBased {
			get { return row_based; }
		}

		int factor = 1;
		public int Factor {
			get { return factor; }
		}

		string column;
		public string Column {
			get { return column; }
		}

		public QueryLimit (string name, string label, string column, int factor) : this (name, label, false)
		{
			this.column = column;
			this.factor = factor;
		}

		public QueryLimit (string name, string label, bool row_based)
		{
			this.name = name;
			this.label = label;
			this.row_based = row_based;
		}

		public string ToSql (IntegerQueryValue limit_value)
		{
			return RowBased ? string.Format ("LIMIT {0}", limit_value.ToSql ()) : null;
		}
	}
}
