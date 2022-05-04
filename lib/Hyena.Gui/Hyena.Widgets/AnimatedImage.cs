//
// AnimatedImage.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using Hyena.Gui.Theatrics;

namespace Hyena.Widgets
{
	public class AnimatedImage : Image
	{
		Gdk.Pixbuf pixbuf;
		Gdk.Pixbuf inactive_pixbuf;
		Gdk.Pixbuf[] frames;
		int frame_width;
		int frame_height;
		int max_frames;
		bool active_frozen;

		SingleActorStage stage = new SingleActorStage ();

		public AnimatedImage ()
		{
			stage.Iteration += OnIteration;
			stage.Reset ();
			stage.Actor.CanExpire = false;
		}

		protected AnimatedImage (IntPtr raw) : base (raw)
		{
		}

		protected override void OnShown ()
		{
			base.OnShown ();

			if (active_frozen && !stage.Playing) {
				stage.Play ();
			}
		}

		protected override void OnHidden ()
		{
			base.OnHidden ();

			active_frozen = Active;
			if (stage.Playing) {
				stage.Pause ();
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (allocation != Allocation) {
				base.OnSizeAllocated (allocation);
			}
		}

		public void Load ()
		{
			ExtractFrames ();
			base.Pixbuf = frames[0];
		}

		void OnIteration (object o, EventArgs args)
		{
			if (!Visible) {
				return;
			}

			if (frames == null || frames.Length == 0) {
				return;
			} else if (frames.Length == 1) {
				base.Pixbuf = frames[0];
				return;
			}

			// The first frame is the idle frame, so skip it when animating
			int index = (int)Math.Round ((double)(frames.Length - 2) * stage.Actor.Percent) + 1;
			if (base.Pixbuf != frames[index]) {
				base.Pixbuf = frames[index];
			}
		}

		void ExtractFrames ()
		{
			if (pixbuf == null) {
				throw new ApplicationException ("No source pixbuf specified");
			} else if (pixbuf.Width % frame_width != 0 || pixbuf.Height % frame_height != 0) {
				throw new ApplicationException ("Invalid frame dimensions");
			}

			int rows = pixbuf.Height / frame_height;
			int cols = pixbuf.Width / frame_width;
			int frame_count = rows * cols;

			frames = new Gdk.Pixbuf[max_frames > 0 ? max_frames : frame_count];

			for (int y = 0, n = 0; y < rows; y++) {
				for (int x = 0; x < cols; x++, n++) {
					frames[n] = new Gdk.Pixbuf (pixbuf, x * frame_width, y * frame_height,
						frame_width, frame_height);

					if (max_frames > 0 && n >= max_frames - 1) {
						return;
					}
				}
			}
		}

		public bool Active {
			get { return !Visible ? active_frozen : stage.Playing; }
			set {
				if (value) {
					active_frozen = true;
					if (Visible) {
						stage.Play ();
					}
				} else {
					active_frozen = false;
					if (stage.Playing) {
						stage.Pause ();
					}

					if (inactive_pixbuf != null) {
						base.Pixbuf = inactive_pixbuf;
					} else if (frames != null && frames.Length > 1) {
						base.Pixbuf = frames[0];
					} else {
						base.Pixbuf = null;
					}
				}
			}
		}

		public int FrameWidth {
			get { return frame_width; }
			set { frame_width = value; }
		}

		public int FrameHeight {
			get { return frame_height; }
			set { frame_height = value; }
		}

		public int MaxFrames {
			get { return max_frames; }
			set { max_frames = value; }
		}

		public new Gdk.Pixbuf Pixbuf {
			get { return pixbuf; }
			set { pixbuf = value; }
		}

		public Gdk.Pixbuf InactivePixbuf {
			get { return inactive_pixbuf; }
			set {
				inactive_pixbuf = value;
				if (!Active) {
					base.Pixbuf = value;
				}
			}
		}
	}
}
