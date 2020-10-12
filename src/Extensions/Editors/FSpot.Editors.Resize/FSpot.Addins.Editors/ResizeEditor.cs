//
// ResizeEditor.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

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
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (State.Items.First ().DefaultVersion.Uri))
				using (Pixbuf p = img.Load ())
					max = Math.Max (p.Width, p.Height);

			size = new SpinButton (128, max, 10);
			size.Value = max;
			return size;
		}
	}
}
