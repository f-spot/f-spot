/*
 * InfoBox.cs
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing  <lewing@novell.com>
 * 	Gabriel Burt
 *	Stephane Delcroix  <stephane@delcroix.org>
 * 	Mike Gemuende <mike@gemuende.de>
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */
 
using Gtk;
using System;
using System.IO;
using FSpot.Utils;
using FSpot.Widgets;
using Mono.Unix;


namespace FSpot {
	
// FIXME TODO: We want to use something like EClippedLabel here throughout so it handles small sizes
// gracefully using ellipsis.

	public class InfoBox : ScrolledWindow {
		Delay update_delay;
		bool up_to_date = false;
		Sidebar sidebar;
		
		
		private IBrowsableCollection collection;
		
		public IBrowsableCollection Collection {
			set {
				collection = value;
				
				if (sidebar != null && !sidebar.isActive (this))
					up_to_date = false;
				else
					update_delay.Start ();
			}
			
			get {
				return collection;
			}
		}

		public Sidebar ParentSidebar {
			set {
				this.sidebar = value;
			}
		}
		
		public delegate void VersionIdChangedHandler (InfoBox info_box, uint version_id);
		public event VersionIdChangedHandler VersionIdChanged;
		
		// Widgetry.
		private VBox main_vbox;
		
		private InfoVBox info_vbox;
		private VBox locations_vbox;
		private VBox tags_vbox;
		
		private int thumbnail_size = 32;
		
		private void HandleExposeEvent (object sender, ExposeEventArgs args)
		{
			if (!up_to_date)
			{
				update_delay.Start ();
			}
		}

		private void HandleInfoVBoxVersionIdChanged (InfoVBox info_vbox, uint version_id)
		{
			if (VersionIdChanged != null)
				VersionIdChanged (this, version_id);
		}

		private void SetupWidgets ()
		{
			main_vbox = new VBox ();
			main_vbox.Spacing = 48;
			
			AddWithViewport (main_vbox);
			((Viewport) Child).ShadowType = ShadowType.None;
			BorderWidth = 3;
			
			info_vbox = new InfoVBox ();
			info_vbox.VersionIdChanged += HandleInfoVBoxVersionIdChanged;
			
			VBox vbox = new VBox ();
			vbox.Spacing = 12;
			Label title = new Label (String.Format ("<b>{0}</b>", Catalog.GetString ("Image")));
			title.UseMarkup = true;
			title.Xalign = 0;
			vbox.PackStart (title, false, false, 3);
			vbox.PackStart (info_vbox, false, false, 3);
			main_vbox.PackStart (vbox, false, false, 0);
			
			vbox = new VBox ();
			vbox.Spacing = 12;

			title = new Label (String.Format ("<b>{0}</b>", Catalog.GetString ("Exported Locations")));
			title.UseMarkup = true;
			title.Xalign = 0;
			vbox.PackStart (title, false, false, 3);
			
			locations_vbox = new VBox ();
			vbox.PackStart (locations_vbox, false, false, 3);
			main_vbox.PackStart (vbox, false, false, 0);
			
			vbox = new VBox ();
			vbox.Spacing = 12;
			
			title = new Label (String.Format ("<b>{0}</b>", Catalog.GetString ("Tags")));
			title.UseMarkup = true;
			title.Xalign = 0;
			vbox.PackStart (title, false, false, 3);
			
			tags_vbox = new VBox ();
			tags_vbox.Spacing = 3;
			vbox.PackStart (tags_vbox, false, false, 3);
			
			main_vbox.PackStart (vbox, false, false, 0);
			main_vbox.ShowAll ();
		}
		
		public void HandleSelectionChanged (IBrowsableCollection collection) {
			Collection = collection;
		}

		public void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args) {
			if (sidebar != null && !sidebar.isActive (this))
				up_to_date = false;
			else
				update_delay.Start ();
		}

		private void Clear ()
		{
			locations_vbox.Hide ();
			tags_vbox.Hide ();
		}		
		
		public bool Update ()
		{
			up_to_date = true;
			if (collection != null && collection.Count > 0)
			{
				if (collection.Count == 1)
					UpdateSingleSelection (collection [0]);
				else
					UpdateMultipleSelection ();
			} else {
				info_vbox.Clear ();
				Clear ();
			}
			return false;
		}
		
		public void UpdateMultipleSelection ()
		{
			locations_vbox.Hide ();
			tags_vbox.Hide ();
			info_vbox.UpdateMultipleSelection (collection);
		}
			
		public void UpdateSingleSelection (IBrowsableItem photo)
		{
			info_vbox.UpdateSingleSelection (photo);
			
			if (photo == null) {
				Clear ();
				return;
			}
			
			Photo p = photo as Photo;
			if (p != null) {			
				if (Core.Database != null)
				{
					foreach (Widget widget in locations_vbox.Children) {
						locations_vbox.Remove (widget);
					}
				
					foreach (ExportItem export in Core.Database.Exports.GetByImageId (p.Id, p.DefaultVersionId)) {
						string url = GetExportUrl (export);
						string label = GetExportLabel (export);
						if (url == null || label == null)
							continue;

						if (url.StartsWith ("/"))
						{
	            	    	// do a lame job of creating a URI out of local paths
	            	    	url  = "file://" + url;
	            	    }
	            	    	LinkButton lb = new Gtk.LinkButton (label);
		
							lb.Relief = ReliefStyle.None;
						
							lb.Uri = url;
							lb.Clicked += OnLinkButtonClicked;
							locations_vbox.PackStart (lb, false, false, 0);
	            	}
	            }
	            	    
	            foreach (Widget widget in tags_vbox.Children) {
	            	tags_vbox.Remove (widget);
				}
				
				foreach (Tag tag in photo.Tags) {
					HBox hbox = new HBox ();
					hbox.Spacing = 6;
					
					Label label = new Label (tag.Name);
					label.Xalign = 0;

					Gdk.Pixbuf icon = tag.Icon;

					Category category = tag.Category;
					while (icon == null && category != null) {
						icon = category.Icon;
						category = category.Category;
					}
					
					if (icon == null)
						continue;
					
					Gdk.Pixbuf scaled_icon;
					if (icon.Width == thumbnail_size) {
						scaled_icon = icon;
					} else {
						scaled_icon = icon.ScaleSimple (thumbnail_size, thumbnail_size, Gdk.InterpType.Bilinear);
					}
					
					hbox.PackStart (new Image (scaled_icon), false, false, 0);
					hbox.PackStart (label, false, false, 0);
					
					tags_vbox.PackStart (hbox, false, false, 0);
				}
	            	    
			}
			
			if (locations_vbox.Children.Length != 0)
				locations_vbox.ShowAll ();
			else
				locations_vbox.Hide ();

			if (tags_vbox.Children.Length != 0)
				tags_vbox.ShowAll ();
			else
				tags_vbox.Hide ();
		}
		
		static void OnLinkButtonClicked (object o, EventArgs args)
	    {
	    	GnomeUtil.UrlShow ((o as LinkButton).Uri);
	    }
		
		private static string GetExportLabel (ExportItem export)
		{
			switch (export.ExportType) {
			case ExportStore.FlickrExportType:
				string[] split_token = export.ExportToken.Split (':');
				return String.Format ("Flickr ({0})", split_token[1]);
			case ExportStore.OldFolderExportType:	//Obsolete, remove after db rev4
				return Catalog.GetString ("Folder");
			case ExportStore.FolderExportType:
				return Catalog.GetString ("Folder");
			case ExportStore.PicasaExportType:
				return Catalog.GetString ("Picasaweb");
			case ExportStore.SmugMugExportType:
				return Catalog.GetString ("SmugMug");
			case ExportStore.Gallery2ExportType:
				return Catalog.GetString ("Gallery2");
			default:
				return null;
			}
		}
			
		private static string GetExportUrl (ExportItem export)
		{
			switch (export.ExportType) {
			case ExportStore.FlickrExportType:
				string[] split_token = export.ExportToken.Split (':');
				return String.Format ("http://www.{0}/photos/{1}/{2}/", split_token[2],
	                                                     split_token[0], split_token[3]);
			case ExportStore.FolderExportType:
				Gnome.Vfs.Uri uri = new Gnome.Vfs.Uri (export.ExportToken);
				return (uri.HasParent) ? uri.Parent.ToString () : export.ExportToken;
			case ExportStore.Gallery2ExportType:
				string[] split_item = export.ExportToken.Split (':');
				return String.Format ("{0}:{1}?g2_itemId={2}",split_item[0], split_item[1], split_item[2]);
			case ExportStore.OldFolderExportType:	//This is obsolete and meant to be removed once db reach rev4
			case ExportStore.PicasaExportType:
			case ExportStore.SmugMugExportType:
				return export.ExportToken;
			default:
				return null;
			}
		}

		// Constructor.
		public InfoBox ()
		{
			SetupWidgets ();
			update_delay = new Delay (Update);
			update_delay.Start ();
			ExposeEvent += HandleExposeEvent;
			
			BorderWidth = 6;
		}

	}
}
			
