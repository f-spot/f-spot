//
// ScrolledWindow.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Gtk;

namespace Hyena.Widgets
{
	public class ScrolledWindow : Gtk.ScrolledWindow
	{
		Widget adjustable;
		RoundedFrame rounded_frame;

		public ScrolledWindow ()
		{
		}

		public void AddWithFrame (Widget widget)
		{
			var frame = new RoundedFrame ();
			frame.Add (widget);
			frame.Show ();

			Add (frame);
			ProbeAdjustable (widget);
		}

		protected override void OnAdded (Widget widget)
		{
			if (widget is RoundedFrame) {
				rounded_frame = (RoundedFrame)widget;
				rounded_frame.Added += OnFrameWidgetAdded;
				rounded_frame.Removed += OnFrameWidgetRemoved;
			}

			base.OnAdded (widget);
		}

		protected override void OnRemoved (Widget widget)
		{
			if (widget == rounded_frame) {
				rounded_frame.Added -= OnFrameWidgetAdded;
				rounded_frame.Removed -= OnFrameWidgetRemoved;
				rounded_frame = null;
			}

			base.OnRemoved (widget);
		}

		void OnFrameWidgetAdded (object o, AddedArgs args)
		{
			if (rounded_frame != null) {
				ProbeAdjustable (args.Widget);
			}
		}

		void OnFrameWidgetRemoved (object o, RemovedArgs args)
		{
			if (adjustable != null && adjustable == args.Widget) {
				Hadjustment = null;
				Vadjustment = null;
				adjustable = null;
			}
		}

		void ProbeAdjustable (Widget widget)
		{
			Type type = widget.GetType ();

			PropertyInfo hadj_prop = type.GetProperty ("Hadjustment");
			PropertyInfo vadj_prop = type.GetProperty ("Vadjustment");

			if (hadj_prop == null || vadj_prop == null) {
				return;
			}

			object hadj_value = hadj_prop.GetValue (widget, null);
			object vadj_value = vadj_prop.GetValue (widget, null);

			if (hadj_value == null || vadj_value == null
				|| hadj_value.GetType () != typeof (Adjustment)
				|| vadj_value.GetType () != typeof (Adjustment)) {
				return;
			}

			Hadjustment = (Adjustment)hadj_value;
			Vadjustment = (Adjustment)vadj_value;
		}
	}
}
