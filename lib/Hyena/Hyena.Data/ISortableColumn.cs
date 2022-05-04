//
// ISortableColumn.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public interface ISortableColumn
	{
		string SortKey { get; }
		SortType SortType { get; set; }
		Hyena.Query.QueryField Field { get; }
		string Id { get; }
	}
}
