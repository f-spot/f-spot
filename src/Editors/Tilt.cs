/*
 * TiltEditor.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using System;
using Gtk;
using FSpot;
using Cairo;

namespace FSpot.Editors {
	public sealed class Tilt : EffectEditor {
		Scale scale;

		public Tilt (PhotoImageView view) : base (view)
		{
		}

		protected override void SetView (PhotoImageView view)
		{
			base.SetView (view);
			effect = new Widgets.Tilt (info);
		}

		protected override Widget CreateControls ()
		{
			VBox box = new VBox ();
			scale = new HScale (-45, 45, 1);
			scale.Value = 0.0;
			scale.ValueChanged += HandleValueChanged;
			scale.WidthRequest = 250;
			box.PackStart (scale);
			HBox actions = new HBox ();
			Button apply = new Button ("Apply");
			apply.Clicked += HandleApply;
			actions.PackStart (apply);
			Button cancel = new Button ("Cancel");
			cancel.Clicked += HandleCancel;
			actions.PackStart (cancel);
			box.PackStart (actions);
			return box;
		}

		private void HandleApply (object sender, EventArgs args)
		{
			System.Console.WriteLine ("Clicked");
			TiltAction action = new TiltAction (view.Item, ((Widgets.Tilt)effect).Angle);
			action.Activate ();
			Close ();
		}

		private void HandleCancel (object sender, EventArgs args)
		{
			Close ();
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			((Widgets.Tilt)effect).Angle = scale.Value * Math.PI / 180;
			view.QueueDraw ();
		}
	}
}
