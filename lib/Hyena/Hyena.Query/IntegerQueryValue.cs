//
// IntegerQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Xml;

using FSpot.Resources.Lang;

namespace Hyena.Query
{
	public class IntegerQueryValue : QueryValue
	{
		public static readonly Operator Equal = new Operator ("equals", Strings.Is, "= {0}", "=", "==", ":");
		public static readonly Operator NotEqual = new Operator ("notEqual", Strings.IsNot, "!= {0}", true, "!=", "!:");
		public static readonly Operator LessThanEqual = new Operator ("lessThanEquals", Strings.AtMost, "<= {0}", "<=");
		public static readonly Operator GreaterThanEqual = new Operator ("greaterThanEquals", Strings.AtLeast, ">= {0}", ">=");
		public static readonly Operator LessThan = new Operator ("lessThan", Strings.LessThan, "< {0}", "<");
		public static readonly Operator GreaterThan = new Operator ("greaterThan", Strings.MoreThan, "> {0}", ">");

		protected long value;

		public override string XmlElementName {
			get { return "int"; }
		}

		public override void ParseUserQuery (string input)
		{
			IsEmpty = !long.TryParse (input, out value);
		}

		public override void LoadString (string input)
		{
			ParseUserQuery (input);
		}

		public override void ParseXml (XmlElement node)
		{
			ParseUserQuery (node.InnerText);
		}

		public void SetValue (int value)
		{
			SetValue ((long)value);
		}

		public virtual void SetValue (long value)
		{
			this.value = value;
			IsEmpty = false;
		}

		protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual, LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		public override object Value {
			get { return value; }
		}

		public long IntValue {
			get { return value; }
		}

		public virtual long DefaultValue {
			get { return 0; }
		}

		public virtual long MinValue {
			get { return long.MinValue; }
		}

		public virtual long MaxValue {
			get { return long.MaxValue; }
		}

		public override string ToSql (Operator op)
		{
			return Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
