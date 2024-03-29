//
// FlickrRemote.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008-2009 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/*
 * Simple upload based on the api at
 * http://www.flickr.com/services/api/upload.api.html
 *
 * Modified by acs in order to use Flickr.Net
 *
 * Modified in order to use the new Auth API
 *
 * We use now the search API also
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

using FlickrNet;

using FSpot.Core;
using FSpot.Filters;
using FSpot.Settings;


namespace FSpot.Exporters.Flickr
{
	public class FlickrRemote
	{
		public static LicenseCollection licenses;
		FlickrNet.Flickr flickr;

		OAuthRequestToken requestToken;
		OAuthAccessToken accessToken;

		public bool ExportTags;
		public bool ExportTagHierarchy;
		public bool ExportIgnoreTopLevel;
		public ProgressItem Progress;

		public const string TOKEN_FLICKR = Preferences.ExportTokens + "flickr";
		public const string TOKEN_23HQ = Preferences.ExportTokens + "23hq";
		public const string TOKEN_ZOOOMR = Preferences.ExportTokens + "zooomr";

		public FlickrRemote (OAuthAccessToken token, Service service)
		{
			if (token == null) {
				flickr = new FlickrNet.Flickr (service.ApiKey, service.Secret);
				accessToken = null;
			} else {
				flickr = new FlickrNet.Flickr (service.ApiKey, service.Secret, token.Token);
				accessToken = token;
				flickr.OAuthAccessToken = token.Token;
				flickr.OAuthAccessTokenSecret = token.TokenSecret;
			}

			flickr.CurrentService = service.Id;
		}

		public OAuthAccessToken Token {
			get { return accessToken; }
			set {
				accessToken = value;
				flickr.OAuthAccessToken = value.Token;
				flickr.OAuthAccessTokenSecret = value.TokenSecret;
			}
		}

		public FlickrNet.Flickr Connection {
			get { return flickr; }
		}

		public LicenseCollection GetLicenses ()
		{
			// Licenses won't change normally in a user session
			if (licenses == null) {
				try {
					licenses = flickr.PhotosLicensesGetInfo ();
				} catch (FlickrNet.FlickrApiException e) {
					Logger.Log.Error (e.Code + ": " + e.OriginalMessage);
					return null;
				}
			}
			return licenses;
		}

		public List<string> Search (string[] tags, int licenseId)
		{
			var photos_url = new List<string> ();
			// Photos photos = flickr.PhotosSearchText (tags, licenseId);
			var options = new PhotoSearchOptions ();
			options.Tags = string.Join (",", tags);
			PhotoCollection photos = flickr.PhotosSearch (options);

			if (photos != null) {
				foreach (FlickrNet.Photo photo in photos) {
					photos_url.Add (photo.ThumbnailUrl);
				}
			}

			return photos_url;
		}

		public List<string> Search (string tags, int licenseId)
		{
			var photos_url = new List<string> ();
			var options = new PhotoSearchOptions ();
			options.Tags = tags;
			PhotoCollection photos = flickr.PhotosSearch (options);

			if (photos != null) {
				foreach (FlickrNet.Photo photo in photos) {
					photos_url.Add (photo.ThumbnailUrl);
				}
			}
			return photos_url;
		}

		public OAuthAccessToken CheckLogin (string oauth_verification_code)
		{
			// CF: https://www.flickr.com/services/api/auth.oauth.html
			// OAuth flow step 1: Get a new request token if we don't already have one
			try {
				if (flickr.AuthOAuthCheckToken () == null) {
					requestToken = flickr.OAuthGetRequestToken ("oob");
					if (requestToken == null) {
						Logger.Log.Error ("ERROR: Unable to Obtain OAuth Request token");
						return null;
					} else {
						Logger.Log.Debug ("Victory! got a Requet Token: " + requestToken.Token);
					}
				}
			} catch (Exception e) {
				Logger.Log.Error ("Error logging in: " + e.Message);
				return null;
			}

			// OAuth flow step 2: Ask for permissions

			// OAuth flow step 3: Exchange Request token for Access token

			if (accessToken == null) {
				try {
					Logger.Log.Debug ("No token available, trying to obtain access token with requestToken={" + requestToken.Token + "},verificationCode={" + oauth_verification_code + "}");
					accessToken = flickr.OAuthGetAccessToken (requestToken, oauth_verification_code);
					Logger.Log.Debug ("Obtained access token:" + accessToken.Token);
					flickr.OAuthAccessToken = accessToken.Token;
					flickr.OAuthAccessTokenSecret = accessToken.TokenSecret;
				} catch (FlickrApiException ex) {
					Logger.Log.Error ("Problems logging in to Flickr - " + ex.OriginalMessage);
					return null;
				}
			}

			return accessToken;
		}

		public string Upload (IPhoto photo, IFilter filter, bool is_public, bool is_family, bool is_friend)
		{
			if (accessToken == null) {
				throw new Exception ("Must Login First");
			}
			// FIXME flickr needs rotation
			string error_verbose;

			using (var request = new FilterRequest (photo.DefaultVersion.Uri)) {

				try {
					string tags = null;

					filter.Convert (request);
					string path = request.Current.LocalPath;

					if (ExportTags && photo.Tags != null) {
						var taglist = new StringBuilder ();
						Core.Tag[] t = photo.Tags;
						Core.Tag tag_iter = null;

						for (int i = 0; i < t.Length; i++) {
							if (i > 0)
								taglist.Append (',');

							taglist.Append (string.Format ("\"{0}\"", t[i].Name));

							// Go through the tag parents
							if (ExportTagHierarchy) {
								tag_iter = t[i].Category;
								while (tag_iter != App.Instance.Database.Tags.RootCategory && tag_iter != null) {
									// Skip top level tags because they have no meaning in a linear tag database
									if (ExportIgnoreTopLevel && tag_iter.Category == App.Instance.Database.Tags.RootCategory) {
										break;
									}

									// FIXME Look if the tag is already there!
									taglist.Append (',');
									taglist.Append (string.Format ("\"{0}\"", tag_iter.Name));
									tag_iter = tag_iter.Category;
								}
							}

						}

						tags = taglist.ToString ();
					}
					try {
						string photoid =
							flickr.UploadPicture (path, photo.Name, photo.Description, tags, is_public, is_family, is_friend);
						return photoid;
					} catch (FlickrException ex) {
						Logger.Log.Error ("Problems uploading picture: " + ex.Message);
						error_verbose = ex.ToString ();
					}
				} catch (Exception e) {
					// FIXME we need to distinguish between file IO errors and xml errors here
					throw new Exception ("Error while uploading", e);
				}
			}

			throw new Exception (error_verbose);
		}

		public void TryWebLogin ()
		{
			// First we get the OAuth Token
			requestToken = flickr.OAuthGetRequestToken ("oob");
			// Then we calculate the OAuth Authorization URL
			string login_url = flickr.OAuthCalculateAuthorizationUrl (requestToken.Token, AuthLevel.Write);
			Logger.Log.Debug ("FlickrRemote: Opening browser to ask user permissions at {0}", login_url);
			// Redirection time. Now we can ask permission.
			GtkBeans.Global.ShowUri (null, login_url);
		}

		public class Service
		{
			public string ApiKey;
			public string Secret;
			public SupportedService Id;
			public string Name;
			public string PreferencePath;

			public static Service[] Supported = {
			new Service (SupportedService.Flickr, "Flickr.com", "c6b39ee183385d9ce4ea188f85945016", "0a951ac44a423a04", TOKEN_FLICKR),
			new Service (SupportedService.TwentyThreeHQ, "23hq.com", "c6b39ee183385d9ce4ea188f85945016", "0a951ac44a423a04", TOKEN_23HQ),
			new Service (SupportedService.Zooomr, "Zooomr.com", "a2075d8ff1b7b059df761649835562e4", "6c66738681", TOKEN_ZOOOMR)
		};

			public Service (SupportedService id, string name, string api_key, string secret, string pref)
			{
				Id = id;
				ApiKey = api_key;
				Secret = secret;
				Name = name;
				PreferencePath = pref;
			}

			public static Service FromSupported (SupportedService id)
			{
				foreach (Service s in Supported) {
					if (s.Id == id)
						return s;
				}

				throw new ArgumentException ("Unknown service type");
			}
		}
	}
}
