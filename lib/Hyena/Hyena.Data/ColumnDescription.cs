//
// ColumnDescription.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace Hyena.Data
{
    public class ColumnDescription
    {
        private string title;
        private string long_title;
        private double width;
        private bool visible;
        private string property;

        private bool initialized;

        public event EventHandler VisibilityChanged;
        public event EventHandler WidthChanged;

        public ColumnDescription (string property, string title, double width) : this (property, title, width, true)
        {
        }

        public ColumnDescription (string property, string title, double width, bool visible)
        {
            this.property = property;
            this.title = title;
            this.long_title = title;
            Width = width;
            Visible = visible;
            initialized = true;
        }

        protected virtual void OnVisibilityChanged ()
        {
            EventHandler handler = VisibilityChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected virtual void OnWidthChanged ()
        {
            EventHandler handler = WidthChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public string Title {
            get { return title; }
            set { title = value; }
        }

        public string LongTitle {
            get { return long_title; }
            set { long_title = value; }
        }

        public double Width {
            get { return width; }
            set {
                if (Double.IsNaN (value)) {
                    return;
                }

                double old = width;
                width = value;

                if (initialized && value != old) {
                    OnWidthChanged ();
                }
            }
        }

        public int OrderHint { get; set; }

        public string Property {
            get { return property; }
            set { property = value; }
        }

        public bool Visible {
            get { return visible; }
            set {
                bool old = Visible;
                visible = value;

                if(initialized && value != old) {
                    OnVisibilityChanged ();
                }
            }
        }
    }
}
