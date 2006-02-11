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

public class FlickrRemote {
    
	// This should be private but you know, this is Free Software ;-)
	// Keys from acs
	private static string     _apikey = "c6b39ee183385d9ce4ea188f85945016";
	private static string     _sharedsecret = "0a951ac44a423a04";
	
	public static Licenses    licenses;
	private string            frob;
	private string            token;
	private Auth              auth;
	private Flickr            flickr;
	
	public bool               ExportTags;
	public FSpot.ProgressItem Progress;
	
	public FlickrRemote (string token)
	{
		if (token == null || token.Length == 0) {
			this.flickr = new Flickr (_apikey, _sharedsecret);
			this.token = null;
		} else {
			this.flickr = new Flickr (_apikey, _sharedsecret, token);
			this.token = token;
		}
	}

	public string Token {
		get { return token; }
		set {
			token = value;
			flickr.ApiToken = value;
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
				flickr.ApiToken = token;

				return auth;
			} catch (FlickrNet.FlickrException ex) {
				Console.WriteLine ("ERROR: Problems login in Flickr - "+ex.Verbose);

				return null;
			}
		}

		auth = flickr.AuthCheckToken ("token");
		return auth;
	}
	
	public string Upload (IBrowsableItem photo, bool scale, int size, bool copy_metadata, bool is_public, bool is_family, bool is_friend)
	{
		if (token == null) {            
			throw new Exception ("Must Login First");
		}
		// FIXME flickr needs rotation
		
		string path = photo.DefaultVersionUri.LocalPath; 
		FileInfo file = new FileInfo (path);
		string   error_verbose;
		
		try {            
			string tags = null;
			
			if (scale) {
				path = PixbufUtils.Resize (path, size, copy_metadata);
				file = new FileInfo (path);
			}
			
			if (ExportTags && photo.Tags != null) {
				StringBuilder taglist = new StringBuilder ();
				Tag [] t = photo.Tags;
				
				for (int i = 0; i < t.Length; i++) {
					if (i > 0)
						taglist.Append (",");
					
					taglist.Append (t[i].Name);
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
		} finally {
			if (file != null && scale)
				file.Delete ();
		}
		
		throw new System.Exception (error_verbose);
	}
	
	public void TryWebLogin () {
		frob = flickr.AuthGetFrob ();
		string login_url = flickr.AuthCalcUrl (frob, FlickrNet.AuthLevel.Write);
		Gnome.Url.Show (login_url);
	}
}
