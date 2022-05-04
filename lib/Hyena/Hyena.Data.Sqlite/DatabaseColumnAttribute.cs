//
// DatabaseColumnAttribute.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data.Sqlite
{
	[Flags]
	public enum DatabaseColumnConstraints
	{
		NotNull = 1,
		PrimaryKey = 2,
		Unique = 4
	}

	public abstract class AbstractDatabaseColumnAttribute : Attribute
	{
		string column_name;
		bool select = true;

		public AbstractDatabaseColumnAttribute ()
		{
		}

		public AbstractDatabaseColumnAttribute (string column_name)
		{
			this.column_name = column_name;
		}

		public string ColumnName {
			get { return column_name; }
		}

		public bool Select {
			get { return select; }
			set { select = value; }
		}
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class DatabaseColumnAttribute : AbstractDatabaseColumnAttribute
	{
		DatabaseColumnConstraints contraints;
		string default_value;
		string index;

		public DatabaseColumnAttribute ()
		{
		}

		public DatabaseColumnAttribute (string column_name) : base (column_name)
		{
		}

		public DatabaseColumnConstraints Constraints {
			get { return contraints; }
			set { contraints = value; }
		}

		public string DefaultValue {
			get { return default_value; }
			set { default_value = value; }
		}

		public string Index {
			get { return index; }
			set { index = value; }
		}
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class VirtualDatabaseColumnAttribute : AbstractDatabaseColumnAttribute
	{
		string target_table;
		string local_key;
		string foreign_key;

		public VirtualDatabaseColumnAttribute (string column_name, string target_table, string local_key, string foreign_key)
			: base (column_name)
		{
			this.target_table = target_table;
			this.local_key = local_key;
			this.foreign_key = foreign_key;
		}

		public string TargetTable {
			get { return target_table; }
		}

		public string LocalKey {
			get { return local_key; }
		}

		public string ForeignKey {
			get { return foreign_key; }
		}
	}
}
