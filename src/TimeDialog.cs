using System;
using Gtk;
using Gnome;

namespace FSpot {
	public class TimeDialog : GladeDialog 
	{
		[Glade.Widget] ScrolledWindow view_scrolled;
		[Glade.Widget] ScrolledWindow tray_scrolled;

		[Glade.Widget] Button back_button;
		[Glade.Widget] Button forward_button;

		[Glade.Widget] Label name_label;
		[Glade.Widget] Label old_label;
		[Glade.Widget] Label count_label;

		[Glade.Widget] DateEdit date_edit;

		[Glade.Widget] Frame tray_frame;

		IBrowsableCollection collection;
		IconView tray;
		PhotoImageView view;

		public TimeDialog (IBrowsableCollection collection) : base ("time_dialog")
		{
			this.collection = collection;

			tray = new TrayView (collection);
			tray_scrolled.Add (tray);
			tray.Selection.Changed += HandleSelectionChanged;

			view = new PhotoImageView (collection);
			view_scrolled.Add (view);
			view.Item.Changed += HandleItemChanged;
			view.Item.MoveFirst ();

			forward_button.Clicked += HandleForwardClicked;
			back_button.Clicked += HandleBackClicked;

			date_edit.TimeChanged += HandleTimeChanged;
			date_edit.DateChanged += HandleTimeChanged;

			Dialog.ShowAll ();
			HandleCollectionChanged (collection);
		}

		void HandleTimeChanged (object sender, EventArgs args)
		{
			System.Console.WriteLine ("time changed {0}", date_edit.Time);
		}

		void HandleItemChanged (BrowsablePointer pointer, BrowsablePointerChangedArgs args)
		{
			BrowsablePointer Item = view.Item;

			back_button.Sensitive = (view.Item.Index > 0 && collection.Count > 0);
			forward_button.Sensitive = (view.Item.Index < collection.Count - 1);

			if (view.Item.IsValid) {
				IBrowsableItem item = Item.Current;
				
				name_label.Text = item.Name;;
				old_label.Text = item.Time.ToString ();
				
				int i = collection.Count > 0 ? view.Item.Index + 1: 0;
				// This indicates the current photo is photo {0} of {1} out of photos
				count_label.Text = System.String.Format (Mono.Posix.Catalog.GetString ("{0} of {1}"), i, collection.Count);

				date_edit.Time = item.Time.ToUniversalTime ();
			}
		}

		void HandleForwardClicked (object sender, EventArgs args)
		{
			view.Item.MoveNext ();
		}

		void HandleBackClicked (object sender, EventArgs args)
		{
			view.Item.MovePrevious ();
		}

		void HandleSelectionChanged (IBrowsableCollection sender)
		{
			if (sender.Count > 0) {
				view.Item.Index = ((IconView.SelectionCollection)sender).Ids[0];

			}
		}

		void HandleCollectionChanged (IBrowsableCollection collection)
		{
			tray_frame.Visible = collection.Count > 1;
		}
	}
}
