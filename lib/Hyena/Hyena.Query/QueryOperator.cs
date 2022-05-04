//
// QueryOperator.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	public class Operator : IAliasedObject
	{
		public string name;
		public string Name {
			get { return name; }
		}

		public string label;
		public string Label {
			get { return label ?? Name; }
			set { label = value; }
		}

		string[] aliases;
		public string[] Aliases {
			get { return aliases; }
		}

		public string PrimaryAlias {
			get { return aliases[0]; }
		}

		string sql_format;
		public string SqlFormat {
			get { return sql_format; }
		}

		// FIXME get rid of this
		bool is_not;
		public bool IsNot {
			get { return is_not; }
		}

		internal Operator (string name, string label, string sql_format, params string[] userOps) : this (name, label, sql_format, false, userOps)
		{
		}

		internal Operator (string name, string label, string sql_format, bool is_not, params string[] userOps)
		{
			this.name = name;
			this.label = label;
			this.sql_format = sql_format;
			aliases = userOps;
			this.is_not = is_not;
		}
	}
}
