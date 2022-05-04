//
// ExactStringQueryValue.cs
//
// Authors:
//   John Millikin <jmillikin@gmail.com>
//
// Copyright (C) 2009 John Millikin
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	// A query value that requires the string match exactly
	public class ExactStringQueryValue : StringQueryValue
	{
		public override string ToSql (Operator op)
		{
			return string.IsNullOrEmpty (value) ? null : EscapeString (op, value.ToLower ());
		}
	}
}
