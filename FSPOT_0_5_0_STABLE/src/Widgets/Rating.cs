/*
 * Rating.cs
 *
 * Author[s]
 *    Gabriel Burt (original widget in Banshee)
 *    Cosme Sevestre (original porting to F-Spot)
 *    Stephane Delcroix
 *
 * Copyright (C) 2006 by the respective authors.
 *
 * This is free software, see COPYING for details
 *
 */

using Gtk;
using Gdk;
using System;
using FSpot.Utils;

namespace FSpot.Widgets
{
	public class Rating : Gtk.EventBox
	{
		int rating;
		Pixbuf display_pixbuf;
		public object RatedObject;
		bool mouse_over;
		bool editable;

		protected static int max_rating = 5;
		static Pixbuf icon_rated;
		static Pixbuf icon_blank;

		public event EventHandler Changed;
		
		public Rating () : this (0, true)
		{
		}

		public Rating (bool editable) : this (0, editable)
		{
		}

		public Rating (int rating) : this (rating, true)
		{
		} 

		public Rating (int rating, bool editable)
		{
			this.rating = rating;
			this.editable = editable;
			
			MouseOver = false;
			EnterNotifyEvent += HandleMouseEnter;
			LeaveNotifyEvent += HandleMouseLeave;
			
			VisibleWindow = false;
			CanFocus = true;
			
			display_pixbuf = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, Width, Height);
			
			display_pixbuf.Fill (0xffffff00);
			DrawRating (DisplayPixbuf, Value);
			Add (new Gtk.Image (display_pixbuf));
			
			ShowAll ();
		}
		
		~Rating ()
		{
			display_pixbuf.Dispose ();
			display_pixbuf = null;
			
			icon_rated = null;
			icon_blank = null;
		}
		
		public Pixbuf DrawRating (int val)
		{
			Pixbuf buf = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, Width, Height);
			DrawRating (buf, val);
			return buf;
		}
		
		public virtual void DrawRating (Pixbuf pbuf, int val)
		{
			// Clean pixbuf
			pbuf.Fill (0xffffff00);
			
			//Stars
			for (int i = 0; i < MaxRating; i ++)
				if (i <= val - 1)
					IconRated.CopyArea (0, 0, IconRated.Width, IconRated.Height, 
							pbuf, (i + 1) * IconRated.Width, 0);
				else {
					IconNotRated.CopyArea (0, 0, IconRated.Width, IconRated.Height, 
							pbuf, (i + 1) * IconRated.Width, 0);
				}
		}

		public void SetValueFromPosition (int x)
		{
			Value = RatingFromPosition (x);
		}
		
		private int RatingFromPosition (double x)
		{
			int pos = (int) (x / (double) IconRated.Width);
			
			return pos;
		}
		
		private void HandleMouseEnter (object sender, EventArgs args)
		{
			mouse_over = true;
			Redraw ();
		}
		
		private void HandleMouseLeave (object sender, EventArgs args)
		{
			mouse_over = false;
			Redraw ();
		}
		
		// Event Handlers
		[GLib.ConnectBefore]
		protected override bool OnButtonPressEvent (Gdk.EventButton eb)
		{
			if (editable) {
				if (eb.Button != 1)
					return false;
			
				Value = RatingFromPosition (eb.X);
			}
			return true;
		}
		
		public bool HandleKeyPress (Gdk.EventKey ek)
		{
			return this.OnKeyPressEvent (ek);
		}
		
		[GLib.ConnectBefore]
		protected override bool OnKeyPressEvent (Gdk.EventKey ek)
		{
			if (editable) {
				switch (ek.Key) {
				case Gdk.Key.Up:
				case Gdk.Key.Right:
				case Gdk.Key.plus:
				case Gdk.Key.equal:
					Value ++;
					return true;
					
				case Gdk.Key.Down:
				case Gdk.Key.Left:
				case Gdk.Key.minus:
					Value --;
					return true;
				}
				
				if (ek.KeyValue >= (48 + 1) &&
				       ek.KeyValue <= (48 + MaxRating) &&
				       ek.KeyValue <= 59) {
					Value = (int) ek.KeyValue - 48;
					return true;
				}
				
				return false;
			} else
				return true;
		}
		
		[GLib.ConnectBefore]
		protected override bool OnScrollEvent (EventScroll args)
		{
			return HandleScroll (args);
		}

		public bool HandleScroll (EventScroll args)
		{
			if (editable) {
				switch (args.Direction) {
				case Gdk.ScrollDirection.Up:
				case Gdk.ScrollDirection.Right:
					Value ++;
					return true;
					
				case Gdk.ScrollDirection.Down:
				case Gdk.ScrollDirection.Left:
					Value --;
					return true;
				}
				
				return false;
			} else
				return true;
		}
		
		[GLib.ConnectBefore]
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (editable) {
				// TODO draw highlights onmouseover a rating? (and clear on leaveNotify)
				if (evnt.State != Gdk.ModifierType.Button1Mask)
					return false;
			
				Value = RatingFromPosition (evnt.X);
			}
			return true;
		}
		
		[GLib.ConnectBefore]
		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			return true;
		}
		
		[GLib.ConnectBefore]
		protected override bool OnFocused (DirectionType direction)
		{
			return true;
		}

		private void Redraw()
		{
			DrawRating (DisplayPixbuf, Value);
			QueueDraw ();
		}

		// Event Changed Dispatcher
		private void OnChanged ()
		{
			Redraw();
			EventHandler changed = Changed;
			
			if (changed != null)
				changed (this, new EventArgs ());
		}
		
		// Properties
		public int Value {
			get { return rating; }
			
			set {
				// Same rating
				if (rating == value)
					return;
				// Remove.trash.1-5 rating
				if (value >= 0 && value <= max_rating) {
					rating = value;
					OnChanged ();
				}
			}
		}
		
		public Pixbuf DisplayPixbuf {
			get { return display_pixbuf; }
		}
		
		public bool MouseOver {
			get { return mouse_over; }
			set { mouse_over = value; }
		}
		
		public int MaxRating {
			get { return max_rating; }
		}
		
		public virtual int NumLevels {
			get { return max_rating + 1; }
		}
		
		public virtual Pixbuf IconRated {
			get {
				if (icon_rated == null)
					icon_rated = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "rating-rated", 16, (Gtk.IconLookupFlags)0);
				
				return icon_rated;
			}
			
			set { icon_rated = value; }
		}
		
		public virtual Pixbuf IconNotRated {
			get {
				if (icon_blank == null)
					icon_blank = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "rating-blank", 16, (Gtk.IconLookupFlags)0);
				
				return icon_blank;
			}
			
			set { icon_blank = value; }
		}
		
		public virtual int Width {
			get { return IconRated.Width * NumLevels; }
		}
		
		public virtual int Height {
			get { return IconRated.Height; }
		}
	}

	public class RatingSmall : Rating
	{
		static Pixbuf icon_rated_small;
		static Pixbuf icon_blank_small;
		
		public RatingSmall () : base (0)
		{
		}

		public RatingSmall (int rating) : base (rating) 
		{
		}

		public RatingSmall (bool editable) : base (editable)
		{
		}

		public RatingSmall (int rating, bool editable) : base (rating, editable)
		{
		}

		public override void DrawRating (Pixbuf pbuf, int val)
		{
			// Clean pixbuf
			pbuf.Fill (0xffffff00);
			
			//Stars
			for (int i = 0; i < MaxRating; i ++)
				if (i <= val - 1)
					IconRated.CopyArea (0, 0, IconRated.Width, IconRated.Height, 
							pbuf, i * IconRated.Width, 0);
		}
		
		public override Pixbuf IconRated {
			get {
				if (icon_rated_small == null)
					icon_rated_small = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "rating-rated", 16, (Gtk.IconLookupFlags)0);
				
				return icon_rated_small;
			}
		}
		
		public override Pixbuf IconNotRated {
			get {
				if (icon_blank_small == null)
					icon_blank_small = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "rating-blank", 16, (Gtk.IconLookupFlags)0);
				
				return icon_blank_small;
			}
		}
		
		public override int NumLevels {
			get { return max_rating; }
		}
	}
}
