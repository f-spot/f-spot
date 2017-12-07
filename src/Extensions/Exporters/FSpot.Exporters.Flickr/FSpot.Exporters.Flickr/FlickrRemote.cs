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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
using System.Text;
using System.Collections.Generic;

using FlickrNet;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Settings;

using Hyena;

namespace FSpot.Exporters.Flickr
{
    public class FlickrRemote
    {
        public static LicenseCollection licenses;
        string frob;
        string token;
        Auth auth;
        FlickrNet.Flickr flickr;

        public bool ExportTags;
        public bool ExportTagHierarchy;
        public bool ExportIgnoreTopLevel;
        public ProgressItem Progress;

        public const string TOKEN_FLICKR = Preferences.APP_FSPOT_EXPORT_TOKENS + "flickr";
        public const string TOKEN_23HQ = Preferences.APP_FSPOT_EXPORT_TOKENS + "23hq";
        public const string TOKEN_ZOOOMR = Preferences.APP_FSPOT_EXPORT_TOKENS + "zooomr";

        public FlickrRemote (string token, Service service)
        {
            if (string.IsNullOrEmpty (token)) {
                flickr = new FlickrNet.Flickr (service.ApiKey, service.Secret);
                this.token = null;
            } else {
                flickr = new FlickrNet.Flickr (service.ApiKey, service.Secret, token);
                this.token = token;
            }

            flickr.CurrentService = service.Id;
        }

        public string Token {
            get { return token; }
            set {
                token = value;
                flickr.AuthToken = value;
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
                    Log.Error (e.Code + ": " + e.OriginalMessage);
                    return null;
                }
            }
            return licenses;
        }

        public List<string> Search (string [] tags, int licenseId)
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

        public Auth CheckLogin ()
        {
            try {
                if (frob == null) {
                    frob = flickr.AuthGetFrob ();
                    if (frob == null) {
                        Log.Error ("ERROR: Problems login in Flickr. Don't have a frob");
                        return null;
                    }
                }
            } catch (Exception e) {
                Log.Error ("Error logging in: {0}", e.Message);
                return null;
            }

            if (token == null) {
                try {
                    auth = flickr.AuthGetToken (frob);
                    token = auth.Token;
                    flickr.AuthToken = token;

                    return auth;
                } catch (FlickrApiException ex) {
                    Log.Error ("Problems logging in to Flickr - " + ex.OriginalMessage);
                    return null;
                }
            }

            auth = flickr.AuthCheckToken ("token");
            return auth;
        }

        public string Upload (IPhoto photo, IFilter filter, bool is_public, bool is_family, bool is_friend)
        {
            if (token == null) {
                throw new Exception ("Must Login First");
            }
            // FIXME flickr needs rotation
            string error_verbose;

            using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {

                try {
                    string tags = null;

                    filter.Convert (request);
                    string path = request.Current.LocalPath;

                    if (ExportTags && photo.Tags != null) {
                        var taglist = new StringBuilder ();
                        Core.Tag [] t = photo.Tags;
                        Core.Tag tag_iter = null;

                        for (int i = 0; i < t.Length; i++) {
                            if (i > 0)
                                taglist.Append (",");

                            taglist.Append (string.Format ("\"{0}\"", t [i].Name));

                            // Go through the tag parents
                            if (ExportTagHierarchy) {
                                tag_iter = t [i].Category;
                                while (tag_iter != App.Instance.Database.Tags.RootCategory && tag_iter != null) {
                                    // Skip top level tags because they have no meaning in a linear tag database
                                    if (ExportIgnoreTopLevel && tag_iter.Category == App.Instance.Database.Tags.RootCategory) {
                                        break;
                                    }

                                    // FIXME Look if the tag is already there!
                                    taglist.Append (",");
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
                        Log.Error ("Problems uploading picture: " + ex.Message);
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
            frob = flickr.AuthGetFrob ();
            string login_url = flickr.AuthCalcUrl (frob, AuthLevel.Write);

            GtkBeans.Global.ShowUri (null, login_url);
        }

        public class Service
        {
            public string ApiKey;
            public string Secret;
            public SupportedService Id;
            public string Name;
            public string PreferencePath;

            public static Service [] Supported = {
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
