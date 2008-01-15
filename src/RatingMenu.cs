/*
 * RatingMenu.cs
 *
 * Author(s)
 * 	Bengt Thuree  <bengt@thuree.com>
 * 	Stephane Delcroix  <stephane@delcroix.org>
 * 	
 * See COPYING for licence terms
 *
 */

using Gtk;
using System;

public class RatingMenu : Menu {
	private MenuItem parent_item;

	public delegate void RatingSelectedHandler (int r);
	public event RatingSelectedHandler RatingSelected;
	
	public class RatingMenuItem : Gtk.MenuItem {
		private int rating;

		public int Rating {
			get { return rating; }
			set { rating = value; }
		}

		public RatingMenuItem (string label): base (label)
		{
			Rating = -1;
		}

		public RatingMenuItem (int r)
		{
			Rating = r;
			if (r >= 0) {
				FSpot.Widgets.RatingSmall rating_small = new FSpot.Widgets.RatingSmall(r,false);
				Add(rating_small);
			} else {
				Label unrated = new Label(Mono.Unix.Catalog.GetString("Not rated"));
				Add(unrated);
			}
		}
	}

	public RatingMenu (MenuItem item) 
	{
		if (item != null) {
			item.Submenu = this;
			item.Activated += HandlePopulate;
			parent_item = item;
		}
	}

	protected RatingMenu (IntPtr raw) : base (raw) {}

	private void HandlePopulate (object obj, EventArgs args)
	{
		Populate(this);
	}

	bool populated = false;

        public void Populate ()
	{
		Populate (this);
	}

        public void Populate (Gtk.Menu parent)
	{
		if (!populated) {
			for (int k = -1; k <= 5; k ++) {
				RatingMenuItem item = new RatingMenuItem (k);
				parent.Append (item);
				item.ShowAll ();
				item.Activated += HandleActivate;
			}
			populated = true;
		}
	}
	
	void HandleActivate (object obj, EventArgs args)
	{
		if (RatingSelected != null) {
			RatingMenuItem t = obj as RatingMenuItem;
			if (t != null)
				RatingSelected (t.Rating);
			else 
				Console.WriteLine ("Item was not rating usable");
		}
	}
}
