//
// ISortable.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public interface ISortable
	{
		bool Sort (ISortableColumn column);
		ISortableColumn SortColumn { get; }
	}
}
