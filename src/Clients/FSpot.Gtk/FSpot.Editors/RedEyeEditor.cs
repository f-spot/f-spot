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
		public RedEyeEditor () : base (PhotoEditorCatalog.RedEye)
		{
			ApplyLabel = Strings.FixExclamation;
		}

		public override Widget ConfigurationWidget ()
		{
			return new Label (Strings.SelectTheEyesYouWishToFix);
		}


	}
}
