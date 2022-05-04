//
// NullQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query.Gui
{
	public class NullQueryValueEntry : QueryValueEntry
	{
		protected NullQueryValue query_value;

		public NullQueryValueEntry () : base ()
		{
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set { query_value = value as NullQueryValue; }
		}
	}
}
