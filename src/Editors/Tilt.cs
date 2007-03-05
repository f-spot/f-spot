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
using Mono.Unix;

namespace FSpot.Editors {
	public sealed class Tilt : EffectEditor {
		Scale scale;

		public Tilt (PhotoImageView view) : base (view)
		{
		}

		protected override string GetTitle ()
		{
			return Catalog.GetString ("Straighten");
		}

		protected override void SetView (PhotoImageView view)
		{
			base.SetView (view);
			effect = new Widgets.Tilt (info);
		}

		protected override Widget CreateControls ()
		{
			VBox box = new VBox ();
			box.Spacing = 12;
			box.BorderWidth = 12;
			scale = new HScale (-45, 45, 1);
			scale.Value = 0.0;
			scale.ValueChanged += HandleValueChanged;
			scale.WidthRequest = 250;
			box.PackStart (scale);
			HBox actions = new HBox ();
			actions.Spacing = 12;
			Button cancel = new Button ("Cancel");
			cancel.Clicked += HandleCancel;
			actions.PackStart (cancel);
			Button apply = new Button ("Apply");
			apply.Clicked += HandleApply;
			actions.PackStart (apply);
			box.PackStart (actions);
			return box;
		}

		private void HandleApply (object sender, EventArgs args)
		{
			System.Console.WriteLine ("Clicked");
			TiltAction action = new TiltAction (view.Item, ((Widgets.Tilt)effect).Angle);
			action.Activate ();
			Destroy ();
		}

		private void HandleCancel (object sender, EventArgs args)
		{
			Destroy ();
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			((Widgets.Tilt)effect).Angle = scale.Value * Math.PI / 180;
			view.QueueDraw ();
		}
	}
}
