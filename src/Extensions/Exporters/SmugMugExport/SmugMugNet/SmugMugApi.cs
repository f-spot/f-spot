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
using FSpot.Utils;
using Hyena;

namespace SmugMugNet
{
	public struct Credentials
	{
		public string session_id;
		public int user_id;
		public string password_hash;

		public string SessionID {
			get { return session_id; }
			set { session_id = value; }
		}

		public int UserID {
			get { return user_id; }
		}

		public string PasswordHash {
			get { return password_hash; }
		}

		public Credentials(string session_id, int user_id, string password_hash)
		{
			this.session_id = session_id;
			this.user_id = user_id;
			this.password_hash = password_hash;
		}
	}

	public struct Category
	{
		public Category( string title, int id)
		{
			this.title = title;
			this.category_id = id;
		}

		private int category_id;
		public int CategoryID
		{
			get { return category_id; }
			set { category_id = value; }
		}

		private string title;
		public string Title
		{
			get { return title; }
			set { title = value; }
		}
	}

	public struct Album
	{
		public Album(string title, int id)
		{
			this.album_id = id;
			this.title = title;
		}

		private int album_id;
		public int AlbumID
		{
			get { return album_id; }
			set { album_id = value; }
		}

		private string title;
		public string Title
		{
			get { return title; }
			set { title = value; }
		}
	}

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

	public class SmugMugProxy
	{
		// FIXME: this getting should be done over https
		private const string GET_URL = "https://api.SmugMug.com/hack/rest/";
		private const string POST_URL = "https://upload.SmugMug.com/hack/rest/";
		// key from massis
		private const string APIKEY = "umtr0zB2wzwTZDhF2BySidg0hY0le3K6";
		private const string VERSION = "1.1.1";

		// rest methods
		private const string LOGIN_WITHPASS_METHOD = "smugmug.login.withPassword";
		private const string LOGIN_WITHHASH_METHOD = "smugmug.login.withHash";
		private const string LOGOUT_METHOD = "smugmug.logout";
		private const string ALBUMS_CREATE_METHOD = "smugmug.albums.create";
		private const string ALBUMS_GET_URLS_METHOD = "smugmug.images.getURLs";
		private const string ALBUMS_GET_METHOD = "smugmug.albums.get";
		private const string CATEGORIES_GET_METHOD = "smugmug.categories.get";

		// parameter constants
		private const string EMAIL = "EmailAddress";
		private const string PASSWORD = "Password";
		private const string USER_ID = "UserID";
		private const string PASSWORD_HASH = "PasswordHash";
		private const string SESSION_ID = "SessionID";
		private const string CATEGORY_ID = "CategoryID";
		private const string IMAGE_ID = "ImageID";
		private const string TITLE = "Title";
		private const string ID = "id";

		public static Credentials LoginWithPassword (string username, string password)
		{
			string url = FormatGetUrl (LOGIN_WITHPASS_METHOD, new SmugMugParam (EMAIL, username), new SmugMugParam (PASSWORD, password));
			XmlDocument doc = GetResponseXml (url);

			string sessionId = doc.SelectSingleNode ("/rsp/Login/SessionID").InnerText;
			int userId = int.Parse (doc.SelectSingleNode ("/rsp/Login/UserID").InnerText);
			string passwordHash = doc.SelectSingleNode ("/rsp/Login/PasswordHash").InnerText;

			return new Credentials (sessionId, userId, passwordHash);
		}

		public static string LoginWithHash (int user_id, string password_hash)
		{
			string url = FormatGetUrl (LOGIN_WITHHASH_METHOD, new SmugMugParam (USER_ID, user_id), new SmugMugParam (PASSWORD_HASH, password_hash));
			XmlDocument doc = GetResponseXml(url);

			return doc.SelectSingleNode ("/rsp/Login/SessionID").InnerText;
		}

		public static void Logout (string session_id)
		{
			string url = FormatGetUrl (LOGOUT_METHOD, new SmugMugParam (SESSION_ID, session_id));
			GetResponseXml (url);
		}

		public static Album[] GetAlbums (string session_id)
		{
			string url = FormatGetUrl (ALBUMS_GET_METHOD, new SmugMugParam(SESSION_ID, session_id));
			XmlDocument doc = GetResponseXml (url);
			XmlNodeList albumNodes = doc.SelectNodes ("/rsp/Albums/Album");

			Album[] albums = new Album[albumNodes.Count];

			for (int i = 0; i < albumNodes.Count; i++)
			{
				XmlNode current = albumNodes[i];
				albums[i] = new Album (current.SelectSingleNode (TITLE).InnerText, int.Parse (current.Attributes[ID].Value));
			}
			return albums;
		}

		public static Uri GetAlbumUrl (int image_id, string session_id)
		{
			string url = FormatGetUrl(ALBUMS_GET_URLS_METHOD, new SmugMugParam(IMAGE_ID, image_id), new SmugMugParam(SESSION_ID, session_id));
			XmlDocument doc = GetResponseXml(url);

			string album_url = doc.SelectSingleNode("/rsp/ImageURLs/Image/AlbumURL").InnerText;

			return new Uri(album_url);
		}

		public static Category[] GetCategories (string session_id)
		{
			string url = FormatGetUrl(CATEGORIES_GET_METHOD, new SmugMugParam (SESSION_ID, session_id));
			XmlDocument doc = GetResponseXml (url);

			XmlNodeList categoryNodes = doc.SelectNodes ("/rsp/Categories/Category");
			Category[] categories = new Category[categoryNodes.Count];

			for (int i = 0; i < categoryNodes.Count; i++)
			{
				XmlNode current = categoryNodes[i];
				categories[i] = new Category (current.SelectSingleNode (TITLE).InnerText, int.Parse (current.Attributes[ID].Value));
			}
			return categories;
		}

		public static Album CreateAlbum (string title, int category_id, string session_id)
		{
			return CreateAlbum (title, category_id, session_id, true);
		}

		public static Album CreateAlbum (string title, int category_id, string session_id, bool is_public)
		{
			int public_int = is_public ? 1 : 0;
			string url = FormatGetUrl (ALBUMS_CREATE_METHOD, new SmugMugParam (TITLE, title), new SmugMugParam (CATEGORY_ID, category_id), new SmugMugParam (SESSION_ID, session_id), new SmugMugParam ("Public", public_int));
			XmlDocument doc = GetResponseXml (url);

			int id = int.Parse(doc.SelectSingleNode("/rsp/Create/Album").Attributes[ID].Value);

			return new Album(title, id);
		}

		public static int Upload (string path, int album_id, string session_id)
		{
			FileInfo file = new FileInfo(path);

			if (!file.Exists)
				throw new ArgumentException("Image does not exist: " + file.FullName);

			try
			{
				WebClient client = new WebClient ();
				client.BaseAddress = "http://upload.smugmug.com";
				client.Headers.Add ("Cookie:SMSESS=" + session_id);

				NameValueCollection queryStringCollection = new NameValueCollection ();
				queryStringCollection.Add ("AlbumID", album_id.ToString());
				// Temporarily disabled because rest doesn't seem to return the ImageID anymore
				// queryStringCollection.Add ("ResponseType", "REST");
				// luckily JSON still holds it
				queryStringCollection.Add ("ResponseType", "JSON");
				client.QueryString = queryStringCollection;

				byte[] responseArray = client.UploadFile ("http://upload.smugmug.com/photos/xmladd.mg", "POST", file.FullName);
				string response = Encoding.ASCII.GetString (responseArray);

				// JSon approach
				Regex id_regex = new Regex ("\\\"id\\\":( )?(?<image_id>\\d+),");
				Match m  = id_regex.Match (response);

				int id = -1;

				if (m.Success)
					id = int.Parse (m.Groups["image_id"].Value);

				return id;

				// REST approach, disabled for now
				//XmlDocument doc = new XmlDocument ();
				//doc.LoadXml (response);
				// return int.Parse (doc.SelectSingleNode ("/rsp/ImageID").InnerText);

			}
			catch (Exception ex)
			{
				throw new SmugMugUploadException ("Error uploading image: " + file.FullName, ex.InnerException);
			}
		}

		private static string FormatGetUrl(string method_name, params SmugMugParam[] parameters)
		{
			StringBuilder builder = new StringBuilder (string.Format ("{0}{1}/?method={2}", GET_URL, VERSION, method_name));

			foreach (SmugMugParam param in parameters)
				builder.Append (param.ToString ());

			builder.Append (new SmugMugParam ("APIKey", APIKEY));
			return builder.ToString();
		}

		private static XmlDocument GetResponseXml (string url)
		{
			HttpWebRequest request = HttpWebRequest.Create (url) as HttpWebRequest;
			request.Credentials = CredentialCache.DefaultCredentials;
			WebResponse response = request.GetResponse ();

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (new StreamReader (response.GetResponseStream ()).ReadToEnd ());
			CheckResponseXml (doc);

			response.Close ();
			return doc;
		}

		private static void CheckResponseXml (XmlDocument doc)
		{
			if (doc.SelectSingleNode("/rsp").Attributes["stat"].Value == "ok")
				return;

			string message = doc.SelectSingleNode ("/rsp/err").Attributes["msg"].Value;
			throw new SmugMugException (message);
		}

		private class SmugMugParam
		{
			string name;
			object value;

			public SmugMugParam (string name, object value)
			{
				this.name = name;
				this.value = (value is String ? System.Web.HttpUtility.UrlEncode ((string)value) : value);
			}

			public string Name
			{
				get {return name;}
			}

			public object Value
			{
				get {return value;}
			}

			public override string ToString()
			{
				return string.Format("&{0}={1}", Name, Value);
			}
		}
	}

	public class SmugMugException : ApplicationException
	{
		public SmugMugException(string message) : base (message)
		{
			Log.Debug (message);
		}

		public SmugMugException (string message, Exception innerException) : base (message, innerException)
		{
			Log.Debug (message, innerException.ToString());
		}
	}

	public sealed class SmugMugUploadException : ApplicationException
	{
		public SmugMugUploadException (string message, Exception innerException) : base (message, innerException)
		{
			Log.Debug (message, innerException.ToString ());
		}
	}
}
