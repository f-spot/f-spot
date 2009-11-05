using FlickrNet;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using Mono.Unix;

using FSpot;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpotFlickrExport {
	public class TwentyThreeHQExport : FlickrExport
	{
		public override void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.TwentyThreeHQ, selection, false);
		}
	}

	public class ZooomrExport : FlickrExport
	{
		public override void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.Zooomr, selection, false);
		}
	}

	public class FlickrExport : FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.Dialog	  dialog;
		[Glade.Widget] Gtk.CheckButton    scale_check;
		[Glade.Widget] Gtk.CheckButton    meta_check;
		[Glade.Widget] Gtk.CheckButton    tag_check;
		[Glade.Widget] Gtk.CheckButton    hierarchy_check;
		[Glade.Widget] Gtk.CheckButton    ignore_top_level_check;
		[Glade.Widget] Gtk.CheckButton    open_check;
		[Glade.Widget] Gtk.SpinButton     size_spin;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Button         auth_flickr;
		[Glade.Widget] Gtk.Button         auth_done_flickr;
		[Glade.Widget] Gtk.ProgressBar    used_bandwidth;
		[Glade.Widget] Gtk.Button         do_export_flickr;
		[Glade.Widget] Gtk.Label          auth_label;
		[Glade.Widget] Gtk.RadioButton    public_radio;
		[Glade.Widget] Gtk.CheckButton    family_check;
		[Glade.Widget] Gtk.CheckButton    friend_check;

		private Glade.XML xml;
		private string dialog_name = "flickr_export_dialog";
		System.Threading.Thread command_thread;
		ThreadProgressDialog progress_dialog;
		ProgressItem progress_item;

		public const string EXPORT_SERVICE = "flickr/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string TAGS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "tags";
		public const string STRIP_META_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "strip_meta";
		public const string PUBLIC_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "public";
		public const string FAMILY_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "family";
		public const string FRIENDS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "friends";
		public const string TAG_HIERARCHY_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "tag_hierarchy";
		public const string IGNORE_TOP_LEVEL_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "ignore_top_level";

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
					used_bandwidth.Visible = false;
					break;
				case State.Connected:
					auth_flickr.Sensitive = true;
					do_export_flickr.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Return to this window after you have finished the authorization process on {0} and click the \"Complete Authorization\" button below"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Complete Authorization");
					used_bandwidth.Visible = false;
					break;
				case State.InAuth:
					auth_flickr.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Logging into {0}"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Checking credentials...");
					do_export_flickr.Sensitive = false;
					used_bandwidth.Visible = false;
					break;
				case State.Authorized:
					do_export_flickr.Sensitive = true;
					auth_flickr.Sensitive = true;
					auth_label.Text = System.String.Format (Catalog.GetString ("Welcome {0} you are connected to {1}"),
										auth.User.Username,
										current_service.Name);
					auth_flickr.Label = String.Format (Catalog.GetString ("Sign in as a different user"), auth.User.Username);
					used_bandwidth.Visible = !fr.Connection.PeopleGetUploadStatus().IsPro &&
									fr.Connection.PeopleGetUploadStatus().BandwidthMax > 0;
					if (used_bandwidth.Visible) {
						used_bandwidth.Fraction = fr.Connection.PeopleGetUploadStatus().PercentageUsed;
						used_bandwidth.Text = string.Format (Catalog.GetString("Used {0} of your allowed {1} monthly quota"),
									GLib.Format.SizeForDisplay (fr.Connection.PeopleGetUploadStatus().BandwidthUsed),
									GLib.Format.SizeForDisplay (fr.Connection.PeopleGetUploadStatus().BandwidthMax));
					}
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

		public FlickrExport ()
		{

		}

		public virtual void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.Flickr, selection, false);
		}

		public void Run (SupportedService service, IBrowsableCollection selection, bool display_tags)
		{
			this.selection = selection;
			this.current_service = FlickrRemote.Service.FromSupported (service);

			IconView view = new IconView (selection);
			view.DisplayTags = display_tags;
			view.DisplayDates = false;

			xml = new Glade.XML (null, "FlickrExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);

			public_radio.Toggled += HandlePublicChanged;
			tag_check.Toggled += HandleTagChanged;
			hierarchy_check.Toggled += HandleHierarchyChanged;
			HandleTagChanged (null, null);
			HandleHierarchyChanged (null, null);

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;
			auth_flickr.Clicked += HandleClicked;
			auth_text = string.Format (auth_label.Text, current_service.Name);
			auth_label.Text = auth_text;
			used_bandwidth.Visible = false;

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (BROWSER_KEY);
			LoadPreference (TAGS_KEY);
			LoadPreference (TAG_HIERARCHY_KEY);
			LoadPreference (IGNORE_TOP_LEVEL_KEY);
			LoadPreference (STRIP_META_KEY);
			LoadPreference (PUBLIC_KEY);
			LoadPreference (FAMILY_KEY);
			LoadPreference (FRIENDS_KEY);
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
			} catch (FlickrApiException e) {
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

		private class DateComparer : IComparer
		{
			public int Compare (object left, object right)
			{
				return DateTime.Compare ((left as IBrowsableItem).Time, (right as IBrowsableItem).Time);
			}
		}

		private void Upload () {
			progress_item = new ProgressItem ();
			progress_item.Changed += HandleProgressChanged;
			fr.Connection.OnUploadProgress += HandleFlickrProgress;

			System.Collections.ArrayList ids = new System.Collections.ArrayList ();
			IBrowsableItem [] photos = selection.Items;
			Array.Sort (photos, new DateComparer ());

			for (int index = 0; index < photos.Length; index++) {
				try {
					IBrowsableItem photo = photos [index];
					progress_dialog.Message = System.String.Format (
                                                Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);

					progress_dialog.Fraction = photo_index / (double)selection.Count;
					photo_index++;
					progress_dialog.ProgressText = System.String.Format (
						Catalog.GetString ("{0} of {1}"), photo_index,
						selection.Count);

					info = new FileInfo (photo.DefaultVersionUri.LocalPath);
					FilterSet stack = new FilterSet ();
					if (scale)
						stack.Add (new ResizeFilter ((uint)size));

					string id = fr.Upload (photo, stack, is_public, is_family, is_friend);
					ids.Add (id);

					if (App.Instance.Database != null && photo is FSpot.Photo)
						App.Instance.Database.Exports.Create ((photo as FSpot.Photo).Id,
									      (photo as FSpot.Photo).DefaultVersionId,
									      ExportStore.FlickrExportType,
									      auth.User.UserId + ":" + auth.User.Username + ":" + current_service.Name + ":" + id);

				} catch (System.Exception e) {
					progress_dialog.Message = String.Format (Catalog.GetString ("Error Uploading To {0}: {1}"),
										 current_service.Name,
										 e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					System.Console.WriteLine (e);

					if (progress_dialog.PerformRetrySkip ()) {
						index--;
						photo_index--;
					}
				}
			}
			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (open && ids.Count != 0) {
				string view_url;
				if (current_service.Name == "Zooomr.com")
					view_url = string.Format ("http://www.{0}/photos/{1}/", current_service.Name, auth.User.Username);
				else {
					view_url = string.Format ("http://www.{0}/tools/uploader_edit.gne?ids", current_service.Name);
					bool first = true;

					foreach (string id in ids) {
						view_url = view_url + (first ? "=" : ",") + id;
						first = false;
					}
				}

				GtkBeans.Global.ShowUri (Dialog.Screen, view_url);
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

		private void HandleTagChanged (object sender, EventArgs args)
		{
			hierarchy_check.Sensitive = tag_check.Active;
		}

		private void HandleHierarchyChanged (object sender, EventArgs args)
		{
			ignore_top_level_check.Sensitive = hierarchy_check.Active;
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
			fr.ExportTagHierarchy = hierarchy_check.Active;
			fr.ExportIgnoreTopLevel = ignore_top_level_check.Active;
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
			progress_dialog = new ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();

			// Save these settings for next time
			Preferences.Set (SCALE_KEY, scale);
			Preferences.Set (SIZE_KEY, size);
			Preferences.Set (BROWSER_KEY, open);
			Preferences.Set (TAGS_KEY, tag_check.Active);
			Preferences.Set (STRIP_META_KEY, meta_check.Active);
			Preferences.Set (PUBLIC_KEY, public_radio.Active);
			Preferences.Set (FAMILY_KEY, family_check.Active);
			Preferences.Set (FRIENDS_KEY, friend_check.Active);
			Preferences.Set (TAG_HIERARCHY_KEY, hierarchy_check.Active);
			Preferences.Set (IGNORE_TOP_LEVEL_KEY, ignore_top_level_check.Active);
			Preferences.Set (current_service.PreferencePath, fr.Token);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key))
					scale_check.Active = Preferences.Get<bool> (key);
				break;
				
			case SIZE_KEY:
				size_spin.Value = (double) Preferences.Get<int> (key);
				break;
				
			case BROWSER_KEY:
				if (open_check.Active != Preferences.Get<bool> (key))
					open_check.Active = Preferences.Get<bool> (key);
				break;
				
			case TAGS_KEY:
				if (tag_check.Active != Preferences.Get<bool> (key))
					tag_check.Active = Preferences.Get<bool> (key);
				break;
				
			case TAG_HIERARCHY_KEY:
				if (hierarchy_check.Active != Preferences.Get<bool> (key))
					hierarchy_check.Active = Preferences.Get<bool> (key);
				break;
				
			case IGNORE_TOP_LEVEL_KEY:
				if (ignore_top_level_check.Active != Preferences.Get<bool> (key))
					ignore_top_level_check.Active = Preferences.Get<bool> (key);
				break;
				
			case STRIP_META_KEY:
				if (meta_check.Active != Preferences.Get<bool> (key))
					meta_check.Active = Preferences.Get<bool> (key);
				break;
				
			case FlickrRemote.TOKEN_FLICKR:
			case FlickrRemote.TOKEN_23HQ:
			case FlickrRemote.TOKEN_ZOOOMR:
				token = Preferences.Get<string> (key);
				break;
				
			case PUBLIC_KEY:
				if (public_radio.Active != Preferences.Get<bool> (key))
					public_radio.Active = Preferences.Get<bool> (key);
				break;
				
			case FAMILY_KEY:
				if (family_check.Active != Preferences.Get<bool> (key))
					family_check.Active = Preferences.Get<bool> (key);
				break;
				
			case FRIENDS_KEY:
				if (friend_check.Active != Preferences.Get<bool> (key))
					friend_check.Active = Preferences.Get<bool> (key);
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

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}
	}
}
