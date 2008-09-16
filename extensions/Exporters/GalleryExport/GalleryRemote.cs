using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using FSpot;
using FSpot.UI.Dialog;

/* These classes are based off the documentation at
 *
 * http://codex.gallery2.org/index.php/Gallery_Remote:Protocol
 */

namespace GalleryRemote {
	public enum AlbumPermission : byte
	{
		None = 0,
		Add = 1,
		Write = 2,
		Delete = 4,
		DeleteAlbum = 8,
		CreateSubAlbum = 16
	}

	public class Album : IComparable {
		public int RefNum;
		public string Name = null;
		public string Title = null;
		public string Summary = null;
		public int ParentRefNum;
		public int ResizeSize;
		public int ThumbSize;
		public ArrayList Images = null;
		public string BaseURL;

		Gallery gallery;

		public AlbumPermission Perms = AlbumPermission.None;

		public Album Parent {
			get {
				if (ParentRefNum != 0)
					return gallery.LookupAlbum (ParentRefNum);
				else
					return null;
			}
		}

		protected ArrayList parents = null;
		public ArrayList Parents {
			get {
				if (parents != null)
					return parents;

				if (Parent == null) {
				       parents = new ArrayList ();
				} else {
					parents = Parent.Parents.Clone () as ArrayList;
					parents.Add (Parent.RefNum);
				}

				return parents;
			}
		}

		public Gallery Gallery {
			get { return gallery; }
		}

		public Album (Gallery gallery, string name, int ref_num)
		{
			Name = name;
			this.gallery = gallery;
			this.RefNum = ref_num;
			Images = new ArrayList ();
		}

		public void Rename (string name)
		{
			gallery.MoveAlbum (this, name);
		}

		public void Add (FSpot.IBrowsableItem item)
		{
			Add (item, item.DefaultVersionUri.LocalPath);
		}

		public int Add (FSpot.IBrowsableItem item, string path)
		{
			if (item == null)
				Console.WriteLine ("NO PHOTO");

			return gallery.AddItem (this,
					 path,
					 Path.GetFileName (item.DefaultVersionUri.LocalPath),
					 item.Name,
					 item.Description,
					 true);
		}

		public string GetUrl ()
		{
			return gallery.GetAlbumUrl(this);
		}

		public int CompareTo (Object obj)
		{
			Album other = obj as Album;

			int numThis = this.Parents.Count;
			int numOther = other.Parents.Count;
			int thisVal = -1, otherVal = -1;

			//find where they first differ
			int maxIters = Math.Min (numThis, numOther);
			int i = 0;
			while (i < maxIters) {
				thisVal = (int)this.Parents[i];
				otherVal = (int)other.Parents[i];
				if (thisVal != otherVal) {
					break;
				}
				i++;
			}

			int retVal;
			if (i < numThis && i < numOther) {
				//Parentage differed
				retVal = thisVal.CompareTo (otherVal);

			} else if (i < numThis) {
				//other shorter
				thisVal = (int)this.Parents[i];
				retVal = thisVal.CompareTo (other.RefNum);

				//if equal, we want to make the shorter one come first
				if (retVal == 0)
					retVal = 1;

			} else if (i < numOther) {
				//this shorter
				otherVal = (int)other.Parents[i];
				retVal = this.RefNum.CompareTo (otherVal);

				//if equal, we want to make the shorter one come first
				if (retVal == 0)
					retVal = -1;

			} else {
				//children of the same parent
				retVal = this.RefNum.CompareTo (other.RefNum);
			}

			return retVal;
		}
	}

	public class Image {
		public string Name;
		public int RawWidth;
		public int RawHeight;
		public string ResizedName;
		public int ResizedWidth;
		public int ResizedHeight;
		public string ThumbName;
		public int ThumbWidth;
		public int ThumbHeight;
		public int RawFilesize;

		public string Caption;
		public string Description;
		public int Clicks;

		public Album Owner;

		public string Url;

		public Image (Album album, string name) {
			Name = name;
			Owner = album;
		}
	}

	public enum ResultCode {
		Success = 0,
		MajorVersionInvalid = 101,
		MajorMinorVersionInvalid = 102,
		VersionFormatInvalid = 103,
		VersionMissing = 104,
		PasswordWrong = 201,
		LoginMissing = 202,
		UnknownComand = 301,
		NoAddPermission = 401,
		NoFilename = 402,
		UploadPhotoFailed = 403,
		NoWritePermission = 404,
		NoCreateAlbumPermission = 501,
		CreateAlbumFailed = 502,
		// This result is specific to this implementation
		UnknownResponse = 1000
	}

	public class GalleryException : System.Exception {
		string response_text;

		public string ResponseText {
			get { return response_text; }
		}

		public GalleryException (string text) : base (text)
		{
		}

		public GalleryException (string text, string full_response) : base (text)
		{
			response_text = full_response;
		}
	}

	public class GalleryCommandException : GalleryException {
		ResultCode status;

		public GalleryCommandException (string status_text, ResultCode result) : base (status_text) {
			status = result;
		}

		public ResultCode Status {
			get {
				return status;
			}
		}
	}

	public abstract class Gallery
	{
		protected Uri uri;
		public Uri Uri{
			get {
				return uri;
			}
		}


		protected string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		string auth_token;
		public string AuthToken {
			get {
				return auth_token;
			}
			set {
				auth_token = value;
			}
		}

		protected GalleryVersion version;

		public GalleryVersion Version {
			get {
				return version;
			}
		}

		protected ArrayList albums;
		public ArrayList Albums{
			get {
				return albums;
			}
		}

		public bool expect_continue = true;

		protected CookieContainer cookies = null;
		public FSpot.ProgressItem Progress = null;

		public abstract void Login (string username, string passwd);
		public abstract ArrayList FetchAlbums ();
		public abstract ArrayList FetchAlbumsPrune ();
		public abstract bool MoveAlbum (Album album, string end_name);
		public abstract int AddItem (Album album, string path, string filename, string caption, string description, bool autorotate);
		//public abstract Album AlbumProperties (string album);
		public abstract bool NewAlbum (string parent_name, string name, string title, string description);
		public abstract ArrayList FetchAlbumImages (Album album, bool include_ablums);

		public abstract string GetAlbumUrl (Album album);

		public Gallery (string name)
		{
			this.name = name;
			cookies = new CookieContainer ();
			albums = new ArrayList ();
		}

		public static GalleryVersion DetectGalleryVersion (string url)
		{
			//Figure out if the url is for G1 or G2
			Console.WriteLine ("Detecting Gallery version");

			GalleryVersion version;

			if (url.EndsWith (Gallery1.script_name)) {
				version = GalleryVersion.Version1;
			} else if (url.EndsWith (Gallery2.script_name)) {
				version = GalleryVersion.Version2;
			} else {
				//check what script is available on the server

				FormClient client = new FormClient ();

				try {
					client.Submit (new Uri (Gallery.FixUrl (url, Gallery1.script_name)));
					version =  GalleryVersion.Version1;

				} catch (System.Net.WebException) {
					try {
						client.Submit (new Uri (Gallery.FixUrl (url, Gallery2.script_name)));
						version =  GalleryVersion.Version2;

					} catch (System.Net.WebException) {
						//Uh oh, neither version detected
						version = GalleryVersion.VersionUnknown;
					}
				}
			}

			Console.WriteLine ("Detected: " + version.ToString());
			return version;
		}


		public bool IsConnected ()
		{

			bool retVal = true;
			//Console.WriteLine ("^^^^^^^Checking IsConnected");
			foreach (Cookie cookie in cookies.GetCookies(Uri)) {
				bool isExpired = cookie.Expired;
				//Console.WriteLine (cookie.Name + " " + (isExpired ? "expired" : "valid"));
				if (isExpired)
					retVal = false;
			}
			//return cookies.GetCookies(Uri).Count > 0;
			return retVal;
		}
		//Reads until it finds the start of the response
		protected StreamReader findResponse (HttpWebResponse response)
		{
			StreamReader reader = new StreamReader (response.GetResponseStream (), Encoding.UTF8);
			if (reader == null)
				throw new GalleryException (Catalog.GetString ("Error reading server response"));

			string line;
			string full_response = null;
			while ((line = reader.ReadLine ()) != null) {
				full_response += line;
				if (line.IndexOf ("#__GR2PROTO__", 0) > -1)
					break;
			}

			if (line == null) {
				// failed to find the response
				throw new GalleryException (Catalog.GetString ("Server returned response without Gallery content"), full_response);
			}

			return reader;

		}

		protected string [] GetNextLine (StreamReader reader)
		{
			char [] value_split = new char [1] {'='};
			bool haveLine = false;
			string[] array = null;
			while(!haveLine) {
				string line = reader.ReadLine ();
				//Console.WriteLine ("READING: " + line);
				if (line != null) {
					array = line.Split (value_split, 2);
					haveLine = !LineIgnored (array);
				}
				else {
					//end of input
					return null;
				}
			}

			return array;
		}

		private bool LineIgnored (string[] line)
		{
			if (line[0].StartsWith ("debug")) {
				return true;
			} else if (line[0].StartsWith ("can_create_root")) {
				return true;
			} else {
				return false;
			}
		}

		protected bool ParseLogin (HttpWebResponse response)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {

				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("server_version")) {
						//FIXME we should use the to determine what capabilities the server has
					} else if (data[0].StartsWith ("auth_token")) {
						AuthToken = data[1];
					} else {
						Console.WriteLine ("Unparsed Line in ParseLogin(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				return true;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public ArrayList ParseFetchAlbums (HttpWebResponse response)
		{
			//Console.WriteLine ("in ParseFetchAlbums()");
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			albums = new ArrayList ();
			try {

				Album current_album = null;
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					//Console.WriteLine ("Parsing Line: {0}={1}", data[0], data[1]);
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("album.name")) {
						//this is the URL name
						int ref_num = -1;
						if (this.Version == GalleryVersion.Version1) {
							string [] segments = data[0].Split (new char[1]{'.'});
							ref_num = int.Parse (segments[segments.Length -1]);
						} else {
							ref_num = int.Parse (data[1]);
						}
						current_album = new Album (this, data[1], ref_num);
						albums.Add (current_album);
						//Console.WriteLine ("current_album: " + data[1]);
					} else if (data[0].StartsWith ("album.title")) {
						//this is the display name
						current_album.Title = data[1];
					} else if (data[0].StartsWith ("album.summary")) {
						current_album.Summary = data[1];
					} else if (data[0].StartsWith ("album.parent")) {
						//FetchAlbums and G2 FetchAlbumsPrune return ints
						//G1 FetchAlbumsPrune returns album names (and 0 for root albums)
						try {
							current_album.ParentRefNum = int.Parse (data[1]);
						} catch (System.FormatException) {
							current_album.ParentRefNum = LookupAlbum (data[1]).RefNum;
						}
						//Console.WriteLine ("album.parent data[1]: " + data[1]);
					} else if (data[0].StartsWith ("album.resize_size")) {
						current_album.ResizeSize = int.Parse (data[1]);
					} else if (data[0].StartsWith ("album.thumb_size")) {
						current_album.ThumbSize = int.Parse (data[1]);
					} else if (data[0].StartsWith ("album.info.extrafields")) {
						//ignore, this is the album description
					} else if (data[0].StartsWith ("album.perms.add")) {
						if (data[1] == "true")
							current_album.Perms |= AlbumPermission.Add;
					} else if (data[0].StartsWith ("album.perms.write")) {
						if (data[1] == "true")
							current_album.Perms |= AlbumPermission.Write;
					} else if (data[0].StartsWith ("album.perms.del_item")) {
						if (data[1] == "true")
							current_album.Perms |= AlbumPermission.Delete;
					} else if (data[0].StartsWith ("album.perms.del_alb")) {
						if (data[1] == "true")
							current_album.Perms |= AlbumPermission.DeleteAlbum;
					} else if (data[0].StartsWith ("album.perms.create_sub")) {
						if (data[1] == "true")
							current_album.Perms |= AlbumPermission.CreateSubAlbum;
					} else if (data[0].StartsWith ("album_count")) {
						if (Albums.Count != int.Parse (data[1]))
							Console.WriteLine ("Parsed album count does not match album_count.  Something is amiss");
					} else if (data[0].StartsWith ("auth_token")) {
						AuthToken = data [1];
					} else {
						Console.WriteLine ("Unparsed Line in ParseFetchAlbums(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				//Console.WriteLine (After parse albums.Count + " albums parsed");
				return albums;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public int ParseAddItem (HttpWebResponse response)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			int item_id = 0;
			try {

				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("auth_token")) {
						AuthToken = data[1];
					} else if (data[0].StartsWith ("item_name")) {
						item_id = int.Parse (data [1]);
					} else {
						Console.WriteLine ("Unparsed Line in ParseAddItem(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				return item_id;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public bool ParseNewAlbum (HttpWebResponse response)
		{
			return ParseBasic (response);
		}

		public bool ParseMoveAlbum (HttpWebResponse response)
		{
			return ParseBasic (response);
		}
		/*
		public Album ParseAlbumProperties (HttpWebResponse response)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {

				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("auto-resize")) {
						//ignore
					} else {
						Console.WriteLine ("Unparsed Line in ParseBasic(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				return true;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}
		*/

		private bool ParseBasic (HttpWebResponse response)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {

				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("auth_token")) {
						AuthToken = data[1];
					} else {
						Console.WriteLine ("Unparsed Line in ParseBasic(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text + " Status: " + status);
					throw new GalleryCommandException (status_text, status);
				}

				return true;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public Album LookupAlbum (string name)
		{
			Album match = null;

			foreach (Album album in albums) {
				if (album.Name == name) {
					match = album;
					break;
				}
			}
			return match;
		}

		public Album LookupAlbum (int ref_num)
		{
			// FIXME this is really not the best way to do this
			Album match = null;

			foreach (Album album in albums) {
				if (album.RefNum == ref_num) {
					match = album;
					break;
				}
			}
			return match;
		}

		public static string FixUrl(string url, string end)
		{
			string fixedUrl = url;
			if (!url.EndsWith (end)) {
				if (!url.EndsWith ("/"))
					fixedUrl = url + "/";
				fixedUrl = fixedUrl + end;
			}
			return fixedUrl;

		}

		public void PopupException (GalleryCommandException e, Gtk.Dialog d)
		{
			System.Console.WriteLine(String.Format ("{0} : {1} ({2})", e.Message, e.ResponseText, e.Status));
			HigMessageDialog md =
				new HigMessageDialog (d,
						      Gtk.DialogFlags.Modal |
						      Gtk.DialogFlags.DestroyWithParent,
						      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
						      Catalog.GetString ("Error while creating new album"),
						      String.Format (Catalog.GetString ("The following error was encountered while attempting to perform the requested operation:\n{0} ({1})"), e.Message, e.Status));
			md.Run ();
			md.Destroy ();
		}

	}

	public class Gallery1 : Gallery
	{
		public const string script_name = "gallery_remote2.php";
		public Gallery1 (string url) : this (url, url) {}
		public Gallery1 (string name, string url) : base (name)
		{
			this.uri = new Uri (FixUrl (url, script_name));
			version = GalleryVersion.Version1;
		}

		public override void Login (string username, string passwd)
		{
			//Console.WriteLine ("Gallery1: Attempting to login");
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "login");
			client.Add ("protocol_version", "2.3");
			client.Add ("uname", username);
			client.Add ("password", passwd);

			ParseLogin (client.Submit (uri));
		}

		public override ArrayList FetchAlbums ()
		{
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "fetch-albums");
			client.Add ("protocol_version", "2.3");

			return ParseFetchAlbums (client.Submit (uri));
		}


		public override bool MoveAlbum (Album album, string end_name)
		{
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "move-album");
			client.Add ("protocol_version", "2.7");
			client.Add ("set_albumName", album.Name);
			client.Add ("set_destalbumName", end_name);

			return ParseMoveAlbum (client.Submit (uri));
		}

		public override int AddItem (Album album,
				     string path,
				     string filename,
				     string caption,
				     string description,
				     bool autorotate)
		{
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "add-item");
			client.Add ("protocol_version", "2.9");
			client.Add ("set_albumName", album.Name);
			client.Add ("caption", caption);
			client.Add ("userfile_name", filename);
			client.Add ("force_filename", filename);
			client.Add ("auto_rotate", autorotate ? "yes" : "no");
			client.Add ("userfile", new FileInfo (path));
			client.Add ("extrafield.Description", description);
			client.expect_continue = expect_continue;

			return ParseAddItem (client.Submit (uri, Progress));
		}

		/*
		public override Album AlbumProperties (string album)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "album-properties");
			client.Add ("protocol_version", "2.3");
			client.Add ("set_albumName", album);

			return ParseAlbumProperties (client.Submit (uri));
		}
		*/

		public override bool NewAlbum (string parent_name,
				      string name,
				      string title,
				      string description)
		{
			FormClient client = new FormClient (cookies);
			client.Multipart = true;
			client.Add ("cmd", "new-album");
			client.Add ("protocol_version", "2.8");
			client.Add ("set_albumName", parent_name);
			client.Add ("newAlbumName", name);
			client.Add ("newAlbumTitle", title);
			client.Add ("newAlbumDesc", description);

			return ParseNewAlbum (client.Submit (uri));
		}

		public override ArrayList FetchAlbumImages (Album album, bool include_ablums)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-album-images");
			client.Add ("protocol_version","2.3");
			client.Add ("set_albumName", album.Name);
			client.Add ("albums_too", include_ablums ? "yes" : "no");

			album.Images = ParseFetchAlbumImages (client.Submit (uri), album);
			return album.Images;
		}

		public override ArrayList FetchAlbumsPrune ()
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-albums-prune");
			client.Add ("protocol_version", "2.3");
			client.Add ("check_writable", "no");
			ArrayList a = ParseFetchAlbums (client.Submit (uri));
			a.Sort();
			return a;
		}

		public ArrayList ParseFetchAlbumImages (HttpWebResponse response, Album album)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {
				Image current_image = null;
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("image.name")) {
						current_image = new Image (album, data[1]);
						album.Images.Add (current_image);
					} else if (data[0].StartsWith ("image.raw_width")) {
						current_image.RawWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
						current_image.RawHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
						current_image.RawHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_filesize")) {
					} else if (data[0].StartsWith ("image.capturedate.year")) {
					} else if (data[0].StartsWith ("image.capturedate.mon")) {
					} else if (data[0].StartsWith ("image.capturedate.mday")) {
					} else if (data[0].StartsWith ("image.capturedate.hours")) {
					} else if (data[0].StartsWith ("image.capturedate.minutes")) {
					} else if (data[0].StartsWith ("image.capturedate.seconds")) {
					} else if (data[0].StartsWith ("image.hidden")) {
					} else if (data[0].StartsWith ("image.resizedName")) {
						current_image.ResizedName = data[1];
					} else if (data[0].StartsWith ("image.resized_width")) {
						current_image.ResizedWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.resized_height")) {
						current_image.ResizedHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.thumbName")) {
						current_image.ThumbName = data[1];
					} else if (data[0].StartsWith ("image.thumb_width")) {
						current_image.ThumbWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.thumb_height")) {
						current_image.ThumbHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.caption")) {
						current_image.Caption = data[1];
					} else if (data[0].StartsWith ("image.extrafield.Description")) {
						current_image.Description = data[1];
					} else if (data[0].StartsWith ("image.clicks")) {
						try {
							current_image.Clicks = int.Parse (data[1]);
						} catch (System.FormatException) {
							current_image.Clicks = 0;
						}
					} else if (data[0].StartsWith ("baseurl")) {
						album.BaseURL = data[1];
					} else if (data[0].StartsWith ("image_count")) {
						if (album.Images.Count != int.Parse (data[1]))
							Console.WriteLine ("Parsed image count for " + album.Name + "(" + album.Images.Count + ") does not match image_count (" + data[1] + ").  Something is amiss");
					} else {
						Console.WriteLine ("Unparsed Line in ParseFetchAlbumImages(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}


				//Set the Urls for downloading the images.
				string baseUrl = album.BaseURL + "/";
				foreach (Image image in album.Images) {
					image.Url = baseUrl + image.Name;
				}

				return album.Images;
			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public override string GetAlbumUrl (Album album)
		{
			string url = Uri.ToString();
			url = url.Remove (url.Length - script_name.Length, script_name.Length);

			string path = album.Name;

			url = url + path;
			url = url.Replace (" ", "+");
			return url;
		}


	}
	public class Gallery2 : Gallery
	{
		public const string script_name = "main.php";

		public Gallery2 (string url) : this (url, url) {}
		public Gallery2 (string name, string url) : base (name)
		{
			this.uri = new Uri (FixUrl (url, script_name));
			version = GalleryVersion.Version2;
		}

		public override void Login (string username, string passwd)
		{
			Console.WriteLine ("Gallery2: Attempting to login");
			FormClient client = new FormClient (cookies);

			client.Add ("g2_form[cmd]", "login");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[uname]", username);
			client.Add ("g2_form[password]", passwd);
			AddG2Specific (client);

			ParseLogin (client.Submit (uri));
		}

		public override ArrayList FetchAlbums ()
		{
			//FetchAlbums doesn't exist for G2, we have to use FetchAlbumsPrune()
			return FetchAlbumsPrune ();
		}


		public override bool MoveAlbum (Album album, string end_name)
		{
			FormClient client = new FormClient (cookies);

			client.Add ("g2_form[cmd]", "move-album");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[set_albumName]", album.Name);
			client.Add ("g2_form[set_destalbumName]", end_name);
			AddG2Specific (client);

			return ParseMoveAlbum (client.Submit (uri));
		}

		public override int AddItem (Album album,
				     string path,
				     string filename,
				     string caption,
				     string description,
				     bool autorotate)
		{
			FormClient client = new FormClient (cookies);

			client.Add ("g2_form[cmd]", "add-item");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[set_albumName]", album.Name);
			client.Add ("g2_form[caption]", caption);
			client.Add ("g2_form[userfile_name]", filename);
			client.Add ("g2_form[force_filename]", filename);
			client.Add ("g2_form[auto_rotate]", autorotate ? "yes" : "no");
			client.Add ("g2_form[extrafield.Description]", description);
			client.Add ("g2_userfile", new FileInfo (path));
			client.expect_continue = expect_continue;
			AddG2Specific (client);

			return ParseAddItem (client.Submit (uri, Progress));
		}

		/*
		public override Album AlbumProperties (string album)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "album-properties");
			client.Add ("protocol_version", "2.3");
			client.Add ("set_albumName", album);

			return ParseAlbumProperties (client.Submit (uri));
		}
		*/

		public override bool NewAlbum (string parent_name,
				      string name,
				      string title,
				      string description)
		{
			FormClient client = new FormClient (cookies);
			client.Multipart = true;
			client.Add ("g2_form[cmd]", "new-album");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[set_albumName]", parent_name);
			client.Add ("g2_form[newAlbumName]", name);
			client.Add ("g2_form[newAlbumTitle]", title);
			client.Add ("g2_form[newAlbumDesc]", description);
			AddG2Specific (client);

			return ParseNewAlbum (client.Submit (uri));
		}

		public override ArrayList FetchAlbumImages (Album album, bool include_ablums)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("g2_form[cmd]", "fetch-album-images");
			client.Add ("g2_form[protocol_version]","2.10");
			client.Add ("g2_form[set_albumName]", album.Name);
			client.Add ("g2_form[albums_too]", include_ablums ? "yes" : "no");
			AddG2Specific (client);

			album.Images = ParseFetchAlbumImages (client.Submit (uri), album);
			return album.Images;
		}

		public override ArrayList FetchAlbumsPrune ()
		{
			FormClient client = new FormClient (cookies);
			client.Add ("g2_form[cmd]", "fetch-albums-prune");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[check_writable]", "no");
			AddG2Specific (client);

			ArrayList a = ParseFetchAlbums (client.Submit (uri));
			a.Sort();
			return a;
		}

		private void AddG2Specific (FormClient client)
		{
			if (AuthToken != null && AuthToken != String.Empty)
				client.Add("g2_authToken", AuthToken);
			client.Add("g2_controller", "remote.GalleryRemote");
		}

		public ArrayList ParseFetchAlbumImages (HttpWebResponse response, Album album)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {
				Image current_image = null;
				string baseUrl = Uri.ToString() + "?g2_view=core.DownloadItem&g2_itemId=";
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("image.name")) {
						//for G2 this is the number used to download the image.
						current_image = new Image (album, "awaiting 'title'");
						album.Images.Add (current_image);
						current_image.Url = baseUrl + data[1];
					} else if (data[0].StartsWith ("image.title")) {
						//for G2 the "title" is the name"
						current_image.Name = data[1];
					} else if (data[0].StartsWith ("image.raw_width")) {
						current_image.RawWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
						current_image.RawHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
						current_image.RawHeight = int.Parse (data[1]);
					//ignore these for now
					} else if (data[0].StartsWith ("image.raw_filesize")) {
					} else if (data[0].StartsWith ("image.forceExtension")) {
					} else if (data[0].StartsWith ("image.capturedate.year")) {
					} else if (data[0].StartsWith ("image.capturedate.mon")) {
					} else if (data[0].StartsWith ("image.capturedate.mday")) {
					} else if (data[0].StartsWith ("image.capturedate.hours")) {
					} else if (data[0].StartsWith ("image.capturedate.minutes")) {
					} else if (data[0].StartsWith ("image.capturedate.seconds")) {
					} else if (data[0].StartsWith ("image.hidden")) {
					} else if (data[0].StartsWith ("image.resizedName")) {
						current_image.ResizedName = data[1];
					} else if (data[0].StartsWith ("image.resized_width")) {
						current_image.ResizedWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.resized_height")) {
						current_image.ResizedHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.thumbName")) {
						current_image.ThumbName = data[1];
					} else if (data[0].StartsWith ("image.thumb_width")) {
						current_image.ThumbWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.thumb_height")) {
						current_image.ThumbHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.caption")) {
						current_image.Caption = data[1];
					} else if (data[0].StartsWith ("image.extrafield.Description")) {
						current_image.Description = data[1];
					} else if (data[0].StartsWith ("image.clicks")) {
						try {
							current_image.Clicks = int.Parse (data[1]);
						} catch (System.FormatException) {
							current_image.Clicks = 0;
						}
					} else if (data[0].StartsWith ("baseurl")) {
						album.BaseURL = data[1];
					} else if (data[0].StartsWith ("image_count")) {
						if (album.Images.Count != int.Parse (data[1]))
							Console.WriteLine ("Parsed image count for " + album.Name + "(" + album.Images.Count + ") does not match image_count (" + data[1] + ").  Something is amiss");
					} else {
						Console.WriteLine ("Unparsed Line in ParseFetchAlbumImages(): {0}={1}", data[0], data[1]);
					}
				}
				Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Console.WriteLine (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				return album.Images;

			} finally {
				if (reader != null)
					reader.Close ();

				response.Close ();
			}
		}

		public override string GetAlbumUrl (Album album)
		{
			return Uri.ToString() + "?g2_view=core.ShowItem&g2_itemId=" + album.Name;
		}

	}

	public enum GalleryVersion : byte
	{
		VersionUnknown = 0,
		Version1 = 1,
		Version2 = 2
	}
}
