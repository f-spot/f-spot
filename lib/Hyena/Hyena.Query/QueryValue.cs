//
// QueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Xml;

namespace Hyena.Query
{
	public abstract class QueryValue
	{
		static List<Type> subtypes = new List<Type> ();
		public static void AddValueType (Type type)
		{
			if (!subtypes.Contains (type)) {
				subtypes.Add (type);
			}
		}

		public static QueryValue CreateFromUserQuery (string input, QueryField field)
		{
			if (field == null) {
				QueryValue val = new StringQueryValue ();
				val.ParseUserQuery (input);
				return val;
			} else {
				foreach (QueryValue val in field.CreateQueryValues ()) {
					val.ParseUserQuery (input);
					if (!val.IsEmpty) {
						return val;
					}
				}
			}

			return null;
		}

		public static QueryValue CreateFromStringValue (string input, QueryField field)
		{
			if (field == null) {
				QueryValue val = new StringQueryValue ();
				val.LoadString (input);
				return val;
			} else {
				foreach (QueryValue val in field.CreateQueryValues ()) {
					val.LoadString (input);
					if (!val.IsEmpty) {
						return val;
					}
				}
			}

			return null;
		}

		public static QueryValue CreateFromXml (XmlElement parent, QueryField field)
		{
			if (field != null) {
				foreach (QueryValue val in field.CreateQueryValues ()) {
					if (CreateFromXml (val, parent)) {
						return val;
					}
				}
				return null;
			} else {
				foreach (Type subtype in subtypes) {
					var val = Activator.CreateInstance (subtype) as QueryValue;
					if (CreateFromXml (val, parent)) {
						return val;
					}
				}
			}
			return null;
		}

		static bool CreateFromXml (QueryValue val, XmlElement parent)
		{
			XmlElement val_node = parent[val.XmlElementName];
			if (val_node != null) {
				val.ParseXml (val_node);
				return !val.IsEmpty;
			}
			return false;
		}

		bool is_empty = true;
		public bool IsEmpty {
			get { return is_empty; }
			protected set { is_empty = value; }
		}

		public abstract object Value { get; }
		public abstract string XmlElementName { get; }
		public abstract AliasedObjectSet<Operator> OperatorSet { get; }

		public abstract void LoadString (string input);
		public abstract void ParseXml (XmlElement node);

		public virtual void AppendXml (XmlElement node)
		{
			node.InnerText = Value.ToString ();
		}

		public abstract void ParseUserQuery (string input);

		public virtual string ToUserQuery ()
		{
			return Value.ToString ();
		}

		public override string ToString ()
		{
			return Value.ToString ();
		}

		public string ToSql ()
		{
			return ToSql (null);
		}

		public abstract string ToSql (Operator op);
	}
}
