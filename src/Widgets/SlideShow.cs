//
// FSpot.Widgets.SlideShow.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is open source software. See COPYING for details.
//

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;
using Mono.Addins;
using FSpot.Bling;
using FSpot.Extensions;

namespace FSpot.Widgets
{
	public class SlideShow : DrawingArea
	{
		bool running;
		BrowsablePointer item;
#region Public API

		public SlideShow (BrowsablePointer item) : this (item, 6000, false)
		{
		}

		public SlideShow (BrowsablePointer item, uint interval_ms, bool init) : base ()
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

			flip = new Delay (interval_ms, delegate {item.MoveNext (true); return true;});
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
		Delay flip;
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
				if (prev != null)
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

				using (ImageFile img = ImageFile.Create (item.Current.DefaultVersionUri)) {
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
					} catch (Exception) {
						next = PixbufUtils.ErrorPixbuf;
					}
				}
		}

		double progress = 0;
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
			if (prev != null)
				prev.Dispose ();
			if (next != null)
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
