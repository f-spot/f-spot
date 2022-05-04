//
// ModelSelection.cs.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

using Hyena.Collections;

namespace Hyena.Data
{
	public class ModelSelection<T> : IEnumerable<T>
	{
		IListModel<T> model;
		Selection selection;

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
				yield return model[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion

	}
}
