//
// QueryOperator.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;

namespace Hyena.Query
{
    public class Operator : IAliasedObject
    {
        public string name;
        public string Name {
            get { return name; }
        }

        public string label;
        public string Label {
            get { return label ?? Name; }
            set { label = value; }
        }

        private string [] aliases;
        public string [] Aliases {
            get { return aliases; }
        }

        public string PrimaryAlias {
            get { return aliases [0]; }
        }

        private string sql_format;
        public string SqlFormat {
            get { return sql_format; }
        }

        // FIXME get rid of this
        private bool is_not;
        public bool IsNot {
            get { return is_not; }
        }

        internal Operator (string name, string label, string sql_format, params string [] userOps) : this (name, label, sql_format, false, userOps)
        {
        }

        internal Operator (string name, string label, string sql_format, bool is_not, params string [] userOps)
        {
            this.name = name;
            this.label = label;
            this.sql_format = sql_format;
            this.aliases = userOps;
            this.is_not = is_not;
        }
    }
}
