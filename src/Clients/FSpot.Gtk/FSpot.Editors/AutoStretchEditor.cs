//
// AutoStretchEditor.cs
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
	class AutoStretchEditor : Editor
	{
		public AutoStretchEditor () : base (Strings.AutoColor, "autocolor")
		{
			// FIXME: need tooltip Catalog.GetString ("Automatically adjust the colors")
			CanHandleMultiple = true;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			var autostretch = new AutoStretch (input, input_profile);
			return autostretch.Adjust ();
		}
	}
}
