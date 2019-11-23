//
// ArrayModelCache.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
    public abstract class ArrayModelCache<T> : ModelCache<T> where T : ICacheableItem, new ()
    {
        protected T [] cache;
        protected long offset = -1;
        protected long limit = 0;

        public ArrayModelCache (ICacheableModel model) : base (model)
        {
            cache = new T [Model.FetchCount];
        }

        public override bool ContainsKey (long i)
        {
            return (i >= offset &&
                    i <= (offset + limit));
        }

        public override void Add (long i, T item)
        {
            if (cache.Length != Model.FetchCount) {
                cache = new T [Model.FetchCount];
                Clear ();
            }

            if (offset == -1 || i < offset || i >= (offset + cache.Length)) {
                offset = i;
                limit = 0;
            }

            cache [i - offset] = item;
            limit++;
        }

        public override T this [long i] {
            get { return cache [i - offset]; }
        }

        public override void Clear ()
        {
            offset = -1;
            limit = 0;
        }
    }
}
