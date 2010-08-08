/*
 * RedEyeEditor.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using FSpot;
using FSpot.Utils;
using Gdk;
using Gtk;
using Mono.Unix;
using System;

namespace FSpot.Editors {
	class RedEyeEditor : Editor {
		public RedEyeEditor () : base (Catalog.GetString ("Red-eye Reduction"), "red-eye-remove") {
			NeedsSelection = true;
			ApplyLabel = Catalog.GetString ("Fix!");
		}

		public override Widget ConfigurationWidget () {
			return new Label(Catalog.GetString ("Select the eyes you wish to fix."));
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
			Rectangle selection = FSpot.Utils.PixbufUtils.TransformOrientation ((int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Width : input.Height,
											    (int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Height : input.Width,
											    State.Selection, State.PhotoImageView.PixbufOrientation);
			int threshold = Preferences.Get<int> (Preferences.EDIT_REDEYE_THRESHOLD);
			return PixbufUtils.RemoveRedeye (input, selection, threshold);
		}
	}
}
