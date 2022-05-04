//
// DateQueryValue.cs
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
	public class DateQueryValue : QueryValue
	{
		//public static readonly Operator Equal              = new Operator ("equals", "= {0}", "==", "=", ":");
		//public static readonly Operator NotEqual           = new Operator ("notEqual", "!= {0}", true, "!=", "!:");
		//public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", "<= {0}", "<=");
		//public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", ">= {0}", ">=");
		public static readonly Operator LessThan = new Operator ("lessThan", Strings.Before, "< {0}", true, "<");
		public static readonly Operator GreaterThan = new Operator ("greaterThan", Strings.After, ">= {0}", ">");

		protected DateTime value = DateTime.Now;

		public override string XmlElementName {
			get { return "date"; }
		}

		//protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual, LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
		protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (LessThan, GreaterThan);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		public override object Value {
			get { return value; }
		}

		public override void ParseUserQuery (string input)
		{
			try {
				value = DateTime.Parse (input);
				IsEmpty = false;
			} catch {
				IsEmpty = true;
			}
		}

		public override string ToUserQuery ()
		{
			if (value.Hour == 0 && value.Minute == 0 && value.Second == 0) {
				return value.ToString ("yyyy-MM-dd");
			} else {
				return value.ToString ();
			}
		}

		public void SetValue (DateTime date)
		{
			value = date;
			IsEmpty = false;
		}

		public override void LoadString (string val)
		{
			try {
				SetValue (DateTime.Parse (val));
			} catch {
				IsEmpty = true;
			}
		}

		public override void ParseXml (XmlElement node)
		{
			try {
				LoadString (node.InnerText);
			} catch {
				IsEmpty = true;
			}
		}

		public override string ToSql (Operator op)
		{
			if (op == GreaterThan) {
				return DateTimeUtil.FromDateTime (value.AddDays (1.0)).ToString (System.Globalization.CultureInfo.InvariantCulture);
			} else {
				return DateTimeUtil.FromDateTime (value).ToString (System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		public DateTime DateTime {
			get { return value; }
		}
	}
}
