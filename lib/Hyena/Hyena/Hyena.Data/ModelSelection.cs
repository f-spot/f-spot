//
// ModelSelection.cs.cs
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
using System.Collections;
using System.Collections.Generic;

using Hyena.Collections;

namespace Hyena.Data
{
    //public class ModelSelection<T> : IList<T>
    public class ModelSelection<T> : IEnumerable<T>
    {
        private IListModel<T> model;
        private Selection selection;

#region Properties

        /*public T this [int index] {
            get {
                if (index >= selection.Count)
                    throw new ArgumentOutOfRangeException ("index");
                //return model [selection [index]];
                return default(T);
            }
        }*/

        public int Count {
            get { return selection.Count; }
        }

#endregion

        public ModelSelection (IListModel<T> model, Selection selection)
        {
            this.model = model;
            this.selection = selection;
        }

#region Methods

        /*public int IndexOf (T value)
        {
            //selection.IndexOf (model.IndexOf (value));
            return -1;
        }*/

        public IEnumerator<T> GetEnumerator ()
        {
            foreach (int i in selection) {
                yield return model [i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

#endregion

    }
}
