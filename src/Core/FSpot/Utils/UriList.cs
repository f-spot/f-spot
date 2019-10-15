//
// UriList.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

using Hyena;

namespace FSpot.Utils
{
    public class UriList : List<SafeUri>
    {
        public UriList ()
	    {
        }

        public UriList (string data)
        {
            LoadFromStrings (data.Split ('\n'));
        }

        public UriList (IEnumerable<SafeUri> uris)
        {
            foreach (SafeUri uri in uris) {
                Add (uri);
            }
        }

        void LoadFromStrings (IEnumerable<string> items)
        {
            foreach (string i in items) {
                if (!i.StartsWith ("#")) {
                    SafeUri uri;
                    String s = i;
                    
                    if (i.EndsWith ("\r")) {
                        s = i.Substring (0, i.Length - 1);
                    }
                    
                    try {
                        uri = new SafeUri (s);
                    } catch {
                        continue;
                    }
                    Add (uri);
                }
            }
        }

        public void AddUnknown (string unknown)
        {
            Add (new SafeUri (unknown));
        }

        public override string ToString ()
        {
            var list = new StringBuilder ();
            
            foreach (SafeUri uri in this) {
                if (uri == null)
                    break;
                
                list.Append (uri + Environment.NewLine);
            }
            
            return list.ToString ();
        }
    }
}
