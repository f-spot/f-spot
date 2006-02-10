namespace FSpot {
	public class FlickrExport : GladeDialog {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.CheckButton    scale_check;
		[Glade.Widget] Gtk.CheckButton    meta_check;
		[Glade.Widget] Gtk.CheckButton    tag_check;
		[Glade.Widget] Gtk.CheckButton    open_check;
		//[Glade.Widget] Gtk.Entry          email_entry;
		[Glade.Widget] Gtk.SpinButton     size_spin;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Button         auth_flickr;
		[Glade.Widget] Gtk.Button         auth_done_flickr;
		[Glade.Widget] Gtk.Button         do_export_flickr;
		
		System.Threading.Thread command_thread;
		ThreadProgressDialog progress_dialog;
		ProgressItem progress_item;
		
		bool open;
		bool scale;
		bool copy_metadata;

		string token;

		int photo_index;
		int size;

		FlickrRemote fr;

		public FlickrExport (IBrowsableCollection selection) : base ("flickr_export_dialog")
		{
			this.selection = selection;

			IconView view = new IconView (selection);
			view.DisplayDates = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;
			auth_flickr.Clicked += HandleLogin;

			LoadPreference (Preferences.EXPORT_FLICKR_SCALE);
			LoadPreference (Preferences.EXPORT_FLICKR_SIZE);
			LoadPreference (Preferences.EXPORT_FLICKR_BROWSER);
			LoadPreference (Preferences.EXPORT_FLICKR_TAGS);
			LoadPreference (Preferences.EXPORT_FLICKR_STRIP_META);
			LoadPreference (Preferences.EXPORT_FLICKR_TOKEN);

			do_export_flickr.Sensitive = false;
			if (token != null && token.Length > 0) {
				GLib.Idle.Add (IdleLogin);
			}

			fr = new FlickrRemote (token);
		}

		public void HandleSizeActive (object sender, System.EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		private void Login () {
			fr.Progress = null;
			fr.tryWebLogin();
			token = fr.Token;
		}

		private void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double) selection.Count;
		}
		
		private void Upload () {
			fr.Progress = new ProgressItem ();
			fr.Progress.Changed += HandleProgressChanged;
			System.Collections.ArrayList ids = new System.Collections.ArrayList ();
			try {
				foreach (IBrowsableItem photo in selection.Items) {
					progress_dialog.Message = System.String.Format (
                                                Mono.Posix.Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);

					progress_dialog.Fraction = photo_index / (double)selection.Count;
					photo_index++;
					progress_dialog.ProgressText = System.String.Format (
						Mono.Posix.Catalog.GetString ("{0} of {1}"), photo_index, 
						selection.Count);

					string id = fr.Upload (photo, scale, size, copy_metadata);
					ids.Add (id);
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Upload Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
				}
			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Uploading To Flickr");
			}

			if (open && ids.Count != 0) {
				string view_url = "http://www.flickr.com/tools/uploader_edit.gne?ids";
				bool first = true;

				foreach (string id in ids) {
					view_url = view_url + (first ? "=" : ",") + id;
					first = false;
				}
				GnomeUtil.UrlShow (null, view_url);
			}
		}
		
		private void HandleLogin (object sender, System.EventArgs args)
		{
			Login ();
			do_export_flickr.Sensitive = true;
		}
		
		private bool IdleLogin ()
		{
			HandleLogin (null, null);
			return false;
		}
		
		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}
			
			if (!fr.CheckLogin()) {
				do_export_flickr.Sensitive = false;
				HigMessageDialog md = 
					new HigMessageDialog (Dialog, 
							      Gtk.DialogFlags.Modal |
							      Gtk.DialogFlags.DestroyWithParent,
							      Gtk.MessageType.Error, Gtk.ButtonsType.Ok, 
							      Mono.Posix.Catalog.GetString ("Unable to log on."),
							      Mono.Posix.Catalog.GetString ("F-Spot was unable to log on to Flickr.  Make sure you have given the authentication using Flickr web browser interface."));
				md.Run ();
				md.Destroy ();
				return;
			}
			
			fr.ExportTags = tag_check.Active;
			open = open_check.Active;
			scale = scale_check.Active;
			copy_metadata = open_check.Active;

			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new  System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
			
			Dialog.Destroy ();
			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();
			
			// Save these settings for next time
			Preferences.Set (Preferences.EXPORT_FLICKR_SCALE, scale);
			Preferences.Set (Preferences.EXPORT_FLICKR_SIZE, size);
			Preferences.Set (Preferences.EXPORT_FLICKR_BROWSER, open);
			Preferences.Set (Preferences.EXPORT_FLICKR_TAGS, tag_check.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_STRIP_META, meta_check.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_TOKEN, fr.Token);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			//System.Console.WriteLine("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.EXPORT_FLICKR_SCALE:
				if (scale_check.Active != (bool) val)
					scale_check.Active = (bool) val;
				break;

			case Preferences.EXPORT_FLICKR_SIZE:
				size_spin.Value = (double) (int) val;
				break;

			case Preferences.EXPORT_FLICKR_BROWSER:
				if (open_check.Active != (bool) val)
					open_check.Active = (bool) val;
				break;

			case Preferences.EXPORT_FLICKR_TAGS:
				if (tag_check.Active != (bool) val)
					tag_check.Active = (bool) val;
				break;

			case Preferences.EXPORT_FLICKR_STRIP_META:
				if (meta_check.Active != (bool) val)
					meta_check.Active = (bool) val;
				break;
			case Preferences.EXPORT_FLICKR_TOKEN:
				token = (string) val;
			        break;
				/*				
			case Preferences.EXPORT_FLICKR_EMAIL:
				email_entry.Text = (string) val;
				break;
				*/
			}
		}
	}

}
