// Gallery2.cs
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
	public class Gallery2 : Gallery
	{
		public const string script_name = "main.php";

		public Gallery2 (string url) : this (url, url)
		{
		}

		public Gallery2 (string name, string url) : base (name)
		{
			Uri = new Uri (FixUrl (url, script_name));
			Version = GalleryVersion.Version2;
		}

		public override void Login (string username, string passwd)
		{
			Log.Debug ("Gallery2: Attempting to login");
			FormClient client = new FormClient (cookies);

			client.Add ("g2_form[cmd]", "login");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[uname]", username);
			client.Add ("g2_form[password]", passwd);
			AddG2Specific (client);

			ParseLogin (client.Submit (Uri));
		}

		public override List<Album> FetchAlbums ()
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
			client.Add ("g2_form[cmd]", "new-album");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[set_albumName]", parent_name);
			client.Add ("g2_form[newAlbumName]", name);
			client.Add ("g2_form[newAlbumTitle]", title);
			client.Add ("g2_form[newAlbumDesc]", description);
			AddG2Specific (client);

			return ParseNewAlbum (client.Submit (Uri));
		}

		public override List<Image> FetchAlbumImages (Album album, bool include_ablums)
		{
			FormClient client = new FormClient (cookies);
			client.Add ("g2_form[cmd]", "fetch-album-images");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[set_albumName]", album.Name);
			client.Add ("g2_form[albums_too]", include_ablums ? "yes" : "no");
			AddG2Specific (client);

			album.Images = ParseFetchAlbumImages (client.Submit (Uri), album);
			return album.Images;
		}

		public override List<Album> FetchAlbumsPrune ()
		{
			FormClient client = new FormClient (cookies);
			client.Add ("g2_form[cmd]", "fetch-albums-prune");
			client.Add ("g2_form[protocol_version]", "2.10");
			client.Add ("g2_form[check_writable]", "no");
			AddG2Specific (client);

			List<Album> a = ParseFetchAlbums (client.Submit (Uri));
			a.Sort ();
			return a;
		}

		private void AddG2Specific (FormClient client)
		{
			if (AuthToken != null && AuthToken != string.Empty)
				client.Add ("g2_authToken", AuthToken);
			client.Add ("g2_controller", "remote.GalleryRemote");
		}

		public List<Image> ParseFetchAlbumImages (HttpWebResponse response, Album album)
		{
			string [] data;
			StreamReader reader = null;
			ResultCode status = ResultCode.UnknownResponse;
			string status_text = "Error: Unable to parse server response";
			try {
				Image current_image = null;
				string baseUrl = Uri.ToString () + "?g2_view=core.DownloadItem&g2_itemId=";
				reader = findResponse (response);
				while ((data = GetNextLine (reader)) != null) {
					if (data [0] == "status")
						status = (ResultCode)int.Parse (data [1]);
					else if (data [0].StartsWith ("status_text")) {
						status_text = data [1];
						Log.Debug ($"StatusText : {data[1]}");
					} else if (data [0].StartsWith ("image.name")) {
						//for G2 this is the number used to download the image.
						current_image = new Image (album, "awaiting 'title'");
						album.Images.Add (current_image);
						current_image.Url = baseUrl + data [1];
					} else if (data [0].StartsWith ("image.title"))
						//for G2 the "title" is the name"
						current_image.Name = data [1];
					else if (data [0].StartsWith ("image.raw_width"))
						current_image.RawWidth = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.raw_height"))
						current_image.RawHeight = int.Parse (data [1]);
					else if (data [0].StartsWith ("image.raw_height"))
						current_image.RawHeight = int.Parse (data [1]);
					//ignore these for now
					else if (data [0].StartsWith ("image.raw_filesize")) {
					} else if (data [0].StartsWith ("image.forceExtension")) {
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
						Log.Debug ($"Unparsed Line in ParseFetchAlbumImages(): {data[0]}={data[1]}");
				}
				Log.Debug ($"Found: {response.Cookies.Count} cookies");
				if (status != ResultCode.Success) {
					Log.Debug (status_text);
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
			return Uri.ToString () + "?g2_view=core.ShowItem&g2_itemId=" + album.Name;
		}
	}
}
