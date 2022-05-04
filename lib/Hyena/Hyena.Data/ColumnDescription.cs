//
// ColumnDescription.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data
{
	public class ColumnDescription
	{
		string title;
		string long_title;
		double width;
		bool visible;
		string property;

		bool initialized;

		public event EventHandler VisibilityChanged;
		public event EventHandler WidthChanged;

		public ColumnDescription (string property, string title, double width) : this (property, title, width, true)
		{
		}

		public ColumnDescription (string property, string title, double width, bool visible)
		{
			this.property = property;
			this.title = title;
			long_title = title;
			Width = width;
			Visible = visible;
			initialized = true;
		}

		protected virtual void OnVisibilityChanged ()
		{
			VisibilityChanged?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnWidthChanged ()
		{
			WidthChanged?.Invoke (this, EventArgs.Empty);
		}

		public string Title {
			get { return title; }
			set { title = value; }
		}

		public string LongTitle {
			get { return long_title; }
			set { long_title = value; }
		}

		public double Width {
			get { return width; }
			set {
				if (double.IsNaN (value)) {
					return;
				}

				double old = width;
				width = value;

				if (initialized && value != old) {
					OnWidthChanged ();
				}
			}
		}

		public int OrderHint { get; set; }

		public string Property {
			get { return property; }
			set { property = value; }
		}

		public bool Visible {
			get { return visible; }
			set {
				bool old = Visible;
				visible = value;

				if (initialized && value != old) {
					OnVisibilityChanged ();
				}
			}
		}
	}
}
