//
// ResizeEditor.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using System;

using FSpot.Core;
using FSpot.Editors;
using FSpot.Imaging;

using Gtk;
using Gdk;

using Hyena;

using Mono.Unix;

namespace FSpot.Addins.Editors
{
	class ResizeEditor : Editor
	{
		SpinButton size;

		public ResizeEditor () : base (Catalog.GetString ("Resize"), null) {
			CanHandleMultiple = false;
			HasSettings = true;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Pixbuf output = (Pixbuf) input.Clone ();
			double ratio = (double)size.Value / Math.Max (output.Width, output.Height);
			return output.ScaleSimple ((int)(output.Width * ratio), (int)(output.Height * ratio), InterpType.Bilinear);
		}

		public override Widget ConfigurationWidget ()
		{
			int max;
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (State.Items[0].DefaultVersion.Uri))
				using (Pixbuf p = img.Load ())
					max = Math.Max (p.Width, p.Height);

			size = new SpinButton (128, max, 10);
			size.Value = max;
			return size;
		}
	}
}
