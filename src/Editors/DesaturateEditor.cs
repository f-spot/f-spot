/*
 * DesaturateEditor.cs
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
    class DesaturateEditor : Editor {
        public DesaturateEditor () : base (Catalog.GetString ("Desaturate"), "color-desaturate") {
			// FIXME: need tooltip Catalog.GetString ("Convert the photo to black and white")
			CanHandleMultiple = true;
        }

        protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
            Desaturate desaturate = new Desaturate (input, input_profile);
            return desaturate.Adjust ();
        }
    }
}
