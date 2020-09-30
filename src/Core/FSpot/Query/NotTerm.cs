//  NotTerm.cs
//
//  Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public class NotTerm : LogicalTerm
	{
		public LogicalTerm Term { get; private set; }

		public NotTerm (LogicalTerm term)
		{
			Term = term;
		}

		public override string SqlClause ()
		{
			return $" NOT ({Term.SqlClause ()}) ";
		}
	}
}
