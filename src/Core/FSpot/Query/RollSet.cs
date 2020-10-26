//
// RollSet.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.con>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using FSpot.Core;

namespace FSpot.Query
{
	public class RollSet : IQueryCondition
	{
		readonly Roll[] rolls;

		public RollSet (Roll[] rolls)
		{
			this.rolls = rolls;
		}

		public RollSet (Roll roll) : this (new Roll[] { roll })
		{
		}

		public string SqlClause ()
		{
			//Building something like " photos.roll_id IN (3, 4, 7) " 
			var sb = new StringBuilder (" photos.roll_id IN (");
			for (int i = 0; i < rolls.Length; i++) {
				sb.Append (rolls[i].Id);
				if (i != rolls.Length - 1)
					sb.Append (", ");
			}
			sb.Append (") ");
			return sb.ToString ();
		}
	}
}
