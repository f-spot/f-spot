//
// NullQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
    public class NullQueryValue : QueryValue
    {
        public static readonly Operator IsNullOrEmpty  = new Operator ("empty", Catalog.GetString ("empty"), "IN (NULL, '', 0)", true, "!");

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

        private NullQueryValue ()
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
            node.InnerText = String.Empty;
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
