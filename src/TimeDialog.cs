using Gtk;

namespace FSpot {
	public class TimeDialog : GladeDialog 
	{
		[Glade.Widget] ScrolledWindow view_scrolled;
		[Glade.Widget] ScrolledWindow tray_scrolled;
		IBrowsableCollection collection;
		IconView tray;
		PhotoImageView view;

		public TimeDialog (IBrowsable collection) : base ("time_dialog")
		{
			tray = new TrayView (collection);
			tray_scrolled.Add (tray);
		       
			view = new PhotoImageView (collection);
			view_scrolled.Add (view);
			
			Dialog.ShowAll ();
		}
	}
}
