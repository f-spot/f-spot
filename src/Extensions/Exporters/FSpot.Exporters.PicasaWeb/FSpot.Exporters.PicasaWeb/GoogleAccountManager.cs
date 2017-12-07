//
// GoogleAccountManager.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
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
using System.Collections;
using System.Collections.Generic;

using Hyena;

using Gnome.Keyring;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAccountManager
	{
		private static GoogleAccountManager instance;
		private const string keyring_item_name = "Google Account";
		List<GoogleAccount> accounts;

		public delegate void AccountListChangedHandler (GoogleAccountManager manager, GoogleAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;

		public static GoogleAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new GoogleAccountManager ();
			}

			return instance;
		}

		private GoogleAccountManager ()
		{
			accounts = new List<GoogleAccount> ();
			ReadAccounts ();
		}

		public void MarkChanged ()
		{
			MarkChanged (true, null);
		}

		public void MarkChanged (bool write, GoogleAccount changed_account)
		{
			if (write)
				WriteAccounts ();

			if (AccountListChanged != null)
				AccountListChanged (this, changed_account);
		}

		public List<GoogleAccount> GetAccounts ()
		{
			return accounts;
		}

		public void AddAccount (GoogleAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (GoogleAccount account, bool write)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (GoogleAccount account)
		{
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
			} catch {
				return;
			}
			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			request_attributes["username"] = account.Username;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
				}
			} catch (Exception e) {
				Log.DebugException (e);
			}
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
			} catch {
				return;
			}
			foreach (GoogleAccount account in accounts) {
				Hashtable update_request_attributes = new Hashtable();
				update_request_attributes["name"] = keyring_item_name;
				update_request_attributes["username"] = account.Username;

				try {
					Ring.CreateItem(keyring, ItemType.GenericSecret, keyring_item_name, update_request_attributes, account.Password, true);
				} catch {
					continue;
				}
			}
		}

		private void ReadAccounts ()
		{

			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					if(!result.Attributes.ContainsKey("name") || !result.Attributes.ContainsKey("username") ||
						(result.Attributes["name"] as string) != keyring_item_name)
						continue;

					string username = (string)result.Attributes["username"];
					string password = result.Secret;

					if (username == null || username == string.Empty || password == null || password == string.Empty)
						throw new ApplicationException ("Invalid username/password in keyring");

					GoogleAccount account = new GoogleAccount(username, password);
					if (account != null)
						AddAccount (account, false);

				}
			} catch (Exception e) {
				Log.DebugException (e);
			}

			MarkChanged ();
		}
	}
}
