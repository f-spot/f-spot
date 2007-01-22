/*
 * Editors/SoftFocus.cs
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
using System;

namespace FSpot.Editors {
	public class SoftFocus : EffectEditor {
		Widgets.SoftFocus soft; 
		Scale scale;
		bool double_buffer;

		public SoftFocus (PhotoImageView view) : base (view)
		{
		}


		protected override void SetView (PhotoImageView value)
		{

			if (view != null)
				view.DoubleBuffered = double_buffer;

		
			base.SetView (value);
			
			if (value == null)
				return;

			soft = new Widgets.SoftFocus (info);
			effect = (IEffect) soft;
			double_buffer = (view.WidgetFlags & WidgetFlags.DoubleBuffered) == WidgetFlags.DoubleBuffered;
			view.DoubleBuffered = true;
		}

		protected override Widget CreateControls ()
		{
			VBox box = new VBox ();
			scale = new HScale (0, 45, 1);
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

		private void HandleCancel (object sender, EventArgs args)
		{
			Close ();
		}

		private void HandleApply (object sender, EventArgs args)
		{
			Console.WriteLine ("wake up man, this is never going to work ;)");
			Close ();
		}

		private void HandleValueChanged (object sender, EventArgs args)
		{
			soft.Radius = ((Scale)sender).Value;
			if (view != null)
				view.QueueDraw ();
		}
	}
}
