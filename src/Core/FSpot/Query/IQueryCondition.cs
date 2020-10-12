//
// IQueryCondition.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.con>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public interface IQueryCondition
	{
		string SqlClause ();
	}
}
