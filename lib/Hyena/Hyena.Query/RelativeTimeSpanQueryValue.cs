//
// RelativeTimeSpanQueryValue.cs
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
	public class RelativeTimeSpanQueryValue : TimeSpanQueryValue
	{
		// The SQL operators in these Operators are reversed from normal on purpose
		public static new readonly Operator GreaterThan = new Operator ("greaterThan", Strings.MoreThan, "< {0}", true, ">");
		public static new readonly Operator LessThan = new Operator ("lessThan", Strings.LessThan, "> {0}", "<");
		public static new readonly Operator GreaterThanEqual = new Operator ("greaterThanEquals", Strings.AtLeast, "<= {0}", true, ">=");
		public static new readonly Operator LessThanEqual = new Operator ("lessThanEquals", Strings.AtMost, ">= {0}", "<=");

		protected static new AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (GreaterThan, LessThan, GreaterThanEqual, LessThanEqual);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		public static RelativeTimeSpanQueryValue RelativeToNow (DateTime since)
		{
			var qv = new RelativeTimeSpanQueryValue ();
			qv.SetRelativeValue ((since - DateTime.Now).TotalSeconds, TimeFactor.Second);
			qv.DetermineFactor ();
			return qv;
		}

		public override string XmlElementName {
			get { return "date"; }
		}

		public override double Offset {
			get { return -offset; }
		}

		public override void SetUserRelativeValue (double offset, TimeFactor factor)
		{
			SetRelativeValue (-offset, factor);
		}

		public override void AppendXml (XmlElement node)
		{
			base.AppendXml (node);
			node.SetAttribute ("type", "rel");
		}

		public override string ToSql (Operator op)
		{
			return DateTimeUtil.FromDateTime (DateTime.Now + TimeSpan.FromSeconds ((double)offset)).ToString (System.Globalization.CultureInfo.InvariantCulture);
		}

		protected override string FactorString (TimeFactor factor, double count, bool translate)
		{
			string result = base.FactorString (factor, count, translate);
			return (result == null) ? null : string.Format (translate ? Strings.XAgo : "{0} ago", result);
		}
	}
}
