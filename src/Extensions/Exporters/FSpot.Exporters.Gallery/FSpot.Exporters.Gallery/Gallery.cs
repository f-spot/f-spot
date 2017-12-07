//  Gallery.cs
// 
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
// 
//  Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
// 
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
// 
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  

/* These classes are based off the documentation at
 *
 * http://codex.gallery2.org/index.php/Gallery_Remote:Protocol
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using Mono.Unix;

using Hyena;
using Hyena.Widgets;

namespace FSpot.Exporters.Gallery
{
	public abstract class Gallery
	{
		#region Properties
		public Uri Uri { get; protected set; }
		public string Name { get; set; }
		public string AuthToken { get; set; }
		public GalleryVersion Version { get; protected set; }
		public List<Album> Albums { get; protected set; }
		#endregion

		public bool expect_continue = true;
		protected CookieContainer cookies = null;
		public FSpot.ProgressItem Progress = null;

		public abstract void Login (string username, string passwd);
		public abstract List<Album> FetchAlbums ();
		public abstract List<Album> FetchAlbumsPrune ();
		public abstract bool MoveAlbum (Album album, string end_name);
		public abstract int AddItem (Album album, string path, string filename, string caption, string description, bool autorotate);
		//public abstract Album AlbumProperties (string album);
		public abstract bool NewAlbum (string parent_name, string name, string title, string description);
		public abstract List<Image> FetchAlbumImages (Album album, bool include_ablums);
		public abstract string GetAlbumUrl (Album album);

		public Gallery (string name)
		{
			Name = name;
			cookies = new CookieContainer ();
			Albums = new List<Album> ();
		}

		public static GalleryVersion DetectGalleryVersion (string url)
		{
			//Figure out if the url is for G1 or G2
			Log.Debug ("Detecting Gallery version");

			GalleryVersion version;

			if (url.EndsWith (Gallery1.script_name))
				version = GalleryVersion.Version1;
			else if (url.EndsWith (Gallery2.script_name))
				version = GalleryVersion.Version2;
			else {
				//check what script is available on the server
				FormClient client = new FormClient ();

				try {
					client.Submit (new Uri (Gallery.FixUrl (url, Gallery1.script_name)));
					version = GalleryVersion.Version1;

				} catch (System.Net.WebException) {
					try {
						client.Submit (new Uri (Gallery.FixUrl (url, Gallery2.script_name)));
						version = GalleryVersion.Version2;

					} catch (System.Net.WebException) {
						//Uh oh, neither version detected
						version = GalleryVersion.VersionUnknown;
					}
				}
			}

			Log.Debug ("Detected: " + version.ToString ());
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

		/// <summary>
		/// Reads until it finds the start of the response
		/// </summary>
		/// <returns>
		/// The response
		/// </returns>
		/// <param name='response'>
		/// Response.
		/// </param>
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

			if (line == null)
				// failed to find the response
				throw new GalleryException (Catalog.GetString ("Server returned response without Gallery content"), full_response);
			return reader;
		}

		protected string [] GetNextLine (StreamReader reader)
		{
			char [] value_split = new char[1] {'='};
			bool haveLine = false;
			string[] array = null;
			while (!haveLine) {
				string line = reader.ReadLine ();
				//Console.WriteLine ("READING: " + line);
				if (line != null) {
					array = line.Split (value_split, 2);
					haveLine = !LineIgnored (array);
				} else
					//end of input
					return null;
			}
			return array;
		}

		private bool LineIgnored (string[] line)
		{
			if (line [0].StartsWith ("debug") || line [0].StartsWith ("can_create_root"))
				return true;
			return false;
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
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.DebugFormat ("StatusText : {0}", data [1]);
					} else if (data [0].StartsWith ("server_version")) {
						//FIXME we should use the to determine what capabilities the server has
					} else if (data [0].StartsWith ("auth_token"))
						AuthToken = data [1];
					else
						Log.DebugFormat ("Unparsed Line in ParseLogin(): {0}={1}", data [0], data [1]);
				}

				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
					throw new GalleryCommandException (status_text, status);
				}
				return true;
			} finally {
				if (reader != null)
					reader.Close ();
				response.Close ();
			}
		}

		public List<Album> ParseFetchAlbums (HttpWebResponse response)
		{
			//Console.WriteLine ("in ParseFetchAlbums()");
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			Albums = new List<Album> ();

			try {
				Album current_album = null;
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					//Console.WriteLine ("Parsing Line: {0}={1}", data[0], data[1]);
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.DebugFormat ("StatusText : {0}", data [1]);
					} else if (data [0].StartsWith ("album.name")) {
						//this is the URL name
						int ref_num = -1;
						if (this.Version == GalleryVersion.Version1) {
							string [] segments = data [0].Split (new char[1]{'.'});
							ref_num = int.Parse (segments [segments.Length - 1]);
						} else
							ref_num = int.Parse (data [1]);
						current_album = new Album (this, data [1], ref_num);
						Albums.Add (current_album);
						//Console.WriteLine ("current_album: " + data[1]);
					} else if (data [0].StartsWith ("album.title"))
						//this is the display name
						current_album.Title = data [1];
					else if (data [0].StartsWith ("album.summary"))
						current_album.Summary = data [1];
					else if (data [0].StartsWith ("album.parent"))
						//FetchAlbums and G2 FetchAlbumsPrune return ints
						//G1 FetchAlbumsPrune returns album names (and 0 for root albums)
						try {
							current_album.ParentRefNum = int.Parse (data [1]);
						} catch (System.FormatException) {
							current_album.ParentRefNum = LookupAlbum (data [1]).RefNum;
						}
						//Console.WriteLine ("album.parent data[1]: " + data[1]);
					else if (data [0].StartsWith ("album.resize_size"))
						current_album.ResizeSize = int.Parse (data [1]);
					else if (data [0].StartsWith ("album.thumb_size"))
						current_album.ThumbSize = int.Parse (data [1]);
					else if (data [0].StartsWith ("album.info.extrafields")) {
						//ignore, this is the album description
					} else if (data [0].StartsWith ("album.perms.add") && data [1] == "true")
						current_album.Perms |= AlbumPermission.Add;
					else if (data [0].StartsWith ("album.perms.write") && data [1] == "true")
						current_album.Perms |= AlbumPermission.Write;
					else if (data [0].StartsWith ("album.perms.del_item") && data [1] == "true")
						current_album.Perms |= AlbumPermission.Delete;
					else if (data [0].StartsWith ("album.perms.del_alb") && data [1] == "true")
						current_album.Perms |= AlbumPermission.DeleteAlbum;
					else if (data [0].StartsWith ("album.perms.create_sub") && data [1] == "true")
						current_album.Perms |= AlbumPermission.CreateSubAlbum;
					else if (data [0].StartsWith ("album_count"))
					if (Albums.Count != int.Parse (data [1]))
						Log.Warning ("Parsed album count does not match album_count.  Something is amiss");
					else if (data [0].StartsWith ("auth_token"))
						AuthToken = data [1];
					else
						Log.DebugFormat ("Unparsed Line in ParseFetchAlbums(): {0}={1}", data [0], data [1]);

				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
					throw new GalleryCommandException (status_text, status);
				}

				//Console.WriteLine (After parse albums.Count + " albums parsed");
				return Albums;
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
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.DebugFormat ("StatusText : {0}", data [1]);
					} else if (data [0].StartsWith ("auth_token"))
						AuthToken = data [1];
					else if (data [0].StartsWith ("item_name"))
						item_id = int.Parse (data [1]);
					else
						Log.DebugFormat ("Unparsed Line in ParseAddItem(): {0}={1}", data [0], data [1]);
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
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
						Log.Debug ("StatusText : {0}", data[1]);
					} else if (data[0].StartsWith ("auto-resize")) {
						//ignore
					} else {
						Log.Debug ("Unparsed Line in ParseBasic(): {0}={1}", data[0], data[1]);
					}
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
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
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.DebugFormat ("StatusText : {0}", data [1]);
					} else if (data [0].StartsWith ("auth_token"))
						AuthToken = data [1];
					else
						Log.DebugFormat ("Unparsed Line in ParseBasic(): {0}={1}", data [0], data [1]);
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text + " Status: " + status);
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
			// FIXME: this is really not the best way to do this
			Album match = null;

			foreach (Album album in Albums) {
				if (album.RefNum == ref_num) {
					match = album;
					break;
				}
			}
			return match;
		}

		public static string FixUrl (string url, string end)
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
			Log.DebugFormat ("{0} : {1} ({2})", e.Message, e.ResponseText, e.Status);
			HigMessageDialog md =
				new HigMessageDialog (d,
						      Gtk.DialogFlags.Modal |
					Gtk.DialogFlags.DestroyWithParent,
						      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
						      Catalog.GetString ("Error while creating new album"),
						      string.Format (Catalog.GetString ("The following error was encountered while attempting to perform the requested operation:\n{0} ({1})"), e.Message, e.Status));
			md.Run ();
			md.Destroy ();
		}
	}


}
