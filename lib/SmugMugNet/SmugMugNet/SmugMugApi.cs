/*
 * SmugMugApi.cs
 *
 * Authors:
 *   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 *
 * Copyright (C) 2006 Thomas Van Machelen
 * This is free software. See COPYING for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Specialized;
using Hyena;

namespace SmugMugNet
{
	public class SmugMugApi
	{
		private string username = String.Empty;
		private string password = String.Empty;
		private bool connected = false;

		private Credentials credentials;
		private const string VERSION = "1.1.1";
		private Category[] categories;

		public bool Connected
		{
			get { return connected; }
		}

		public SmugMugApi (string email_address, string password)
		{
			this.username = email_address;
			this.password = password;
		}

		public bool Login ()
		{
			if (this.username.Length == 0 | this.password.Length == 0)
			{
				throw new SmugMugException("There is no username or password.");
			}

			if (this.connected == false && this.credentials.UserID == 0)
			{
				try
				{
					this.credentials = SmugMugProxy.LoginWithPassword (this.username, this.password);
					this.connected = true;
				}
				catch
				{
					return false;
				}
			}
			else
			{
				LoginWithHash ();
			}

			return true;
		}

		private void LoginWithHash ()
		{
			try {
				string session_id = SmugMugProxy.LoginWithHash (this.credentials.UserID, this.credentials.PasswordHash);

				if (session_id != null && session_id.Length > 0)
				{
					this.credentials.SessionID = session_id;
				}
				else
				{
					throw new SmugMugException ("SessionID was empty");
				}
			}
			catch (Exception ex) {
				throw new SmugMugException ("A login error occured, SessionID may be invalid.", ex.InnerException);
			}
		}

		public void Logout ()
		{
			if (!connected)
				return;

			if (this.credentials.SessionID == null && this.credentials.SessionID.Length == 0)
				return;

			SmugMugProxy.Logout (this.credentials.SessionID);
			connected = false;
			this.credentials = new Credentials (null, 0, null);
		}

		public Category[] GetCategories ()
		{
			if (this.categories == null)
			{
				try {
					this.categories = SmugMugProxy.GetCategories (credentials.SessionID);
				}
				catch (Exception ex) {
					throw new SmugMugException ("Could not retrieve Categories", ex.InnerException);
				}
			}
			return this.categories;
		}

		public Album CreateAlbum (string title, int category_id, bool is_public)
		{
			try {
				return SmugMugProxy.CreateAlbum (title, category_id, credentials.SessionID, is_public);
			}
			catch (Exception ex) {
				throw new SmugMugException ("Could not create album", ex.InnerException);
			}
		}

		public Album[] GetAlbums ()
		{
			try {
				return SmugMugProxy.GetAlbums(credentials.SessionID);
			}
			catch (Exception ex) {
				throw new SmugMugException ("Could not get albums", ex.InnerException);
			}
		}

		public Uri GetAlbumUrl (int image_id)
		{
			try {
				return SmugMugProxy.GetAlbumUrl (image_id, credentials.SessionID);
			}
			catch (Exception ex) {
				throw new SmugMugException ("Could not get album url", ex.InnerException);
			}
		}

		public int Upload (string path, int album_id)
		{
			try {
				return SmugMugProxy.Upload (path, album_id, credentials.SessionID);
			}
			catch (Exception ex) {
				throw new SmugMugException ("Could not upload file", ex.InnerException);
			}
		}
	}
}
