/*
 * FacebookExport.cs
 *
 * Authors:
 *   George Talusan <george@convolve.ca>
 *   Stephane Delcroix <stephane@delcroix.org>
 *   Jim Ramsay <i.am@jimramsay.com>
 *
 * Copyright (C) 2007 George Talusan
 * Copyright (c) 2008 Novell, Inc.
 * Later changes (2009) by Jim Ramsay
 */

using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Gtk;
using Gnome.Keyring;
using Glade;

using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Filters;
using FSpot.Platform;

using Mono.Facebook;

namespace FSpot.Exporter.Facebook
{
	internal class FacebookAccount
	{
		static string keyring_item_name = "Facebook Account";

		static string api_key = "c23d1683e87313fa046954ea253a240e";
		static string secret = "743e9a2e6a1c35ce961321bceea7b514";

		FacebookSession facebookSession;
		bool connected = false;

		public FacebookAccount ()
		{
			SessionInfo info = ReadSessionInfo ();
			if (info != null) {
				facebookSession = new FacebookSession (api_key, info);
				/* TODO: Check if the session is still valid? */
				connected = true;
			}
		}

		public Uri GetLoginUri ()
		{
			FacebookSession session = new FacebookSession (api_key, secret);
			Uri uri = session.CreateToken();
			facebookSession = session;
			connected = false;
			return uri;
		}

		public FacebookSession Facebook
		{
			get { return facebookSession; }
		}

		public bool Authenticated
		{
			get { return connected; }
		}

		bool SaveSessionInfo (SessionInfo info)
		{
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
				Log.DebugFormat ("Got default keyring {0}", keyring);
			} catch (Exception e) {
				Log.DebugException (e);
				return false;
			}

			Hashtable attribs = new Hashtable();
			attribs["name"] = keyring_item_name;
			attribs["uid"] = info.UId.ToString ();
			attribs["session_key"] = info.SessionKey;
			try {
				Ring.CreateItem (keyring, ItemType.GenericSecret, keyring_item_name, attribs, info.Secret, true);
			} catch (Exception e) {
				Log.DebugException (e);
				return false;
			}

			return true;
		}

		SessionInfo ReadSessionInfo ()
		{
			SessionInfo info = null;

			Hashtable request_attributes = new Hashtable ();
			request_attributes["name"] = keyring_item_name;
			try {
				foreach (ItemData result in Ring.Find (ItemType.GenericSecret, request_attributes)) {
					if (!result.Attributes.ContainsKey ("name") ||
						!result.Attributes.ContainsKey ("uid") ||
						!result.Attributes.ContainsKey ("session_key") ||
						(result.Attributes["name"] as string) != keyring_item_name)
							continue;

					string session_key = (string)result.Attributes["session_key"];
					long uid = Int64.Parse((string)result.Attributes["uid"]);
					string secret = result.Secret;
					info = new SessionInfo (session_key, uid, secret);
					break;
				}
			} catch (Exception e) {
				Log.DebugException (e);
			}

			return info;
		}

		bool ForgetSessionInfo()
		{
			string keyring;
			bool success = false;

			try {
				keyring = Ring.GetDefaultKeyring();
			} catch (Exception e) {
				Log.DebugException (e);
				return false;
			}

			Hashtable request_attributes = new Hashtable ();
			request_attributes["name"] = keyring_item_name;
			try {
				foreach (ItemData result in Ring.Find (ItemType.GenericSecret, request_attributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
					success = true;
				}
			} catch (Exception e) {
				Log.DebugException (e);
			}

			return success;
		}

		public bool Authenticate ()
		{
			if (connected)
				return true;
			try {
				SessionInfo info = facebookSession.GetSession();
				connected = true;
				if (SaveSessionInfo (info))
					Log.Information ("Saved session information to keyring");
				else
					Log.Warning ("Could not save session information to keyring");
			} catch (Exception e) {
				connected = false;
				Log.DebugException (e);
			}
			return connected;
		}

		public void Deauthenticate ()
		{
			connected = false;
			ForgetSessionInfo ();
		}

	}

	internal class AlbumStore : ListStore
	{
		private Album[] _albums;

		public AlbumStore (Album[] albums) : base (typeof (string))
		{
			_albums = albums;

			foreach (Album album in Albums)
				AppendValues (album.Name);
		}

		public Album[] Albums
		{
			get { return _albums; }
		}
	}

	internal class TagStore : ListStore
	{
		private List<Mono.Facebook.Tag> _tags;

		private Dictionary<long, User> _friends;

		public TagStore (FacebookSession session, List<Mono.Facebook.Tag> tags, Dictionary<long, User> friends) : base (typeof (string))
		{
			_tags = tags;
			_friends = friends;

			foreach (Mono.Facebook.Tag tag in Tags) {
				long subject = tag.Subject;
				User info = _friends [subject];
				if (info == null ) {
					try {
						info = session.GetUserInfo (new long[] { subject }, new string[] { "first_name", "last_name" }) [0];
					}
					catch (FacebookException) {
						continue;
					}
				}
				AppendValues (String.Format ("{0} {1}", info.FirstName ?? "", info.LastName ?? ""));
			}
		}

		public List<Mono.Facebook.Tag> Tags
		{
			get { return _tags ?? new List<Mono.Facebook.Tag> (); }
		}
	}

	internal class FacebookTagPopup
	{
		private Dictionary<long, User> _friends;

		private Gtk.Window _popup;

		public FacebookTagPopup (Dictionary<long, User> friends)
		{
			Friends = friends;

			Glade.XML xml = new Glade.XML (null, "FacebookExport.glade", "facebook_tag_popup", "f-spot");
			xml.Autoconnect (this);

			Popup = xml.GetWidget ("facebook_tag_popup") as Gtk.Window;
			Popup.Show ();
		}

		public Dictionary<long, User> Friends
		{
			get { return _friends; }
			set { _friends = value; }
		}

		protected Gtk.Window Popup
		{
			get { return _popup; }
			set { _popup = value; }
		}
	}

	public class FacebookExport : IExporter
	{
		private int size = 604;

		private FacebookAccount account;
		private Dictionary<long, User> friends;

		/* parallel arrays */
		private int current_item;
		private IBrowsableItem[] items;
		private string[] captions;
		private List<Mono.Facebook.Tag>[] tags;

		private Glade.XML xml;
		private string dialog_name = "facebook_export_dialog";
		private Gtk.Dialog dialog;

		private FSpot.Widgets.IconView thumbnail_iconview;

		ThreadProgressDialog progress_dialog;

		[Widget]VBox album_info_vbox;
		[Widget]VBox picture_info_vbox;
		[Widget]HBox log_buttons_hbox;
		[Widget]HButtonBox dialog_action_area;
		[Widget]Button login_button;
		[Widget]Button logout_button;
		[Widget]ProgressBar login_progress;
		[Widget]RadioButton existing_album_radiobutton;
		[Widget]RadioButton create_album_radiobutton;
		[Widget]ComboBox existing_album_combobox;
		[Widget]Table new_album_info_table;
		[Widget]Entry album_name_entry;
		[Widget]Entry album_location_entry;
		[Widget]Entry album_description_entry;
		[Widget]ScrolledWindow thumbnails_scrolled_window;
		[Widget]TextView caption_textview;
		[Widget]TreeView tag_treeview;
		[Widget]EventBox tag_image_eventbox;

		Gtk.Image tag_image;
		int tag_image_height;
		int tag_image_width;

		System.Threading.Thread command_thread;

		public FacebookExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			CreateDialog ();

			items = selection.Items;

			if (items.Length > 60) {
				HigMessageDialog mbox = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Too many images to export"), Catalog.GetString ("Facebook only permits 60 photographs per album.  Please refine your selection and try again."));
				mbox.Run ();
				mbox.Destroy ();
				return;
			}

			captions = new string [items.Length];
			tags = new List<Mono.Facebook.Tag> [items.Length];

			thumbnail_iconview = new FSpot.Widgets.IconView (selection);
			thumbnail_iconview.DisplayDates = false;
			thumbnail_iconview.DisplayTags = false;
			thumbnail_iconview.DisplayRatings = false;
			thumbnail_iconview.ButtonPressEvent += HandleThumbnailIconViewButtonPressEvent;
			thumbnail_iconview.KeyPressEvent += HandleThumbnailIconViewKeyPressEvent;
			thumbnail_iconview.Show ();
			thumbnails_scrolled_window.Add (thumbnail_iconview);

			login_button.Clicked += HandleLoginClicked;
			logout_button.Clicked += HandleLogoutClicked;

			create_album_radiobutton.Toggled += HandleCreateAlbumToggled;
			create_album_radiobutton.Active = true;

			existing_album_radiobutton.Toggled += HandleExistingAlbumToggled;

			CellRendererText cell = new CellRendererText ();
			existing_album_combobox.PackStart (cell, false);

			tag_image_eventbox.ButtonPressEvent += HandleTagImageButtonPressEvent;

			tag_treeview.Sensitive = false;
			caption_textview.Sensitive = false;

			DoLogout ();

			Dialog.Response += HandleResponse;
			Dialog.Show ();

			account = new FacebookAccount();
			if (account.Authenticated)
				DoLogin ();
		}

		public void CreateDialog ()
		{
			xml = new Glade.XML (null, "FacebookExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);
		}

		Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}

		public void HandleLoginClicked (object sender, EventArgs args)
		{
			if (!account.Authenticated) {
				Uri uri = account.GetLoginUri ();
				GtkBeans.Global.ShowUri (Dialog.Screen, uri.ToString ());

				HigMessageDialog mbox = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Info, Gtk.ButtonsType.Ok, Catalog.GetString ("Waiting for authentication"), Catalog.GetString ("F-Spot will now launch your browser so that you can log into Facebook.  Turn on the \"Save my login information\" checkbox on Facebook and F-Spot will log into Facebook automatically from now on."));

				mbox.Run ();
				mbox.Destroy ();

				LoginProgress (0.0, Catalog.GetString (" Authenticating..."));
				account.Authenticate ();
			}
			DoLogin ();
		}

		void DoLogin ()
		{
			if (!account.Authenticated) {
				HigMessageDialog error = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Error logging into Facebook"), Catalog.GetString ("There was a problem logging into Facebook.  Check your credentials and try again."));
				error.Run ();
				error.Destroy ();

				DoLogout ();
			}
			else {
				log_buttons_hbox.Sensitive = false;
				dialog_action_area.Sensitive = false;

				try {
					LoginProgress (0.2, Catalog.GetString ("Session established, fetching user info..."));
					User me = account.Facebook.GetLoggedInUser ().GetUserInfo ();

					LoginProgress (0.4, Catalog.GetString ("Session established, fetching friend list..."));
					Friend[] friend_list = account.Facebook.GetFriends ();
					long[] uids = new long [friend_list.Length];

					for (int i = 0; i < friend_list.Length; i++)
						uids [i] = friend_list [i].UId;

					LoginProgress (0.6, Catalog.GetString ("Session established, fetching friend details..."));
					User[] infos = account.Facebook.GetUserInfo (uids, new string[] { "first_name", "last_name" });
					friends = new Dictionary<long, User> ();

					foreach (User user in infos)
						friends.Add (user.UId, user);

					LoginProgress (0.8, Catalog.GetString ("Session established, fetching photo albums..."));
					Album[] albums = account.Facebook.GetAlbums ();
					AlbumStore store = new AlbumStore (albums);
					existing_album_combobox.Model = store;
					existing_album_combobox.Active = 0;

					album_info_vbox.Sensitive = true;
					picture_info_vbox.Sensitive = true;
					login_button.Visible = false;
					logout_button.Visible = true;
					log_buttons_hbox.Sensitive = true;
					dialog_action_area.Sensitive = true;
					// Note for translators: {0} and {1} are respectively firstname and surname of the user
					LoginProgress (1.0, String.Format (Catalog.GetString ("{0} {1} is logged into Facebook"), me.FirstName, me.LastName));
				} catch (Exception e) {
					Log.DebugException (e);
					HigMessageDialog error = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Error connecting to Facebook"), Catalog.GetString ("There was an unexpected problem when downloading your information from Facebook."));
					error.Run ();
					error.Destroy ();

					log_buttons_hbox.Sensitive = true;
					dialog_action_area.Sensitive = true;

					DoLogout ();
				}
			}
		}

		void HandleLogoutClicked (object sender, EventArgs args)
		{
			account.Deauthenticate ();
			DoLogout ();
		}

		void DoLogout ()
		{
			login_button.Visible = true;
			logout_button.Visible = false;

			login_progress.Fraction = 0;
			login_progress.Text = Catalog.GetString ("You are not logged in.");

			album_info_vbox.Sensitive = false;
			picture_info_vbox.Sensitive = false;
		}

		void HandleCreateAlbumToggled (object sender, EventArgs args)
		{
			if (create_album_radiobutton.Active == false)
				return;

			new_album_info_table.Sensitive = true;
			existing_album_combobox.Sensitive = false;
		}

		void HandleExistingAlbumToggled (object sender, EventArgs args)
		{
			if (existing_album_radiobutton.Active == false)
				return;

			new_album_info_table.Sensitive = false;
			existing_album_combobox.Sensitive = true;
		}

		void HandleThumbnailIconViewButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
		{
			int old_item = current_item;
			current_item = thumbnail_iconview.CellAtPosition ((int) args.Event.X, (int) args.Event.Y, false);

			if (current_item < 0 || current_item >=  items.Length) {                                current_item = old_item;
				return;
                        }

			captions [old_item] = caption_textview.Buffer.Text;

			string caption = captions [current_item];
			if (caption == null)
				captions [current_item] = caption = "";
			caption_textview.Buffer.Text = caption;
			caption_textview.Sensitive = true;

			tag_treeview.Model = new TagStore (account.Facebook, tags [current_item], friends);

			IBrowsableItem item = items [current_item];

			if (tag_image_eventbox.Children.Length > 0) {
				tag_image_eventbox.Remove (tag_image);
				tag_image.Destroy ();
			}

			using (Gdk.Pixbuf data = PixbufUtils.ScaleToMaxSize (ThumbnailFactory.LoadThumbnail (item.DefaultVersionUri), 400, 400)) {
				tag_image_height = data.Height;
				tag_image_width = data.Width;
				tag_image = new Gtk.Image (data);
				tag_image_eventbox.Add (tag_image);
				tag_image_eventbox.ShowAll ();
			}
		}

		void HandleTagImageButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
		{
			double x = args.Event.X;
			double y = args.Event.Y;

			// translate the centered image to top left corner
			double tag_image_center_x = tag_image_width / 2;
			double tag_image_center_y = tag_image_height / 2;

			double allocation_center_x = tag_image_eventbox.Allocation.Width / 2;
			double allocation_center_y = tag_image_eventbox.Allocation.Height / 2;

			double dx = allocation_center_x - tag_image_center_x;
			double dy = allocation_center_y - tag_image_center_y;

			if (dx < 0)
				dx = 0;
			if (dy < 0)
				dy = 0;

			x -= dx;
			y -= dy;

			// bail if we're in the eventbox but not the image
			if (x < 0 || x > tag_image_width)
				return;
			if (y < 0 || y > tag_image_height)
				return;

			//FacebookTagPopup popup = new FacebookTagPopup (friends);
		}

		void HandleThumbnailIconViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			thumbnail_iconview.Selection.Clear ();
		}

		void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			if (account != null) {
				Dialog.Hide ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
				command_thread.Name = Mono.Unix.Catalog.GetString ("Uploading Pictures");

				progress_dialog = new ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();
			}
		}

		void LoginProgress (double percentage, string message)
		{
			login_progress.Fraction = percentage;
			login_progress.Text = message;
			Log.Debug (message);
			while (Application.EventsPending ()) Application.RunIteration ();
		}

		void Upload ()
		{
			Album album = null;

			if (create_album_radiobutton.Active) {
				string name = album_name_entry.Text;
				if (name.Length == 0) {
					HigMessageDialog mbox = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Album must have a name"), Catalog.GetString ("Please name your album or choose an existing album."));
					mbox.Run ();
					mbox.Destroy ();
					return;
				}

				string description = album_description_entry.Text;
				string location = album_location_entry.Text;

				try {
					album = account.Facebook.CreateAlbum (name, description, location);
				}
				catch (FacebookException fe) {
					HigMessageDialog mbox = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Creating a new album failed"), String.Format (Catalog.GetString ("An error occurred creating a new album.\n\n{0}"), fe.Message));
					mbox.Run ();
					mbox.Destroy ();
					return;
				}
			}
			else {
				AlbumStore store = (AlbumStore) existing_album_combobox.Model;
				album = store.Albums [existing_album_combobox.Active];
			}

			long sent_bytes = 0;

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());
			filters.Add (new ResizeFilter ((uint) size));

			for (int i = 0; i < items.Length; i++) {
				try {
					IBrowsableItem item = items [i];

					FileInfo file_info;
					Console.WriteLine ("uploading {0}", i);

					progress_dialog.Message = String.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"), item.Name, i + 1, items.Length);
					progress_dialog.ProgressText = string.Empty;
					progress_dialog.Fraction = i / (double) items.Length;

					FilterRequest request = new FilterRequest (item.DefaultVersionUri);
					filters.Convert (request);

					file_info = new FileInfo (request.Current.LocalPath);

					Mono.Facebook.Photo photo = album.Upload (captions [i] ?? "", request.Current.LocalPath);

					sent_bytes += file_info.Length;
				}
				catch (Exception e) {
					progress_dialog.Message = String.Format (Catalog.GetString ("Error Uploading To Facebook: {0}"), e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					Console.WriteLine (e);

					if (progress_dialog.PerformRetrySkip ())
						i--;
				}
			}

			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			Dialog.Destroy ();
		}
	}
}
