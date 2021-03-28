//
// GalleryAccountManager.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using FSpot.Settings;
using Hyena;

namespace FSpot.Exporters.Gallery
{
	public class GalleryAccountManager
	{
		private static GalleryAccountManager instance;
		string xml_path;
		List<GalleryAccount> accounts;

		public delegate void AccountListChangedHandler (GalleryAccountManager manager, GalleryAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;

		public static GalleryAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new GalleryAccountManager ();
			}
			return instance;
		}

		private GalleryAccountManager ()
		{
			// FIXME this xml file path should be be retrieved from a central location not hard coded there
			xml_path = Path.Combine (FSpotConfiguration.BaseDirectory, "Accounts.xml");

			accounts = new List<GalleryAccount> ();
			ReadAccounts ();
		}

		public void MarkChanged (bool write = true, GalleryAccount changed_account = null)
		{
			if (write)
				WriteAccounts ();

			if (AccountListChanged != null)
				AccountListChanged (this, changed_account);
		}

		public List<GalleryAccount> GetAccounts ()
		{
			return accounts;
		}

		public void AddAccount (GalleryAccount account, bool write = true)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (GalleryAccount account)
		{
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter (xml_path, System.Text.Encoding.Default);

			writer.Formatting = System.Xml.Formatting.Indented;
			writer.Indentation = 2;
			writer.IndentChar = ' ';

			writer.WriteStartDocument (true);

			writer.WriteStartElement ("GalleryRemote");
			foreach (GalleryAccount account in accounts) {
				writer.WriteStartElement ("Account");
				writer.WriteElementString ("Name", account.Name);

				writer.WriteElementString ("Url", account.Url);
				writer.WriteElementString ("Username", account.Username);
				writer.WriteElementString ("Password", account.Password);
				writer.WriteElementString ("Version", account.Version.ToString());
				writer.WriteEndElement (); //Account
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
		}

		private GalleryAccount ParseAccount (System.Xml.XmlNode node)
		{
			if (node.Name != "Account")

				return null;

			string name = null;
			string url = null;
			string username = null;
			string password = null;
			GalleryVersion version = GalleryVersion.VersionUnknown;

			foreach (System.Xml.XmlNode child in node.ChildNodes) {
				if (child.Name == "Name") {
					name = child.ChildNodes [0].Value;

				} else if (child.Name == "Url") {
					url = child.ChildNodes [0].Value;
				} else if (child.Name == "Password") {
					password = child.ChildNodes [0].Value;
				} else if (child.Name == "Username") {
					username = child.ChildNodes [0].Value;
				} else if (child.Name == "Version") {
					string versionString = child.ChildNodes [0].Value;
					if (versionString == "Version1")
						version = GalleryVersion.Version1;
					else if (versionString == "Version2")
						version = GalleryVersion.Version2;
					else
						Log.Error ("Unexpected versions string: " + versionString);
				}
			}
			return new GalleryAccount (name, url, username, password, version);
		}

		private void ReadAccounts ()
		{

			if (! File.Exists (xml_path)) {
				MarkChanged ();
				return;
			}

			try {
				string query = "//GalleryRemote/Account";
				System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();

				//System.Console.WriteLine ("xml_path: " + xml_path);
				doc.Load (xml_path);
				System.Xml.XmlNodeList nodes = doc.SelectNodes (query);

				//System.Console.WriteLine ("selected {0} nodes match {1}", nodes.Count, query);
				foreach (System.Xml.XmlNode node in nodes) {
					GalleryAccount account = ParseAccount (node);
					if (account != null)
						AddAccount (account, false);

				}
			} catch (System.Exception e) {
				// FIXME do something
				Log.Exception ("Exception loading gallery accounts", e);
			}

			MarkChanged ();
		}
	}
}
