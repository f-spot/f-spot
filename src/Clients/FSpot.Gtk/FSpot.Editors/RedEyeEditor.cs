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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
			return new Label(Catalog.GetString ("Select the eyes you wish to fix."));
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Rectangle selection = FSpot.Utils.PixbufUtils.TransformOrientation ((int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Width : input.Height,
											    (int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Height : input.Width,
											    State.Selection, State.PhotoImageView.PixbufOrientation);
			int threshold = Preferences.Get<int> (Preferences.EDIT_REDEYE_THRESHOLD);
			return PixbufUtils.RemoveRedeye (input, selection, threshold);
		}
	}
}
