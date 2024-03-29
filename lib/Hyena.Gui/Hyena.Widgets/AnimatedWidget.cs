//
// AnimatedVboxActor.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gdk;

using Gtk;

using Hyena.Gui.Theatrics;

namespace Hyena.Widgets
{
	enum AnimationState
	{
		Coming,
		Idle,
		IntendingToGo,
		Going
	}

	class AnimatedWidget : Container
	{
		public event EventHandler WidgetDestroyed;

		public Widget Widget;
		public Easing Easing;
		public Blocking Blocking;
		public AnimationState AnimationState;
		public uint Duration;
		public double Bias = 1.0;
		public int Width;
		public int Height;
		public int StartPadding;
		public int EndPadding;
		public LinkedListNode<AnimatedWidget> Node;

		readonly bool horizontal;
		double percent;
		Rectangle widget_alloc;
		Pixmap canvas;

		public AnimatedWidget (Widget widget, uint duration, Easing easing, Blocking blocking, bool horizontal)
		{
			this.horizontal = horizontal;
			Widget = widget;
			Duration = duration;
			Easing = easing;
			Blocking = blocking;
			AnimationState = AnimationState.Coming;

			Widget.Parent = this;
			Widget.Destroyed += OnWidgetDestroyed;
			ShowAll ();
		}

		protected AnimatedWidget (IntPtr raw) : base (raw)
		{
		}

		public double Percent {
			get { return percent; }
			set {
				percent = value * Bias;
				QueueResizeNoRedraw ();
			}
		}

		void OnWidgetDestroyed (object sender, EventArgs args)
		{
			if (!IsRealized) {
				return;
			}

			canvas = new Pixmap (GdkWindow, widget_alloc.Width, widget_alloc.Height);
			canvas.DrawDrawable (Style.BackgroundGC (State), GdkWindow,
				widget_alloc.X, widget_alloc.Y, 0, 0, widget_alloc.Width, widget_alloc.Height);

			if (AnimationState != AnimationState.Going) {
				WidgetDestroyed (this, args);
			}
		}

		#region Overrides

		protected override void OnRemoved (Widget widget)
		{
			if (widget == Widget) {
				widget.Unparent ();
				Widget = null;
			}
		}

		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;

			var attributes = new Gdk.WindowAttr ();
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = Gdk.WindowClass.InputOutput;
			attributes.EventMask = (int)Gdk.EventMask.ExposureMask;

			GdkWindow = new Gdk.Window (Parent.GdkWindow, attributes, 0);
			GdkWindow.UserData = Handle;
			GdkWindow.Background = Style.Background (State);
			Style.Attach (GdkWindow);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (Widget != null) {
				Requisition req = Widget.SizeRequest ();
				widget_alloc.Width = req.Width;
				widget_alloc.Height = req.Height;
			}

			if (horizontal) {
				Width = Choreographer.PixelCompose (percent, widget_alloc.Width + StartPadding + EndPadding, Easing);
				Height = widget_alloc.Height;
			} else {
				Width = widget_alloc.Width;
				Height = Choreographer.PixelCompose (percent, widget_alloc.Height + StartPadding + EndPadding, Easing);
			}

			requisition.Width = Width;
			requisition.Height = Height;
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (Widget != null) {
				if (horizontal) {
					widget_alloc.Height = allocation.Height;
					widget_alloc.X = StartPadding;
					if (Blocking == Blocking.Downstage) {
						widget_alloc.X += allocation.Width - widget_alloc.Width;
					}
				} else {
					widget_alloc.Width = allocation.Width;
					widget_alloc.Y = StartPadding;
					if (Blocking == Blocking.Downstage) {
						widget_alloc.Y = allocation.Height - widget_alloc.Height;
					}
				}

				if (widget_alloc.Height > 0 && widget_alloc.Width > 0) {
					Widget.SizeAllocate (widget_alloc);
				}
			}
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (canvas != null) {
				GdkWindow.DrawDrawable (Style.BackgroundGC (State), canvas,
					0, 0, widget_alloc.X, widget_alloc.Y, widget_alloc.Width, widget_alloc.Height);
				return true;
			} else {
				return base.OnExposeEvent (evnt);
			}
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			if (Widget != null) {
				callback (Widget);
			}
		}

		#endregion

	}
}
