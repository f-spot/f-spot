//
// QueryField.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Hyena.Query
{
	public class QueryField : IAliasedObject
	{
		bool no_custom_format;
		bool column_lowered;

		Type[] value_types;
		public Type[] ValueTypes {
			get { return value_types; }
		}

		string name;
		public string Name {
			get { return name; }
			set { name = value; }
		}

		string property_name;
		public string PropertyName {
			get { return property_name; }
			set { property_name = value; }
		}

		string label;
		public string Label {
			get { return label; }
			set { label = value; }
		}

		string short_label;
		public string ShortLabel {
			get { return short_label ?? label; }
			set { short_label = value; }
		}

		string[] aliases;
		public string[] Aliases {
			get { return aliases; }
		}

		public string PrimaryAlias {
			get { return aliases[0]; }
		}

		string column;
		public string Column {
			get { return column; }
		}

		bool is_default;
		public bool IsDefault {
			get { return is_default; }
		}

		public QueryField (string name, string propertyName, string label, string column, params string[] aliases)
			: this (name, propertyName, label, column, false, aliases)
		{
		}

		public QueryField (string name, string propertyName, string label, string column, bool isDefault, params string[] aliases)
			: this (name, propertyName, label, column, new Type[] { typeof (StringQueryValue) }, isDefault, aliases)
		{
		}

		public QueryField (string name, string propertyName, string label, string column, Type valueType, params string[] aliases)
			: this (name, propertyName, label, column, new Type[] { valueType }, false, aliases)
		{
		}

		public QueryField (string name, string propertyName, string label, string column, Type[] valueTypes, params string[] aliases)
			: this (name, propertyName, label, column, valueTypes, false, aliases)
		{
		}

		public QueryField (string name, string propertyName, string label, string column, Type[] valueTypes, bool isDefault, params string[] aliases)
		{
			this.name = name;
			property_name = propertyName;
			this.label = label;
			this.column = column;
			value_types = valueTypes;
			is_default = isDefault;
			this.aliases = aliases;

			no_custom_format = (Column.IndexOf ("{0}") == -1 && Column.IndexOf ("{1}") == -1);
			column_lowered = (Column.IndexOf ("Lowered") != -1);

			if (!no_custom_format) {
				// Ensure we have parens around any custom 'columns' that may be an OR of two columns
				this.column = string.Format ("({0})", this.column);
			}

			foreach (Type value_type in valueTypes) {
				QueryValue.AddValueType (value_type);
			}
		}

		public IEnumerable<QueryValue> CreateQueryValues ()
		{
			foreach (Type type in ValueTypes) {
				yield return Activator.CreateInstance (type) as QueryValue;
			}
		}

		public string ToTermString (string op, string value)
		{
			return ToTermString (PrimaryAlias, op, value);
		}

		public string ToSql (Operator op, QueryValue qv)
		{
			string value = qv.ToSql (op) ?? string.Empty;

			if (op == null) op = qv.OperatorSet.First;

			var sb = new StringBuilder ();

			if (no_custom_format) {
				string column_with_key = Column;
				if (qv is StringQueryValue && !(column_lowered || qv is ExactStringQueryValue)) {
					column_with_key = string.Format ("HYENA_SEARCH_KEY({0})", Column);
				}
				sb.AppendFormat ("{0} {1}", column_with_key, string.Format (op.SqlFormat, value));

				if (op.IsNot) {
					return string.Format ("({0} IS NULL OR {1})", Column, sb.ToString ());
				} else {
					return string.Format ("({0} IS NOT NULL AND {1})", Column, sb.ToString ());
				}
			} else {
				sb.AppendFormat (
					Column, string.Format (op.SqlFormat, value),
					value, op.IsNot ? "NOT" : null
				);
			}

			return sb.ToString ();
		}

		public static string ToTermString (string alias, string op, string value)
		{
			if (!string.IsNullOrEmpty (value)) {
				value = string.Format (
					"{1}{0}{1}",
					value, value.IndexOf (" ") == -1 ? string.Empty : "\""
				);
			} else {
				value = string.Empty;
			}
			return string.IsNullOrEmpty (alias)
				? value
				: string.Format ("{0}{1}{2}", alias, op, value);
		}
	}
}
