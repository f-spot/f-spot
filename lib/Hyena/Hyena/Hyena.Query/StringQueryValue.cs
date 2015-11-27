//
// StringQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Xml;
using System.Text;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public class StringQueryValue : QueryValue
    {
        private const string ESCAPE_CLAUSE = " ESCAPE '\\'";

        public static readonly Operator Contains       = new Operator ("contains", Catalog.GetString ("contains"), "LIKE '%{0}%'" + ESCAPE_CLAUSE, ":");
        public static readonly Operator DoesNotContain = new Operator ("doesNotContain", Catalog.GetString ("doesn't contain"), "NOT LIKE '%{0}%'" + ESCAPE_CLAUSE, true, "!:");
        public static readonly Operator Equal          = new Operator ("equals", Catalog.GetString ("is"), "= '{0}'", "==");
        public static readonly Operator NotEqual       = new Operator ("notEqual", Catalog.GetString ("is not"), "!= '{0}'", true, "!=");
        public static readonly Operator StartsWith     = new Operator ("startsWith", Catalog.GetString ("starts with"), "LIKE '{0}%'" + ESCAPE_CLAUSE, "=");
        public static readonly Operator EndsWith       = new Operator ("endsWith", Catalog.GetString ("ends with"), "LIKE '%{0}'" + ESCAPE_CLAUSE, ":=");

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
            IsEmpty = String.IsNullOrEmpty (value);
        }

        public override void ParseXml (XmlElement node)
        {
            value = node.InnerText;
            IsEmpty = String.IsNullOrEmpty (value);
        }

        public override void LoadString (string str)
        {
            ParseUserQuery (str);
        }

        public override string ToSql (Operator op)
        {
            return String.IsNullOrEmpty (value) ? null : EscapeString (op, Hyena.StringUtil.SearchKey (value));
        }

        protected static string EscapeString (Operator op, string orig)
        {
            orig = orig.Replace ("'", "''");

            if (op == Contains   || op == DoesNotContain ||
                op == StartsWith || op == EndsWith) {
                return StringUtil.EscapeLike (orig);
            }

            return orig;
        }
    }
}
