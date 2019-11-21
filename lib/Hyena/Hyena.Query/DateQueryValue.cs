//
// DateQueryValue.cs
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
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public class DateQueryValue : QueryValue
    {
        //public static readonly Operator Equal              = new Operator ("equals", "= {0}", "==", "=", ":");
        //public static readonly Operator NotEqual           = new Operator ("notEqual", "!= {0}", true, "!=", "!:");
        //public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", "<= {0}", "<=");
        //public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", ">= {0}", ">=");
        public static readonly Operator LessThan           = new Operator ("lessThan", Catalog.GetString ("before"), "< {0}", true, "<");
        public static readonly Operator GreaterThan        = new Operator ("greaterThan", Catalog.GetString ("after"), ">= {0}", ">");

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
