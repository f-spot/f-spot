using FlickrNet;
using System;
using System.IO;
using System.Threading;
using Mono.Unix;
using FSpot.Filters;

namespace FSpot {
	public class TwentyThreeHQExport : FlickrExport
	{
		public override void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.TwentyThreeHQ, selection, true);
		}
	}

	public class FlickrExport : GladeDialog, FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.CheckButton    scale_check;
		[Glade.Widget] Gtk.CheckButton    meta_check;
		[Glade.Widget] Gtk.CheckButton    tag_check;
		[Glade.Widget] Gtk.CheckButton    open_check;
		[Glade.Widget] Gtk.SpinButton     size_spin;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Button         auth_flickr;
		[Glade.Widget] Gtk.Button         auth_done_flickr;
		[Glade.Widget] Gtk.Button         do_export_flickr;
		[Glade.Widget] Gtk.Label          auth_label;
		[Glade.Widget] Gtk.RadioButton    public_radio;
		[Glade.Widget] Gtk.CheckButton    family_check;
		[Glade.Widget] Gtk.CheckButton    friend_check;

		System.Threading.Thread command_thread;
		ThreadProgressDialog progress_dialog;
		ProgressItem progress_item;

		bool open;
		bool scale;
		bool copy_metadata;

		bool is_public;
		bool is_friend;
		bool is_family;

		string token;

		int photo_index;
		int size;
		Auth auth;

		FlickrRemote fr;
		private FlickrRemote.Service current_service;

		string auth_text;
		private State state;

		private enum State {
			Disconnected,
			Connected,
			InAuth,
			Authorized
		}

		private State CurrentState {
			get { return state; }
			set {
				switch (value) {
				case State.Disconnected:
					auth_label.Text = auth_text;
					auth_flickr.Sensitive = true;
					do_export_flickr.Sensitive = false;
					auth_flickr.Label = Catalog.GetString ("Authorize");
					break;
				case State.Connected:
					auth_flickr.Sensitive = true;
					do_export_flickr.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Return to this window after you have finished the authorization process on {0} and click the \"Complete Authorization\" button below"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Complete Authorization");
					break;
				case State.InAuth:
					auth_flickr.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Logging into {0}"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Checking credentials...");
					do_export_flickr.Sensitive = false;
					break;
				case State.Authorized:
					do_export_flickr.Sensitive = true;
					auth_flickr.Sensitive = true;
					auth_label.Text = System.String.Format (Catalog.GetString ("Welcome {0} you are connected to {1}"),
										auth.User.Username,
										current_service.Name);
					auth_flickr.Label = String.Format (Catalog.GetString ("Sign in as a different user"), auth.User.Username);
					break;
				}
				state = value;
			}
		}

		public FlickrExport (IBrowsableCollection selection, bool display_tags) :
			this (SupportedService.Flickr, selection, display_tags)
		{ }


		public FlickrExport (SupportedService service, IBrowsableCollection selection, bool display_tags) : this ()
		{
			Run (service, selection, display_tags);
		}

		public FlickrExport () : base ("flickr_export_dialog")
		{
			
		}

		public virtual void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.Flickr, selection, true);
		}

		public void Run (SupportedService service, IBrowsableCollection selection, bool display_tags)
		{
			this.selection = selection;
			this.current_service = FlickrRemote.Service.FromSupported (service);

			IconView view = new IconView (selection);
			view.DisplayTags = display_tags;
			view.DisplayDates = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);
			
			public_radio.Toggled += HandlePublicChanged;

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;
			auth_flickr.Clicked += HandleClicked;
			auth_text = string.Format (auth_label.Text, current_service.Name);
			auth_label.Text = auth_text;

			LoadPreference (Preferences.EXPORT_FLICKR_SCALE);
			LoadPreference (Preferences.EXPORT_FLICKR_SIZE);
			LoadPreference (Preferences.EXPORT_FLICKR_BROWSER);
			LoadPreference (Preferences.EXPORT_FLICKR_TAGS);
			LoadPreference (Preferences.EXPORT_FLICKR_STRIP_META);
			LoadPreference (Preferences.EXPORT_FLICKR_PUBLIC);
			LoadPreference (Preferences.EXPORT_FLICKR_FAMILY);
			LoadPreference (Preferences.EXPORT_FLICKR_FRIENDS);
			LoadPreference (current_service.PreferencePath);

			do_export_flickr.Sensitive = false;
			fr = new FlickrRemote (token, current_service);
			if (token != null && token.Length > 0) {
				StartAuth ();
			}
		}

		public bool StartAuth ()
		{
			CurrentState = State.InAuth;
			if (command_thread == null || ! command_thread.IsAlive) {
				command_thread = new Thread (new ThreadStart (CheckAuthorization));
				command_thread.Start ();
			}
			return true;
		}

		public void CheckAuthorization ()
		{
			AuthorizationEventArgs args = new AuthorizationEventArgs ();

			try {
				args.Auth = fr.CheckLogin ();
			} catch (FlickrException e) {
				args.Exception = e;
			}

			Gtk.Application.Invoke (this, args, delegate (object sender, EventArgs sargs) {
				AuthorizationEventArgs wargs = (AuthorizationEventArgs) sargs;

				do_export_flickr.Sensitive = wargs.Auth != null;
				if (wargs.Auth != null) {
					token = wargs.Auth.Token;
					auth = wargs.Auth;
					CurrentState = State.Authorized;
					Preferences.Set (current_service.PreferencePath, token);
				} else {
					CurrentState = State.Disconnected;
				}
			});
		}

		private class AuthorizationEventArgs : System.EventArgs {
			Exception e;
			Auth auth;

			public Exception Exception {
				get { return e; }
				set { e = value; }
			}

			public Auth Auth {
				get { return auth; }
				set { auth = value; }
			}
			
			public AuthorizationEventArgs ()
			{
			}
		}

		public void HandleSizeActive (object sender, System.EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		private void Logout ()
		{
			token = null;
			auth = null;
			fr = new FlickrRemote (token, current_service);
			Preferences.Set (current_service.PreferencePath, String.Empty);
			CurrentState = State.Disconnected;
		}

		private void Login () 
		{
			try {
				fr = new FlickrRemote (token, current_service);
				fr.TryWebLogin();
				CurrentState = State.Connected;
			} catch (FlickrException e) {
				if (e.Code == 98) {
					Logout ();
					Login ();
				} else {
					HigMessageDialog md = 
						new HigMessageDialog (Dialog, 
								      Gtk.DialogFlags.Modal |
								      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								      Catalog.GetString ("Unable to log on"), e.Message);

					md.Run ();
					md.Destroy ();
					CurrentState = State.Disconnected;
				}
			}
		}

		private void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double) selection.Count;
		}

		FileInfo info;
		private void HandleFlickrProgress (object sender, UploadProgressEventArgs args)
		{
			if (args.UploadComplete) {
				progress_dialog.Fraction = photo_index / (double) selection.Count;				
				progress_dialog.ProgressText = String.Format (Catalog.GetString ("Waiting for response {0} of {1}"),
									      photo_index, selection.Count);
			}
			progress_dialog.Fraction = (photo_index - 1.0 + (args.Bytes / (double) info.Length)) / (double) selection.Count;		      
		}
		
		private void Upload () {
			progress_item = new ProgressItem ();
			progress_item.Changed += HandleProgressChanged;
			fr.Connection.OnUploadProgress += HandleFlickrProgress;

			System.Collections.ArrayList ids = new System.Collections.ArrayList ();
			for (int index = 0; index < selection.Items.Length; index++) {
				try {
				 	IBrowsableItem photo = selection.Items [index];
					progress_dialog.Message = System.String.Format (
                                                Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);

					progress_dialog.Fraction = photo_index / (double)selection.Count;
					photo_index++;
					progress_dialog.ProgressText = System.String.Format (
						Catalog.GetString ("{0} of {1}"), photo_index, 
						selection.Count);
					
					info = new FileInfo (photo.DefaultVersionUri.LocalPath);
					FilterSet stack = new Filters.FilterSet ();
					if (scale)
						stack.Add (new ResizeFilter ((uint)size));
					
					string id = fr.Upload (photo, stack, is_public, is_family, is_friend);
					ids.Add (id);

					if (Core.Database != null && photo is Photo)
						Core.Database.Exports.Create ((photo as Photo).Id,
									      (photo as Photo).DefaultVersionId,
									      ExportStore.FlickrExportType,
									      auth.User.UserId + ":" + auth.User.Username + ":" + current_service.Name + ":" + id);
                                        
					progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
				} catch (System.Exception e) {
					progress_dialog.Message = String.Format (Catalog.GetString ("Error Uploading To {0}: {1}"),
										 current_service.Name,
										 e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					System.Console.WriteLine (e);

					if (progress_dialog.PerformRetrySkip ())
					 	index--;
				}
			}

			if (open && ids.Count != 0) {
				string view_url = string.Format ("http://www.{0}/tools/uploader_edit.gne?ids", current_service.Name);
				bool first = true;

				foreach (string id in ids) {
					view_url = view_url + (first ? "=" : ",") + id;
					first = false;
				}

				GnomeUtil.UrlShow (progress_dialog, view_url);
			}
		}
		
		private void HandleClicked (object sender, System.EventArgs args)
		{
			switch (CurrentState) {
			case State.Disconnected:
				Login ();
				break;
			case State.Connected:
				StartAuth ();
				break;
			case State.InAuth:
				break;
			case State.Authorized:
				Logout ();
				Login ();
				break;
			}
		}
		
		private void HandlePublicChanged (object sender, EventArgs args)
		{
			bool sensitive = ! public_radio.Active;
			friend_check.Sensitive = sensitive;
			family_check.Sensitive = sensitive;
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				if (command_thread != null && command_thread.IsAlive)
					command_thread.Abort ();

				Dialog.Destroy ();
				return;
			}
			
			if (fr.CheckLogin() == null) {
				do_export_flickr.Sensitive = false;
				HigMessageDialog md = 
					new HigMessageDialog (Dialog, 
							      Gtk.DialogFlags.Modal |
							      Gtk.DialogFlags.DestroyWithParent,
							      Gtk.MessageType.Error, Gtk.ButtonsType.Ok, 
							      Catalog.GetString ("Unable to log on."),
							      string.Format (Catalog.GetString ("F-Spot was unable to log on to {0}.  Make sure you have given the authentication using {0} web browser interface."),
							      		     current_service.Name));
				md.Run ();
				md.Destroy ();
				return;
			}

			fr.ExportTags = tag_check.Active;
			open = open_check.Active;
			scale = scale_check.Active;
			copy_metadata = !meta_check.Active;
			is_public = public_radio.Active;
			is_family = family_check.Active;
			is_friend = friend_check.Active;
			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new Thread (new ThreadStart (Upload));
			command_thread.Name = Catalog.GetString ("Uploading Pictures");
			
			Dialog.Destroy ();
			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();
			
			// Save these settings for next time
			Preferences.Set (Preferences.EXPORT_FLICKR_SCALE, scale);
			Preferences.Set (Preferences.EXPORT_FLICKR_SIZE, size);
			Preferences.Set (Preferences.EXPORT_FLICKR_BROWSER, open);
			Preferences.Set (Preferences.EXPORT_FLICKR_TAGS, tag_check.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_STRIP_META, meta_check.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_PUBLIC, public_radio.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_FAMILY, family_check.Active);
			Preferences.Set (Preferences.EXPORT_FLICKR_FRIENDS, friend_check.Active);
			Preferences.Set (current_service.PreferencePath, fr.Token);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			//System.Console.WriteLine("Setting {0} to {1}", key, val);
			bool active;

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
			case Preferences.EXPORT_TOKEN_FLICKR:
			case Preferences.EXPORT_TOKEN_23HQ:
			case Preferences.EXPORT_TOKEN_ZOOOMR:
				token = (string) val;
			        break;
			case Preferences.EXPORT_FLICKR_PUBLIC:
				active = (bool) val;
				if (public_radio.Active != active)
					public_radio.Active = active;
				break;
			case Preferences.EXPORT_FLICKR_FAMILY:
				active = (bool) val;
				if (family_check.Active != active)
					family_check.Active = active;
				break;
			case Preferences.EXPORT_FLICKR_FRIENDS:
				active = (bool) val;
				if (friend_check.Active != active)
					friend_check.Active = active;
				break;
				/*				
			case Preferences.EXPORT_FLICKR_EMAIL:

				/*				
			case Preferences.EXPORT_FLICKR_EMAIL:
				email_entry.Text = (string) val;
				break;
				*/
			}
		}

	}
}
