//
// RelativeTimeSpanQueryValue.cs
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
    public class RelativeTimeSpanQueryValue : TimeSpanQueryValue
    {
        // The SQL operators in these Operators are reversed from normal on purpose
        public static new readonly Operator GreaterThan        = new Operator ("greaterThan", Catalog.GetString ("more than"), "< {0}", true, ">");
        public static new readonly Operator LessThan           = new Operator ("lessThan", Catalog.GetString ("less than"), "> {0}", "<");
        public static new readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", Catalog.GetString ("at least"), "<= {0}", true, ">=");
        public static new readonly Operator LessThanEqual      = new Operator ("lessThanEquals", Catalog.GetString ("at most"), ">= {0}", "<=");

        protected static new AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (GreaterThan, LessThan, GreaterThanEqual, LessThanEqual);
        public override AliasedObjectSet<Operator> OperatorSet {
            get { return operators; }
        }

        public static RelativeTimeSpanQueryValue RelativeToNow (DateTime since)
        {
            RelativeTimeSpanQueryValue qv = new RelativeTimeSpanQueryValue ();
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
            return DateTimeUtil.FromDateTime (DateTime.Now + TimeSpan.FromSeconds ((double) offset)).ToString (System.Globalization.CultureInfo.InvariantCulture);
        }

        protected override string FactorString (TimeFactor factor, double count, bool translate)
        {
            string result = base.FactorString (factor, count, translate);
            return (result == null) ? null : String.Format (
                translate ? Catalog.GetString ("{0} ago") : "{0} ago", result);
        }
    }
}
