/*
 * IQueryCondition.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public interface IQueryCondition
	{
		string SqlClause ();
	}
}
