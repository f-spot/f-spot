/* 
 * ImageDisplay.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using Gtk;
using Gdk;
using System;
using Cairo;
using FSpot;
using FSpot.Utils;

namespace FSpot.Widgets {
	[Binding(Gdk.Key.Up, "Up")]
	[Binding(Gdk.Key.Down, "Down")]
	[Binding(Gdk.Key.space, "Pan")]
	[Binding(Gdk.Key.R, "RevealImage")]
	[Binding(Gdk.Key.P, "PushImage")]
	public class ImageDisplay : Gtk.EventBox {
		ImageInfo current;
		ImageInfo next;
		BrowsablePointer item;
		ITransition transition;
		Delay delay;
		int index = 0;
		int block_size = 256;

		ITransition Transition {
			get { return transition; }
			set { 
				if (transition != null) 
					transition.Dispose ();

				transition = value;
				

				if (transition != null)
					delay.Start ();
				else 
					delay.Stop ();
			}
		}

		public ImageDisplay (BrowsablePointer item) 
		{
			this.item = item;
			CanFocus = true;
			current = new ImageInfo (item.Current.DefaultVersionUri);
			if (item.Collection.Count > item.Index + 1) {
				next = new ImageInfo (item.Collection [item.Index + 1].DefaultVersionUri);
			}
			delay = new Delay (30, new GLib.IdleHandler (DrawFrame));
		}

		protected override void OnDestroyed ()
		{
			if (current != null) {
				current.Dispose ();
				current = null;
			}

		        if (next != null) {
				next.Dispose ();
				next = null;
			}

			Transition = null;
			
			base.OnDestroyed ();
		}
	
		public bool Up ()
		{
			Console.WriteLine ("Up");
			Transition = new Dissolve (current, next);
			return true;
		}

		public bool Down ()
		{
			Console.WriteLine ("down");
			Transition = new Dissolve (next, current);
			return true;
		}

		public bool Pan ()
		{
			Console.WriteLine ("space");
			Transition = new Wipe (current, next);
			return true;
		}
		
		public bool RevealImage ()
		{
			Console.WriteLine ("r");
			Transition = new Reveal (current, next);
			return true;
		}

		public bool PushImage ()
		{
			Console.WriteLine ("p");
			Transition = new Push (current, next);
			return true;
		}


		public bool DrawFrame ()
		{
			if (Transition != null)
				Transition.OnEvent (this);

			return true;
		}
		
		private static void SetClip (Context ctx, Gdk.Rectangle area) 
		{
			ctx.MoveTo (area.Left, area.Top);
			ctx.LineTo (area.Right, area.Top);
			ctx.LineTo (area.Right, area.Bottom);
			ctx.LineTo (area.Left, area.Bottom);
			
			ctx.ClosePath ();
			ctx.Clip ();
		}
		
		private static void SetClip (Context ctx, Region region)
		{
			foreach (Gdk.Rectangle area in region.GetRectangles ()) {
				ctx.MoveTo (area.Left, area.Top);
				ctx.LineTo (area.Right, area.Top);
				ctx.LineTo (area.Right, area.Bottom);
				ctx.LineTo (area.Left, area.Bottom);
					
				ctx.ClosePath ();
			}
			ctx.Clip ();
		}

		private void OnExpose (Context ctx, Region region)
		{
			SetClip (ctx, region);
			if (Transition != null) {
				bool done = false;
				foreach (Gdk.Rectangle area in GetRectangles (region)) {
					BlockProcessor proc = new BlockProcessor (area, block_size);
					Gdk.Rectangle subarea;
					while (proc.Step (out subarea)) {
						ctx.Save ();
						SetClip (ctx, subarea);
						done = ! Transition.OnExpose (ctx, Allocation);
						ctx.Restore ();
					}
				}
				if (done) {
					System.Console.WriteLine ("frames = {0}", Transition.Frames);
					Transition = null;
				}
			} else {
				ctx.Operator = Operator.Source;
				SurfacePattern p = new SurfacePattern (current.Surface);
				p.Filter = Filter.Fast;
				SetClip (ctx, region);
				ctx.Matrix = current.Fill (Allocation);

				ctx.Source = p;
				ctx.Paint ();
				p.Destroy ();
			}
		}

		private static Gdk.Rectangle [] GetRectangles (Gdk.Region region)
		{
#if true
			return new Gdk.Rectangle [] { region.Clipbox };
#else
			return region.GetRectangles ();
#endif
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			bool double_buffer = false;
			base.OnExposeEvent (args);

			Context ctx = Gdk.CairoHelper.Create (GdkWindow);
			//Surface glitz = CairoUtils.CreateGlitzSurface (GdkWindow);
			//Context ctx = new Context (glitz);
			if (double_buffer) {
				ImageSurface cim = new ImageSurface (Format.RGB24, 
								     Allocation.Width, 
								     Allocation.Height);

				Context buffer = new Context (cim);
				OnExpose (buffer, args.Region);

				SurfacePattern sur = new SurfacePattern (cim);
				sur.Filter = Filter.Fast;
				ctx.Source = sur;
				SetClip (ctx, args.Region);

				ctx.Paint ();

				((IDisposable)buffer).Dispose ();
				((IDisposable)cim).Dispose ();
				sur.Destroy ();
			} else {
				OnExpose (ctx, args.Region);
			}

			//glitz.Flush ();
			((IDisposable)ctx).Dispose ();
			return true;
		}
	}
}

