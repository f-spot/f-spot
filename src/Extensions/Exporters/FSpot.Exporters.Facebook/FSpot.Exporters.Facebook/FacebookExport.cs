//
// FacebookExport.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Jim Ramsay <i.am@jimramsay.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2009 Jim Ramsay
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Mono.Unix;

using Gtk;
using Gnome.Keyring;

using FSpot.Core;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Filters;

using Hyena;
using Hyena.Widgets;

using Mono.Facebook;

namespace FSpot.Exporters.Facebook
{
	internal class FacebookAccount
	{
		static string keyring_item_name = "Facebook Account";

		static string api_key = "c23d1683e87313fa046954ea253a240e";

		/* INSECURE! According to:
		 *
		 * http://wiki.developers.facebook.com/index.php/Desktop_App_Auth_Process
		 *
		 * We should *NOT* put our secret code here, but do an external
		 * authorization using our own PHP page somewhere.
		 */
		static string secret = "743e9a2e6a1c35ce961321bceea7b514";

		FacebookSession facebookSession;
		bool connected = false;

		public FacebookAccount ()
		{
			SessionInfo info = ReadSessionInfo ();
			if (info != null) {
				facebookSession = new FacebookSession (api_key, info);
				try {
					/* This basically functions like a ping to ensure the
					 * session is still valid:
					 */
					facebookSession.HasAppPermission("offline_access");
					connected = true;
				} catch (FacebookException) {
					connected = false;
				}
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

		public bool RevokePermission (string permission)
		{
			return facebookSession.RevokeAppPermission(permission);
		}

		public bool GrantPermission (string permission, Window parent)
		{
			if (facebookSession.HasAppPermission(permission))
				return true;

			Uri uri = facebookSession.GetGrantUri (permission);
			GtkBeans.Global.ShowUri (parent.Screen, uri.ToString ());

			HigMessageDialog mbox = new HigMessageDialog (parent, DialogFlags.DestroyWithParent | DialogFlags.Modal,
					MessageType.Info, ButtonsType.Ok, Catalog.GetString ("Waiting for authorization"),
					Catalog.GetString ("F-Spot will now launch your browser so that you can enable the permission you just selected.\n\nOnce you are directed by Facebook to return to this application, click \"Ok\" below." ));

			mbox.Run ();
			mbox.Destroy ();

			return facebookSession.HasAppPermission(permission);
		}

		public bool HasPermission(string permission)
		{
			return facebookSession.HasAppPermission(permission);
		}

		public FacebookSession Facebook => facebookSession;

		public bool Authenticated => connected;

		bool SaveSessionInfo (SessionInfo info)
		{
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
			} catch (KeyringException e) {
				Log.DebugException (e);
				return false;
			}

			Hashtable attribs = new Hashtable {
				["name"] = keyring_item_name,
				["uid"] = info.uid.ToString (),
				["session_key"] = info.session_key
			};
			//Dictionary<string,string> attribs = new  Dictionary<string, string> ();
			try {
				Ring.CreateItem (keyring, ItemType.GenericSecret, keyring_item_name, attribs, info.secret, true);
			} catch (KeyringException e) {
				Log.DebugException (e);
				return false;
			}

			return true;
		}

		SessionInfo ReadSessionInfo ()
		{
			SessionInfo info = null;

			Hashtable requestAttributes = new Hashtable {
				["name"] = keyring_item_name
			};
			//Dictionary<string, string> request_attributes = new Dictionary<string, string> ();
			try {
				foreach (ItemData result in Ring.Find (ItemType.GenericSecret, requestAttributes)) {
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
			} catch (KeyringException e) {
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
			} catch (KeyringException e) {
				Log.DebugException (e);
				return false;
			}

			Hashtable requestAttributes = new Hashtable {
				["name"] = keyring_item_name
			};
			//Dictionary<string,string> request_attributes = new Dictionary<string, string> ();
			try {
				foreach (ItemData result in Ring.Find (ItemType.GenericSecret, requestAttributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
					success = true;
				}
			} catch (KeyringException e) {
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
			} catch (KeyringException e) {
				connected = false;
				Log.DebugException (e);
			} catch (FacebookException fe) {
				connected = false;
				Log.DebugException (fe);
			}
			return connected;
		}

		public void Deauthenticate ()
		{
			connected = false;
			ForgetSessionInfo ();
		}
	}

	class TagStore : ListStore
	{
		readonly List<Mono.Facebook.Tag> tags;

		public TagStore (FacebookSession session, List<Mono.Facebook.Tag> tags, Dictionary<long, User> friends) : base (typeof (string))
		{
			this.tags = tags;

			foreach (Mono.Facebook.Tag tag in Tags) {
				long subject = tag.Subject;
				User info = friends [subject];
				if (info == null ) {
					try {
						info = session.GetUserInfo (new [] { subject }, new [] { "first_name", "last_name" }) [0];
					}
					catch (FacebookException) {
						continue;
					}
				}
				AppendValues ($"{info.first_name ?? ""} {info.last_name ?? ""}");
			}
		}

		public List<Mono.Facebook.Tag> Tags => tags ?? new List<Mono.Facebook.Tag> ();
	}

	public class FacebookExport : IExporter
	{
		int size = 720;
		int max_photos_per_album = 200;
		FacebookExportDialog dialog;
		TaskProgressDialog progressDialog;
		Task task;
		Album album = null;

		public void Run (IBrowsableCollection selection)
		{
			dialog = new FacebookExportDialog (selection);

			if (selection.Items.Count () > max_photos_per_album) {
				HigMessageDialog mbox = new HigMessageDialog (dialog,
						DialogFlags.DestroyWithParent | DialogFlags.Modal, MessageType.Error,
						ButtonsType.Ok, Catalog.GetString ("Too many images to export"),
						string.Format (Catalog.GetString ("Facebook only permits {0} photographs per album.  Please refine your selection and try again."), max_photos_per_album));
				mbox.Run ();
				mbox.Destroy ();
				return;
			}

			if (dialog.Run () != (int)ResponseType.Ok) {
				dialog.Destroy ();
				return;
			}

			if (dialog.CreateAlbum) {
				string name = dialog.AlbumName;
				if (string.IsNullOrEmpty (name)) {
					HigMessageDialog mbox = new HigMessageDialog (dialog, DialogFlags.DestroyWithParent | DialogFlags.Modal,
							MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Album must have a name"),
							Catalog.GetString ("Please name your album or choose an existing album."));
					mbox.Run ();
					mbox.Destroy ();
					return;
				}

				string description = dialog.AlbumDescription;
				string location = dialog.AlbumLocation;

				try {
					album = dialog.Account.Facebook.CreateAlbum (name, description, location);
				}
				catch (FacebookException fe) {
					HigMessageDialog mbox = new HigMessageDialog (dialog, DialogFlags.DestroyWithParent | DialogFlags.Modal,
							MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Creating a new album failed"),
							string.Format (Catalog.GetString ("An error occurred creating a new album.\n\n{0}"), fe.Message));
					mbox.Run ();
					mbox.Destroy ();
					return;
				}
			} else {
				album = dialog.ActiveAlbum;
			}

			if (dialog.Account != null) {
				dialog.Hide ();

				task = new Task (Upload);

				progressDialog = new TaskProgressDialog (task, Catalog.GetString ("Uploading Pictures"));
				progressDialog.Start ();
			}

			dialog.Destroy ();
		}

		void Upload ()
		{
			IPhoto [] items = dialog.Items;
			string [] captions = dialog.Captions;
			dialog.StoreCaption ();

			long sent_bytes = 0;

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());
			filters.Add (new ResizeFilter ((uint) size));

			for (var i = 0; i < items.Length; i++) {
				try {
					IPhoto item = items [i];

					Log.DebugFormat ($"uploading {i}");

					progressDialog.Message = string.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"), item.Name, i + 1, items.Length);
					progressDialog.ProgressText = string.Empty;
					progressDialog.Fraction = i / (double) items.Length;

					FilterRequest request = new FilterRequest (item.DefaultVersion.Uri);
					filters.Convert (request);

					var fileInfo = new FileInfo (request.Current.LocalPath);

					album.Upload (captions [i] ?? "", request.Current.LocalPath);

					sent_bytes += fileInfo.Length;
				}
				catch (Exception e) {
					progressDialog.Message = string.Format (Catalog.GetString ("Error Uploading To Facebook: {0}"), e.Message);
					progressDialog.ProgressText = Catalog.GetString ("Error");
					Log.DebugException (e);

					if (progressDialog.PerformRetrySkip ())
						i--;
				}
			}

			progressDialog.Message = Catalog.GetString ("Done Sending Photos");
			progressDialog.Fraction = 1.0;
			progressDialog.ProgressText = Catalog.GetString ("Upload Complete");
			progressDialog.ButtonLabel = Stock.Ok;

			var li = new LinkButton ("http://www.facebook.com/group.php?gid=158960179844&ref=mf", Catalog.GetString ("Visit F-Spot group on Facebook"));
			progressDialog.VBoxPackEnd (li);
			li.ShowAll ();
		}
	}
}
