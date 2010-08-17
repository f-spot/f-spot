using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Hyena;
using Hyena.Widgets;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Imaging;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using Mono.Google;
using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAccountManager
	{
		private static GoogleAccountManager instance;
		private const string keyring_item_name = "Google Account";
		ArrayList accounts;

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
			accounts = new ArrayList ();
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

		public ArrayList GetAccounts ()
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

					if (username == null || username == String.Empty || password == null || password == String.Empty)
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
