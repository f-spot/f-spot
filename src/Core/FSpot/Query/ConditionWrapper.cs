//
// ConditionWrapper.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public class ConditionWrapper : IQueryCondition
	{
		readonly string condition;

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
