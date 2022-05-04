//
// QueryFieldSet.cs
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
	public class QueryFieldSet : AliasedObjectSet<QueryField>
	{
		public QueryFieldSet (params QueryField[] fields) : base (fields)
		{
		}

		public QueryField[] Fields {
			get { return Objects; }
		}
	}
}
