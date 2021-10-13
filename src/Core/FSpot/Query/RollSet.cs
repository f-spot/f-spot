//
// RollSet.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.con>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;

using FSpot.Models;

namespace FSpot.Query
{
	public class RollSet : IQueryCondition
	{
		readonly List<Roll> rolls;

		public RollSet (List<Roll> rolls)
		{
			this.rolls = rolls;
		}

		public RollSet (Roll roll) : this (new List<Roll> {roll})
		{
		}

		public string SqlClause ()
		{
			//Building something like " photos.roll_id IN (3, 4, 7) " 
			System.Text.StringBuilder sb = new System.Text.StringBuilder (" photos.roll_id IN (");
			for (int i = 0; i < rolls.Count; i++) {
				sb.Append (rolls [i].Id);
				if (i != rolls.Count - 1)
					sb.Append (", ");
			}
			sb.Append (") ");
			return sb.ToString ();	
		}
	}
}
