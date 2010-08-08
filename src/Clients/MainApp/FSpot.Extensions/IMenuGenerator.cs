/*
 * FSpot.Extensions.IMenuGenerator
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

namespace FSpot.Extensions
{
	public interface IMenuGenerator
	{
		Gtk.Menu GetMenu ();
		void OnActivated (object sender, System.EventArgs e);
	}
}
