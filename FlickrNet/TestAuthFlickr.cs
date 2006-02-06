using System;
using System.Collections;
using System.Collections.Specialized;
using FlickrNet;

// Testing the new authentication system

public class TestAuthFlickr {
    // The Glimmr API Key
	private string _apikey_old = "2557e1ad5b7da6ffbe97387a7dad71b1";

    // My key
    private static string _apikey = "c6b39ee183385d9ce4ea188f85945016";
    private static string _sharedsecret = "0a951ac44a423a04";
    // private string _email = "acs@barrapunto.com";
    // private string _password = "flickr";

    private Flickr uploader;
    private string photoURL;
    
    public string TestAuth () {
        // Flickr downloader = new Flickr (this._apikey, this._email, this._password );
        uploader = new Flickr (_apikey, _sharedsecret);
        return uploader.AuthGetFrob ();
        // Looking to licenses
        try {
           Licenses licenses = uploader.PhotosLicensesGetInfo(); 
           //foreach (License license in licenses.LicenseCollection) {
           //    Console.WriteLine("License: {0} {1}", license.LicenseName, license.LicenseId);
           //}
        } catch( FlickrNet.FlickrException e ) {
			Console.WriteLine( e.Code + ": " + e.Verbose );
		}
        // Searching some photos with a license
        int licenseNumber = 5; // att-sa
        string text = "infinity";
        string[] tags = {"infinity", "love"};
        photoURL = "http://www.flickr.com/photo_zoom.gne?id=";        

        //for (int i=0; i < 2; i++) {
        //    searchPags (text, 10, 1);
        //    searchText (text);
        //    searchTextLicense (text, licenseNumber);
        //    searchTags (tags);
        //}
    }

    private void timeUsed (long startTime) {
        long endTime = DateTime.Now.Ticks;
        TimeSpan timeTaken = new TimeSpan(endTime - startTime);
        Console.WriteLine("--> Search: {0}", timeTaken.ToString());
    }

    private void searchText (string text) {
        long startTime = DateTime.Now.Ticks;
        Console.WriteLine ("Searching text " + text);
        try {
            Photos photos = uploader.PhotosSearchText (text);
            showResults (photos);
        } catch( FlickrNet.FlickrException e ) {
			Console.WriteLine( e.Code + ": " + e.Verbose );
		}    
        timeUsed (startTime);
    }

    private void searchTextLicense (string text, int license) {
        long startTime = DateTime.Now.Ticks;
        Console.WriteLine ("Searching text " + text + " license " + license);
        try {
            Photos photos = uploader.PhotosSearchText (text, license);
            showResults (photos);
        } catch( FlickrNet.FlickrException e ) {
			Console.WriteLine( e.Code + ": " + e.Verbose );
		}
        timeUsed (startTime);
    }
    
    private void searchPags (string text, int results_per_page, int page) {
        long startTime = DateTime.Now.Ticks;
        Console.WriteLine ("Searching text with page " + text );
        try {
            //Photos photos = uploader.PhotosSearch(null, "", 0, text, DateTime.MinValue, DateTime.MinValue, 0, results_per_page, page);
            //showResults (photos);
        } catch( FlickrNet.FlickrException e ) {
            Console.WriteLine( e.Code + ": " + e.Verbose );
        }
        timeUsed (startTime);
    }

    private void searchTags (string[] tags) {
        long startTime = DateTime.Now.Ticks;
        Console.WriteLine ("Searching tags " + tags);
        try {
            Photos photos = uploader.PhotosSearch(tags);
            showResults (photos);
        } catch( FlickrNet.FlickrException e ) {
            Console.WriteLine( e.Code + ": " + e.Verbose );
		}
        timeUsed (startTime);
    }

    private void showResults (Photos photos) {
        Console.WriteLine ("Total photos: {0}", photos.TotalPhotos);
        foreach (Photo photo in photos.PhotoCollection) {
            // Console.WriteLine ("Photo name: {0} {1} {2} {3}", photo.Title, photoURL+photo.PhotoId, photo.LargeUrl, photo.ThumbnailUrl);
        }
    }

    public static void Main (string[] args) {
        Console.WriteLine ("Testig the Flickr API");
        TestAuthFlickr flickr = new TestAuthFlickr ();
        string frob = flickr.TestAuth();
        Console.WriteLine ("Frob: " + frob);
        string login_url = flickr.uploader.AuthCalcUrl (frob, FlickrNet.AuthLevel.Write);
        
        Console.WriteLine ("Please, login in Flickr using the next URL");
        Console.WriteLine ("Login link: " + login_url);
        Console.WriteLine ("Press any key when you have login in ...");
        Console.ReadLine ();
        Console.WriteLine ("Trying to get the token");
        try {
            Auth auth = flickr.uploader.AuthGetToken(frob);
            Console.WriteLine ("We have the token!" + auth.Token);
            flickr.uploader.ApiToken = auth.Token;
            // Time to upload an image
            flickr.uploader.UploadPicture ("/home/acs/fotos/dvd-sarge/sarge31.png");
            
        } catch (FlickrNet.FlickrException ex) {
            Console.WriteLine ("ERROR: Problems uploading photo to Flickr - "+ex.Verbose);
        } 
    }
}

/* mcs TestAuthFlickr.cs -r:FlickrNet.dll */
