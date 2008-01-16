/*
 * RollSet.cs
 *
 * Author(s):
 * 	Bengt Thuree
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is frees software. See COPYING for details.
 */

namespace FSpot.Query
{
	public class RollSet : IQueryCondition
	{
		private Roll [] rolls;

		public RollSet (Roll [] rolls)
		{
			this.rolls = rolls;
		}

		public RollSet (Roll roll) : this (new Roll[] {roll})
		{
		}

		public string SqlClause ()
		{
			//Building something like " photos.roll_id IN (3, 4, 7) " 
			System.Text.StringBuilder sb = new System.Text.StringBuilder (" photos.roll_id IN (");
			for (int i = 0; i < rolls.Length; i++) {
				sb.Append (rolls [i].Id);
				if (i != rolls.Length - 1)
					sb.Append (", ");
			}
			sb.Append (") ");
			return sb.ToString ();	
		}
	}
}
