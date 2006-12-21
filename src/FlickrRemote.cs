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
using System.IO;
using System.Text;
using System.Collections;
using FlickrNet;
using FSpot;
using FSpot.Filters;

public class FlickrRemote {
	public static Licenses    licenses;
	private string            frob;
	private string            token;
	private Auth              auth;
	private Flickr            flickr;
	
	public bool               ExportTags;
	public FSpot.ProgressItem Progress;
	
	public FlickrRemote (string token, Service service)
	{
		if (token == null || token.Length == 0) {
			this.flickr = new Flickr (service.ApiKey, service.Secret);
			this.token = null;
		} else {
			this.flickr = new Flickr (service.ApiKey, service.Secret, token);
			this.token = token;
		}

		this.flickr.CurrentService = service.Id;
	}

	public string Token {
		get { return token; }
		set {
			token = value;
			flickr.AuthToken = value;
		}
	}

	public Flickr Connection {
		get { return flickr; }
	}

	public License[] GetLicenses () 
	{
		// Licenses won't change normally in a user session
		if (licenses == null) {
			try {
				licenses = flickr.PhotosLicensesGetInfo(); 
			} catch (FlickrNet.FlickrException e ) {
				Console.WriteLine ( e.Code + ": " + e.Verbose );
				return null;
			}
		}
		return licenses.LicenseCollection;
	}
	
	public ArrayList Search (string[] tags, int licenseId)
	{
		ArrayList photos_url = new ArrayList ();
		// Photos photos = flickr.PhotosSearchText (tags, licenseId);
		Photos photos = flickr.PhotosSearch (tags);
	
		if (photos != null) {
			foreach (FlickrNet.Photo photo in photos.PhotoCollection) {
				photos_url.Add (photo.ThumbnailUrl);
			}
		}

		return photos_url;
	}
	
	public ArrayList Search (string tags, int licenseId) 
	{
		ArrayList photos_url = new ArrayList ();
		Photos photos = flickr.PhotosSearchText (tags, licenseId);

		if (photos != null) {
			foreach (FlickrNet.Photo photo in photos.PhotoCollection) {
				photos_url.Add (photo.ThumbnailUrl);
			}
		}
		return photos_url;
	}
	
	public Auth CheckLogin () 
	{
		if (frob == null) {
			frob = flickr.AuthGetFrob ();
			if (frob ==  null) {
				Console.WriteLine ("ERROR: Problems login in Flickr. Don't have a frob");
				return null;
			}
		}

		if (token == null) {
			try {
				auth = flickr.AuthGetToken(frob);
				token = auth.Token;
				flickr.AuthToken = token;

				return auth;
			} catch (FlickrNet.FlickrException ex) {
				Console.WriteLine ("ERROR: Problems login in Flickr - "+ex.Verbose);

				return null;
			}
		}

		auth = flickr.AuthCheckToken ("token");
		return auth;
	}
	
	public string Upload (IBrowsableItem photo, IFilter filter, bool is_public, bool is_family, bool is_friend)
	{
		if (token == null) {            
			throw new Exception ("Must Login First");
		}
		// FIXME flickr needs rotation
		string  error_verbose;
		
		using (FilterRequest request = new FilterRequest (photo.DefaultVersionUri)) {
			
			try {            
				string tags = null;
				

				filter.Convert (request);
				string path = request.Current.LocalPath;
				
				if (ExportTags && photo.Tags != null) {
					StringBuilder taglist = new StringBuilder ();
					Tag [] t = photo.Tags;
					
					for (int i = 0; i < t.Length; i++) {
						if (i > 0)
							taglist.Append (",");
						
						taglist.Append (String.Format ("\"{0}\"", t[i].Name));
					}
					
					tags = taglist.ToString ();
				}
				try {
					string photoid = 
						flickr.UploadPicture (path, photo.Name, photo.Description, tags, is_public, is_family, is_friend);
					return photoid;
				} catch (FlickrNet.FlickrException ex) {
					Console.WriteLine ("Problems uploading picture: " + ex.ToString());
					error_verbose = ex.ToString();
				}
			} catch (Exception e) {
				// FIXME we need to distinguish between file IO errors and xml errors here
				throw new System.Exception ("Error while uploading", e);
			}
		}
			
		throw new System.Exception (error_verbose);
	}
	
	public void TryWebLogin () {
		frob = flickr.AuthGetFrob ();
		string login_url = flickr.AuthCalcUrl (frob, FlickrNet.AuthLevel.Write);

		GnomeUtil.UrlShow (null, login_url);
	}

	public class Service {
		public string ApiKey;
		public string Secret;
		public SupportedService Id;
		public string Name;
		public string PreferencePath;
		
		public static Service [] Supported = {
			new Service (SupportedService.Flickr, "Flickr.com", "c6b39ee183385d9ce4ea188f85945016", "0a951ac44a423a04", Preferences.EXPORT_TOKEN_FLICKR),
			new Service (SupportedService.TwentyThreeHQ, "23hq.com", "c6b39ee183385d9ce4ea188f85945016", "0a951ac44a423a04", Preferences.EXPORT_TOKEN_23HQ),
			new Service (SupportedService.Zooomr, "Zooomr.com", "unknown", "unknown", Preferences.EXPORT_TOKEN_ZOOOMR)
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
			
			throw new System.ArgumentException ("Unknown service type");
		}
	}
}
