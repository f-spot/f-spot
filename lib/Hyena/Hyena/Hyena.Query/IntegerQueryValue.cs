//
// IntegerQueryValue.cs
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
    public class IntegerQueryValue : QueryValue
    {
        public static readonly Operator Equal              = new Operator ("equals", Catalog.GetString ("is"), "= {0}", "=", "==", ":");
        public static readonly Operator NotEqual           = new Operator ("notEqual", Catalog.GetString ("is not"), "!= {0}", true, "!=", "!:");
        public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", Catalog.GetString ("at most"), "<= {0}", "<=");
        public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", Catalog.GetString ("at least"), ">= {0}", ">=");
        public static readonly Operator LessThan           = new Operator ("lessThan", Catalog.GetString ("less than"), "< {0}", "<");
        public static readonly Operator GreaterThan        = new Operator ("greaterThan", Catalog.GetString ("more than"), "> {0}", ">");

        protected long value;

        public override string XmlElementName {
            get { return "int"; }
        }

        public override void ParseUserQuery (string input)
        {
            IsEmpty = !Int64.TryParse (input, out value);
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
            SetValue ((long) value);
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
            get { return Int64.MinValue; }
        }

        public virtual long MaxValue {
            get { return Int64.MaxValue; }
        }

        public override string ToSql (Operator op)
        {
            return Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
