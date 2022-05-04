//
// EnumQueryValue.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using FSpot.Resources.Lang;

namespace Hyena.Query
{
	public abstract class EnumQueryValue : QueryValue
	{
		public static readonly Operator Equal = new Operator ("equals", Strings.Is, "= {0}", "=", "==", ":");
		public static readonly Operator NotEqual = new Operator ("notEqual", Strings.IsNot, "!= {0}", true, "!=", "!:");

		protected int value;

		public abstract IEnumerable<EnumQueryValueItem> Items { get; }

		public override string XmlElementName {
			get { return "int"; }
		}

		public override object Value {
			get { return value; }
		}

		public void SetValue (int value)
		{
			this.value = value;
			IsEmpty = false;
		}

		protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual);
		public override AliasedObjectSet<Operator> OperatorSet {
			get { return operators; }
		}

		public override void ParseUserQuery (string input)
		{
			foreach (var item in Items) {
				if (input == item.ID.ToString () || input == item.Name || item.Aliases.Contains (input)) {
					value = item.ID;
					IsEmpty = false;
					break;
				}
			}
		}

		public override void ParseXml (XmlElement node)
		{
			IsEmpty = !int.TryParse (node.InnerText, out value);
		}

		public override void LoadString (string str)
		{
			ParseUserQuery (str);
		}

		public override string ToSql (Operator op)
		{
			return Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	public sealed class EnumQueryValueItem : IAliasedObject
	{
		public int ID { get; private set; }
		public string Name { get; private set; }
		public string DisplayName { get; private set; }
		public string[] Aliases { get; private set; }

		public EnumQueryValueItem (int id, string name, string display_name, params string[] aliases)
		{
			ID = id;
			Name = name;
			DisplayName = display_name;
			Aliases = aliases;
		}
	}
}
