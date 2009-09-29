/*
 * GPList.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;

namespace GPhoto2
{
	public abstract class GPList<T> : GPObject, IEnumerable<T>
	{
		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return new Enumerator<T> (this);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return new Enumerator<T> (this);
		}

		public abstract int Count {get; }
		public abstract T this [int index] { get; }

		public GPList (Func<HandleRef, ErrorCode> cleaner) : base (cleaner)
		{
		}

		class Enumerator<U> : IEnumerator<U>, IEnumerator
		{
			int current;
			int count;
			GPList<U> list;

			public Enumerator (GPList<U> list)
			{
				this.list = list;
				Reset ();
			}

			public void Reset ()
			{
				current = -1;
				count = list.Count;
			}

			U IEnumerator<U>.Current {
				get {
					if (current < 0 || current >= count)
						throw new InvalidOperationException ();
					return list[current];
				}
			}

			object IEnumerator.Current {
				get {
					if (current < 0 || current >= count)
						throw new InvalidOperationException ();
					return list[current];
				}
			}

			public bool MoveNext ()
			{
				current ++;
				if (current >= count)
					return false;
				return true;
			}

			public void Dispose ()
			{
			}
		}
	}
}
