/*
 * IQueryCondition.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Query
{
	public interface IQueryCondition
	{
		string SqlClause ();
	}
}
