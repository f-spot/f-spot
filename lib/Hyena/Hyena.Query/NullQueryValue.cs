//
// NullQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml;

using FSpot.Resources.Lang;

namespace Hyena.Query
{
	public class NullQueryValue : QueryValue
	{
		public static readonly Operator IsNullOrEmpty = new Operator ("empty", Strings.Empty, "IN (NULL, '', 0)", true, "!");

		public static readonly NullQueryValue Instance = new NullQueryValue ();

		public override string XmlElementName {
			get { return "empty"; }
		}

		public override object Value {
			get { return null; }
		}

		protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (IsNullOrEmpty);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		NullQueryValue ()
		{
			IsEmpty = false;
		}

		public override void ParseUserQuery (string input)
		{
		}

		public override void LoadString (string input)
		{
		}

		public override void ParseXml (XmlElement node)
		{
		}

		public override void AppendXml (XmlElement node)
		{
			node.InnerText = string.Empty;
		}

		public void SetValue (string str)
		{
		}

		public override string ToSql (Operator op)
		{
			return null;
		}
	}
}
