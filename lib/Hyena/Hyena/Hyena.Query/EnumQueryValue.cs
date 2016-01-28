//
// EnumQueryValue.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public abstract class EnumQueryValue : QueryValue
    {
        public static readonly Operator Equal    = new Operator ("equals", Catalog.GetString ("is"), "= {0}", "=", "==", ":");
        public static readonly Operator NotEqual = new Operator ("notEqual", Catalog.GetString ("is not"), "!= {0}", true, "!=", "!:");

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
            IsEmpty = !Int32.TryParse (node.InnerText, out value);
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
