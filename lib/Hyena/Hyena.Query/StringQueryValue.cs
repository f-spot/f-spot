//
// StringQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml;

using FSpot.Resources.Lang;

namespace Hyena.Query
{
	public class StringQueryValue : QueryValue
	{
		const string ESCAPE_CLAUSE = " ESCAPE '\\'";

		public static readonly Operator Contains = new Operator ("contains", Strings.Contains, "LIKE '%{0}%'" + ESCAPE_CLAUSE, ":");
		public static readonly Operator DoesNotContain = new Operator ("doesNotContain", Strings.DoesntContain, "NOT LIKE '%{0}%'" + ESCAPE_CLAUSE, true, "!:");
		public static readonly Operator Equal = new Operator ("equals", Strings.Is, "= '{0}'", "==");
		public static readonly Operator NotEqual = new Operator ("notEqual", Strings.IsNot, "!= '{0}'", true, "!=");
		public static readonly Operator StartsWith = new Operator ("startsWith", Strings.StartsWith, "LIKE '{0}%'" + ESCAPE_CLAUSE, "=");
		public static readonly Operator EndsWith = new Operator ("endsWith", Strings.EndsWith, "LIKE '%{0}'" + ESCAPE_CLAUSE, ":=");

		protected string value;

		public override string XmlElementName {
			get { return "string"; }
		}

		public override object Value {
			get { return value; }
		}

		protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Contains, DoesNotContain, Equal, NotEqual, StartsWith, EndsWith);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		public override void ParseUserQuery (string input)
		{
			value = input;
			IsEmpty = string.IsNullOrEmpty (value);
		}

		public override void ParseXml (XmlElement node)
		{
			value = node.InnerText;
			IsEmpty = string.IsNullOrEmpty (value);
		}

		public override void LoadString (string str)
		{
			ParseUserQuery (str);
		}

		public override string ToSql (Operator op)
		{
			return string.IsNullOrEmpty (value) ? null : EscapeString (op, Hyena.StringUtil.SearchKey (value));
		}

		protected static string EscapeString (Operator op, string orig)
		{
			orig = orig.Replace ("'", "''");

			if (op == Contains || op == DoesNotContain ||
				op == StartsWith || op == EndsWith) {
				return StringUtil.EscapeLike (orig);
			}

			return orig;
		}
	}
}
