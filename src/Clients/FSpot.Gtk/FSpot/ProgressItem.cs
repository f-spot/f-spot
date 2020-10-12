//
// ProgressItem.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot
{
	public class ProgressItem
	{
		public delegate void ChangedHandler (ProgressItem item);
		public event ChangedHandler Changed;

		readonly object progressLock = new object ();

		double value;
		public double Value {
			get {
				lock (progressLock) {
					return value;
				}
			}
			set {
				lock (progressLock) {
					this.value = value;
					Changed?.Invoke (this);
				}
			}
		}
	}
}
