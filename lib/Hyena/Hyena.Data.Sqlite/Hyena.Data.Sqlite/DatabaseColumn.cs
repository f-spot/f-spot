//
// DatabaseColumn.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Text;

namespace Hyena.Data.Sqlite
{
    public abstract class AbstractDatabaseColumn
    {
        private readonly FieldInfo field_info;
        private readonly PropertyInfo property_info;
        private readonly Type type;
        private readonly string column_type;
        private readonly string name;

        protected AbstractDatabaseColumn (FieldInfo field_info, AbstractDatabaseColumnAttribute attribute)
            : this (attribute, field_info, field_info.FieldType)
        {
            this.field_info = field_info;
        }

        protected AbstractDatabaseColumn (PropertyInfo property_info, AbstractDatabaseColumnAttribute attribute) :
            this (attribute, property_info, property_info.PropertyType)
        {
            if (!property_info.CanRead || (attribute.Select && !property_info.CanWrite)) {
                throw new Exception (String.Format (
                    "{0}: The property {1} must have both a get and a set " +
                    "block in order to be bound to a database column.",
                    property_info.DeclaringType,
                    property_info.Name)
                );
            }
            this.property_info = property_info;
        }

        private AbstractDatabaseColumn (AbstractDatabaseColumnAttribute attribute, MemberInfo member_info, Type type)
        {
            try {
                column_type = SqliteUtils.GetType (type);
            } catch (Exception e) {
                throw new Exception(string.Format(
                    "{0}.{1}: {2}", member_info.DeclaringType, member_info.Name, e.Message));
            }
            this.name = attribute.ColumnName ?? member_info.Name;
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
        private DatabaseColumnAttribute attribute;

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
            DatabaseColumn column = o as DatabaseColumn;
            return o != null && column.Name.Equals (Name);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }
    }

    internal sealed class VirtualDatabaseColumn : AbstractDatabaseColumn
    {
        private VirtualDatabaseColumnAttribute attribute;

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

        public DbColumn(string name, DatabaseColumnConstraints constraints, string default_value)
        {
            Name = name;
            Constraints = constraints;
            DefaultValue = default_value;
        }
    }
}
