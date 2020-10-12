//
// UntaggedCondition.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.con>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public class UntaggedCondition : IQueryCondition
	{
		public string SqlClause ()
		{
			return " photos.id NOT IN (SELECT DISTINCT photo_id FROM photo_tags) ";
		}
	}
}
