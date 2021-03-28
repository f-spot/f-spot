//
// FlickrExport.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008-2009 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using FlickrNet;

using FSpot.Core;
using FSpot.Database;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.UI.Dialog;
using FSpot.Widgets;

using Gtk;

using Hyena;
using Hyena.Widgets;

using Mono.Unix;

namespace FSpot.Exporters.Flickr
{
	public class FlickrExport : Extensions.IExporter
	{
		IBrowsableCollection selection;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton tag_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton hierarchy_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton ignore_top_level_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton open_check;
		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;
		[GtkBeans.Builder.Object] Entry oauth_verification_code;
		[GtkBeans.Builder.Object] Gtk.Button auth_flickr;
		[GtkBeans.Builder.Object] Gtk.ProgressBar used_bandwidth;
		[GtkBeans.Builder.Object] Gtk.Button do_export_flickr;
		[GtkBeans.Builder.Object] Gtk.Label auth_label;
		[GtkBeans.Builder.Object] Gtk.RadioButton public_radio;
		[GtkBeans.Builder.Object] Gtk.CheckButton family_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton friend_check;
#pragma warning restore 649

		GtkBeans.Builder builder;
		string dialog_name = "flickr_export_dialog";
		Thread command_thread;
		ThreadProgressDialog progress_dialog;
		ProgressItem progress_item;

		public const string EXPORT_SERVICE = "flickr/";
		public const string SCALE_KEY = Preferences.ExportKey + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.ExportKey + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.ExportKey + EXPORT_SERVICE + "browser";
		public const string TAGS_KEY = Preferences.ExportKey + EXPORT_SERVICE + "tags";
		public const string PUBLIC_KEY = Preferences.ExportKey + EXPORT_SERVICE + "public";
		public const string FAMILY_KEY = Preferences.ExportKey + EXPORT_SERVICE + "family";
		public const string FRIENDS_KEY = Preferences.ExportKey + EXPORT_SERVICE + "friends";
		public const string TAG_HIERARCHY_KEY = Preferences.ExportKey + EXPORT_SERVICE + "tag_hierarchy";
		public const string IGNORE_TOP_LEVEL_KEY = Preferences.ExportKey + EXPORT_SERVICE + "ignore_top_level";

		bool open;
		bool scale;

		bool is_public;
		bool is_friend;
		bool is_family;

		OAuthAccessToken token;

		int photo_index;
		int size;

		FlickrRemote fr;
		FlickrRemote.Service current_service;

		string auth_text;
		State state;

		enum State
		{
			// not connected to flickr at all
			Disconnected,
			// Got the request token, opened the browser, waiting for verification code from the end user
			Connected,
			// Trying to get access token
			InAuth,
			// logged to flickr with a valid access token
			Authorized
		}

		State CurrentState {
			get { return state; }
			set {
				switch (value) {
				case State.Disconnected:
					auth_label.Text = auth_text;
					auth_flickr.Sensitive = true;
					oauth_verification_code.Visible = false;
					oauth_verification_code.Sensitive = false;
					do_export_flickr.Sensitive = false;
					auth_flickr.Label = Catalog.GetString ("Authorize");
					used_bandwidth.Visible = false;
					break;
				case State.Connected:
					auth_flickr.Sensitive = true;
					do_export_flickr.Sensitive = false;
					oauth_verification_code.Visible = true;
					oauth_verification_code.Sensitive = true;
					auth_label.Text = string.Format (Catalog.GetString ("Return to this window after you have finished the authorization process on {0} and click the \"Complete Authorization\" button below"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Complete Authorization");
					used_bandwidth.Visible = false;
					break;
				case State.InAuth:
					auth_flickr.Sensitive = false;
					oauth_verification_code.Visible = true;
					oauth_verification_code.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Logging into {0}"), current_service.Name);
					auth_flickr.Label = Catalog.GetString ("Checking credentials...");
					do_export_flickr.Sensitive = false;
					used_bandwidth.Visible = false;
					break;
				case State.Authorized:
					do_export_flickr.Sensitive = true;
					auth_flickr.Sensitive = true;
					oauth_verification_code.Visible = false;
					oauth_verification_code.Sensitive = false;
					auth_label.Text = string.Format (Catalog.GetString ("Welcome, {0}. You are connected to {1}."),
										token.Username,
										current_service.Name);
					auth_flickr.Label = string.Format (Catalog.GetString ("Sign in as a different user"));
					used_bandwidth.Visible = !fr.Connection.PeopleGetUploadStatus ().IsPro &&
									fr.Connection.PeopleGetUploadStatus ().BandwidthMax > 0;
					if (used_bandwidth.Visible) {
						used_bandwidth.Fraction = fr.Connection.PeopleGetUploadStatus ().PercentageUsed;
						used_bandwidth.Text = string.Format (Catalog.GetString ("Used {0} of your allowed {1} monthly quota"),
									GLib.Format.SizeForDisplay (fr.Connection.PeopleGetUploadStatus ().BandwidthUsed),
									GLib.Format.SizeForDisplay (fr.Connection.PeopleGetUploadStatus ().BandwidthMax));
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
			current_service = FlickrRemote.Service.FromSupported (service);

			var view = new TrayView (selection);
			view.DisplayTags = display_tags;
			view.DisplayDates = false;

			builder = new GtkBeans.Builder (null, "flickr_export.ui", null);
			builder.Autoconnect (this);

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
			LoadPreference (PUBLIC_KEY);
			LoadPreference (FAMILY_KEY);
			LoadPreference (FRIENDS_KEY);
			LoadPreference (current_service.PreferencePath);

			do_export_flickr.Sensitive = false;
			fr = new FlickrRemote (token, current_service);
			if (token != null) {
				StartAuth ();
			}
		}

		public bool StartAuth ()
		{
			CurrentState = State.InAuth;
			if (command_thread == null || !command_thread.IsAlive) {
				command_thread = new Thread (new ThreadStart (CheckAuthorization));
				command_thread.Start ();
			}
			return true;
		}

		public void CheckAuthorization ()
		{
			var args = new AuthorizationEventArgs ();
			try {
				token = fr.CheckLogin (oauth_verification_code.Text);
			} catch (FlickrException e) {
				args.Exception = e;
			} catch (Exception e) {
				var md =
					new HigMessageDialog (Dialog,
								  Gtk.DialogFlags.Modal |
								  Gtk.DialogFlags.DestroyWithParent,
								  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								  Catalog.GetString ("Unable to log on"), e.Message);

				md.Run ();
				md.Destroy ();
				return;
			}

			ThreadAssist.ProxyToMain (() => {
				do_export_flickr.Sensitive = token != null;
				if (token != null) {
					CurrentState = State.Authorized;
					Preferences.Set (current_service.PreferencePath, token.Token);
					Preferences.Set (current_service.PreferencePath + "secret", token.TokenSecret);
					Preferences.Set (current_service.PreferencePath + "userId", token.UserId);
					Preferences.Set (current_service.PreferencePath + "userName", token.Username);
				} else {
					CurrentState = State.Disconnected;
				}
			});
		}

		class AuthorizationEventArgs : EventArgs
		{
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
		}

		public void HandleSizeActive (object sender, EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		void Logout ()
		{
			token = null;
			fr = new FlickrRemote (token, current_service);
			Preferences.Set (current_service.PreferencePath, string.Empty);
			CurrentState = State.Disconnected;
		}

		void Login ()
		{
			try {
				fr = new FlickrRemote (token, current_service);
				fr.TryWebLogin ();
				CurrentState = State.Connected;
			} catch (Exception e) {
				if (e is FlickrApiException && (e as FlickrApiException).Code == 98) {
					Logout ();
					Login ();
				} else {
					var md =
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

		void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double)selection.Count;
		}

		FileInfo info;
		void HandleFlickrProgress (object sender, UploadProgressEventArgs args)
		{
			if (args.UploadComplete) {
				progress_dialog.Fraction = photo_index / (double)selection.Count;
				progress_dialog.ProgressText = string.Format (Catalog.GetString ("Waiting for response {0} of {1}"),
										  photo_index, selection.Count);
			}
			progress_dialog.Fraction = (photo_index - 1.0 + (args.BytesSent / (double)info.Length)) / (double)selection.Count;
		}

		class DateComparer : IComparer<IPhoto>
		{
			public int Compare (IPhoto left, IPhoto right)
			{
				return DateTime.Compare (left.Time, right.Time);
			}
		}

		void Upload ()
		{
			progress_item = new ProgressItem ();
			progress_item.Changed += HandleProgressChanged;
			fr.Connection.OnUploadProgress += HandleFlickrProgress;

			var ids = new List<string> ();
			IPhoto[] photos = selection.Items.ToArray ();
			Array.Sort (photos, new DateComparer ());

			for (int index = 0; index < photos.Length; index++) {
				try {
					IPhoto photo = photos[index];
					progress_dialog.Message = string.Format (Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);
					progress_dialog.Fraction = photo_index / (double)selection.Count;
					photo_index++;
					progress_dialog.ProgressText = string.Format (
						Catalog.GetString ("{0} of {1}"), photo_index,
						selection.Count);

					info = new FileInfo (photo.DefaultVersion.Uri.LocalPath);
					var stack = new FilterSet ();
					if (scale)
						stack.Add (new ResizeFilter ((uint)size));

					string id = fr.Upload (photo, stack, is_public, is_family, is_friend);
					ids.Add (id);

					if (App.Instance.Database != null && photo is Photo)
						App.Instance.Database.Exports.Create ((photo as Photo).Id,
										  (photo as Photo).DefaultVersionId,
										  ExportStore.FlickrExportType,
										  token.UserId + ":" + token.Username + ":" + current_service.Name + ":" + id);

				} catch (Exception e) {
					progress_dialog.Message = string.Format (Catalog.GetString ("Error Uploading To {0}: {1}"),
										 current_service.Name,
										 e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					Log.Exception (e);

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
					view_url = string.Format ("http://www.{0}/photos/{1}/", current_service.Name, token.Username);
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

		void HandleClicked (object sender, EventArgs args)
		{
			Log.Debug ("Current state: " + CurrentState);
			Log.Debug ("Current verification text: " + oauth_verification_code.Text);
			switch (CurrentState) {
			// not connected to flickr at all. Initiate OAuth login
			case State.Disconnected:
				Login ();
				break;
			// we were waiting for the verification code. Check that and attempt to complete the OAuth flow with it
			case State.Connected:
				StartAuth ();
				break;
			case State.InAuth:
				break;
			// we were logged in, so logout
			case State.Authorized:
				Logout ();
				Login ();
				break;
			}
		}

		void HandlePublicChanged (object sender, EventArgs args)
		{
			bool sensitive = !public_radio.Active;
			friend_check.Sensitive = sensitive;
			family_check.Sensitive = sensitive;
		}

		void HandleTagChanged (object sender, EventArgs args)
		{
			hierarchy_check.Sensitive = tag_check.Active;
		}

		void HandleHierarchyChanged (object sender, EventArgs args)
		{
			ignore_top_level_check.Sensitive = hierarchy_check.Active;
		}

		void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				if (command_thread != null && command_thread.IsAlive)
					command_thread.Abort ();

				Dialog.Destroy ();
				return;
			}

			if (fr.CheckLogin (oauth_verification_code.Text) == null) {
				do_export_flickr.Sensitive = false;
				var md =
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
			Preferences.Set (PUBLIC_KEY, public_radio.Active);
			Preferences.Set (FAMILY_KEY, family_check.Active);
			Preferences.Set (FRIENDS_KEY, friend_check.Active);
			Preferences.Set (TAG_HIERARCHY_KEY, hierarchy_check.Active);
			Preferences.Set (IGNORE_TOP_LEVEL_KEY, ignore_top_level_check.Active);
			Preferences.Set (current_service.PreferencePath, fr.Token.Token);
			Preferences.Set (current_service.PreferencePath + "secret", fr.Token.TokenSecret);
			Preferences.Set (current_service.PreferencePath + "userId", fr.Token.UserId);
			Preferences.Set (current_service.PreferencePath + "userName", fr.Token.Username);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				scale_check.Active = Preferences.Get<bool> (key);
				break;

			case SIZE_KEY:
				size_spin.Value = (double)Preferences.Get<int> (key);
				break;

			case BROWSER_KEY:
				open_check.Active = Preferences.Get<bool> (key);
				break;

			case TAGS_KEY:
				tag_check.Active = Preferences.Get<bool> (key);
				break;

			case TAG_HIERARCHY_KEY:
				hierarchy_check.Active = Preferences.Get<bool> (key);
				break;

			case IGNORE_TOP_LEVEL_KEY:
				ignore_top_level_check.Active = Preferences.Get<bool> (key);
				break;

			case FlickrRemote.TOKEN_FLICKR:
			case FlickrRemote.TOKEN_23HQ:
			case FlickrRemote.TOKEN_ZOOOMR:
				token = new OAuthAccessToken ();
				token.Token = Preferences.Get<string> (key);
				token.TokenSecret = Preferences.Get<string> (key + "secret");
				token.UserId = Preferences.Get<string> (key + "userId");
				token.Username = Preferences.Get<string> (key + "userName");
				break;

			case PUBLIC_KEY:
				public_radio.Active = Preferences.Get<bool> (key);
				break;

			case FAMILY_KEY:
				family_check.Active = Preferences.Get<bool> (key);
				break;

			case FRIENDS_KEY:
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

		Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));

				return dialog;
			}
		}
	}
}
