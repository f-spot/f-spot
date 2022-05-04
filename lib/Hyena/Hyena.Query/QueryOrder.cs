//
// QueryOrder.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	public class QueryOrder
	{
		public string Name { get; private set; }
		public string Label { get; private set; }
		public string OrderSql { get; private set; }
		public QueryField Field { get; private set; }
		public bool Ascending { get; private set; }

		public QueryOrder (string name, string label, string order_sql, QueryField field, bool asc)
		{
			Name = name;
			Label = label;
			OrderSql = order_sql;
			Field = field;
			Ascending = asc;
		}

		public string ToSql ()
		{
			return string.Format ("ORDER BY {0}", OrderSql);
		}
	}
}
