//
// TimeSpanQueryValue.cs
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
using System.Globalization;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public enum TimeFactor {
        Second = 1,
        Minute = 60,
        Hour   = 3600,
        Day    = 3600*24,
        Week   = 3600*24*7,
        Month  = 3600*24*30,
        Year   = 3600*24*365
    }

    public class TimeSpanQueryValue : IntegerQueryValue
    {
        /*public static readonly Operator Equal              = new Operator ("equals", "= {0}", "==", "=", ":");
        public static readonly Operator NotEqual           = new Operator ("notEqual", "!= {0}", true, "!=", "!:");
        public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", "<= {0}", "<=");
        public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", ">= {0}", ">=");
        public static readonly Operator LessThan           = new Operator ("lessThan", "< {0}", "<");
        public static readonly Operator GreaterThan        = new Operator ("greaterThan", "> {0}", ">");*/

        protected double offset = 0;
        protected TimeFactor factor = TimeFactor.Second;

        public override string XmlElementName {
            get { return "timespan"; }
        }

        //protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual, LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
        protected static AliasedObjectSet<Operator> ops = new AliasedObjectSet<Operator> (LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
        public override AliasedObjectSet<Operator> OperatorSet {
            get { return operators; }
        }

        public override object Value {
            get { return offset; }
        }

        public virtual double Offset {
            get { return offset; }
        }

        public TimeFactor Factor {
            get { return factor; }
        }

        public double FactoredValue {
            get { return Offset / (double) factor; }
        }

        // FIXME replace period in following with culture-dependent character
        private static Regex number_regex = new Regex ("\\d+(\\.\\d+)?", RegexOptions.Compiled);
        public override void ParseUserQuery (string input)
        {
            Match match = number_regex.Match (input);
            if (match != Match.Empty && match.Groups.Count > 0) {
                double val;
                try {
                    val = Convert.ToDouble (match.Groups[0].Captures[0].Value);
                } catch (FormatException) {
                    val = Convert.ToDouble (match.Groups[0].Captures[0].Value, NumberFormatInfo.InvariantInfo);
                }
                foreach (TimeFactor factor in Enum.GetValues (typeof(TimeFactor))) {
                    if (input == FactorString (factor, val, true) || input == FactorString (factor, val, false)) {
                        SetUserRelativeValue (val, factor);
                        return;
                    }
                }
            }
            IsEmpty = true;
        }

        public override string ToUserQuery ()
        {
            return FactorString (factor, FactoredValue, true);
        }

        public virtual void SetUserRelativeValue (double offset, TimeFactor factor)
        {
            SetRelativeValue (offset, factor);
        }

        public void SetRelativeValue (double offset, TimeFactor factor)
        {
            this.factor = factor;
            this.offset = (long) (offset * (double)factor);
            IsEmpty = false;
        }

        public override void LoadString (string val)
        {
            try {
                SetRelativeValue (Convert.ToDouble (val), TimeFactor.Second);
                DetermineFactor ();
            } catch {
                IsEmpty = true;
            }
        }

        protected void DetermineFactor ()
        {
            double val = Math.Abs (offset);
            foreach (TimeFactor factor in Enum.GetValues (typeof(TimeFactor))) {
                if (val >= (double) factor) {
                    this.factor = factor;
                }
            }
        }

        public override void ParseXml (XmlElement node)
        {
            try {
                LoadString (node.InnerText);
                if (node.HasAttribute ("factor")) {
                    this.factor = (TimeFactor) Enum.Parse (typeof(TimeFactor), node.GetAttribute ("factor"));
                }
            } catch {
                IsEmpty = true;
            }
        }

        public override void AppendXml (XmlElement node)
        {
            base.AppendXml (node);
            node.SetAttribute ("factor", factor.ToString ());
        }

        public override string ToSql (Operator op)
        {
            return Convert.ToString (offset * 1000, System.Globalization.CultureInfo.InvariantCulture);
        }

        protected virtual string FactorString (TimeFactor factor, double count, bool translate)
        {
            string result = null;
            int plural_count = StringUtil.DoubleToPluralInt (count);
            if (translate) {
                switch (factor) {
                    case TimeFactor.Second: result = Catalog.GetPluralString ("{0} second", "{0} seconds", plural_count); break;
                    case TimeFactor.Minute: result = Catalog.GetPluralString ("{0} minute", "{0} minutes", plural_count); break;
                    case TimeFactor.Hour:   result = Catalog.GetPluralString ("{0} hour",   "{0} hours", plural_count); break;
                    case TimeFactor.Day:    result = Catalog.GetPluralString ("{0} day",    "{0} days", plural_count); break;
                    case TimeFactor.Week:   result = Catalog.GetPluralString ("{0} week",   "{0} weeks", plural_count); break;
                    case TimeFactor.Month:  result = Catalog.GetPluralString ("{0} month",  "{0} months", plural_count); break;
                    case TimeFactor.Year:   result = Catalog.GetPluralString ("{0} year",   "{0} years", plural_count); break;
                    default: return null;
                }
            } else {
                switch (factor) {
                    case TimeFactor.Second: result = plural_count == 1 ? "{0} second" : "{0} seconds"; break;
                    case TimeFactor.Minute: result = plural_count == 1 ? "{0} minute" : "{0} minutes"; break;
                    case TimeFactor.Hour:   result = plural_count == 1 ? "{0} hour"   : "{0} hours"; break;
                    case TimeFactor.Day:    result = plural_count == 1 ? "{0} day"    : "{0} days"; break;
                    case TimeFactor.Week:   result = plural_count == 1 ? "{0} week"   : "{0} weeks"; break;
                    case TimeFactor.Month:  result = plural_count == 1 ? "{0} month"  : "{0} months"; break;
                    case TimeFactor.Year:   result = plural_count == 1 ? "{0} year"   : "{0} years"; break;
                    default: return null;
                }
            }

            return String.Format (result, StringUtil.DoubleToTenthsPrecision (
                count, false, translate ? NumberFormatInfo.CurrentInfo : NumberFormatInfo.InvariantInfo));
        }
    }
}