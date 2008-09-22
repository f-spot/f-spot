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
    class SepiaEditor : Editor {
        public SepiaEditor () : base (Catalog.GetString ("Sepia Tone"), "color-sepia") {
			// FIXME: need tooltip Catalog.GetString ("Convert the photo to sepia tones")
			CanHandleMultiple = true;
        }

        protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
            SepiaTone sepia = new SepiaTone (input, input_profile);
            return sepia.Adjust ();
        }
    }
}
