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

using FSpot.Resources.Lang;
using FSpot.Settings;

using Gdk;

using Gtk;

namespace FSpot.Editors
{
	class RedEyeEditor : Editor
	{
		public RedEyeEditor () : base (Strings.RedEyeReduction, "red-eye-remove")
		{
			// FIXME: ??? need tooltip Strings.RemoveRedEyeFromThePhoto;
			NeedsSelection = true;
			ApplyLabel = Strings.FixExclamation;
		}

		public override Widget ConfigurationWidget ()
		{
			return new Label (Strings.SelectTheEyesYouWishToFix);
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
