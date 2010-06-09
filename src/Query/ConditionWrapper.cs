/*
 * ConditionWrapper.cs
 *
 * Author(s)
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Query
{
	public class ConditionWrapper : IQueryCondition
	{
		string condition;

		public ConditionWrapper (string condition)
		{
			this.condition = condition;
		}

		public string SqlClause ()
		{
			return condition;
		}
	}
}
