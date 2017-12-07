//
// SlideShow.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;

using Mono.Addins;

using FSpot.Core;
using FSpot.Bling;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Transitions;
using FSpot.Utils;

namespace FSpot.Widgets
{
	public class SlideShow : DrawingArea
	{
		bool running;
		BrowsablePointer item;
		int loadRetries;
#region Public API

		public SlideShow (BrowsablePointer item) : this (item, 6000, false)
		{
		}

		public SlideShow (BrowsablePointer item, uint interval_ms, bool init)
		{
			this.item = item;
			DoubleBuffered = false;
			AppPaintable = true;
			CanFocus = true;
			item.Changed += HandleItemChanged;

			foreach (TransitionNode transition in AddinManager.GetExtensionNodes ("/FSpot/SlideShow")) {
				if (this.transition == null)
					this.transition = transition.Transition;
				transitions.Add (transition.Transition);
			}

			flip = new DelayedOperation (interval_ms, delegate {item.MoveNext (true); return true;});
			animation = new DoubleAnimation (0, 1, new TimeSpan (0, 0, 2), HandleProgressChanged, GLib.Priority.Default);

			if (init) {
				HandleItemChanged (null, null);
			}
		}

		SlideShowTransition transition;
		public SlideShowTransition Transition {
            get { return transition; }
			set {
				if (value == transition)
					return;
				transition = value;
				QueueDraw ();
			}
		}

		List<SlideShowTransition> transitions = new List<SlideShowTransition> ();
		public IEnumerable<SlideShowTransition> Transitions {
			get { return transitions; }
		}

		DoubleAnimation animation;
		DelayedOperation flip;
		public void Start ()
		{
			running = true;
			flip.Start ();
		}

		public void Stop ()
		{
			running = false;
			flip.Stop ();
		}
#endregion

#region Event Handlers
		Pixbuf prev, next;
		object sync_handle = new object ();
		void HandleItemChanged (object sender, EventArgs e)
		{
			flip.Stop ();
			if (running)
				flip.Start ();
			lock (sync_handle) {
				if (prev != null && prev != PixbufUtils.ErrorPixbuf)
					prev.Dispose ();
				prev = next;

				LoadNext ();

				if (animation.IsRunning)
					animation.Stop ();
				progress = 0;
				animation.Start ();
			}
		}

		void LoadNext ()
		{
			if (next != null) {
				next = null;
			}

			if (item == null || item.Current == null)
				return;

			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (item.Current.DefaultVersion.Uri)) {
				try {
					using (var pb =  img.Load ()) {
						double scale = Math.Min ((double)Allocation.Width/(double)pb.Width, (double)Allocation.Height/(double)pb.Height);
						int w = (int)(pb.Width * scale);
						int h = (int)(pb.Height * scale);

						if (w > 0 && h > 0)
							next = pb.ScaleSimple ((int)(pb.Width * scale), (int)(pb.Height * scale), InterpType.Bilinear);
					}
					Cms.Profile screen_profile;
					if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile))
						FSpot.ColorManagement.ApplyProfile (next, screen_profile);
					loadRetries = 0;
				} catch (Exception) {
					next = PixbufUtils.ErrorPixbuf;
					if (++loadRetries < 10)
						item.MoveNext (true);
					else
						loadRetries = 0;
				}
			}
		}

		double progress;
		void HandleProgressChanged (double progress)
		{
			lock (sync_handle) {
				this.progress = progress;
				QueueDraw ();
			}
		}
#endregion

#region Gtk Widgetry
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			lock (sync_handle) {
				transition.Draw (args.Window, prev, next, Allocation.Width, Allocation.Height, progress);
			}
			return true;
		}
		protected override void OnDestroyed ()
		{
			if (prev != null && prev != PixbufUtils.ErrorPixbuf)
				prev.Dispose ();
			if (next != null && next != PixbufUtils.ErrorPixbuf)
				next.Dispose ();

			base.OnDestroyed ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			LoadNext ();
			QueueDraw ();
		}

		protected override void OnUnrealized ()
		{
			flip.Stop ();
			base.OnUnrealized ();
		}
#endregion
	}
}