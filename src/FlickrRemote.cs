/*
 * Simple upload based on the api at 
 * http://www.flickr.com/services/api/upload.api.html
 */
using System;
using System.IO;
using System.Text;

public class FlickrRemote {
	// This is the uo
	public static string UploadUrl = "http://www.flickr.com/tools/uploader_go.gne";
	public static string AuthUrl = "http://www.flickr.com/tools/auth.gne";
	
	string email;
	string passwd;

	public bool ExportTags;
	public FSpot.ProgressItem Progress;

	public FlickrRemote ()
	{
		//FIXME this api is lame
	}

	public void Upload (Photo photo)
	{
		if (email == null || passwd == null)
			throw new Exception ("Must Login First");

		// FIXME flickr needs rotation

		FormClient client = new FormClient ();
		client.Add ("email", email);
		client.Add ("password", passwd);
		client.Add ("photo", new FileInfo (photo.DefaultVersionPath));
		if (photo.Description != null) {
			client.Add ("description", photo.Description);
		}
		if (ExportTags && photo.Tags != null) {
			StringBuilder taglist = new StringBuilder ();

			foreach (Tag t in photo.Tags) {
				taglist.Append (t.Name + " ");
			}
			
			client.Add ("tags", taglist.ToString ());
		}

		Stream response = client.Submit (UploadUrl, this.Progress).GetResponseStream ();
		StreamReader reader = new StreamReader (response, Encoding.UTF8);

		// FIXME we need to parse reponse
		Console.WriteLine (reader.ReadToEnd ());
	}

	public void Login (string email, string passwd)
	{
		FormClient client = new FormClient ();
		client.Add ("email", email);
		client.Add ("password", passwd);

		Stream response = client.Submit (AuthUrl, this.Progress).GetResponseStream ();
		StreamReader reader = new StreamReader (response, Encoding.UTF8);

		Console.WriteLine (reader.ReadToEnd ());

		this.email = email;
		this.passwd = passwd;
	}
}
