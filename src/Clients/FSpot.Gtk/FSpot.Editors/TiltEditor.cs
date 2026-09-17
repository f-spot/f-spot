//
// TiltEditor.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace FSpot.Editors
{
	// TODO: there were keybindings (left/right) to adjust tilt, maybe they should be added back.
	class TiltEditor : Editor
	{
		Scale scale;

		public TiltEditor () : base (PhotoEditorCatalog.Tilt)
		{
		}

		public override Widget ConfigurationWidget ()
		{
			scale = new HScale (-45, 45, 1);
			scale.Value = 0.0;
			scale.ValueChanged += HandleValueChanged;
			return scale;
		}

		protected override PhotoEditorContext CreateContext ()
		{
			var context = base.CreateContext ();
			context.TiltDegrees = scale.Value;
			return context;
		}

		void HandleValueChanged (object sender, EventArgs args)
		{
			UpdatePreview ();
		}
	}
}
