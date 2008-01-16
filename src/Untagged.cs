/*
 * Untagged.cs
 * 
 * Author(s)
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public class Untagged : IQueryCondition
	{
		public string SqlClause ()
		{
			return " photos.id NOT IN (SELECT DISTINCT photo_id FROM photo_tags) ";
		}
	}
}
