//
// DatabaseColumn.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Hyena.Data.Sqlite
{
	public abstract class AbstractDatabaseColumn
	{
		readonly FieldInfo field_info;
		readonly PropertyInfo property_info;
		readonly Type type;
		readonly string column_type;
		readonly string name;

		protected AbstractDatabaseColumn (FieldInfo field_info, AbstractDatabaseColumnAttribute attribute)
			: this (attribute, field_info, field_info.FieldType)
		{
			this.field_info = field_info;
		}

		protected AbstractDatabaseColumn (PropertyInfo property_info, AbstractDatabaseColumnAttribute attribute) :
			this (attribute, property_info, property_info.PropertyType)
		{
			if (!property_info.CanRead || (attribute.Select && !property_info.CanWrite)) {
				throw new Exception (string.Format (
					"{0}: The property {1} must have both a get and a set " +
					"block in order to be bound to a database column.",
					property_info.DeclaringType,
					property_info.Name)
				);
			}
			this.property_info = property_info;
		}

		AbstractDatabaseColumn (AbstractDatabaseColumnAttribute attribute, MemberInfo member_info, Type type)
		{
			try {
				column_type = SqliteUtils.GetType (type);
			} catch (Exception e) {
				throw new Exception (string.Format (
					"{0}.{1}: {2}", member_info.DeclaringType, member_info.Name, e.Message));
			}
			name = attribute.ColumnName ?? member_info.Name;
			this.type = type;
		}

		public object GetRawValue (object target)
		{
			return field_info != null ? field_info.GetValue (target) : property_info.GetValue (target, null);
		}

		public object GetValue (object target)
		{
			object result = GetRawValue (target);
			return SqliteUtils.ToDbFormat (type, result);
		}

		public void SetValue (object target, object value)
		{
			if (field_info != null) {
				field_info.SetValue (target, value);
			} else {
				property_info.SetValue (target, value, null);
			}
		}

		public string Name { get { return name; } }
		public string Type { get { return column_type; } }
		public Type ValueType { get { return type; } }
	}

	public sealed class DatabaseColumn : AbstractDatabaseColumn
	{
		DatabaseColumnAttribute attribute;

		public DatabaseColumn (FieldInfo field_info, DatabaseColumnAttribute attribute)
			: base (field_info, attribute)
		{
			this.attribute = attribute;
		}

		public DatabaseColumn (PropertyInfo property_info, DatabaseColumnAttribute attribute)
			: base (property_info, attribute)
		{
			this.attribute = attribute;
		}

		public DatabaseColumnConstraints Constraints {
			get { return attribute.Constraints; }
		}

		public string DefaultValue {
			get { return attribute.DefaultValue; }
		}

		public string Index {
			get { return attribute.Index; }
		}

		public string Schema {
			get {
				return SqliteUtils.BuildColumnSchema (Type, Name, attribute.DefaultValue, attribute.Constraints);
			}
		}

		public override bool Equals (object o)
		{
			var column = o as DatabaseColumn;
			return o != null && column.Name.Equals (Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
	}

	sealed class VirtualDatabaseColumn : AbstractDatabaseColumn
	{
		VirtualDatabaseColumnAttribute attribute;

		public VirtualDatabaseColumn (FieldInfo field_info, VirtualDatabaseColumnAttribute attribute)
			: base (field_info, attribute)
		{
			this.attribute = attribute;
		}

		public VirtualDatabaseColumn (PropertyInfo property_info, VirtualDatabaseColumnAttribute attribute)
			: base (property_info, attribute)
		{
			this.attribute = attribute;
		}

		public string TargetTable {
			get { return attribute.TargetTable; }
		}

		public string LocalKey {
			get { return attribute.LocalKey; }
		}

		public string ForeignKey {
			get { return attribute.ForeignKey; }
		}
	}

	public struct DbColumn
	{
		public readonly string Name;
		public readonly DatabaseColumnConstraints Constraints;
		public readonly string DefaultValue;

		public DbColumn (string name, DatabaseColumnConstraints constraints, string default_value)
		{
			Name = name;
			Constraints = constraints;
			DefaultValue = default_value;
		}
	}
}
