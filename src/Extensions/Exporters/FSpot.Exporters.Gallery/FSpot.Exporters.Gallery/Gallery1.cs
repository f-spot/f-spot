// Gallery1.cs
//
// Author:
//      Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/* These classes are based off the documentation at
 *
 * http://codex.gallery2.org/index.php/Gallery_Remote:Protocol
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Hyena;

namespace FSpot.Exporters.Gallery
{
	public class Gallery1 : Gallery
	{
		public const string script_name = "gallery_remote2.php";

		public Gallery1 (string url) : this (url, url)
		{
		}

		public Gallery1 (string name, string url) : base (name)
		{
			Uri = new Uri (FixUrl (url, script_name));
			Version = GalleryVersion.Version1;
		}

		public override void Login (string username, string passwd)
		{
			//Console.WriteLine ("Gallery1: Attempting to login");
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "login");
			client.Add ("protocol_version", "2.3");
			client.Add ("uname", username);
			client.Add ("password", passwd);

			ParseLogin (client.Submit (Uri));
		}

		public override List<Album> FetchAlbums ()
		{
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "fetch-albums");
			client.Add ("protocol_version", "2.3");

			return ParseFetchAlbums (client.Submit (Uri));
		}

		public override bool MoveAlbum (Album album, string end_name)
		{
			FormClient client = new FormClient (cookies);

			client.Add ("cmd", "move-album");
			client.Add ("protocol_version", "2.7");
			client.Add ("set_albumName", album.Name);
			client.Add ("set_destalbumName", end_name);

			return ParseMoveAlbum (client.Submit (Uri));
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

			return ParseAddItem (client.Submit (Uri, Progress));
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

			return ParseNewAlbum (client.Submit (Uri));
		}

		public override List<Image> FetchAlbumImages (Album album, bool include_ablums)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-album-images");
			client.Add ("protocol_version", "2.3");
			client.Add ("set_albumName", album.Name);
			client.Add ("albums_too", include_ablums ? "yes" : "no");

			album.Images = ParseFetchAlbumImages (client.Submit (Uri), album);
			return album.Images;
		}

		public override List<Album> FetchAlbumsPrune ()
		{
			FormClient client = new FormClient (cookies);
			client.Add ("cmd", "fetch-albums-prune");
			client.Add ("protocol_version", "2.3");
			client.Add ("check_writable", "no");
			List<Album> a = ParseFetchAlbums (client.Submit (Uri));
			a.Sort ();
			return a;
		}

		public List<Image> ParseFetchAlbumImages (HttpWebResponse response, Album album)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {
				Image current_image = null;
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.DebugFormat ("StatusText : {0}", data [1]);
					} else if (data [0].StartsWith ("image.name")) {
						current_image = new Image (album, data [1]);
						album.Images.Add (current_image);
					} else if (data [0].StartsWith ("image.raw_width"))
						current_image.RawWidth = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.raw_height"))
						current_image.RawHeight = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.raw_height"))
						current_image.RawHeight = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.raw_filesize")) {
					} else if (data [0].StartsWith ("image.capturedate.year")) {
					} else if (data [0].StartsWith ("image.capturedate.mon")) {
					} else if (data [0].StartsWith ("image.capturedate.mday")) {
					} else if (data [0].StartsWith ("image.capturedate.hours")) {
					} else if (data [0].StartsWith ("image.capturedate.minutes")) {
					} else if (data [0].StartsWith ("image.capturedate.seconds")) {
					} else if (data [0].StartsWith ("image.hidden")) {
					} else if (data [0].StartsWith ("image.resizedName"))
						current_image.ResizedName = data [1];
					else if (data [0].StartsWith ("image.resized_width"))
						current_image.ResizedWidth = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.resized_height"))
						current_image.ResizedHeight = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.thumbName"))
						current_image.ThumbName = data [1];
					else if (data [0].StartsWith ("image.thumb_width"))
						current_image.ThumbWidth = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.thumb_height"))
						current_image.ThumbHeight = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.caption"))
						current_image.Caption = data [1];
					else if (data [0].StartsWith ("image.extrafield.Description"))
						current_image.Description = data [1];
					else if (data [0].StartsWith ("image.clicks"))
						try {
							current_image.Clicks = int.Parse (data [1]);
						} catch (System.FormatException) {
							current_image.Clicks = 0;
						}
					else if (data [0].StartsWith ("baseurl"))
						album.BaseURL = data [1];
					else if (data [0].StartsWith ("image_count"))
					if (album.Images.Count != int.Parse (data [1]))
						Log.Warning ("Parsed image count for " + album.Name + "(" + album.Images.Count + ") does not match image_count (" + data [1] + ").  Something is amiss");
					else
						Log.DebugFormat ("Unparsed Line in ParseFetchAlbumImages(): {0}={1}", data [0], data [1]);
				}
				//Console.WriteLine ("Found: {0} cookies", response.Cookies.Count);
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
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
			string url = Uri.ToString ();
			url = url.Remove (url.Length - script_name.Length, script_name.Length);

			string path = album.Name;

			url = url + path;
			url = url.Replace (" ", "+");
			return url;
		}
	}
}
