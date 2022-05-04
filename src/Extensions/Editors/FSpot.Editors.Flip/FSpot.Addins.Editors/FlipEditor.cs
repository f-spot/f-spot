//
// FlipEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;
using FSpot.Resources.Lang;

using Gdk;

namespace FSpot.Addins.Editors
{
	class FlipEditor : Editor
	{
		public FlipEditor () : base (Strings.Flip, "object-flip-horizontal")
		{
			CanHandleMultiple = true;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			var output = (Pixbuf)input.Clone ();
			return output.Flip (true);
		}
	}
}
