//
// SoftFocusEditor.cs
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

using Gtk;

namespace FSpot.Editors
{
	// TODO: This had a keybinding e. Maybe we should add it back, but did people even knew it?
	class SoftFocusEditor : Editor
	{
		Scale scale;

		public SoftFocusEditor () : base (PhotoEditorCatalog.SoftFocus)
		{
		}

		public override Widget ConfigurationWidget ()
		{
			scale = new HScale (0, 1, .01);
			scale.Value = 0.5;
			scale.ValueChanged += HandleValueChanged;
			return scale;
		}

		protected override PhotoEditorContext CreateContext ()
		{
			var context = base.CreateContext ();
			context.SoftFocusRadius = scale.Value;
			return context;
		}

		void HandleValueChanged (object sender, System.EventArgs args)
		{
			UpdatePreview ();
		}
	}
}
