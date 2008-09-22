/*
 * FlipEditor.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using FSpot;
using FSpot.Editors;
using Gdk;
using Mono.Unix;

namespace FSpot.Addins.Editors {
    class FlipEditor : Editor {
        public FlipEditor () : base (Catalog.GetString ("Flip"), "object-flip-horizontal") {
			CanHandleMultiple = true;
        }

        protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
			Pixbuf output = (Pixbuf) input.Clone ();
			return output.Flip (true);
        }
    }
}
