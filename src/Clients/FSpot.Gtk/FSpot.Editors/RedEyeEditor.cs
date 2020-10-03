//
// RedEyeEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Settings;

using Gdk;

using Gtk;

using Mono.Unix;

namespace FSpot.Editors
{
	class RedEyeEditor : Editor
	{
		public RedEyeEditor () : base (Catalog.GetString ("Red-eye Reduction"), "red-eye-remove")
		{
			// FIXME: ??? need tooltip Catalog.GetString ("Remove red-eye form the photo")
			NeedsSelection = true;
			ApplyLabel = Catalog.GetString ("Fix!");
		}

		public override Widget ConfigurationWidget ()
		{
			return new Label (Catalog.GetString ("Select the eyes you wish to fix."));
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Rectangle selection = FSpot.Utils.PixbufUtils.TransformOrientation ((int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Width : input.Height,
												(int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Height : input.Width,
												State.Selection, State.PhotoImageView.PixbufOrientation);
			int threshold = Preferences.Get<int> (Preferences.EditRedeyeThreshold);
			return PixbufUtils.RemoveRedeye (input, selection, threshold);
		}
	}
}
