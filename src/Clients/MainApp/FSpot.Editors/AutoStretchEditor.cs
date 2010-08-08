/*
 * SepiaEditor.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using FSpot;
using FSpot.ColorAdjustment;
using Gdk;
using Mono.Unix;

namespace FSpot.Editors {
    class AutoStretchEditor : Editor {
        public AutoStretchEditor () : base (Catalog.GetString ("Auto Color"), "autocolor") {
			// FIXME: need tooltip Catalog.GetString ("Automatically adjust the colors")
			CanHandleMultiple = true;
        }

        protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
            AutoStretch autostretch = new AutoStretch (input, input_profile);
            return autostretch.Adjust ();
        }
    }
}
