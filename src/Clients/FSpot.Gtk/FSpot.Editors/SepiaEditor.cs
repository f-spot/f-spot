//
// SepiaEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.ColorAdjustment;
using FSpot.Resources.Lang;

using Gdk;

namespace FSpot.Editors
{
	class SepiaEditor : Editor
	{
		public SepiaEditor () : base (Strings.SepiaTone, "color-sepia")
		{
			// FIXME: need tooltip Catalog.GetString ("Convert the photo to sepia tones")
			CanHandleMultiple = true;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			var sepia = new SepiaTone (input, input_profile);
			return sepia.Adjust ();
		}
	}
}
