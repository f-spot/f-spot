//
// FSpotTabbloExport.TabbloExportModel
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2009 Wojciech Dzierzanowski
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using FSpot.Utils;

using Mono.Tabblo;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace FSpotTabbloExport {

	class TabbloExportModel : Mono.Tabblo.IPreferences {

		private FSpot.IBrowsableCollection photo_collection;

		private string username;
		private string password;

		private bool attach_tags = false;
		private FSpot.Tag [] attached_tags;
		private bool remove_tags = false;
		private FSpot.Tag [] removed_tags;

		private static readonly FSpot.Tag [] no_tags = new FSpot.Tag [0];


		// `FSpot.Preferences' constants.
		private const string PrefPrefix =
				FSpot.Preferences.APP_FSPOT_EXPORT + "tabblo/";
		private const string PrefAttachTags =
				PrefPrefix + "attach_tags";
		private const string PrefAttachedTags =
				PrefPrefix + "attached_tags";
		private const string PrefRemoveTags =
				PrefPrefix + "remove_tags";
		private const string PrefRemovedTags =
				PrefPrefix + "removed_tags";

		// Keyring constants.
		private const string KeyringItemName = "Tabblo Account";
		private const string KeyringItemApp = "FSpotTabbloExport";
		private const string KeyringItemNameAttr = "name";
		private const string KeyringItemUsernameAttr = "username";
		private const string KeyringItemAppAttr = "application";


		// The photos.

		internal FSpot.IBrowsableCollection PhotoCollection {
			get {
				return photo_collection;
			}
			set {
				photo_collection = value;
			}
		}

		internal FSpot.IBrowsableItem [] Photos {
			get {
				return photo_collection.Items;
			}
		}


		// `Mono.Tabblo.IPreferences' implementation.

		internal event EventHandler UsernameChanged;
		public string Username {
			get {
				return null != username
						? username : string.Empty;
			}
			internal set {
				string old_value = username;
				username = value;
				OnMaybePropertyChanged (old_value, username,
						UsernameChanged);
			}
		}

		internal event EventHandler PasswordChanged;
		public string Password {
			get {
				return null != password
						? password : string.Empty;
			}
			internal set {
				string old_value = password;
				password = value;
				OnMaybePropertyChanged (old_value, password,
						PasswordChanged);
			}
		}

		// FIXME:  Hopefully, we'll have a use for this one day.  Then
		//         we'll have to actually implement the property.
		public string Privacy {
			get {
				return "circle";
			}
		}


		// The tags.

		internal event EventHandler AttachTagsChanged;
		internal bool AttachTags {
			get {
				return attach_tags;
			}
			set {
				bool old_value = attach_tags;
				attach_tags = value;
				OnMaybePropertyChanged (old_value, attach_tags,
						AttachTagsChanged);
			}
		}

		internal event EventHandler AttachedTagsChanged;
		internal FSpot.Tag [] AttachedTags {
			get {
				return null != attached_tags
						? attached_tags	: no_tags;
			}
			set {
				FSpot.Tag [] old_value = attached_tags;
				attached_tags = value;
				OnMaybePropertyChanged (old_value, attached_tags,
						AttachedTagsChanged);
			}
		}

		internal event EventHandler RemoveTagsChanged;
		internal bool RemoveTags {
			get {
				return remove_tags;
			}
			set {
				bool old_value = remove_tags;
				remove_tags = value;
				OnMaybePropertyChanged (old_value, remove_tags,
						RemoveTagsChanged);
			}
		}

		internal event EventHandler RemovedTagsChanged;
		internal FSpot.Tag [] RemovedTags {
			get {
				return null != removed_tags
						? removed_tags : no_tags;
			}
			set {
				FSpot.Tag [] old_value = removed_tags;
				removed_tags = value;
				OnMaybePropertyChanged (old_value, removed_tags,
						RemovedTagsChanged);
			}
		}


		private void OnMaybePropertyChanged (object old_value,
				object new_value, EventHandler handler)
		{
			if (!object.Equals (old_value, new_value)
					&& null != handler) {
				handler (this, EventArgs.Empty);
			}

		}



		internal void Serialize ()
		{
			WriteAccountData ();
			WriteTagPreferences ();
		}

		internal void Deserialize ()
		{
			ReadTagPreferences ();
			ReadAccountData ();
		}


		private void WriteAccountData ()
		{
			try {
				string keyring = Gnome.Keyring
						.Ring.GetDefaultKeyring ();
				
				Hashtable attrs = new Hashtable ();
				attrs [KeyringItemNameAttr] = KeyringItemName;
				attrs [KeyringItemAppAttr] = KeyringItemApp;

				Gnome.Keyring.ItemType type = Gnome.Keyring
						.ItemType.GenericSecret;

				try {
					Gnome.Keyring.ItemData [] items = Gnome
							.Keyring.Ring.Find (
									type,
									attrs);
							
					foreach (Gnome.Keyring.ItemData item
							in items) {
						Gnome.Keyring.Ring.DeleteItem (
								keyring,
								item.ItemID);
					}
				} catch (Gnome.Keyring.KeyringException e) {
					Log.Exception ("Error deleting old "
							+ "account data", e);
				}
				
				attrs [KeyringItemUsernameAttr] = Username;

				Gnome.Keyring.Ring.CreateItem (keyring, type,
						KeyringItemName, attrs,
						Password, true);
				
			} catch (Gnome.Keyring.KeyringException e) {
				Log.Exception ("Error writing account data", e);
			}
		}

		private void ReadAccountData ()
		{
			string new_username = string.Empty;
			string new_password = string.Empty;

			Hashtable attrs = new Hashtable ();
			attrs [KeyringItemNameAttr] = KeyringItemName;
			attrs [KeyringItemAppAttr] = KeyringItemApp;
			
			try {
				Gnome.Keyring.ItemType type = Gnome.Keyring
						.ItemType.GenericSecret;
				Gnome.Keyring.ItemData [] items =
						Gnome.Keyring.Ring.Find (
								type, attrs);
				if (1 < items.Length) {
					Log.Warning ("More than one {0} "
							+ " found in keyring",
							KeyringItemName);
				}
			
				if (1 <= items.Length) {
					Log.Debug ("{0} data found in "
							+ "keyring",
							KeyringItemName);
					attrs =	items [0].Attributes;
					new_username = (string) attrs [
						KeyringItemUsernameAttr];
					new_password = items [0].Secret;
				}
				
			} catch (Gnome.Keyring.KeyringException e) {
				Log.Exception ("Error reading account data", e);
			}

			Username = new_username;
			Password = new_password;
		}


		private void WriteTagPreferences ()
		{
			Debug.Assert (!AttachTags
					|| (null != AttachedTags
						&& AttachedTags.Length > 0));
			FSpot.Preferences.Set (PrefAttachedTags,
					ToIds (AttachedTags));
			FSpot.Preferences.Set (PrefAttachTags, AttachTags);

			Debug.Assert (!RemoveTags
					|| (null != RemovedTags
						&& RemovedTags.Length > 0));
			FSpot.Preferences.Set (PrefRemovedTags,
					ToIds (RemovedTags));
			FSpot.Preferences.Set (PrefRemoveTags, RemoveTags);
		}

		private void ReadTagPreferences ()
		{
			int [] attached_tags_pref = null;
			if (FSpot.Preferences.TryGet (PrefAttachedTags,
						out attached_tags_pref)) {
				AttachedTags = ToTags (attached_tags_pref);
			}
			// FIXME:  How do you `java.util.Arrays.toString(int[])'
			//         in C#?
			Log.Debug ("Read from prefs: attached_tags = "
					+ AttachedTags);

			bool attach_tags_pref = false;
			if (FSpot.Preferences.TryGet (PrefAttachTags,
						out attach_tags_pref)) {
				AttachTags = attach_tags_pref
						&& AttachedTags.Length > 0;
			}
			Log.Debug ("Read from prefs: attach_tags_pref = "
					+ attach_tags_pref);

			int [] removed_tags_pref = null;
			if (FSpot.Preferences.TryGet (PrefRemovedTags,
						out removed_tags_pref)) {
				RemovedTags = ToTags (removed_tags_pref);
			}
			// FIXME:  How do you `java.util.Arrays.toString(int[])'
			//         in C#?
			Log.Debug ("Read from prefs: removed_tags_pref = "
					+ removed_tags_pref);

			bool remove_tags_pref = false;
			if (FSpot.Preferences.TryGet (PrefRemoveTags,
						out remove_tags_pref)) {
				RemoveTags = remove_tags_pref
						&& RemovedTags.Length > 0;
			}
			Log.Debug ("Read from prefs: remove_tags_pref = "
					+ remove_tags_pref);
		}
		
		
		private static FSpot.Tag [] ToTags (int [] ids)
		{
			if (null == ids) {
				return null;
			}

			List <FSpot.Tag> tags =
					new List <FSpot.Tag> (ids.Length);
			foreach (int id in ids) {
				FSpot.Tag tag = FSpot.Core.Database.Tags
						.GetTagById (id);
				if (null != tag) {
					tags.Add (tag);
				} else {
					Log.Warning ("No such tag ID in DB: "
							+ id);
				}
			}
			return tags.ToArray ();
		}

		private static int [] ToIds (FSpot.Tag [] tags)
		{
			if (null == tags) {
				return null;
			}

			int [] ids = new int [tags.Length];
			for (int i = 0; i < ids.Length; ++i) {
				ids [i] = (int) tags [i].Id;
			}
			return ids;
		}

	}
}
