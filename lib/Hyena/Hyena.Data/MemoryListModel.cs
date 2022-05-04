//
// MemoryListModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Hyena.Collections;

namespace Hyena.Data
{
	public class MemoryListModel<T> : BaseListModel<T>
	{
		List<T> list;

		public MemoryListModel ()
		{
			list = new List<T> ();
			Selection = new Selection ();
		}

		public override void Clear ()
		{
			lock (list) {
				list.Clear ();
			}

			OnCleared ();
		}

		public override void Reload ()
		{
			OnReloaded ();
		}

		public int IndexOf (T item)
		{
			lock (list) {
				return list.IndexOf (item);
			}
		}

		public void Add (T item)
		{
			lock (list) {
				list.Add (item);
			}
		}

		public void Remove (T item)
		{
			lock (list) {
				list.Remove (item);
			}
		}

		public override T this[int index] {
			get {
				lock (list) {
					if (list.Count <= index || index < 0) {
						return default;
					}

					return list[index];
				}
			}
		}

		public override int Count {
			get {
				lock (list) {
					return list.Count;
				}
			}
		}
	}
}
