using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;

/* These classes are based off the documentation at 
 *
 * http://gallery.menalto.com/modules.php?op=modload&name=GalleryDocs&file=index&page=gallery-remote.protocol.php
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
	
	public class Album {
		public int RefNum;
		public string Name = null;
		public string Title = null;
		public string Summary = null;
		public int ParentRefNum;
		public int ResizeSize;
		public int ThumbSize;
		
		Gallery gallery;
		
		public AlbumPermission Perms = AlbumPermission.None;
		Hashtable extras = null;
		
		public Album (Gallery gallery, string name, int ref_num) 
		{
			Name = name;
			this.gallery = gallery;
			this.RefNum = ref_num;
		}
		
		public Album Parent () 
		{
			if (ParentRefNum != 0)
				return gallery.LookupAlbum (ParentRefNum);
			else
				return null;
		}
		
		public void Rename (string name) 
		{
			gallery.MoveAlbum (this, name);
		}
		
		public void Add (Photo photo) 
		{
			if (photo == null)
				Console.WriteLine ("NO PHOTO");
			
			gallery.AddItem (this, 
					 photo.DefaultVersionPath, 
					 Path.GetFileName (photo.DefaultVersionPath), 
					 photo.Description, 
					 false);
		}
		
		public string GetUrl ()
		{
			// FIXME this is a hack
			string url = gallery.Uri.ToString ();
			string end = "gallery_remote2.php";

			if (url.EndsWith (end))
				url = url.Remove (url.Length - end.Length, end.Length); 

			Album album = this;
			string path = album.Name;
			while (album.Parent () != null) {
				album = album.Parent ();
				path = album.Name + "/" + path;
			}
			url = url + path;
			return url;
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
		public int Clicks;
		
		private Album Owner;
		
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
		LoginMisssing = 202,
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

	public class GalleryException : Exception {
		public GalleryException (string text) : base (text) {
		}
	}
	
	public class GalleryCommandException : GalleryException {
		ResultCode status;

		public GalleryCommandException (string status_text, ResultCode result) : base (status_text) {
			status = result;
		}

		ResultCode Status {
			get {
				return status;
			}
		}
	}

	public class Gallery {
		public ArrayList Albums = null;
		public FSpot.ProgressItem Progress;
		Uri uri;
		public Uri Uri{
			get {
				return uri;
			}
		}
		
		string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		HttpWebRequest request = null;
		CookieContainer cookies = null;
		
		private Album CurrentAlbum;
		private Image CurrentImage;
		
		public Gallery (string url) : this (url, url) {}

		public Gallery (string name, string url)
		{
			this.name = name;
			this.uri = new Uri (url);
			request = (HttpWebRequest)WebRequest.Create(uri);
			cookies = new CookieContainer ();	       
			Albums = new ArrayList ();
		}
		
		private void StreamWrite (Stream stream, string str)
		{
			Byte [] data = Encoding.ASCII.GetBytes (str);
			stream.Write (data, 0, data.Length);
		}
		
		private void AltParseResponse (HttpWebResponse response, FSpot.ProgressItem progress)
		{
			try {
				Stream response_stream = response.GetResponseStream ();
				long length = response.ContentLength;
				
				System.Console.WriteLine ("ContentLength = {0}", length);
				
				int i = 0;
				while (response_stream.ReadByte () >= 0) {
					if (length != -1) 
						progress.Value = i++ / length;
					else {
						if (progress.Value < .9)
							progress.Value += .1;
						else 
							progress.Value = 0;
					}
				}
			} finally {
				response.Close ();
			}
		}

		private void ParseResponse (HttpWebResponse response)
		{
			StreamReader reader = null;
			try {
				Stream response_stream = response.GetResponseStream ();

				reader = new StreamReader (response_stream, Encoding.UTF8);
				
				ParseResult (reader);
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
			} finally {
				if (reader != null)
					reader.Close ();
				
				response.Close ();
			}
		}
		
		private void ParseResult (StreamReader reader)
		{		
			Albums.Clear ();
			
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			
			Album current_album = null;
			Image current_image = null;
			
			string line;
			char [] value_split = new char [1] {'='};
			bool inresult = false;
			
			while ((line = reader.ReadLine ()) != null) {
				System.Console.WriteLine ("read line");
				if (line == "#__GR2PROTO__") {
					inresult = true;
				} else if (inresult) {
					string [] data = line.Split (value_split, 2);
					
				if (data[0] == "status") {
						status = (ResultCode) int.Parse (data [1]);
					} else if (data[0].StartsWith ("status_text")) {
						status_text = data[1];
						Console.WriteLine ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("album.name")) {
						string [] segments = data[0].Split (new char[1]{'.'});
						int ref_num = int.Parse (segments[segments.Length -1]);
						current_album = new Album (this, data[1], ref_num);
						Albums.Add (current_album);
					} else if (data[0].StartsWith ("album.title")) {
						current_album.Title = data[1];
					} else if (data[0].StartsWith ("album.summary")) {
						current_album.Summary = data[1];
					} else if (data[0].StartsWith ("album.parent")) {
						current_album.ParentRefNum = int.Parse (data[1]);
					} else if (data[0].StartsWith ("album.resize_size")) {
						current_album.ResizeSize = int.Parse (data[1]);
					} else if (data[0].StartsWith ("album.thumb_size")) {
						current_album.ResizeSize = int.Parse (data[1]);
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
					} else if (data[0].StartsWith ("image.name")) {
						current_image = new Image (CurrentAlbum, data[1]);
					} else if (data[0].StartsWith ("image.raw_width")) {
						current_image.RawWidth = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
				       current_image.RawHeight = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image.raw_height")) {
						current_image.RawHeight = int.Parse (data[1]);
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
					} else if (data[0].StartsWith ("image.clicks")) {
						current_image.Clicks = int.Parse (data[1]);
					} else if (data[0].StartsWith ("image_count")) {
						
					} else {
						Console.WriteLine ("Unparsed result: name=\"{0}\", value=\"{1}\"", data[0], data[1]);
					}
				} else {
					// FIXME we should really do
					// something more intelligent
					// with bogus response data.
				        Console.WriteLine ("GARBAGE" + line);
				}
			}
			
			if (current_album != null)
				Console.WriteLine ("found {0} albums", Albums.Count);
			 
			if (status != ResultCode.Success) {
				Console.WriteLine (status_text);
				throw new GalleryCommandException (status_text, status);
			}
		}
		
		public void Login (string username, string passwd)
		{
			FormClient client = new FormClient (cookies);
			
			client.Add ("cmd", "login");
			client.Add ("protocol_version", "2.3");
			client.Add ("uname", username);
			client.Add ("password", passwd);
			
			ParseResponse (client.Submit (uri));
		}
		
		public void FetchAlbums ()
		{
			FormClient client = new FormClient (cookies); 
			
			client.Add ("cmd", "fetch-albums");
			client.Add ("protocol_version", "2.3");
			
			ParseResponse (client.Submit (uri));
		}
		
		
		public void MoveAlbum (Album album, string end_name)
		{
			FormClient client = new FormClient (cookies);
			
			client.Add ("cmd", "move-album");
			client.Add ("protocol_version", "2.7");
			client.Add ("set_albumName", album.Name);
			client.Add ("set_destalbumName", end_name);
			
			ParseResponse (client.Submit (uri));
		}
		
		public void AddItem (Album album,
				     string path, 
				     string filename,
				     string caption, 
				     bool autorotate)
		{
			FormClient client = new FormClient (cookies);
			
			client.Add ("cmd", "add-item");
			client.Add ("protocol_version", "2.9");
			client.Add ("set_albumName", album.Name);
			client.Add ("userfile_name", filename);
			client.Add ("auto_rotate", autorotate ? "yes" : "no");
			client.Add ("caption", caption);
			client.Add ("userfile", new FileInfo (path));
			
			ParseResponse (client.Submit (uri, Progress));
		}
		
		public void AlbumProperties (string album)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "album-properties");
			client.Add ("protocol_version", "2.3");
			client.Add ("set_albumName", album);
			
			ParseResponse (client.Submit (uri));
		}
		
		public void NewAlbum (string parent_name, 
				      string name, 
				      string title, 
				      string description)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "new-album");
			client.Add ("protocol_version", "2.8");
			client.Add ("set_AlbumName", parent_name);
			client.Add ("newAlbumName", name);
			client.Add ("newAlbumTitle", title);
			client.Add ("newAlbumDesc", description);
			
			ParseResponse (client.Submit (uri));
		}
		
		public void FetchAlbumImages (string album_name, bool include_ablums)
		{
			FetchAlbumImages (LookupAlbum (album_name), include_ablums);
		}
		
		public void FetchAlbumImages (Album album, bool include_ablums)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-albums-images");
			client.Add ("protocol_version","2.3");
			client.Add ("set_AlbumName", album.Name);
			client.Add ("albums_too", include_ablums ? "yes" : "no");
			
			CurrentAlbum = album;
			
			ParseResponse (client.Submit (uri));
		}
		
		public void FetchAlbumsPrune ()
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-albums-prune");
			client.Add ("protocol_version", "2.3");
			client.Add ("check_writable", "no");
			
			ParseResponse (client.Submit (uri));
		}
		
		public Album LookupAlbum (string name) 
		{
			Album match = null;
			
			foreach (Album album in Albums) {
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
			
			foreach (Album album in Albums) { 
				if (album.RefNum == ref_num) {
					match = album;
					break;
				}
			}
			return match;
		}
	}
}
	
	
	
	
