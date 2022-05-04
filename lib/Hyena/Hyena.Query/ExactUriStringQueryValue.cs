//
// ExactUriStringQueryValue.cs
//
// Authors:
//   Andrés G. Aragoneses <knocte@gmail.com>
//
// Copyright (C) 2010-2011 Andrés G. Aragoneses
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Query
{
	public class ExactUriStringQueryValue : ExactStringQueryValue
	{
		public override string ToSql (Operator op)
		{
			if (string.IsNullOrEmpty (value)) {
				return null;
			}

			string escaped_value = EscapeString (op, Uri.EscapeUriString (value.ToLower ()));
			if (op == StartsWith) {
				return "file://" + escaped_value;
			}
			return escaped_value;
		}
	}
}
