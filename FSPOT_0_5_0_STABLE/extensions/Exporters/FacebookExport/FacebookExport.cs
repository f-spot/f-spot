/*
 * FacebookExport.cs
 *
 * Authors:
 *   George Talusan <george@convolve.ca>
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (C) 2007 George Talusan
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

using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Filters;

using Mono.Facebook;

namespace FSpot.Exporter.Facebook
{
	internal class FacebookAccount
	{
		private static string keyring_item_name = "Facebook Account";

		private static string api_key = "c23d1683e87313fa046954ea253a240e";
		private static string secret = "743e9a2e6a1c35ce961321bceea7b514";

		private FacebookSession facebookSession;
		private bool connected;

		public FacebookAccount ()
		{ }

		public Uri CreateToken ()
		{
			FacebookSession session = new FacebookSession (api_key, secret);
			Uri token = session.CreateToken();
			facebookSession = session;
			connected = false;
			return token;
		}

		public FacebookSession Facebook
		{
			get { return facebookSession; }
		}

		public bool Authenticated
		{
			get {
				try {
					if (connected)
						return true;
					facebookSession.GetSession();
					connected = true;
					return true;
				}
				catch (FacebookException fe) {
					Console.WriteLine (fe);
					return false;
				}
			}
		}

		public bool HasInfiniteSession
		{
			get {
				if (Authenticated)
					return true;

				return true;
			}
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

		FSpot.ThreadProgressDialog progress_dialog;

		[Glade.WidgetAttribute]
		Gtk.VBox album_info_vbox;

		[Glade.WidgetAttribute]
		Gtk.VBox picture_info_vbox;

		[Glade.WidgetAttribute]
		Gtk.Button login_button;

		[Glade.WidgetAttribute]
		Gtk.Button logout_button;

		[Glade.WidgetAttribute]
		Gtk.Label whoami_label;

		[Glade.WidgetAttribute]
		Gtk.RadioButton existing_album_radiobutton;

		[Glade.WidgetAttribute]
		Gtk.RadioButton create_album_radiobutton;

		[Glade.WidgetAttribute]
		Gtk.ComboBox existing_album_combobox;

		[Glade.WidgetAttribute]
		Gtk.HBox new_album_info1_hbox;

		[Glade.WidgetAttribute]
		Gtk.HBox new_album_info2_hbox;

		[Glade.WidgetAttribute]
		Gtk.Entry album_name_entry;

		[Glade.WidgetAttribute]
		Gtk.Entry album_location_entry;

		[Glade.WidgetAttribute]
		Gtk.Entry album_description_entry;

		[Glade.WidgetAttribute]
		Gtk.ScrolledWindow thumbnails_scrolled_window;

		[Glade.WidgetAttribute]
		Gtk.TextView caption_textview;

		[Glade.WidgetAttribute]
		Gtk.TreeView tag_treeview;

		[Glade.WidgetAttribute]
		Gtk.EventBox tag_image_eventbox;

		private Gtk.Image tag_image;
		private int tag_image_height;
		private int tag_image_width;

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
			thumbnail_iconview.ButtonPressEvent += HandleThumbnailIconViewButtonPressEvent;
			thumbnail_iconview.KeyPressEvent += HandleThumbnailIconViewKeyPressEvent;
			thumbnail_iconview.Show ();
			thumbnails_scrolled_window.Add (thumbnail_iconview);

			login_button.Visible = true;
			login_button.Clicked += HandleLoginClicked;

			logout_button.Visible = false;
			logout_button.Clicked += HandleLogoutClicked;

			whoami_label.Text = Catalog.GetString ("You are not logged in.");

			album_info_vbox.Sensitive = false;
			picture_info_vbox.Sensitive = false;

			create_album_radiobutton.Toggled += HandleCreateAlbumToggled;
			create_album_radiobutton.Active = true;

			existing_album_radiobutton.Toggled += HandleExistingAlbumToggled;

			CellRendererText cell = new CellRendererText ();
			existing_album_combobox.PackStart (cell, false);

			tag_image_eventbox.ButtonPressEvent += HandleTagImageButtonPressEvent;

			Dialog.Response += HandleResponse;
			Dialog.Show ();
		}

		public void CreateDialog ()
		{
			xml = new Glade.XML (null, "FacebookExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}

		public void HandleLoginClicked (object sender, EventArgs args)
		{
			account = new FacebookAccount();

			Uri token = account.CreateToken ();
			GnomeUtil.UrlShow (token.ToString ());

			HigMessageDialog mbox = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Info, Gtk.ButtonsType.Ok, Catalog.GetString ("Waiting for authentication"), Catalog.GetString ("F-Spot will now launch your browser so that you can log into Facebook.  Turn on the \"Save my login information\" checkbox on Facebook and F-Spot will log into Facebook automatically from now on."));

			mbox.Run ();
			mbox.Destroy ();

			if (account.Authenticated == false) {
				HigMessageDialog error = new HigMessageDialog (Dialog, Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, Catalog.GetString ("Error logging into Facebook"), Catalog.GetString ("There was a problem logging into Facebook.  Check your credentials and try again."));
				error.Run ();
				error.Destroy ();
			}
			else {
				login_button.Visible = false;
				logout_button.Visible = true;

				album_info_vbox.Sensitive = true;
				picture_info_vbox.Sensitive = true;

				User me = account.Facebook.GetLoggedInUser ().GetUserInfo ();
				// Note for translators: {0} and {1} are respectively firstname and surname of the user 
				whoami_label.Text = String.Format (Catalog.GetString ("{0} {1} is logged into Facebook"), me.FirstName, me.LastName);

				Friend[] friend_list = account.Facebook.GetFriends ();
				long[] uids = new long [friend_list.Length];

				for (int i = 0; i < friend_list.Length; i++)
					uids [i] = friend_list [i].UId;

				User[] infos = account.Facebook.GetUserInfo (uids, new string[] { "first_name", "last_name" });
				friends = new Dictionary<long, User> ();

				foreach (User user in infos)
					friends.Add (user.UId, user);

				Album[] albums = account.Facebook.GetAlbums ();
				AlbumStore store = new AlbumStore (albums);
				existing_album_combobox.Model = store;
				existing_album_combobox.Active = 0;
			}
		}

		private void HandleLogoutClicked (object sender, EventArgs args)
		{
			login_button.Visible = true;
			logout_button.Visible = false;

			whoami_label.Text = Catalog.GetString ("You are not logged in.");

			album_info_vbox.Sensitive = false;
			picture_info_vbox.Sensitive = false;
		}

		private void HandleCreateAlbumToggled (object sender, EventArgs args)
		{
			if (create_album_radiobutton.Active == false)
				return;

			new_album_info1_hbox.Sensitive = true;
			new_album_info2_hbox.Sensitive = true;

			existing_album_combobox.Sensitive = false;
		}

		private void HandleExistingAlbumToggled (object sender, EventArgs args)
		{
			if (existing_album_radiobutton.Active == false)
				return;

			new_album_info1_hbox.Sensitive = false;
			new_album_info2_hbox.Sensitive = false;

			existing_album_combobox.Sensitive = true;
		}

		private void HandleThumbnailIconViewButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
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

			tag_treeview.Model = new TagStore (account.Facebook, tags [current_item], friends);

			IBrowsableItem item = items [current_item];
			string thumbnail_path = ThumbnailGenerator.ThumbnailPath (item.DefaultVersionUri);

			if (tag_image_eventbox.Children.Length > 0) {
				tag_image_eventbox.Remove (tag_image);
				tag_image.Destroy ();
			}

			using (ImageFile image = new ImageFile (thumbnail_path)) {
				Gdk.Pixbuf data = image.Load ();
				data = PixbufUtils.ScaleToMaxSize (data, 400, 400);
				tag_image_height = data.Height;
				tag_image_width = data.Width;
				tag_image = new Gtk.Image (data);
				tag_image_eventbox.Add (tag_image);
				tag_image_eventbox.ShowAll ();
			}
		}

		private void HandleTagImageButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
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

			FacebookTagPopup popup = new FacebookTagPopup (friends);
		}

		private void HandleThumbnailIconViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			thumbnail_iconview.Selection.Clear ();
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			if (account != null) {
				Dialog.Hide ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
				command_thread.Name = Mono.Unix.Catalog.GetString ("Uploading Pictures");

				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();
			}
		}

		private void Upload ()
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
