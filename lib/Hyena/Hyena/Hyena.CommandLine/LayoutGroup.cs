//
// LayoutGroup.cs
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
using System.Collections.Generic;

namespace Hyena.CommandLine
{
    public class LayoutGroup : IEnumerable<LayoutOption>
    {
        private List<LayoutOption> options;
        private string id;
        private string title;

        public LayoutGroup (string id, string title, List<LayoutOption> options)
        {
            this.id = id;
            this.title = title;
            this.options = options;
        }

        public LayoutGroup (string id, string title, params LayoutOption [] options)
            : this (id, title, new List<LayoutOption> (options))
        {
        }

        public IEnumerator<LayoutOption> GetEnumerator ()
        {
            return options.GetEnumerator ();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public void Add (LayoutOption option)
        {
            options.Add (option);
        }

        public void Add (string name, string description)
        {
            options.Add (new LayoutOption (name, description));
        }

        public void Remove (LayoutOption option)
        {
            options.Remove (option);
        }

        public void Remove (string optionName)
        {
            LayoutOption option = FindOption (optionName);
            if (option != null) {
                options.Remove (option);
            }
        }

        private LayoutOption FindOption (string name)
        {
            foreach (LayoutOption option in options) {
                if (option.Name == name) {
                    return option;
                }
            }

            return null;
        }

        public LayoutOption this[int index] {
            get { return options[index]; }
            set { options[index] = value; }
        }

        public int Count {
            get { return options.Count; }
        }

        public string Id {
            get { return id; }
        }

        public string Title {
            get { return title; }
        }

        public IList<LayoutOption> Options {
            get { return options; }
        }
    }
}
