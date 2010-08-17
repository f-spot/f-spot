using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Gtk;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using SmugMugNet;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugAccountManager
	{
		private static SmugMugAccountManager instance;
		private const string keyring_item_name = "SmugMug Account";
		ArrayList accounts;

		public delegate void AccountListChangedHandler (SmugMugAccountManager manager, SmugMugAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;

		public static SmugMugAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new SmugMugAccountManager ();
			}

			return instance;
		}

		private SmugMugAccountManager ()
		{
			accounts = new ArrayList ();
			ReadAccounts ();
		}

		public void MarkChanged ()
		{
			MarkChanged (true, null);
		}

		public void MarkChanged (bool write, SmugMugAccount changed_account)
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

		public void AddAccount (SmugMugAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (SmugMugAccount account, bool write)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (SmugMugAccount account)
		{
			string keyring = Ring.GetDefaultKeyring();
			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			request_attributes["username"] = account.Username;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
				}
			} catch (Exception e) {
				Log.Exception (e);
			}
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			string keyring = Ring.GetDefaultKeyring();
			foreach (SmugMugAccount account in accounts) {
				Hashtable update_request_attributes = new Hashtable();
				update_request_attributes["name"] = keyring_item_name;
				update_request_attributes["username"] = account.Username;

				Ring.CreateItem(keyring, ItemType.GenericSecret, keyring_item_name, update_request_attributes, account.Password, true);
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

					SmugMugAccount account = new SmugMugAccount(username, password);
					if (account != null)
						AddAccount (account, false);

				}
			} catch (Exception e) {
				Log.Exception (e);
			}

			MarkChanged ();
		}
	}
}
