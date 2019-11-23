//
// AliasedObjectSet.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Query
{
    public interface IAliasedObject
    {
        string Name { get; }
        string [] Aliases { get; }
    }

    public class AliasedObjectSet<T> : IEnumerable<T> where T : IAliasedObject
    {
        protected Dictionary<string, T> map = new Dictionary<string, T> ();
        protected List<string> aliases = new List<string> ();
        protected T [] objects;

        public AliasedObjectSet (params T [] objects)
        {
            this.objects = objects;
            foreach (T obj in objects) {
                map [obj.Name.ToLower ()] = obj;
                foreach (string alias in obj.Aliases) {
                    if (!String.IsNullOrEmpty (alias) && alias.IndexOf (" ") == -1) {
                        foreach (string sub_alias in alias.Split(',')) {
                            string lower_alias = sub_alias.ToLower ();
                            map [lower_alias] = obj;
                            if (!aliases.Contains (lower_alias)) {
                                aliases.Add (lower_alias);
                            }
                        }
                    }
                }
            }
            aliases.Sort (SortByLongest);
        }

        private int SortByLongest (string a, string b)
        {
            return b.Length.CompareTo (a.Length);
        }

        public string FindAlias (string input)
        {
            input = input.ToLower ();
            foreach (string alias in aliases) {
                if (input.StartsWith (alias)) {
                    return alias;
                }
            }
            return null;
        }

        public T [] Objects {
            get { return objects; }
        }

        public T First {
            get { return objects [0]; }
        }

        public IEnumerator<T> GetEnumerator ()
        {
            foreach (T o in objects) {
                yield return o;
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public T this [string alias] {
            get {
                if (!String.IsNullOrEmpty (alias) && map.ContainsKey (alias.ToLower ()))
                    return map[alias.ToLower ()];
                return default(T);
            }
        }
    }
}
