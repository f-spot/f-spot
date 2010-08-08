/*
 * FSpot.ProgressItem.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot {
	public class ProgressItem {
		public ProgressItem () {
		}

		public delegate void ChangedHandler (ProgressItem item);
		public event ChangedHandler Changed;

		double value;
		public double Value {
			get {
				lock (this) {
					return value;
				}
			}
			set {
				lock (this) {
					this.value = value;
					if (Changed != null)
						Changed (this);
				}
			}
		}
	}
}
